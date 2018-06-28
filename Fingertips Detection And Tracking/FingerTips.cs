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
using Emgu.CV.CvEnum;
using Emgu.CV.Util;
using HandGestureRecognition.SkinDetector;

namespace Depth_Kinect.tries
{
    public partial class hand_detection : Form
    {
        /// Set /Get the camera properties 
        /// </summary>
        Capture grabber = null;

        /// <summary>
        /// Extract the skin  color like objects from an image
        /// </summary>
        AdaptiveSkinDetector detector = null;

        /// <summary>
        /// Set /Get the image  width 
        /// </summary>
        int width;

        /// <summary>
        /// Set /Get the image  height 
        /// </summary>
        int height,
            count = 0;

        /// <summary>
        /// Used to determine the value of threshold
        /// <value> default value 80 </value>
        /// </summary>
        int threshold = 80;

        /// <summary>
        /// Used to Determine the number of finger peak
        /// </summary>
        int numberOfPeaks = 0;

        /// <summary>
        ///  Used to Determine the number of valley between 2 fingers
        /// </summary>
        int numberOfValleys = 0;

        /// <summary>
        /// Number of detected hands
        /// </summary>
        int numberOfHands = 0;

        /// <summary>
        /// Number of hands required to be detected
        /// </summary>
        int hand_detected = 0,
            kernel_size = 3;



        /// <summary>
        /// area of recatangle
        /// </summary>
        double area = 0.0;

        /// <summary>
        /// Determine the accuracy when approximate a contour to a polygon
        /// <value>default value id 20</value>
        /// </summary>
        double accuracy = 20.0d,
               min_length = 0.0,
               max_length = 0.0;

        /// <summary>
        /// Determine the time interval between the current frame and the next frame
        /// </summary>
        double elapsed_time;


        DIST_TYPE dt = DIST_TYPE.CV_DIST_L2;


        Bgr color_blue = new Bgr(Color.DarkBlue);
        Bgr color2_brown = new Bgr(Color.Brown);

        Gray thresholdValue = new Gray(1);
        Gray MaxValue = new Gray(255);

        /// <summary>
        /// Determine the centre of a circle
        /// </summary>
        PointF center_pt;

        static Image<Gray, byte> newImageG = null;
        static Image<Gray, byte> current_image = null;
        Image<Gray, byte> tempImage = null;



        static Image<Bgr, byte> newImage = null;

        // HandTracking y;

        System.Diagnostics.Stopwatch sw;


        List<Contour<Point>> handCandiate;
        List<Contour<Point>> detected_hand;
        // List<HandTracking> x;

        Dictionary<int, PointF> hand_centers;

        private int camera_index;

        Hsv hsv_min;
        Hsv hsv_max;
        Ycc YCrCb_min;
        Ycc YCrCb_max;
        IColorSkinDetector skinDetector;


        ///
        bool isTrackIntialized = false;
        bool startCapture = false;
        #region Variables
        List<KalmanTracker> TargetKalmanList = new List<KalmanTracker>();
        //Dictionary<int, KalmanTracker> TargetKalmanList = new Dictionary<int, KalmanTracker>();
        Dictionary<int, PointF> TargetList = new Dictionary<int, PointF>();
        List<int> kalmanID = new List<int>();
        //Dictionary<int, PointF> TrackTarget = new Dictionary<int, PointF>();
        Dictionary<int, int> PredictedKalmans = new Dictionary<int, int>();
        int frameNumber = 0;
        #endregion

        struct Tracks
        {
            public int id;
            public KalmanTracker tracker;
            public long age;
            public int totalVisibleCount;
            public int consecutiveInvisibleCount;
            public Tracks(int Id, long fingerAge)
            {
                id = Id;
                age = fingerAge;
                totalVisibleCount = (int)fingerAge;
                consecutiveInvisibleCount = (int)fingerAge;
                tracker = new KalmanTracker();

            }

        }

        List<Tracks> fingerTracker;

        /// <summary>
        /// Get <c> current_image </c> 
        /// it's a read only property 
        /// </summary>
        public static Image<Gray, byte> Current_Image
        {
            get
            {
                return current_image;
            }

        }

        /// <summary>
        /// Get or Set  <c> NewImageG </c> 
        /// it's read / write property 
        /// </summary>
        public static Image<Gray, byte> NewImageG
        {
            get
            {
                return newImageG;
            }
            set
            {
                newImageG = value;
            }

        }

        /// <summary>
        /// Get or Set  <c> NewImage </c> 
        /// it's read / write property 
        /// </summary>
        public static Image<Bgr, byte> NewImage
        {
            get
            {
                return newImage;
            }
            set
            {
                newImage = value;
            }

        }
        List<Point> finger;

        public hand_detection()
        {
            InitializeComponent();

            //  x = new List<HandTracking>(2);
            handCandiate = new List<Contour<Point>>();
            detected_hand = new List<Contour<Point>>();

            hand_centers = new Dictionary<int, PointF>(2);

            grabber = new Emgu.CV.Capture(@"D:\Working\STREAM\test.avi");

            hsv_min = new Hsv(0, 45, 0);
            hsv_max = new Hsv(20, 255, 255);
            YCrCb_min = new Ycc(0, 131, 80);
            YCrCb_max = new Ycc(255, 185, 135);


            fingerTracker = new List<Tracks>();
            // y = null;


            //height = (int)grabber.GetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_FRAME_HEIGHT);
            //width = (int)grabber.GetCaptureProperty(Emgu.CV.CvEnum.CAP_PROP.CV_CAP_PROP_FRAME_WIDTH);



            //detector = new AdaptiveSkinDetector(1, AdaptiveSkinDetector.MorphingMethod.NONE);


            //tempImage = new Image<Gray, Byte>(width, height);

            //current_image = new Image<Gray, byte>(width, height);

            //newImageG = new Image<Gray, byte>(width, height);

            //sw = new System.Diagnostics.Stopwatch();

            //Application.Idle += FrameGrabber;
        }



        /// <summary>
        /// the main function in this class 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void FrameGrabber(object sender, EventArgs e)
        {
            //sw.Start();
            newImage = grabber.QueryFrame();
            CaptureImageBox.Image = newImage;
            skinDetector = new YCrCbSkinDetector();
            Image<Gray, Byte> BinaryHandImage = skinDetector.DetectSkin(newImage, YCrCb_min, YCrCb_max);
            finger = new List<Point>();
            HandDetection(BinaryHandImage);
            //int postPoint = 0;
            //List<Point> lastTry = new List<Point>();
            //for (int j = 0; j < finger.Count; j++)
            //{
            //    postPoint = j + 1;
            //    if (j == finger.Count - 1)
            //        postPoint = 0;
            //    if (finger[j].Y - finger[postPoint].Y < 10)
            //        lastTry.Add(finger[postPoint]);
            //}
            //finger = lastTry;
            for (int j = 0; j < finger.Count; j++)
            {

                newImage.Draw(new Emgu.CV.Structure.CircleF(finger[j], 5), new Bgr(Color.Blue), 6);
            }

            //count++;
            //if (newImage != null)
            //{
            //    current_image = newImage.Convert<Gray, byte>();
            //    detector.Process(newImage, tempImage);

            //    tempImage = tempImage.ThresholdBinary(thresholdValue, MaxValue);
            //    tempImage = tempImage.Dilate(2);
            //    tempImage = tempImage.SmoothMedian(3);

            //    newImageG = current_image.ThresholdBinaryInv(new Gray(threshold), new Gray(255d));
            //    newImageG = newImageG.And(tempImage);
            //    newImageG = newImageG.Dilate(1);

            //    //if (numberOfHands > 0)
            //    //{
            //    //    int tt = numberOfHands;
            //    //    for (int i = 0; i < tt; i++)
            //    //    {
            //    //        if (x[i] != null)
            //    //        {
            //    //            try
            //    //            {
            //    //                x[i].StartTracking(elapsed_time);
            //    //            }


            //    //            catch (Exception ex)
            //    //            {
            //    //                Console.WriteLine("lost traking : number  of hands {0} & list x {1}", numberOfHands, x.Count);
            //    //                int id = x[i].id;
            //    //                hand_centers[id] = x[i].new_center_pt;
            //    //                hand_centers.Remove(id);
            //    //                x.RemoveAt(id);
            //    //                --numberOfHands;

            //    //            }
            //    //        }

            //    //    }

            //    //}


            //    //if (numberOfHands < hand_detected)
            //    //{
            //    //    detected_hand = HandDetection(newImageG);
            //    //    if (detected_hand.Any())// any elements in the list
            //    //    {
            //    //        foreach (Contour<Point> h in detected_hand)
            //    //        {
            //    //            if (numberOfHands < hand_detected)
            //    //            {

            //    //                y = new HandTracking(current_image.Width, current_image.Height, hand_centers[numberOfHands]);

            //    //                y.ExtractFeatures(h);
            //    //                y.id = numberOfHands;
            //    //                x.Add(y);

            //    //                numberOfHands++;

            //    //            }
            //    //            else
            //    //                Console.WriteLine("there is already 2 hands");
            //    //        }
            //    //        detected_hand.Clear();

            //    //    }
            //    //}

            //    sw.Stop();
            //    elapsed_time = sw.Elapsed.TotalMilliseconds;

            //    sw.Reset();
            //    imageBoxSkin.Image = newImage;
            //    imageBoxFrameGrabber.Image = newImageG;





        }





        /// <summary>
        /// hand detection function 
        /// </summary>
        /// <param name="skin">a binary image that contains skin like objects</param>
        /// <returns>a list that contains detected hands</returns>
        private List<Contour<Point>> HandDetection(Image<Gray, byte> skin)
        {

            Point first_peak = new Point(),
                  first_valley = new Point(),
                  reference_peak = new Point(),
                  refernce_valley = new Point();



            double[,] v1 = new double[2, 1],
                      v2 = new double[2, 1];

            double angle;

            int direction,
                length,
                mod;

            bool tester_peak = false,
                tester_valley = false;



            using (MemStorage storage = new MemStorage())
            {


                handCandiate.Clear();

                for (Contour<Point> i = skin.FindContours(Emgu.CV.CvEnum.CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_SIMPLE,
                                                          Emgu.CV.CvEnum.RETR_TYPE.CV_RETR_EXTERNAL,
                                                          storage);
                                    i != null;
                                    i = i.HNext)
                {
                    area = i.BoundingRectangle.Height * i.BoundingRectangle.Width;


                    if (area > 3000 && !(i.Convex))
                    {

                        tester_peak = false;
                        tester_valley = false;
                        skin.ROI = i.BoundingRectangle;

                        this.center_pt = FindCentroidByDistanceTrans(skin);

                        this.center_pt.X += skin.ROI.X;
                        this.center_pt.Y += skin.ROI.Y;
                        skin.ROI = Rectangle.Empty;

                        Contour<Point> tt = i.ApproxPoly(accuracy, storage);

                        LineSegment2D[] edges = PointCollection.PolyLine(tt.ToArray(), true);


                        length = edges.Length;
                        for (int ij = 0; ij < length; ij++)
                        {
                            mod = (ij + 1) % length;

                            v1[0, 0] = edges[ij].P2.X - edges[ij].P1.X;
                            v1[1, 0] = edges[ij].P2.Y - edges[ij].P1.Y;
                            v2[0, 0] = edges[mod].P1.X - edges[mod].P2.X;
                            v2[1, 0] = edges[mod].P1.Y - edges[mod].P2.Y;

                            // this equation is quoted from http://www.mathworks.com/matlabcentral/newsreader/view_thread/276582
                            // and it is working very good

                            angle = Math.Atan2(Math.Abs(det(v1, v2)), dot(v1, v2)) * (180.0 / Math.PI);

                            if (angle < 90)
                            {

                                direction = dir(edges[ij].P1, edges[ij].P2, edges[mod].P2);

                                //if (direction > 0)
                                //{


                                //    if (
                                //        ((edges[ij].Length < max_length && edges[ij].Length > min_length)
                                //        ||
                                //        (edges[mod].Length < max_length && edges[mod].Length > min_length))

                                //        )
                                //    {


                                //        refernce_valley = edges[ij].P2;
                                //      //  finger.Add(refernce_valley);
                                //        numberOfValleys++;


                                //        if (FindDistance(edges[ij].P2, first_valley) < min_length
                                //               && FindDistance(edges[ij].P2, first_valley) > (0.5 * min_length)
                                //            //  && FindDistance(edges[ij].P2,center_pts) > min_length
                                //            //    && FindDistance(edges[ij].P2, center_pts) < max_length 
                                //               )
                                //        {

                                //            if (FindDistance(edges[ij].P2, first_peak) > min_length
                                //                &&
                                //                FindDistance(edges[ij].P2, first_peak) < max_length
                                //                )
                                //            {

                                //             //   finger.Add(first_peak);
                                //                numberOfValleys++;

                                //            }






                                //            else if (FindDistance(edges[ij].P2, refernce_valley) < min_length
                                //                    && FindDistance(edges[ij].P2, refernce_valley) > (0.5 * min_length)
                                //                //   && FindDistance(edges[ij].P2, center_pts) > min_length
                                //                //     && FindDistance(edges[ij].P2, center_pts) < max_length
                                //                    )
                                //            {
                                //           //     finger.Add(refernce_valley);
                                //                numberOfValleys++;


                                //            }
                                //        }
                                //    }

                                //    first_valley = edges[ij].P2;
                                //}
                                if (direction < 0)
                                {

                                    //   vally

                                    if (
                                        (edges[ij].Length < max_length && edges[ij].Length > min_length)
                                             ||
                                        (edges[mod].Length < max_length && edges[mod].Length > min_length)

                                        )
                                    {
                                        // if (!tester_peak)
                                        //  {
                                        tester_peak = true;
                                        reference_peak = edges[ij].P2;
                                        finger.Add(reference_peak);
                                        numberOfPeaks++;


                                        //  }

                                        if (FindDistance(edges[ij].P2, first_peak) > min_length
                                                    &&
                                                 FindDistance(edges[ij].P2, first_peak) < max_length)
                                        {


                                            if (FindDistance(edges[ij].P2, first_valley) > min_length
                                                &&
                                                FindDistance(edges[ij].P2, first_valley) < max_length
                                                )
                                            {

                                                finger.Add(first_valley);
                                                numberOfPeaks++;

                                            }



                                        }

                                        else if (FindDistance(edges[ij].P2, reference_peak) > min_length
                                                    &&
                                                 FindDistance(edges[ij].P2, reference_peak) < max_length)
                                        {
                                            finger.Add(reference_peak);
                                            numberOfPeaks++;

                                        }

                                        first_peak = edges[ij].P2;

                                    }

                                }


                            }


                        }



                        numberOfPeaks = 0;
                        numberOfValleys = 0;


                    }
                }
                trackFingerTips(finger);
            }

            return handCandiate;
        }

        /// <summary>
        /// Extract the biggest Contour in the image 
        /// </summary>
        /// <param name="local">a binary image</param>
        /// <returns>the biggest contour </returns>
        private Contour<Point> ExtractBiggestContour(Image<Gray, byte> local)
        {
            Contour<Point> biggestContour = null;
            MemStorage storage = new MemStorage();

            Contour<Point> contours = FindContours(local, Emgu.CV.CvEnum.CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_SIMPLE, Emgu.CV.CvEnum.RETR_TYPE.CV_RETR_LIST, storage);


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

        /// <summary>
        /// find contours in the image
        /// </summary>
        /// <param name="local"> the image </param>
        /// <param name="cHAIN_APPROX_METHOD">param provided to the opencv function</param>
        /// <param name="rETR_TYPE">param provided to the opencv function</param>
        /// <param name="stor">param provided to the opencv function </param>
        /// <remarks>For more information about previous parameters and contours see opencv book chapter 8 And/Or opencv reference manual v2.1 March 18, 2010  page 343 </remarks>
        /// <returns> the founded contours </returns>

        private Contour<Point> FindContours(Image<Gray, byte> local, CHAIN_APPROX_METHOD cHAIN_APPROX_METHOD, RETR_TYPE rETR_TYPE, MemStorage stor)
        {
            using (Image<Gray, byte> imagecopy = local.Copy()) //since cvFindContours modifies the content of the source, we need to make a clone
            {
                IntPtr seq = IntPtr.Zero;
                CvInvoke.cvFindContours(
                    imagecopy.Ptr,
                    stor.Ptr,
                    ref seq,
                    StructSize.MCvContour,
                    rETR_TYPE,
                    cHAIN_APPROX_METHOD,
                    new Point(local.ROI.X, local.ROI.Y));// because of ROI, the contour is offset or shifted 

                return (seq == IntPtr.Zero) ? null : new Contour<Point>(seq, stor);
            }
        }
        /// <summary>
        /// find the distance between 2 points using Euclidean distance law
        /// </summary>
        /// <param name="p1"><see cref="System.Drawing.Point"/>first point </param>
        /// <param name="p2"><see cref="System.Drawing.Point"/>second point</param>
        /// <returns>the real distance </returns>
        /// <remarks>for more info. visit http://www.mathopenref.com/coorddist.html </remarks>
        private double FindDistance(PointF p1, PointF p2)
        {
            double dx = p2.X - p1.X;
            double dy = p2.Y - p1.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }
        /// <summary>
        /// perform distance transform for a binary (black and white ) image 
        /// </summary>
        /// <param name="binary_image">black and white image</param>
        /// <returns>furthest white point from a black pixel/point</returns>
        /// <remarks>for more info about cvDistTransform function see :- opencv reference manual v2.1 March 18, 2010 page 270 And/Or see opencv book chapter 6 page 205 </remarks>
        private PointF FindCentroidByDistanceTrans(Image<Gray, byte> binary_image)
        {
            double max_value = 0.0d,
                   min_value = 0.0d;

            Point max_location = new Point(0, 0),
                  min_location = new Point(0, 0);

            using (Image<Gray, float> distTransform = new Image<Gray, float>(binary_image.Width, binary_image.Height))
            {


                CvInvoke.cvDistTransform(binary_image, distTransform, dt, kernel_size, null, IntPtr.Zero);
                CvInvoke.cvMinMaxLoc(distTransform, ref min_value, ref max_value, ref min_location, ref max_location, IntPtr.Zero);

                this.min_length = max_value;
                this.max_length = 3 * max_value;
            }




            return max_location;

        }

        /// <summary>
        /// provide the direction of the angel of the middle point for 3 points (upperward or downward direction)
        /// </summary>
        /// <param name="point1"> <see cref="System.Drawing.Point"/>first point</param>
        /// <param name="point2"><see cref="System.Drawing.Point"/>second point</param>
        /// <param name="point3"><see cref="System.Drawing.Point"/>third point</param>
        /// <returns>if the value is positive then the direction is opening upperward otherwise downward </returns>
        private int dir(Point point1, Point point2, Point point3)
        {
            //this equation is quoted from wikipedia http://en.wikipedia.org/wiki/Cross_product#Computational_geometry
            int result = ((point2.X - point1.X) * (point3.Y - point1.Y) - (point2.Y - point1.Y) * (point3.X - point1.X));

            return result;

        }

        /// <summary>
        /// perform dot multiplication for a matrix or a vector
        /// </summary>
        /// <param name="v1">the first (one column)array or vector</param>
        /// <param name="v2">the second (one column)array or vector</param>
        /// <returns>the result </returns>
        private double dot(double[,] v1, double[,] v2)
        {
            return ((v1[0, 0] * v2[0, 0]) + (v1[1, 0] * v2[1, 0]));
        }
        /// <summary>
        /// find the determined of 2 matrix 
        /// </summary>
        /// <param name="v1">first matrix</param>
        /// <param name="v2">second matrix</param>
        /// <returns></returns>
        private double det(double[,] v1, double[,] v2)
        {
            return ((v1[0, 0] * v2[1, 0]) - (v1[1, 0] * v2[0, 0]));
        }

        private void TargetGenerate(PointF p)
        {
            TargetList.Add(TargetKalmanList.Count, p);
            //Create a kalman tracker
            TargetKalmanList.Add(new KalmanTracker());
            TargetKalmanList[TargetKalmanList.Count - 1].UpdateTarget(p);
            TargetKalmanList[TargetKalmanList.Count - 1].SetPredicted(p);

            //$N
            //  TargetsListofPoints.Add(TargetKalmanList.Count - 1,_points);//.Add(p.X,p.Y,Environment.TickCount));
            //   TargetsListofPoints.Add(new PointR(p.X, p.Y, Environment.TickCount));

        }
        float distanceP2P(PointF a, PointF b)
        {
            float d = (float)Math.Sqrt(Math.Abs(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2)));
            return d;
        }
        /************  Track Fingers*/
        void trackFingerTips(List<Point> tips)
        {

            Graphics G = imageBoxSkin.CreateGraphics();
            if (!isTrackIntialized)
            {
                if (tips.Count != 0)
                {
                    for (int i = 0; i < tips.Count; i++)
                    {
                        //TargetGenerate(tips[i]);
                        Tracks finger = new Tracks(i, i + 1);

                        fingerTracker.Add(finger);

                        G.FillEllipse(Brushes.Red, tips[i].X, tips[i].Y, 5, 5);
                    }

                    isTrackIntialized = true;
                }
            }

            else
            {
                List<int> filter_used = new List<int>();
                PointF C = new PointF();
                PointF prevPoint = new PointF();

                for (int T = 0; T < tips.Count; T++)
                {
                    PointF New_location = new PointF(tips[T].X, tips[T].Y);
                    float result = 1000;
                    float distance = 0;
                    int selectedTarget = 0;
                    PointF predit = new PointF();
                    for (int TC = 0; TC < fingerTracker.Count; TC++)
                    // for (int TC = 0; TC < TargetKalmanList.Count; TC++)
                    {
                        //  distance = distanceP2P(New_location, predit = TargetKalmanList[TC].GetPredicted());
                        distance = distanceP2P(New_location, predit = fingerTracker[TC].tracker.GetPredicted());

                        if (result > distance)
                        {
                            result = distance;
                            selectedTarget = TC;
                        }
                    }


                    if (result > 400)
                    {

                        //TargetGenerate(New_location);
                        Tracks finger = new Tracks(fingerTracker.Count, 1);
                        fingerTracker.Add(finger);
                        G.FillEllipse(Brushes.Orange, New_location.X, New_location.Y, 5, 5);

                    }
                    else
                    {

                        filter_used.Add(selectedTarget);

                        //prevPoint = TargetList[selectedTarget];
                        //TargetList[selectedTarget] = New_location;
                        //TargetKalmanList[selectedTarget].UpdateTarget(New_location);

                        fingerTracker[selectedTarget].tracker.UpdateTarget(New_location);

                        C = fingerTracker[selectedTarget].tracker.GetEstimate();
                        G.FillEllipse(Brushes.Black, C.X, C.Y, 5, 5);

                        //   G.DrawLine(Pens.Black, new Point((int)prevPoint.X, (int)prevPoint.Y), new Point((int)C.X, (int)C.Y));
                        //   TargetsListofPoints.Add(new PointR(C.X, C.Y, Environment.TickCount));
                    }

                }
                for (int T = 0; T < TargetKalmanList.Count; T++)
                {
                    if (!filter_used.Contains(T))
                    {
                        ///

                        //    PredictedKalmans.Add(T,PredictedKalmans[T]+1);
                        //  prevPoint = TargetList[T];
                        C = fingerTracker[T].tracker.GetPredicted();
                        // C = TargetKalmanList[T].GetPredicted();
                        G.FillEllipse(Brushes.Blue, C.X, C.Y, 5, 5);
                        // G.DrawLine(assignColor(T), new Point((int)prevPoint.X, (int)prevPoint.Y), new Point((int)C.X, (int)C.Y));
                        //    TargetsListofPoints.Add(new PointR(C.X, C.Y, Environment.TickCount));
                    }

                }
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
        }
        private void StartButton_Click(object sender, EventArgs e)
        {
            if (!startCapture)
            {
                // 
                StartButton.Text = "Stop";
                Application.Idle += FrameGrabber;
            }
            else
            {

                imageBoxSkin.Refresh();
                StartButton.Text = "Resume";
                Application.Idle -= FrameGrabber;
            }
            startCapture = !startCapture;

            if (grabber == null)
            {
                grabber = new Emgu.CV.Capture();

            }
            else
            {
                Application.Idle += FrameGrabber;
            }
        }
    }
}
