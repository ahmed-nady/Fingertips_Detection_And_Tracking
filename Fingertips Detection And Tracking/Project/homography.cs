//using System;
//using System.Collections.Generic;
//using System.ComponentModel;
//using System.Data;
//using System.Drawing;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using System.Windows.Forms;
//using Emgu.CV;
//using Emgu.CV.Structure;
 
//using Emgu.CV.Features2D;
//using Emgu.CV.CvEnum;


//namespace FingerTipsDetectionTracking.Project
//{
//    public partial class homography : Form
//    {
//        Capture leftCamera;
//        Capture rightCamera;
//        bool startCapture = false;
//        bool control = false;
         
//        #region Current mode variables
//        public enum Mode
//        {
//            CornerFinding,
//            HomographyComputing,
//            Completed,
//            Test
//        }
//        Mode currentMode = Mode.CornerFinding;
//        #endregion

//        PointF[] leftPoints = null;
//        PointF[] rightPoints = null;

//        Rectangle leftRect;
//        Rectangle rightRect;

//        HomographyMatrix leftMatrix;
//        HomographyMatrix rightMatrix;

//        public homography()
//        {
//            InitializeComponent();
//            leftCamera = new Capture(0);
//            rightCamera = new Capture(1);
//        }
//        // Conver list of AForge.NET's points to array of .NET points
//        private Point[] ToPointsArray(List<AForge.IntPoint> points)
//        {
//            Point[] array = new Point[points.Count];

//            for (int i = 0, n = points.Count; i < n; i++)
//            {
//                array[i] = new Point(points[i].X, points[i].Y);
//            }

//            return array;
//        }


//        void processImage(object sender, EventArgs e)
//        {
//            Image<Bgr, byte> leftImg = leftCamera.RetrieveBgrFrame();
//            Image<Bgr, byte> rightImg = rightCamera.RetrieveBgrFrame();
//            Image<Bgr, byte> refrenceImage = new Image<Bgr, byte>(@"G:\Documents\Youcam\referenceImage.jpg");
//             Point[] leftCorner = null;
//             Point[] rightCorner = null;
//             Point[] refrenceCorner = null;
            

//             #region Find Corner
//             if (currentMode == Mode.CornerFinding)
//             {
//                 refrenceCorner = detectCorner(refrenceImage);
//                 leftCorner = detectCorner(leftImg);
//                 rightCorner = detectCorner(rightImg);

//                 if (leftCorner != null && rightCorner != null && refrenceCorner != null)
//                 {
//                     MessageBox.Show("Corners in Both Image Detected");
//                     currentMode = Mode.HomographyComputing;
                      
//                 }
//             }
//             #endregion

//             if (currentMode == Mode.HomographyComputing)
//             {
//                leftRect= computeHomography(refrenceCorner, leftCorner, leftImg, refrenceImage);
//                rightRect= computeHomography(refrenceCorner, rightCorner, rightImg, refrenceImage);
//              //   MessageBox.Show("Homography");
//                if (leftRect != null && rightRect != null)
//                {
 
                     
//                    currentMode = Mode.Completed;
//                }
//             }
//             if (currentMode == Mode.Completed)
//             {

//                 leftImg = leftImg.GetSubRect(leftRect);
//                 rightImg = rightImg.GetSubRect(rightRect);
//                 //leftImg.ROI = new Rectangle((int)leftPoints[0].X, (int)leftPoints[0].Y, (int)(leftPoints[1].Y - leftPoints[0].Y),(int) (leftPoints[2].X - leftPoints[1].X));
//                 //rightImg.ROI = new Rectangle((int)rightPoints[0].X, (int)rightPoints[0].Y, (int)(rightPoints[1].Y - rightPoints[0].Y), (int)(rightPoints[2].X - rightPoints[1].X)); ;
//               //  MCvScalar s = new MCvScalar(0, 0, 0);
//                // CvInvoke.cvWarpPerspective(refrenceImage, leftImg, leftMatrix, (int)Emgu.CV.CvEnum.INTER.CV_INTER_NN,s);
//              //   CvInvoke.cvWarpPerspective(refrenceImage, rightImg, rightMatrix, (int)Emgu.CV.CvEnum.INTER.CV_INTER_NN, s); 
//               //  leftImg = leftImg.WarpPerspective(leftMatrix, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC, Emgu.CV.CvEnum.WARP.CV_WARP_INVERSE_MAP, new Bgr(0, 0, 0));
//              //   rightImg = rightImg.WarpPerspective(rightMatrix, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC, Emgu.CV.CvEnum.WARP.CV_WARP_INVERSE_MAP, new Bgr(0, 0, 0));
//              //   leftImg.DrawPolyline(Array.ConvertAll<PointF, Point>(leftPoints, Point.Round), true, new Bgr(255, 0, 0), 5);
//                // rightImg.DrawPolyline(Array.ConvertAll<PointF, Point>(rightPoints, Point.Round), true, new Bgr(255, 0, 0), 5);

//             }
//            if(currentMode ==Mode.Test)
//            {
//                //get the click position
//                Point seedPoint = imageBox1.PointToClient(Cursor.Position);

//                //the color used to fill the region
//                Bgr fillColor = new Bgr(0, 0, 255);

//                Image<Bgr, byte> fillMask = new Image<Bgr, byte>(leftImg.Width + 2, leftImg.Height + 2);
//                MCvConnectedComp comp = new MCvConnectedComp();

//                CvInvoke.cvFloodFill(leftImg.Ptr,
//                seedPoint,
//                new MCvScalar(0, 0, 255),
//                new MCvScalar(10.0, 10.0, 10.0),
//                new MCvScalar(10.0, 10.0, 10.0), out comp,
//                Emgu.CV.CvEnum.CONNECTIVITY.EIGHT_CONNECTED,
//                Emgu.CV.CvEnum.FLOODFILL_FLAG.DEFAULT, fillMask);

//            }





//            imageBox1.Image = leftImg;
//            imageBox2.Image = rightImg;


//        }

//        //HomographyMatrix computeHomography_SURF(Image<Bgr, byte> observedImage)
//        //{
//        //     SURFDetector surfParam = new SURFDetector(500, false);
           
//        //// Image<Gray, Byte> modelImage = new Image<Gray, byte>("box.png");
//        // //extract features from the object image
//        //    ImageFeature<float>[] modelFeatures = surfParam.DetectFeatures(refrenceImage.Convert<Gray, byte>(), null);

//        // //Create a Feature Tracker
//        // Features2DTracker<float> tracker = new Features2DTracker<float>(modelFeatures);
                          
//        // // extract features from the observed image
//        // ImageFeature<float>[] imageFeatures = surfParam.DetectFeatures(observedImage.Convert<Gray, byte>(), null);

//        // Features2DTracker<float>.MatchedImageFeature[] matchedFeatures = tracker.MatchFeature(imageFeatures, 2);
//        // matchedFeatures = Features2DTracker<float>.VoteForUniqueness(matchedFeatures, 0.8);
//        // matchedFeatures = Features2DTracker<float>.VoteForSizeAndOrientation(matchedFeatures, 1.5, 20);
//        // HomographyMatrix homography = Features2DTracker<float>.GetHomographyMatrixFromMatchedFeatures(matchedFeatures);
         
 
         
//        // #region draw the project region on the image
//        // if (homography != null)
//        // {  //draw a rectangle along the projected model
//        //    Rectangle rect = refrenceImage.ROI;
//        //    PointF[] pts = new PointF[] { 
//        //       new PointF(rect.Left, rect.Bottom),
//        //       new PointF(rect.Right, rect.Bottom),
//        //       new PointF(rect.Right, rect.Top),
//        //       new PointF(rect.Left, rect.Top)};
//        //    homography.ProjectPoints(pts);

//        //    for (int i = 0; i < pts.Length; i++)
//        //       // pts[i].Y += modelImage.Height;
//        //        pts[i].X += refrenceImage.Width;

//        //    observedImage.DrawPolyline(Array.ConvertAll<PointF, Point>(pts, Point.Round), true, new Bgr(255, 20, 100), 5);
//        //  }
//        // #endregion

//        // return homography;
//        //}
//        private Rectangle computeHomography(Point[] srcPoints, Point[] imgPoints, Image<Bgr, byte> img, Image<Bgr, byte> refrenceImage)
//        {
//            PointF[] pts1 = new PointF[4];
//            PointF[] pts2 = new PointF[4];
//            PointF[] pts = new PointF[4];
            
//            for (int i = 0; i < 4; i++)
//            {
//                pts1[i] = new PointF(srcPoints[i].X, srcPoints[i].Y);
//                pts2[i] = new PointF(imgPoints[i].X, imgPoints[i].Y);
//            }
       
//            // HomographyMatrix derives from Matrix<double>
//                    HomographyMatrix homography = CameraCalibration.FindHomography(
//                         pts1, // points on the img1?
//                         pts2, // points on the img2?
//                         HOMOGRAPHY_METHOD.RANSAC,
//                         3);
//            Rectangle rect = new Rectangle();
//            #region draw the project region on the image
//                    if (homography != null)
//            {  //draw a rectangle along the projected model
//                     rect = refrenceImage.ROI;
//                 pts = new PointF[] { 
//                   new PointF(rect.Left, rect.Bottom),
//                   new PointF(rect.Right, rect.Bottom),
//                   new PointF(rect.Right, rect.Top),
//                   new PointF(rect.Left, rect.Top)};
//                homography.ProjectPoints(pts);
                        
             

//           // HomographyMatrix homography = CameraCalibration.GetPerspectiveTransform(pts1, pts2);
          

//              ////  refrenceImage.WarpPerspective(homography, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC, Emgu.CV.CvEnum.WARP.CV_WARP_INVERSE_MAP, new Bgr(Color.White));
//              //          MCvScalar s = new MCvScalar(0, 0, 0);
//              // CvInvoke.cvWarpPerspective(img,refrenceImage,homography,(int)Emgu.CV.CvEnum.INTER.CV_INTER_NN, s);              
//            }
//            #endregion
//            return rect;
//        }

//        float[,] convert (Point[] points)
//        {
//            float[,] fPoints = new float[4,2];
//            int c = 0;
//            for (int i = 0; i < points.Length; i++)
//            {
//                fPoints[i,c] = points[i].X;
//                fPoints[i,++c] = points[i].Y;
//                c=0;
//            }
//            return fPoints;
//        }
//        private Point[] detectCorner(Image<Bgr,byte> img)
//        {
//            Point[] pts=null;
//            Image<Gray, Byte> cannyEdges = img.Canny(128, 120);
             
//            using (MemStorage storage = new MemStorage())
//            {//allocate storage for contour approximation
//                for (Contour<Point> contours = cannyEdges.FindContours(); contours != null; contours = contours.HNext)
//                {
//                    Contour<Point> currentContour = contours.ApproxPoly(contours.Perimeter * 0.05, storage);

//                    if (contours.Area > 250) //only consider contours with area greater than 250
//                    {
//                        if (currentContour.Total == 4) //The contour has 3 vertices, it is a triangle
//                        {
//                            pts = currentContour.ToArray();
//                            img.Draw(currentContour.GetMinAreaRect().MinAreaRect(), new Bgr(255, 0, 0), 5);

//                        }
//                    }
//                }
//            }
//            return pts;
//        }
//        private void button1_Click(object sender, EventArgs e)
//        {
//            if (!startCapture)
//            {
//                // FingerTipsTrackingBox.Refresh();
//                button1.Text = "Stop";
//               Application.Idle += processImage;
//            }
//            else
//            {

//                button1.Text = "Resume";
//                Application.Idle -= processImage;
//            }
//            startCapture = !startCapture;
//        }
//    }
//}
