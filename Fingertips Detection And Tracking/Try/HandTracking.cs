using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using System.Collections;
using System.Diagnostics;
using Emgu.CV.UI;
using Emgu.CV.VideoSurveillance;
using System.Runtime.InteropServices;




namespace FingerTipsDetectionTracking.Try
{
    public partial class HandTracking : Form
    {
        FingerTipsDetectionTracking.FingerTip fingerTipDetection;

        private Capture _capture;
        private HaarCascade _faces;
        private MCvAvgComp[][] faceDetected;

        public Image<Bgr, Byte> frame { get; set; }
        public Image<Gray, Byte> grayFrame { get; set; }
        public Image<Bgr, Byte> nextFrame { get; set; }
        public Image<Gray, Byte> nextGrayFrame { get; set; }
        public Image<Bgr, Byte> opticalFlowFrame { get; set; }
        public Image<Gray, Byte> opticalFlowGrayFrame { get; set; }
        public Image<Bgr, Byte> faceImage { get; set; }
        public Image<Bgr, Byte> faceNextImage { get; set; }
        public Image<Gray, Byte> faceGrayImage { get; set; }
        public Image<Gray, Byte> faceNextGrayImage { get; set; }
        public Image<Gray, Single> velx { get; set; }
        public Image<Gray, Single> vely { get; set; }
        public PointF[][] vectorField { get; set; }
        public int vectorFieldX { get; set; }
        public int vectorFieldY { get; set; }
        public Image<Gray, Byte> flow { get; set; }

        public PointF[][] ActualFeature;
        public PointF[] NextFeature;
        public Byte[] Status;
        public float[] TrackError;

        public Rectangle trackingArea;
        public PointF[] hull, nextHull;
        public PointF referenceCentroid, nextCentroid;
        public float sumVectorFieldX;
        public float sumVectorFieldY;

        private HaarCascade haar;
        bool gestureChanged = false;
        public HandTracking()
        {
            fingerTipDetection = new FingerTip();

            InitializeComponent();
            System.Diagnostics.Process.GetCurrentProcess().PriorityClass = System.Diagnostics.ProcessPriorityClass.High;
            _capture = new Capture();//"LucaHead.wmv");
            // adjust path to find your XML file 
            haar = new HaarCascade(@"F:\Working\Final phase\aGest.xml");
        }

        private void buttonInitializeTracking_Click(object sender, EventArgs e)
        {
           
            InitializeFaceTracking();
            Application.Idle += new EventHandler(Application_Idle);
        }
        private void processImage()
        {
            frame = _capture.QueryFrame();//line 1

            if (frame == null)
                return;
            frame._SmoothGaussian(3);


            //  skin._EqualizeHist();

            // Image<Gray, Byte> BinaryHandImage = skinDetector.DetectSkin(skin, YCrCb_min, YCrCb_max);
            //background subtraction
            // Image<Bgr, Byte> BinaryImage =null;
            // CvInvoke.cvAbsDiff(skin, bg, BinaryImage);
            // Image<Bgr, Byte> BinaryImage = skin.AbsDiff(bg); //find the absolute difference 
            //   Image<Gray, Byte> BinaryHandImage = BinaryImage.Convert<Gray, byte>();
            // use glove

            grayFrame = frame.InRange(new Bgr(150, 150, 150), new Bgr(255, 255, 255));
            grayFrame._Erode(3);
            grayFrame._Dilate(3);
            
                Contour<Point> handContour = null;
                using (MemStorage m = new MemStorage())
                {
                    Contour<System.Drawing.Point> MaxContour = new Contour<Point>(m);

                    Contour<System.Drawing.Point> Contours =
                        grayFrame.FindContours(Emgu.CV.CvEnum.CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_SIMPLE, Emgu.CV.CvEnum.RETR_TYPE.CV_RETR_TREE);

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
                        frame.Draw(hull, new Bgr(Color.Green), 2);
                        //Contour<System.Drawing.Point> Contour = handContour.ApproxPoly(handContour.Perimeter * .1);
                        //for (int i = 0; i < indeces.Rows; i++)
                        //{
                        //    skin.Draw(new Emgu.CV.Structure.CircleF(handContour.ElementAt(indeces.Data[i,0]), 5), new Bgr(Color.Blue), 6);
                        //}
                        frame.Draw(handContour, new Bgr(Color.Red), 2);
                        //   Seq<MCvConvexityDefect> defects = handContour.GetConvexityDefacts(m, Emgu.CV.CvEnum.ORIENTATION.CV_CLOCKWISE);
                        //      List<Point> tips = fingerTipDetection.find_fingerTips(defects, handContour.BoundingRectangle);
                        /// List<Point> tips = fingerTipDetection.filtering_tips(defects, handContour);
                        List<Point> tips = fingerTipDetection.findFingerTips(handContour);
                        // Robust Fingertip Tracking with Improved Kalman Filter paper
                        //filter tips
                        Image<Gray, float> distTransform = new Image<Gray, float>(grayFrame.Size);


                        CvInvoke.cvDistTransform(grayFrame, distTransform, Emgu.CV.CvEnum.DIST_TYPE.CV_DIST_L2, 3, null, IntPtr.Zero);
                        distTransform = distTransform.ThresholdBinary(new Gray(10), new Gray(255));
                        distTransform._Erode(3);
                        // distTransform._Dilate(3);
                        Image<Gray, byte> disTransImage = distTransform.Convert<Gray, byte>();
                        Contour<Point> contour = disTransImage.FindContours();
                        contour = fingerTipDetection.ExtractBiggestContour(contour);
                        if (contour != null)
                        {
                            disTransImage.Draw(contour, new Gray(100), 8);
                            
                            // tips= fingerTipDetection.Cluster(tips);
                            for (int i = 0; i < tips.Count; i++)
                            {
                                if (CvInvoke.cvPointPolygonTest(contour, tips[i], true) < -50)
                                    frame.Draw(new Emgu.CV.Structure.CircleF(tips[i], 5), new Bgr(Color.Yellow), 15);
                                else
                                    tips.RemoveAt(i);
                            }
                        }
                        PointF[] array = new PointF[tips.Count];
                       for(int i=0;i<tips.Count ;i++)
                        {
                           array[i] = tips.ElementAt(i);
                       }
                        ActualFeature.SetValue(array,0);
                        
                         

                    
                }
            }

        }
        private void InitializeFaceTracking()
        {
           // _faces = new HaarCascade("haarcascade_frontalface_alt_tree.xml");
            frame = _capture.QueryFrame();
            //We convert it to grayscale
            grayFrame = frame.Convert<Gray, Byte>();
            //// We detect a face using haar cascade classifiers, we'll work only on face area
            //faceDetected = grayFrame.DetectHaarCascade(_faces, 1.1, 1, Emgu.CV.CvEnum.HAAR_DETECTION_TYPE.DO_CANNY_PRUNING, new Size(20, 20));
            //if (faceDetected[0].Length == 1)
            //{
            //    trackingArea = new Rectangle(faceDetected[0][0].rect.X, faceDetected[0][0].rect.Y, faceDetected[0][0].rect.Width, faceDetected[0][0].rect.Height);

            //    // Here we enlarge or restrict the search features area on a smaller or larger face area
            //    float scalingAreaFactor = 0.6f;
            //    int trackingAreaWidth = (int)(faceDetected[0][0].rect.Width * scalingAreaFactor);
            //    int trackingAreaHeight = (int)(faceDetected[0][0].rect.Height * scalingAreaFactor);
            //    int leftXTrackingCoord = faceDetected[0][0].rect.X - (int)(((faceDetected[0][0].rect.X + trackingAreaWidth) - (faceDetected[0][0].rect.X + faceDetected[0][0].rect.Width)) / 2);
            //    int leftYTrackingCoord = faceDetected[0][0].rect.Y - (int)(((faceDetected[0][0].rect.Y + trackingAreaHeight) - (faceDetected[0][0].rect.Y + faceDetected[0][0].rect.Height)) / 2);
            //    trackingArea = new Rectangle(leftXTrackingCoord, leftYTrackingCoord, trackingAreaWidth, trackingAreaHeight);

            //    // Allocating proper working images
            //    faceImage = new Image<Bgr, Byte>(trackingArea.Width, trackingArea.Height);
            //    faceGrayImage = new Image<Gray, Byte>(trackingArea.Width, trackingArea.Height);
            //    frame.ROI = trackingArea;
            //    frame.Copy(faceImage, null);
            //    frame.ROI = Rectangle.Empty;
                faceGrayImage = grayFrame;
                processImage();
                // Detecting good features that will be tracked in following frames
              //  ActualFeature = faceGrayImage.GoodFeaturesToTrack(500, 0.01d, 10d, 3);
                faceGrayImage.FindCornerSubPix(ActualFeature, new Size(10, 10), new Size(-1, -1), new MCvTermCriteria(20, .03d));

                //// Features computed on a different coordinate system are shifted to their original location
                //for (int i = 0; i < ActualFeature[0].Length; i++)
                //{
                //    ActualFeature[0][i].X += trackingArea.X;
                //    ActualFeature[0][i].Y += trackingArea.Y;
                //}

            //    // Computing convex hull                
            //    using (MemStorage storage = new MemStorage())
            //        hull = PointCollection.ConvexHull(ActualFeature[0], storage, Emgu.CV.CvEnum.ORIENTATION.CV_CLOCKWISE).ToArray();

            //    referenceCentroid = FindCentroid(hull);
            //}
        }

        void Application_Idle(object sender, EventArgs e)
        {
            
                

            nextFrame = _capture.QueryFrame();
            ////Uncomment this line if you want to try out dense optical flow
            //faceNextGrayImage = new Image<Gray, byte>(trackingArea.Width, trackingArea.Height);

            if (nextFrame != null )//&& faceDetected[0].Length == 1)
            {
                nextGrayFrame = nextFrame.Convert<Gray, Byte>();
                
                ////Uncomment this lines if you want to try out dense optical flow
                //nextGrayFrame.ROI = trackingArea;
                //nextGrayFrame.Copy(faceNextGrayImage, null);
                //nextGrayFrame.ROI = Rectangle.Empty;

                opticalFlowFrame = new Image<Bgr, Byte>(frame.Width, frame.Height);
                opticalFlowGrayFrame = new Image<Gray, Byte>(frame.Width, frame.Height);
                opticalFlowFrame = nextFrame.Copy();

                ////Uncomment this line if you want to try out dense optical flow
                ComputeDenseOpticalFlow();
                //ComputeMotionFromDenseOpticalFlow();

                //Comment this line if you want to try out dense optical flow
              //  ComputeSparseOpticalFlow();
               // ComputeMotionFromSparseOpticalFlow();

             //   opticalFlowFrame.Draw(new CircleF(referenceCentroid, 1.0f), new Bgr(Color.Goldenrod), 2);
              //  opticalFlowFrame.Draw(new CircleF(nextCentroid, 1.0f), new Bgr(Color.Red), 2);

                imageBoxOpticalFlow.Image = opticalFlowFrame;
              //  InitializeFaceTracking();
                label1.Text = ActualFeature[0].Length.ToString();

                //Updating actual frames and features with the new ones
                frame = nextFrame;
                grayFrame = nextGrayFrame;
                faceGrayImage = faceNextGrayImage;
                faceImage = faceNextImage;
                ActualFeature[0] = NextFeature;
            }
        }

        void ComputeDenseOpticalFlow()
        {
            faceGrayImage = grayFrame;
            faceNextGrayImage = nextGrayFrame;
            // Compute dense optical flow using Horn and Schunk algo
            velx = new Image<Gray, float>(faceGrayImage.Size);
            vely = new Image<Gray, float>(faceNextGrayImage.Size);

            OpticalFlow.HS(faceGrayImage, faceNextGrayImage, true, velx, vely, 0.1d, new MCvTermCriteria(100));

            #region Dense Optical Flow Drawing
            Size winSize = new Size(10, 10);
            vectorFieldX = (int)Math.Round((double)faceGrayImage.Width / winSize.Width);
            vectorFieldY = (int)Math.Round((double)faceGrayImage.Height / winSize.Height);
            sumVectorFieldX = 0f;
            sumVectorFieldY = 0f;
            vectorField = new PointF[vectorFieldX][];
            for (int i = 0; i < vectorFieldX; i++)
            {
                vectorField[i] = new PointF[vectorFieldY];
                for (int j = 0; j < vectorFieldY; j++)
                {
                    Gray velx_gray = velx[j * winSize.Width, i * winSize.Width];
                    float velx_float = (float)velx_gray.Intensity;
                    Gray vely_gray = vely[j * winSize.Height, i * winSize.Height];
                    float vely_float = (float)vely_gray.Intensity;
                    sumVectorFieldX += velx_float;
                    sumVectorFieldY += vely_float;
                    vectorField[i][j] = new PointF(velx_float, vely_float);

                    Cross2DF cr = new Cross2DF(
                        new PointF((i * winSize.Width) + trackingArea.X,
                                   (j * winSize.Height) + trackingArea.Y),
                                   1, 1);
                    opticalFlowFrame.Draw(cr, new Bgr(Color.Red), 1);

                    LineSegment2D ci = new LineSegment2D(
                        new Point((i * winSize.Width) + trackingArea.X,
                                  (j * winSize.Height) + trackingArea.Y),
                        new Point((int)((i * winSize.Width) + trackingArea.X + velx_float),
                                  (int)((j * winSize.Height) + trackingArea.Y + vely_float)));
                    opticalFlowFrame.Draw(ci, new Bgr(Color.Yellow), 1);

                }
            }
            #endregion
        }

        private void ComputeMotionFromDenseOpticalFlow()
        {
            // To be implemented
        }

        private void ComputeSparseOpticalFlow()
        {
            float xdirection = 0;
            float ydirection = 0;
            // Compute optical flow using pyramidal Lukas Kanade Method                
            OpticalFlow.PyrLK(grayFrame, nextGrayFrame,ActualFeature[0], new System.Drawing.Size(10, 10), 3, new MCvTermCriteria(20, 0.03d), out NextFeature, out Status, out TrackError);
            //detect faces from the gray-scale image and store into an array of type 'var',i.e '
            MCvAvgComp[] hands = nextGrayFrame.DetectHaarCascade(haar, 1.4, 4, HAAR_DETECTION_TYPE.DO_CANNY_PRUNING, new Size(25, 25))[0];

            //using (MemStorage storage = new MemStorage())
            //    nextHull = PointCollection.ConvexHull(ActualFeature[0], storage, Emgu.CV.CvEnum.ORIENTATION.CV_CLOCKWISE).ToArray();
            //nextCentroid = FindCentroid(nextHull);
            for (int i = 0; i < NextFeature.Length; i++)
            {
                if (Status[i]==1)
                {
                    xdirection +=NextFeature[i].X-ActualFeature[0][i].X;
                    ydirection += NextFeature[i].Y - ActualFeature[0][i].Y;

                }
                DrawTrackedFeatures(i);
                //Uncomment this to draw optical flow vectors
                DrawFlowVectors(i);
            }
            foreach (var hand in hands)
                {
                    opticalFlowFrame.Draw(hand.rect, new Bgr(Color.Green), 3);
                }
            //if (hands.Length >= 1)
            //     gestureChanged =!gestureChanged;
            //if (gestureChanged)
            //{
            //    if (xdirection > 10)
            //    {
            //        controlModel("zoom", "+{RIGHT}");
            //    }
            //    else if (xdirection < -10)
            //    {
            //        controlModel("zoom", "+{LEFT}");
            //    }
            //}
            //else
            //{

            label4.Text = "xdirection" + xdirection + "ydirection" + ydirection;
            if (xdirection > 50 && ydirection > 50)
                controlModel("zoom", "{RIGHT}");
            else if (xdirection < 50 && ydirection < 50)
                controlModel("zoom", "{LEFT}");
            else if (xdirection > 50 && ydirection < 50)
                controlModel("zoom", "{DOWN}");
            else if (xdirection < 50 && ydirection > 50)
                controlModel("zoom", "{UP}");
              /*  if (xdirection > 20)
                {
                    controlModel("zoom", "{RIGHT}");
                }
                else if (xdirection < -20)
                {
                    controlModel("zoom", "{LEFT}");
                }
           // }
            if (ydirection > 20)
            {
                controlModel("zoom", "{DOWN}");
            }
            else if (ydirection < -20)
            {
                controlModel("zoom", "{UP}");
            }*/
        }
        // Get a handle to an application window.
        [DllImport("USER32.DLL", CharSet = CharSet.Unicode)]
        public static extern IntPtr FindWindow(string lpClassName,
            string lpWindowName);

        // Activate an application window.
        [DllImport("USER32.DLL")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);
        public void controlModel(String gesture, String commands)
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
        private void ComputeMotionFromSparseOpticalFlow()
        {
            float xCentroidsDifference = referenceCentroid.X - nextCentroid.X;
            float yCentroidsDifference = referenceCentroid.Y - nextCentroid.Y;

            float threshold = trackingArea.Width / 5;
            label4.Text = "center";
            if (Math.Abs(xCentroidsDifference) > Math.Abs(yCentroidsDifference))
            {
                if (xCentroidsDifference > threshold)
                    label4.Text = "right";
                if (xCentroidsDifference < -threshold)
                    label4.Text = "left";
            }
            if (Math.Abs(xCentroidsDifference) < Math.Abs(yCentroidsDifference))
            {
                if (yCentroidsDifference > threshold)
                    label4.Text = "up";
                if (yCentroidsDifference < -threshold)
                    label4.Text = "down";
            }
        }

        //Code adapted and improved from: http://blog.csharphelper.com/2010/01/04/find-a-polygons-centroid-in-c.aspx
        // refer to wikipedia for math formulas centroid of polygon http://en.wikipedia.org/wiki/Centroid        
        private PointF FindCentroid(PointF[] Hull)
        {
            // Add the first point at the end of the array.
            int num_points = Hull.Length;
            PointF[] pts = new PointF[num_points + 1];
            Hull.CopyTo(pts, 0);
            pts[num_points] = Hull[0];

            // Find the centroid.
            float X = 0;
            float Y = 0;
            float second_factor;
            for (int i = 0; i < num_points; i++)
            {
                second_factor = pts[i].X * pts[i + 1].Y - pts[i + 1].X * pts[i].Y;
                X += (pts[i].X + pts[i + 1].X) * second_factor;
                Y += (pts[i].Y + pts[i + 1].Y) * second_factor;
            }
            // Divide by 6 times the polygon's area.
            float polygon_area = Math.Abs(SignedPolygonArea(Hull));
            X /= (6 * polygon_area);
            Y /= (6 * polygon_area);

            // If the values are negative, the polygon is
            // oriented counterclockwise so reverse the signs.
            if (X < 0)
            {
                X = -X;
                Y = -Y;
            }
            return new PointF(X, Y);
        }

        private float SignedPolygonArea(PointF[] Hull)
        {
            int num_points = Hull.Length;
            // Get the areas.
            float area = 0;
            for (int i = 0; i < num_points; i++)
            {
                area +=
                    (Hull[(i + 1) % num_points].X - Hull[i].X) *
                    (Hull[(i + 1) % num_points].Y + Hull[i].Y) / 2;
            }
            // Return the result.
            return area;
        }

        private void DrawTrackedFeatures(int i)
        {
            opticalFlowFrame.Draw(new CircleF(new PointF(ActualFeature[0][i].X, ActualFeature[0][i].Y), 1f), new Bgr(Color.Blue), 1);
        }

        private void DrawFlowVectors(int i)
        {
            System.Drawing.Point p = new Point();
            System.Drawing.Point q = new Point();

            p.X = (int)ActualFeature[0][i].X;
            p.Y = (int)ActualFeature[0][i].Y;
            q.X = (int)NextFeature[i].X;
            q.Y = (int)NextFeature[i].Y;

            double angle;
            angle = Math.Atan2((double)p.Y - q.Y, (double)p.X - q.X);

            LineSegment2D line = new LineSegment2D(p, q);
            opticalFlowFrame.Draw(line, new Bgr(255, 0, 0), 1);

            p.X = (int)(q.X + 6 * Math.Cos(angle + Math.PI / 4));
            p.Y = (int)(q.Y + 6 * Math.Sin(angle + Math.PI / 4));
            opticalFlowFrame.Draw(new LineSegment2D(p, q), new Bgr(255, 0, 0), 1);
            p.X = (int)(q.X + 6 * Math.Cos(angle - Math.PI / 4));
            p.Y = (int)(q.Y + 6 * Math.Sin(angle - Math.PI / 4));
            opticalFlowFrame.Draw(new LineSegment2D(p, q), new Bgr(255, 0, 0), 1);
        }

        private void HeadTracking_Load(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            InitializeFaceTracking();
        }

        



    }
}