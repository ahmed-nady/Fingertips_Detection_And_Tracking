using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
 
using Emgu.CV;
using Emgu.CV.Structure;
using HandGestureRecognition.SkinDetector;
using Emgu.CV.VideoSurveillance;
using Recognizer.NDollar;

namespace FingerTipsDetectionTracking.Project
{
    public partial class FingerTipsTracking : Form
    {
        class Tracks
        {
            public int id;
            public KalmanTracker tracker;
            public long age;
            public int totalVisibleCount;
            public int consecutiveInvisibleCount;
            public Tracks(int Id, long fingerAge,PointF pt)
            {
                id = Id;
                age = fingerAge;
                totalVisibleCount = (int)fingerAge;
                consecutiveInvisibleCount = (int)fingerAge;
                tracker = new KalmanTracker();
                tracker.SetPredicted(pt);

            }

        }

        List<Tracks> fingerTracker;
        
        Hsv hsv_min;
        Hsv hsv_max;
        Ycc YCrCb_min;
        Ycc YCrCb_max;
        IColorSkinDetector skinDetector;

        Capture camera;
        Image<Gray, byte> eImg;
        Image<Bgr, byte> testImage;
        bool isTrackIntialized = false;
        bool startCapture = false;
        private IBGFGDetector<Bgr> _forgroundDetector;

        float scale = 1.1f;

        //$N
        
        private List<List<PointR>> _strokes; // Lisa 8/8/2009
        
         List<PointR> TargetsListofPoints = new   List<PointR>();
         GeometricRecognizer rec;

         int frameCount;
        public FingerTipsTracking()
        {
            InitializeComponent();

            camera = new Capture(@"D:\Working\STREAM\00003.MTS");

            hsv_min = new Hsv(0, 45, 0);
            hsv_max = new Hsv(20, 255, 255);
            YCrCb_min = new Ycc(0, 131, 80);
            YCrCb_max = new Ycc(255, 185, 135);

            //$N
            rec = new GeometricRecognizer();


            rec.LoadGesture(@"E:\Documents\ZoomIn.xml");
            rec.LoadGesture(@"E:\Documents\ZoomOut.xml");

            fingerTracker = new List<Tracks>();

            testImage = new Image<Bgr, byte>(@"C:\Users\Public\Pictures\Sample Pictures\Tulips.jpg");
            eImg = new Image<Gray, byte>(@"C:\Users\ahmed nady\Desktop\eImg1.jpg");
            
        }
        private Contour<Point> ExtractBiggestContour(Contour<Point> contours)
        {
            Contour<Point> biggestContour = null;


            Double Result1 = 0;
            Double Result2 = 0;
           
            while (contours != null)
            {
                Result1 = contours.Area;
                if (Result1 > Result2)
                {
                    Result2 = Result1;
                    biggestContour = contours;
                    
                }
                contours = contours.HNext;

            }

            return biggestContour;
        }

        float distanceP2P(Point a, Point b)
        {
            float d = (float)Math.Sqrt(Math.Abs(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2)));
            return d;
        }
        float distanceP2P(PointF a, PointF b)
        {
            float d = (float)Math.Sqrt(Math.Abs(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2)));
            return d;
        }
        float getAngle(Point s, Point f, Point e)
        {
            float l1 = distanceP2P(f, s);
            float l2 = distanceP2P(f, e);
            float dot = (s.X - f.X) * (e.X - f.X) + (s.Y - f.Y) * (e.Y - f.Y);

            //  float angle = (float)Math.Acos(dot / (l1 * l2));
            float angle = (dot / (l1 * l2));
            angle = (float)angle * 180 / (float)Math.PI;
            return angle;
        }
        private void findFingerTips(Contour<Point> handContour, List<Point> tips)
        {

            Point p = new Point();
            Point q = new Point();
            Point r = new Point();
            int k = 16;
            
                Contour<Point> MaxContour = handContour;
                for (int i = 0; i < MaxContour.Total; i++)
                {
                    if (MaxContour.Total < k)
                        break;
                    //p,q,r points for defining vectors
                    p = MaxContour.ElementAt(i);
                    if (i >= k)
                        q = MaxContour.ElementAt(i - k);
                    else
                        q = MaxContour.ElementAt(MaxContour.Total - 1 - i);
                    if (i < MaxContour.Total - k)
                        r = MaxContour.ElementAt(i + k);
                    else
                        r = MaxContour.ElementAt(k - (MaxContour.Total - i));



                    double angle = getAngle(q, p, r);

                    if (angle < 40 && angle > 15)
                    {

                        //int cross = ((q.X - p.X) * (r.Y - p.Y) - (q.Y - p.Y) * (r.X - p.X));
                        //if (cross > 0)
                        //{
                        //    tips.Add(p);
                        //}
                        int contourRadius = 15;
                        Seq<Point> hull = MaxContour.GetConvexHull(Emgu.CV.CvEnum.ORIENTATION.CV_CLOCKWISE);
                        for (int j = 0; j < hull.Total; j++)
                        {
                            Point pointHull = hull.ElementAt(j);

                            if (pointHull.Y - contourRadius <= p.Y && pointHull.Y + contourRadius >= p.Y && pointHull.X - contourRadius <= p.X && pointHull.X + contourRadius >= p.X)
                            {
                                j = hull.Total;
                                tips.Add(p);
                            }
                        }
                    }
                }
            
        }
        
        double getEccentricity(MCvMoments mu){

          return  (Math.Sqrt( mu.m20 - mu.m02 )-4 * mu.m11 * mu.m11)/(Math.Sqrt( mu.m20 + mu.m02 ));

            //double bigSqrt = Math.Sqrt((mu.m20 - mu.m02) * (mu.m20 - mu.m02) + 4 * mu.m11 * mu.m11);
            //return (double)(mu.m20 + mu.m02 + bigSqrt) / (mu.m20 + mu.m02 - bigSqrt);
         }

       

        List<Point> distanceTransform(Image<Gray, byte> binary_image, Image<Bgr, byte> skin)
        {
            Image<Gray, float> distTransform = new Image<Gray, float>(binary_image.Width, binary_image.Height);

            binary_image._Erode(3);
            CvInvoke.cvDistTransform(binary_image, distTransform, Emgu.CV.CvEnum.DIST_TYPE.CV_DIST_L2, 3, null, IntPtr.Zero);
            distTransform = distTransform.ThresholdBinary(new Gray(5), new Gray(255));
            Contour<Point> handContour = null;
            List<Point> fingertips = new List<Point>();
           
            Image<Gray, byte> img = distTransform.Convert<Gray, byte>();
          
            img=img.Canny(200, 100);
            FingerTipsTrackingBox.Image = img;
            using (MemStorage m = new MemStorage())
            {
                Contour<System.Drawing.Point> MaxContour = new Contour<Point>(m);

                Contour<System.Drawing.Point> Contours = img.FindContours(Emgu.CV.CvEnum.CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_SIMPLE, Emgu.CV.CvEnum.RETR_TYPE.CV_RETR_EXTERNAL);

                Contour<Point> discontour = distTransform.Convert<Gray, byte>().FindContours(Emgu.CV.CvEnum.CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_SIMPLE, Emgu.CV.CvEnum.RETR_TYPE.CV_RETR_EXTERNAL);


                handContour = ExtractBiggestContour(Contours);
                Contour<Point> icontour = ExtractBiggestContour(discontour);


                if (icontour != null)
                {
                    for (int j = 0; j < fingertips.Count; j++)
                    {
                        double dst = CvInvoke.cvPointPolygonTest(icontour, new PointF(fingertips[j].X, fingertips[j].Y), true);
                        if (dst > -200)
                            fingertips.RemoveAt(j);
                    }
                    distTransform.Draw(icontour, new Gray(200), 2);
                }
                if (handContour != null )
                {
                  
                    Contour<Point> handContourApproximate = handContour.ApproxPoly(handContour.Perimeter*0.001, m);

                    skin.Draw(handContourApproximate, new Bgr(0, 0, 255), 2);
                    skin.Draw(handContour, new Bgr(255, 0, 0), 2);
                        //PointF[] pts = new PointF[22];
                    Image<Gray, byte> cropImg = new Image<Gray, byte>(60, 60);
                    for (int i = 0; i < handContourApproximate.Total; i++)
                    {
                        //////eliminate corner point closer to boundary
                        //double dst = CvInvoke.cvPointPolygonTest(icontour, new PointF(handContourApproximate[i].X, handContourApproximate[i].Y), true);
                        //if (dst >-80)
                        //    continue;
                       //crop image at each point and >>>>
                        binary_image.ROI = new Rectangle(handContourApproximate[i].X - 30, handContourApproximate[i].Y-30, 60, 60);
                       // cropImg = binary_image.Copy();
                        MCvMoments moment = eImg.GetMoments(true);
                        double eccentricity = getEccentricity(moment);

                        if (eccentricity <1  )
                            fingertips.Add(handContourApproximate[i]);
                    }


                     
                }
            }
            return fingertips;
        }
        
        private List<Point> tipsCulstering(List<Point> fingertips)
        {
            List<Point> newFingers = new List<Point>();
            
            
            List<Point> closePoints = new List<Point>();
            int postPoint = 0;
             
            for (int i = 0; i < fingertips.Count; i++)
            {
                for (int j = (i + 1); j < fingertips.Count; j++)
                {
                    postPoint = j;
                    if (i == fingertips.Count - 1 && j == fingertips.Count-1)
                    {
                        postPoint = 0;
                    }
                   // closePoints.Add(fingertips[i]);

                    if (distanceP2P(fingertips[i], fingertips[postPoint]) <40)
                    {

                        //if (postPoint == 0)
                        //    break;
                        //else
                            closePoints.Add(fingertips[postPoint]);

                    }
                    else
                    {
                        i = j-1;

                        if (closePoints.Count != 0)
                        {
                            closePoints.Add(fingertips[i]);
                            newFingers.Add(closePoints[(int)closePoints.Count / 2]);
                        }
                        else
                            newFingers.Add(fingertips[i]);
                        closePoints.Clear();
                        break;
                    }

                    if (fingertips.Count == closePoints.Count)
                    {
                        closePoints.Add(fingertips[i]);
                        newFingers.Add(closePoints[(int)closePoints.Count / 2]);
                        closePoints.Clear();
                    }
                }
            }
            return newFingers;
                     
        }
      
   
        private void processImage(object sender, EventArgs e)
        {
            Image<Bgr, byte> skin = camera.QueryFrame();//line 1
            CaptureImageBox.Image = skin;
            if (skin == null)
                return;
             skin._SmoothGaussian(3);

            
           //  skin._EqualizeHist();
            skinDetector = new YCrCbSkinDetector();
            Image<Gray, Byte> BinaryHandImage = skinDetector.DetectSkin(skin, YCrCb_min, YCrCb_max);
            BinaryHandImage._Erode(3);
            BinaryHandImage._Dilate(3);
      //      //  BinaryHandImage=  BinaryHandImage.Canny(100, 0);
      ////      FingerTipsTrackingBox.Image = BinaryHandImage;
      //      //// curvature of premetier method

      //        List<Point> tips = distanceTransform(BinaryHandImage, skin);
      //        for (int c = 0; c < tips.Count; c++)
      //        {
      //            skin.Draw(new CircleF((PointF)tips[c], 10), new Bgr(Color.Blue), 2);
      //        }

      //        trackFingerTips(tips);
            Contour<Point> handContour = null;
            using (MemStorage m = new MemStorage())
            {
                Contour<System.Drawing.Point> MaxContour = new Contour<Point>(m);

                Contour<System.Drawing.Point> Contours =
                    BinaryHandImage.FindContours();//Emgu.CV.CvEnum.CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_SIMPLE, Emgu.CV.CvEnum.RETR_TYPE.CV_RETR_TREE);

                //Contours.OrderByDescending <Contour ,double)();

                handContour = ExtractBiggestContour(Contours);

                if (handContour != null)
                {

                    List<Point> fingerTips = new List<Point>();
                    List<Point> tips = new List<Point>();
                    findFingerTips(handContour, fingerTips);

                    tips = tipsCulstering(fingerTips);
                    tips = tipsCulstering(tips);

                    Seq<Point> hull = handContour.GetConvexHull(Emgu.CV.CvEnum.ORIENTATION.CV_CLOCKWISE);



                    skin.Draw(hull, new Bgr(Color.Green), 2);
                    skin.Draw(handContour, new Bgr(Color.Red), 2);
                    for (int c = 0; c < fingerTips.Count; c++)
                    {
                        skin.Draw(new CircleF((PointF)fingerTips[c], 10), new Bgr(Color.Blue), 2);
                    }
                    for (int c = 0; c < tips.Count; c++)
                    {
                        skin.Draw(new CircleF((PointF)tips[c], 15), new Bgr(Color.Yellow), 2);
                    }

                    trackFingerTips(tips);

                }
            }

        }
        /************  Track Fingers*/
        void trackFingerTips(List<Point> tips)
        {

            Graphics G = fingerTipsTrajectoryBox.CreateGraphics();
            if (!isTrackIntialized)
            {
                if (tips.Count != 0)
                {
                    for (int i = 0; i < tips.Count; i++)
                    {
                        
                        fingerTracker.Add(new Tracks(i, i + 1,tips[i]));
                        TargetsListofPoints.Add(new PointR(tips[i].X, tips[i].Y, Environment.TickCount));
                        G.FillEllipse(Brushes.Red, tips[i].X, tips[i].Y, 5, 5);
                    }

                    isTrackIntialized = true;
                }
            }

            else
            {
                frameCount++;
                List<int> filter_used = new List<int>();
                PointF C = new PointF();
                int associatedFinger = 0;
                for (int T = 0; T < tips.Count; T++)
                {
                    PointF New_location = new PointF(tips[T].X, tips[T].Y);
                    float result = 1000;
                    float distance = 0;


                    int selectedTarget = 0;
                    PointF predit = new PointF();
                    for (int TC = 0; TC < fingerTracker.Count; TC++)
                    {
                        distance = distanceP2P(New_location, predit = fingerTracker[TC].tracker.GetPredicted());

                        if (result > distance)
                        {
                            result = distance;
                            selectedTarget = TC;
                        }
                    }



                    if (result > 200 )
                    {
                        //Tracks finger = new Tracks(fingerTracker.Count, 1);
                        fingerTracker.Add(new Tracks(fingerTracker.Count, 1, New_location));
                        G.FillEllipse(Brushes.Orange, New_location.X, New_location.Y, 5, 5);
                        TargetsListofPoints.Add(new PointR(New_location.X, New_location.Y, Environment.TickCount));
                    
                    }
                    else
                    {
                        filter_used.Add(selectedTarget);
                        fingerTracker[selectedTarget].tracker.UpdateTarget(New_location);
                        C = fingerTracker[selectedTarget].tracker.GetEstimate();
                        G.FillEllipse(Brushes.Black, C.X, C.Y, 5, 5);


                        fingerTracker[selectedTarget].age += 1;
                        fingerTracker[selectedTarget].totalVisibleCount += 1;
                        fingerTracker[selectedTarget].consecutiveInvisibleCount = 0;
                        associatedFinger += 1;
                        //   G.DrawLine(Pens.Black, new Point((int)prevPoint.X, (int)prevPoint.Y), new Point((int)C.X, (int)C.Y));
                        TargetsListofPoints.Add(new PointR(C.X, C.Y, Environment.TickCount));
                    }

                }
                for (int T = 0; T < fingerTracker.Count; T++)
                {
                    if (!filter_used.Contains(T))
                    {

                        C = fingerTracker[T].tracker.GetPredicted();
                        G.FillEllipse(Brushes.Blue, C.X, C.Y, 5, 5);

                        //fingerTracker[T].age += 1;
                        //fingerTracker[T].consecutiveInvisibleCount += 1;

                        // G.DrawLine(assignColor(T), new Point((int)prevPoint.X, (int)prevPoint.Y), new Point((int)C.X, (int)C.Y));
                        TargetsListofPoints.Add(new PointR(C.X, C.Y, Environment.TickCount));
                    }

                }

                for (int T = 0; T < fingerTracker.Count; T++)
                {
                    
                   // float visibility = (float)fingerTracker[T].totalVisibleCount /(float) fingerTracker[T].age;
               //    (fingerTracker[T].age < 8 && visibility < .6) ||
                    if (fingerTracker[T].consecutiveInvisibleCount >= 60)
                    {
                        fingerTracker.RemoveAt(T);
                    }
                }

                if( frameCount==100)
                {
                    recognizeGesture();
                    frameCount = 0;
                }
                

            }
        }
     
      
         
         private void recognizeGesture()
        {
             
                if (TargetsListofPoints.Count > 10)
                {
                    NBestList result = rec.Recognize(TargetsListofPoints, fingerTracker.Count);
                    if (result.Score > .5)
                    {
                        if (result.Name == "ZoomIn")
                        {
                            FingerTipsTrackingBox.Image = testImage.Resize(scale, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);
                            scale += .1f;
                        }
                        else if (result.Name == "ZoomOut")
                        {
                            FingerTipsTrackingBox.Image = testImage.Resize(scale, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);
                            scale /= .1f;

                        }
                    } 

                    TargetsListofPoints.Clear();
                    isTrackIntialized = false;
                    fingerTracker.Clear();
                    fingerTipsTrajectoryBox.Refresh();
                }
            }
        

        private void StartButton_Click(object sender, EventArgs e)
        {
            if (!startCapture)
            {
                // 
                StartButton.Text = "Stop";
                Application.Idle += processImage;
            }
            else
            {
                
                fingerTipsTrajectoryBox.Refresh();
                StartButton.Text = "Resume";
                Application.Idle -= processImage;
            }
            startCapture = !startCapture;
       
        }
    }
}
