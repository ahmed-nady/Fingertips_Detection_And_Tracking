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
using Emgu.CV.GPU;
using Recognizer.NDollar;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
   

namespace FingerTipsDetectionTracking
{
    public partial class ConvexHull : Form
    {
        //$N
        List<PointR> TargetsListofPoints = new List<PointR>();
        GeometricRecognizer rec;

        class Tracks
        {
            public int id;
            public KalmanTracker tracker;
            public long age;
            public int totalVisibleCount;
            public int consecutiveInvisibleCount;
            public Tracks(int Id, long fingerAge, PointF pt)
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
        bool isTrackIntialized = false;
        int frameCount = 0;

        bool startCapture = false;
        Hsv hsv_min;
        Hsv hsv_max;
        Ycc YCrCb_min;
        Ycc YCrCb_max;
        IColorSkinDetector skinDetector;
        Capture camera;

        FingerTipsDetectionTracking.FingerTip fingerTipDetection;

        List<Point> candidateTips;
        //Create the font
        MCvFont f = new MCvFont(Emgu.CV.CvEnum.FONT.CV_FONT_HERSHEY_COMPLEX, 1.0, 1.0);
                  
        int count = 0;
        bool indicator = false;
        Point previousPoint = new Point();
        Point previosPoint2 = new Point();
        float previousDistance = 0;
       // private HaarCascade haar; 
        Image<Gray, byte> template ;//= new Image<Gray, byte>(@"C:\Users\ahmed nady\Pictures\template.bmp");
        Image<Bgr, byte> bg;// = new Image<Bgr, byte>(@"C:\Users\ahmed nady\Pictures\Debut\bg.jpg");

        private Point RectStartPoint;
        private Rectangle Rect = new Rectangle();
        private Rectangle RealImageRect = new Rectangle();
        private Brush selectionBrush = new SolidBrush(Color.FromArgb(128, 64, 64, 64));
        private int thickness = 3;

        Image<Bgr, byte> skin;
        Image<Gray, byte> BinaryHandImage;
        bool inDetectionPhase = false;


        Point p1 = new Point();
        Point p2 = new Point();

        public ConvexHull()
        {
            InitializeComponent();
            //@"C:\Users\ahmed nady\Videos\Debut\test.avi" 00023.MTS MOV_0016 @"D:\Working\STREAM\ahmednady.asf"
            camera = new Capture(/*@"C:\Users\ahmed nady\Videos\Debut\5.avi");*/@"F:\Working\Final phase\DataSet\sequence.avi");
            fingerTipDetection = new FingerTip();
            skinDetector = new YCrCbSkinDetector();
            candidateTips = new List<Point>();
            fingerTracker = new List<Tracks>();
            // adjust path to find your XML file 
            //haar = new HaarCascade("FingerTips.xml");
            hsv_min = new Hsv(0, 45, 0);
            hsv_max = new Hsv(20, 255, 255);
            YCrCb_min = new Ycc(0, 131, 80);
            YCrCb_max = new Ycc(255, 185, 135);
            //$N
            rec = new GeometricRecognizer();
            rec.LoadGesture(@"C:\Users\ahmed nady\Documents\TranslateLeft.xml");
            rec.LoadGesture(@"C:\Users\ahmed nady\Documents\TranslateRight.xml");
            rec.LoadGesture(@"C:\Users\ahmed nady\Documents\RotateLeft.xml");
            rec.LoadGesture(@"C:\Users\ahmed nady\Documents\RotateRight.xml");
            rec.LoadGesture(@"C:\Users\ahmed nady\Documents\ZoomIn.xml");
            rec.LoadGesture(@"C:\Users\ahmed nady\Documents\ZoomOut.xml");
            rec.LoadGesture(@"C:\Users\ahmed nady\Documents\KZoomIn.xml");
            rec.LoadGesture(@"C:\Users\ahmed nady\Documents\KZoomOut.xml");
            

        }

        


        //template Matching Method
        private void processImageUsingTemplateMatching(object sender, EventArgs e)
        {
            skin = camera.QueryFrame();//line 1


            if (skin == null)
                return;
            skin._SmoothGaussian(3);
            pictureBox1.Image = skin.Bitmap;
            Thread.Sleep(100);
            if (inDetectionPhase)
            {
                //   skin.ROI = RealImageRect;
                //  BinaryHandImage = skinDetector.DetectSkin(skin, YCrCb_min, YCrCb_max);
                BinaryHandImage = skin.InRange(new Bgr(150, 150, 150), new Bgr(255, 255, 255));
                BinaryHandImage._Erode(3);
                BinaryHandImage._Dilate(3);
                imageBoxSkin.Image = BinaryHandImage;

                candidateTips.Clear();

                using (Image<Gray, float> result = BinaryHandImage.MatchTemplate(template, Emgu.CV.CvEnum.TM_TYPE.CV_TM_CCOEFF_NORMED))
                {
                    /* finding single instance of tip*/
                    double[] minValues, maxValues;
                    Point[] minLocations, maxLocations;
                    result.MinMax(out minValues, out maxValues, out minLocations, out maxLocations);

                    // You can try different values of the threshold. I guess somewhere between 0.75 and 0.95 would be good.
                    if (maxValues[0] > .75)
                    {
                        // This is a match. Do something with it, for example draw a rectangle around it.
                        Rectangle match = new Rectangle(maxLocations[0], template.Size);
                        skin.Draw(match, new Bgr(Color.Red), 2);
                        BinaryHandImage.Draw(match, new Gray(150), 2);
                        TargetsListofPoints.Add(new PointR(maxLocations[0].X, maxLocations[0].Y, Environment.TickCount));

                    }
                    // recognize gesture when user pause for 10 ms
                    if (TargetsListofPoints.Count > 20)
                    {
                        p1.X = (int)TargetsListofPoints[TargetsListofPoints.Count - 1].X;
                        p1.Y = (int)TargetsListofPoints[TargetsListofPoints.Count - 1].Y;
                        p2.X = (int)TargetsListofPoints[TargetsListofPoints.Count - 10].X;
                        p2.Y = (int)TargetsListofPoints[TargetsListofPoints.Count - 10].Y;
                        double dist = fingerTipDetection.distanceP2P(p1, p2);
                        if (dist < 10)
                            recognizeGestureUsingNRecognizer();

                    }
                }
            } 

        }

      void runCommand(String arguments)
        {
            ///c java -jar D:\\Working\\ImageJ\\ij.jar -macro rotate
            ///
            //Create process
            System.Diagnostics.Process pProcess = new System.Diagnostics.Process();
            //strCommand is path and file name of command to run
            pProcess.StartInfo.FileName = "cmd.exe";
            //strCommandParameters are parameters to pass to program
            pProcess.StartInfo.Arguments = arguments;
            //Start the process
            pProcess.Start();
            //Wait for process to finish
            pProcess.WaitForExit();
        }
      // Get a handle to an application window.
      [DllImport("USER32.DLL", CharSet = CharSet.Unicode)]
      public static extern IntPtr FindWindow(string lpClassName,
          string lpWindowName);

      // Activate an application window.
      [DllImport("USER32.DLL")]
      public static extern bool SetForegroundWindow(IntPtr hWnd);
        public void controlModel(String gesture,String commands)
      {
          // Get a handle to the ImageJ 3D Viewer application. The window class 
           // and window name were obtained using the Spy++ tool.
          // IntPtr calculatorHandle = FindWindow("CalcFrame", "Calculator");
           IntPtr ImageJHandle = FindWindow("SunAwtFrame", "ImageJ 3D Viewer");


           // Verify that ImageJ 3D Viewer is a running process. 
           if (ImageJHandle == IntPtr.Zero)
           {
               MessageBox.Show("ImageJ 3D Viewer is not running.");
               return;
           }
          //  if(gesture =="zoom")
          //       runCommand("/c java -jar D:\\Working\\ImageJ\\ij.jar -macro rotate");
           //// Make ImageJ 3D Viewer the foreground application and send it  
           // a set of commands.
           SetForegroundWindow(ImageJHandle);
             
          // SendKeys.SendWait("{NUMLOCK}");
           //arrow right {RIGHT} ,arrow left {LEFT} {DOWN} {UP} for rotation
           //+{LEFT} for translation
          // for (int i = 0; i < 20; i++)
          //     SendKeys.SendWait(commands);
           SendKeys.SendWait(commands);
           
           
      }

        private void processImage(object sender, EventArgs e)
        {

            
           skin = camera.QueryFrame();//line 1

          //  CaptureImageBox.Image = skin;
           
            if (skin == null)
                return;
            pictureBox1.Image = skin.Bitmap;
            skin._SmoothGaussian(3);

            Thread.Sleep(100);
            //  skin._EqualizeHist();
           
            BinaryHandImage = skinDetector.DetectSkin(skin, YCrCb_min, YCrCb_max);
            //background subtraction
           // Image<Bgr, Byte> BinaryImage =null;
           // CvInvoke.cvAbsDiff(skin, bg, BinaryImage);
           // Image<Bgr, Byte> BinaryImage = skin.AbsDiff(bg); //find the absolute difference 
         //   Image<Gray, Byte> BinaryHandImage = BinaryImage.Convert<Gray, byte>();
            // use glove
            
           //   BinaryHandImage = skin.InRange(new Bgr(100, 100, 100), new Bgr(255, 255, 255));
            BinaryHandImage._Erode(3);
            BinaryHandImage._Dilate(3);
          ///  if (inDetectionPhase) {
          ///      BinaryHandImage.ROI = RealImageRect;

            imageBoxSkin.Image = BinaryHandImage;
            Contour<Point> handContour = null;
            count++;
            using (MemStorage m = new MemStorage())
            {
                Contour<System.Drawing.Point> MaxContour = new Contour<Point>(m);

                Contour<System.Drawing.Point> Contours =
                    BinaryHandImage.FindContours(Emgu.CV.CvEnum.CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_SIMPLE, Emgu.CV.CvEnum.RETR_TYPE.CV_RETR_TREE);

                //Contours.OrderByDescending <Contour ,double)();

                handContour = fingerTipDetection.ExtractBiggestContour(Contours);

                if (handContour != null)
                {
                     
                    //  Matrix<int> indeces = new Matrix<int>(handContour.Total, 1);

                    //CvInvoke.cvConvexHull2(handContour.Ptr, indeces.Ptr, Emgu.CV.CvEnum.ORIENTATION.CV_CLOCKWISE,-1);
                    ////  Seq<int> convexHull = new Seq<int>(CvInvoke.cvConvexHull2(handContour, m.Ptr, Emgu.CV.CvEnum.ORIENTATION.CV_CLOCKWISE, -1), m);
                    Seq<Point> hull = handContour.GetConvexHull(Emgu.CV.CvEnum.ORIENTATION.CV_CLOCKWISE);
                    //for (int i = 0; i < hull.Total; i++)
                    //{
                    //    skin.Draw(new Emgu.CV.Structure.CircleF(hull.ElementAt(i), 5), new Bgr(Color.Blue), 6);
                    //}
             //       skin.Draw(hull, new Bgr(Color.Green), 2);
                 //   Contour<System.Drawing.Point> Contour = handContour.ApproxPoly(handContour.Perimeter * .1);
                    //for (int i = 0; i < handContour.Total; i++)
                    //{
                    //    skin.Draw(new Emgu.CV.Structure.CircleF(handContour.ElementAt(i), 5), new Bgr(Color.Blue), 6);
                    //}
                    if (count == 30)
                    {
                        Thread.Sleep(10000);
                        count = 0;
                    }
              //      skin.Draw(handContour, new Bgr(Color.Red), 2);
                       Seq<MCvConvexityDefect> defects = handContour.GetConvexityDefacts(m, Emgu.CV.CvEnum.ORIENTATION.CV_CLOCKWISE);
                        //  List<Point> tips = fingerTipDetection.find_fingerTips(defects, handContour.BoundingRectangle);
                     //List<Point> tips = fingerTipDetection.filtering_tips(defects, handContour);
                  // List<Point> tips = fingerTipDetection.findFingerTips(handContour);
                       List<Point> tips = fingerTipDetection.findFingerTipsUsingK_Curvature(handContour);
                    ////draw Tips
                    // for (int i = 0; i < tips.Count; i++)
                    //     skin.Draw(new Emgu.CV.Structure.CircleF(tips[i], 5), new Bgr(Color.DarkRed), 15);
                    //List<Point> tip = fingerTipDetection.Cluster(tips);
                    // for (int i = 0; i < tip.Count; i++)
                    //     skin.Draw(new Emgu.CV.Structure.CircleF(tip[i], 5), new Bgr(Color.Yellow), 15);

                       // Robust Fingertip Tracking with Improved Kalman Filter paper
                       //filter tips
                       Image<Gray, float> distTransform = new Image<Gray, float>(BinaryHandImage.Size);


                       CvInvoke.cvDistTransform(BinaryHandImage, distTransform, Emgu.CV.CvEnum.DIST_TYPE.CV_DIST_L2, 3, null, IntPtr.Zero);
                       distTransform = distTransform.ThresholdBinary(new Gray(30), new Gray(255));
                       
                       // distTransform._Dilate(3);
                        distTransform._Erode(3);
                       Image<Gray, byte> disTransImage = distTransform.Convert<Gray, byte>();
                       Contour<Point> contour = disTransImage.FindContours();
                       contour = fingerTipDetection.ExtractBiggestContour(contour);
                       if (contour != null)
                       {
                           disTransImage.Draw(contour, new Gray(100), 8);
                           distanceTransformImage.Image = disTransImage;
                           // tips= fingerTipDetection.Cluster(tips);
                           for (int i = 0; i < tips.Count; i++)
                           {
                               if (CvInvoke.cvPointPolygonTest(contour, tips[i], true) < -30)
                                   skin.Draw(new Emgu.CV.Structure.CircleF(tips[i], 5), new Bgr(Color.Yellow), 15);
                               else
                                   tips.RemoveAt(i);
                           }
                       }
                 ///      for (int i = 0; i < tips.Count; i++)
                  ///         skin.Draw(new Emgu.CV.Structure.CircleF(tips[i], 5), new Bgr(Color.Yellow), 15);
                            
                    skin.Draw("FingerTips : " + tips.Count, ref f, new Point(10, 40), new Bgr(0, 255, 0));

                  /*  if (tips.Count == 1)
                        previousPoint = tips[0];
                    else if (tips.Count == 2)
                    {
                        previousPoint = tips[0];
                        previosPoint2 = tips[1];
                    }*/
                    
                    
                    //track tips
                    // trackFingerTips(tips);
                    

                }
            
            }

        }
        void trackFingerTips(List<Point> tips)
        {

            Graphics G = distanceTransformImage.CreateGraphics();
            if (!isTrackIntialized)
            {
                if (tips.Count != 0)
                {
                    for (int i = 0; i < tips.Count; i++)
                    {
                        fingerTracker.Add(new Tracks(i, i + 1, tips[i]));
                        G.FillEllipse(Brushes.Red, tips[i].X, tips[i].Y, 5, 5);
                    }
                    previousPoint = tips[0];
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
                        distance = fingerTipDetection.distanceP2P(New_location, predit = fingerTracker[TC].tracker.GetPredicted());

                        if (result > distance)
                        {
                            result = distance;
                            selectedTarget = TC;
                        }
                    }
                    if (result > 400)
                    {
                        //Tracks finger = new Tracks(fingerTracker.Count, 1);
                        fingerTracker.Add(new Tracks(fingerTracker.Count, 1, New_location));
                        G.FillEllipse(Brushes.Orange, New_location.X, New_location.Y, 5, 5);

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
                        fingerTracker[T].age += 1;
                        fingerTracker[T].consecutiveInvisibleCount += 1;
                        G.FillEllipse(Brushes.Cornsilk, C.X, C.Y, 5, 5);
                        // G.DrawLine(assignColor(T), new Point((int)prevPoint.X, (int)prevPoint.Y), new Point((int)C.X, (int)C.Y));
                       TargetsListofPoints.Add(new PointR(C.X, C.Y, Environment.TickCount));
                    }

                }

                for (int T = 0; T < fingerTracker.Count; T++)
                {

                    float visibility = (float)fingerTracker[T].totalVisibleCount / (float)fingerTracker[T].age;
                    if ((fingerTracker[T].age < 8 && visibility < .6) || fingerTracker[T].consecutiveInvisibleCount >= 10)
                    {
                        fingerTracker.RemoveAt(T);
                    }
                }

                if (tips.Count == 0 && TargetsListofPoints.Count > 20)
                    recognizeGestureUsingNRecognizer();

                //if(tips.Count ==0)
                //{
                //    if (TargetsListofPoints.Count > 10)
                //    {
                //        NBestList result = rec.Recognize(TargetsListofPoints, TargetKalmanList.Count);
                //        if(result.Score >.9)
                //                  MessageBox.Show(String.Format("{0}: {1} ({2}px, {3}{4})\n[{5} out of {6} comparisons made]",
                //   result.Name,
                //   Math.Round(result.Score, 2),
                //   Math.Round(result.Distance, 2),
                //   Math.Round(result.Angle, 2), (char)176,
                //   result.getActualComparisons(),
                //   result.getTotalComparisons()));
                //        TargetsListofPoints.Clear();
                //        isTrackIntialized = false;
                //        TargetKalmanList.Clear();
                //        TargetList.Clear();
                //        fingerTipsTrajectoryBox.Refresh();
                //    }
                //}

            }
          //  recognizeGesture();
            //if( frameCount==100)
            //{
            //    recognizeGesture();
            //    frameCount = 0;
            //}
                
        }


        private void recognizeGestureUsingNRecognizer()
        {
            
                NBestList result = rec.Recognize(TargetsListofPoints,1);
                if (result.Score > .5)
                {
                    if (result.Name == "TranslateRight")
                    {
                        controlModel("translate", "+{RIGHT 10}");
                    }
                    else if (result.Name == "TranslateLeft")
                    {
                        controlModel("translate", "+{LEFT 10}");

                    }
                    else if (result.Name == "ZoomIn")
                    {
                        controlModel("Zoom", "%{UP 10}");
                    }
                    else if (result.Name == "ZoomOut")
                    {
                        controlModel("Zoom", "%{DOWN 10}");
                    }
                    else if (result.Name == "RotateLeft")
                    {
                        controlModel("Rotate", "{LEFT 10}");
                    }
                    else if (result.Name == "RotateRight")
                    {
                        controlModel("Rotate", "{RIGHT 10}");
                    }
                    else if (result.Name == "KZoomIn")
                    {
                        controlModel("Zoom", "%{UP 10}");
                    }
                    else if (result.Name == "KZoomOut")
                    {
                        controlModel("Zoom", "%{DOWN 10}");
                    }
                

                TargetsListofPoints.Clear();
                 
            }
        }
     
        private void recognizeGesture(List<Point> tips)
        {
            //handle translation case
            if(tips.Count ==1)
            {
                //fingerTipDetection.distanceP2P(tips[0], previousPoint);
               // 
               int xdirection = tips[0].X-previousPoint.X;
                int ydirection = tips[0].Y-previousPoint.Y;
                previousPoint = tips[0];
                if (xdirection > 0)
                    controlModel("translate", "+{RIGHT}");
                else
                    controlModel("translate", "+{LEFT}");
                if(ydirection > 0)
                    controlModel("translate", "+{DOWN}");
                else
                    controlModel("translate", "+{UP}");
            }
            //handle Scale case
            else if(tips.Count==2)
            {
                //get distance between 2 pair of point and based on determine 
                float distance = fingerTipDetection.distanceP2P(previousPoint, previosPoint2);
                float distance2 = fingerTipDetection.distanceP2P(tips[0], tips[1]);
                previousPoint = tips[0];
                previosPoint2 = tips[1];

                if(distance < distance2)
                    controlModel("rotate", "{RIGHT}");
                else
                    controlModel("rotate", "{LEFT}");
            }

            //if (TargetsListofPoints.Count > 10)
            //{
            //    NBestList result = rec.Recognize(TargetsListofPoints, fingerTracker.Count);
            //    if (result.Score > .5)
            //    {
            //        if (result.Name == "ZoomIn")
            //        {
            //            MessageBox.Show("Zoom IN");
            //            //FingerTipsTrackingBox.Image = testImage.Resize(scale, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);
            //            //scale += .1f;
            //        }
            //        else if (result.Name == "ZoomOut")
            //        {
            //            MessageBox.Show("Zoom Out");
            //            //FingerTipsTrackingBox.Image = testImage.Resize(scale, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);
            //            //scale /= .1f;

            //        }
            //        else if(result.Name =="rotateLeft")
            //        {
            //            controlModel("zoom", "+{LEFT 20}");
            //        }
            //        else if (result.Name == "rotateRight")
            //        {
            //            controlModel("zoom", "+{RIGHT 20}");
            //        }
            //    }

            //    TargetsListofPoints.Clear();
            //    isTrackIntialized = false;
            //    fingerTracker.Clear();
            //    distanceTransformImage.Refresh();
            //}
        }


        /// <summary>
        /// Convert the coordinates for the image's SizeMode.
        /// </summary>
        /// http://csharphelper.com/blog/2014/10/select-parts-of-a-scaled-image-picturebox-different-sizemode-values-c/</a>
        /// http://csharphelper.com/blog/2014/10/select-parts-of-a-scaled-image-picturebox-different-sizemode-values-c/</a>
        /// <param name="pic"></param>
        /// <param name="X0">out X coordinate</param>
        /// <param name="Y0">out Y coordinate</param>
        /// <param name="x">atual coordinate</param>
        /// <param name="y">atual coordinate</param>
        public static void ConvertCoordinates(PictureBox pic,
            out int X0, out int Y0, int x, int y)
        {
            int pic_hgt = pic.ClientSize.Height;
            int pic_wid = pic.ClientSize.Width;
            int img_hgt = pic.Image.Height;
            int img_wid = pic.Image.Width;

            X0 = x;
            Y0 = y;
            switch (pic.SizeMode)
            {
                case PictureBoxSizeMode.AutoSize:
                case PictureBoxSizeMode.Normal:
                    // These are okay. Leave them alone.
                    break;
                case PictureBoxSizeMode.CenterImage:
                    X0 = x - (pic_wid - img_wid) / 2;
                    Y0 = y - (pic_hgt - img_hgt) / 2;
                    break;
                case PictureBoxSizeMode.StretchImage:
                    X0 = (int)(img_wid * x / (float)pic_wid);
                    Y0 = (int)(img_hgt * y / (float)pic_hgt);
                    break;
                case PictureBoxSizeMode.Zoom:
                    float pic_aspect = pic_wid / (float)pic_hgt;
                    float img_aspect = img_wid / (float)img_wid;
                    if (pic_aspect > img_aspect)
                    {
                        // The PictureBox is wider/shorter than the image.
                        Y0 = (int)(img_hgt * y / (float)pic_hgt);

                        // The image fills the height of the PictureBox.
                        // Get its width.
                        float scaled_width = img_wid * pic_hgt / img_hgt;
                        float dx = (pic_wid - scaled_width) / 2;
                        X0 = (int)((x - dx) * img_hgt / (float)pic_hgt);
                    }
                    else
                    {
                        // The PictureBox is taller/thinner than the image.
                        X0 = (int)(img_wid * x / (float)pic_wid);

                        // The image fills the height of the PictureBox.
                        // Get its height.
                        float scaled_height = img_hgt * pic_wid / img_wid;
                        float dy = (pic_hgt - scaled_height) / 2;
                        Y0 = (int)((y - dy) * img_wid / pic_wid);
                    }
                    break;
            }
        } 
        private void StartButton_Click(object sender, EventArgs e)
        {
            if (!startCapture)
            {
                // 
                StartButton.Text = "Stop";
                Application.Idle += processImage;// processImage;
               // Application.Idle += processImageUsingTemplateMatching;
                
            }
            else
            {

                imageBoxSkin.Refresh();
                StartButton.Text = "Resume";
                Application.Idle -= processImage   ;//processImage;
            //    Application.Idle -= processImageUsingTemplateMatching;
                
                
            }
            startCapture = !startCapture;
        }

        

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            // Determine the initial rectangle coordinates...
            RectStartPoint = e.Location;
            Invalidate();
        }

        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            // Draw the rectangle...
            if (pictureBox1.Image != null)
            {
                if (Rect != null && Rect.Width > 0 && Rect.Height > 0)
                {
                    //Seleciona a ROI
                    e.Graphics.SetClip(Rect, System.Drawing.Drawing2D.CombineMode.Exclude);
                    e.Graphics.FillRectangle(selectionBrush, new Rectangle(0, 0, ((PictureBox)sender).Width, ((PictureBox)sender).Height));
                    //e.Graphics.FillRectangle(selectionBrush, Rect);
                }
            }
        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            #region SETS COORDINATES AT INPUT IMAGE BOX
            int X0, Y0;
            ConvertCoordinates(pictureBox1, out X0, out Y0, e.X, e.Y);
            // labelPostionXY.Text = "Last Position: X:" + X0 + "  Y:" + Y0;

            //Coordinates at input picture box
            if (e.Button != MouseButtons.Left)
                return;
            Point tempEndPoint = e.Location;
            Rect.Location = new Point(
                Math.Min(RectStartPoint.X, tempEndPoint.X),
                Math.Min(RectStartPoint.Y, tempEndPoint.Y));
            Rect.Size = new Size(
                Math.Abs(RectStartPoint.X - tempEndPoint.X),
                Math.Abs(RectStartPoint.Y - tempEndPoint.Y));
            #endregion

            #region SETS COORDINATES AT REAL IMAGE
            //Coordinates at real image - Create ROI
            ConvertCoordinates(pictureBox1, out X0, out Y0,
           RectStartPoint.X, RectStartPoint.Y);
            int X1, Y1;
            ConvertCoordinates(pictureBox1, out X1, out Y1, tempEndPoint.X, tempEndPoint.Y);
            RealImageRect.Location = new Point(
                Math.Min(X0, X1),
                Math.Min(Y0, Y1));
            RealImageRect.Size = new Size(
                Math.Abs(X0 - X1),
                Math.Abs(Y0 - Y1));

            skin.Draw(RealImageRect, new Bgr(Color.Red), thickness);
            pictureBox1.Image = skin.Bitmap;
            #endregion

            ((PictureBox)sender).Invalidate();
        }

        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            //Define ROI. Valida altura e largura para evitar index range exception.
            if (RealImageRect.Width > 0 && RealImageRect.Height > 0)
            {
                skin.ROI = RealImageRect;
                imageBoxSkin.Image = skin;
                inDetectionPhase = true;

                //get FingervTip template
                template = skin.InRange(new Bgr(150, 150, 150), new Bgr(255, 255, 255));
            }

        }
    }
}
