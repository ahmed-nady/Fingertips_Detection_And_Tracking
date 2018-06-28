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
using Emgu.CV.UI;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Features2D;
using Emgu.CV.GPU;
  

namespace FingerTipsDetectionTracking
{
    public partial class Homography : Form
    {

        private Point RectStartPoint;
        private Rectangle Rect = new Rectangle();
        private Rectangle RealImageRect = new Rectangle();
        private Brush selectionBrush = new SolidBrush(Color.FromArgb(128, 64, 64, 64));
        private int thickness = 3;
        Image<Bgr, byte> imgEntrada;

         Capture camera ;
         Image<Rgb, byte> modelImage=null;
         Image<Rgb, byte> observedImage=null;

         ImageFeature<float>[] modelFeatures;
         Features2DTracker<float> tracker;
         SURFDetector surfParam ;

         bool capture = false;
         public Homography()
        {
            InitializeComponent();
            camera = new Capture(@"F:\Working\Final phase\DataSet\20150409_13-34-33.asf");
            surfParam = new SURFDetector(500, false);
            modelImage = new Image<Rgb, byte>(@"C:\Users\ahmed nady\Pictures\modelImage.bmp");
            modelFeatures = surfParam.DetectFeatures(modelImage.Convert<Gray, byte>(), null);

            //Create a Feature Tracker
            tracker = new Features2DTracker<float>(modelFeatures);
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
        private void ProcessFrame(object sender, EventArgs arg)
        {
            modelImage = camera.QueryFrame().Convert<Rgb, byte>();  //line 1

            //detect the black color and it's shape -cirle and track it

            //ColorFiltering filter = new ColorFiltering(); // define a color filter
            //// Define the range of RGB to retain within a processed image
            //filter.Red = new IntRange(0, 75);
            //filter.Green = new IntRange(0, 75);
            //filter.Blue = new IntRange(0, 75);
            //filter.ApplyInPlace(modelImage.Bitmap);

            //CircleF[] circles = modelImage.Convert<Gray,byte>().HoughCircles(new Gray(180), new Gray(120), 1.5, 100, 0, 0)[0];
             

            //foreach (CircleF circle in circles)
            //{
            //    modelImage.Draw(circle, new Rgb(Color.Red), 8);
                
            //}

            imageBox1.Image = modelImage;        //line 2
        }
        private void button1_Click(object sender, EventArgs e)
        {
            if (capture)
            {
                button1.Text = "start";
                //extract features from the object image
                modelFeatures = surfParam.DetectFeatures(modelImage.Convert<Gray,byte>(), null);

                //Create a Feature Tracker
                tracker = new Features2DTracker<float>(modelFeatures);

                Application.Idle -= ProcessFrame;
            }
            else
            {
                button1.Text = "pause";

                Application.Idle += ProcessFrame;
            }
            capture = !capture;
             
             
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Application.Idle += new EventHandler(delegate(object sender1, EventArgs e1)
                {
                    observedImage = camera.QueryFrame().Convert<Rgb,byte>();
          
               //  Stopwatch watch = Stopwatch.StartNew();
                 // extract features from the observed image
                 ImageFeature<float>[] imageFeatures = surfParam.DetectFeatures(observedImage.Convert<Gray, byte>(), null);

                 Features2DTracker<float>.MatchedImageFeature[] matchedFeatures = tracker.MatchFeature(imageFeatures, 2);
                 matchedFeatures = Features2DTracker<float>.VoteForUniqueness(matchedFeatures, 0.8);
                 matchedFeatures = Features2DTracker<float>.VoteForSizeAndOrientation(matchedFeatures, 1.5, 20);
                 HomographyMatrix homography = Features2DTracker<float>.GetHomographyMatrixFromMatchedFeatures(matchedFeatures);
                // watch.Stop();

                 //Merge the object image and the observed image into one image for display
              //**** Image<Rgb, Byte> res = modelImage.ConcateVertical(observedImage);
               

                Image<Rgb, Byte> res = modelImage.ConcateHorizontal(observedImage);


                 //#region draw lines between the matched features
                 //foreach (Features2DTracker<float>.MatchedImageFeature matchedFeature in matchedFeatures)
                 //{
                 //    PointF p = matchedFeature.ObservedFeature.KeyPoint.Point;
                 //    p.Y += modelImage.Height;
                 //    res.Draw(new LineSegment2DF(matchedFeature.SimilarFeatures[0].Feature.KeyPoint.Point, p), new Rgb(0,255,0), 1);

                 //}
                 //#endregion

         
                 #region draw the project region on the image
                 if (homography != null)
                 {  //draw a rectangle along the projected model
                    Rectangle rect = modelImage.ROI;
                    PointF[] pts = new PointF[] { 
                       new PointF(rect.Left, rect.Bottom),
                       new PointF(rect.Right, rect.Bottom),
                       new PointF(rect.Right, rect.Top),
                       new PointF(rect.Left, rect.Top)};
                    homography.ProjectPoints(pts);

                    //for (int i = 0; i < pts.Length; i++)
                    //   pts[i].Y += modelImage.Height;

                    observedImage.DrawPolyline(Array.ConvertAll<PointF, Point>(pts, Point.Round), true, new Rgb(255,20,100), 5);
                 }
                 #endregion
                    
                     imageBox1.Image=observedImage;
      

                });

        }

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            // Determine the initial rectangle coordinates...
            RectStartPoint = e.Location;
            Invalidate();
        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            #region SETS COORDINATES AT INPUT IMAGE BOX
            int X0, Y0;
            ConvertCoordinates(imageBox1, out X0, out Y0, e.X, e.Y);
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
             ConvertCoordinates(imageBox1, out X0, out Y0,
            RectStartPoint.X, RectStartPoint.Y);
            int X1, Y1;
            ConvertCoordinates(imageBox1, out X1, out Y1, tempEndPoint.X, tempEndPoint.Y);
            RealImageRect.Location = new Point(
                Math.Min(X0, X1),
                Math.Min(Y0, Y1));
            RealImageRect.Size = new Size(
                Math.Abs(X0 - X1),
                Math.Abs(Y0 - Y1));

            imgEntrada = new Image<Bgr, byte>(@"C:\Users\ahmed nady\Pictures\modelImage.bmp");
            imgEntrada.Draw(RealImageRect, new Bgr(Color.Red), thickness);
            imageBox1.Image = imgEntrada;
            #endregion

            ((PictureBox)sender).Invalidate();
        }

        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            // Draw the rectangle...
            if (imageBox1.Image != null)
            {
                if (Rect != null && Rect.Width > 0 && Rect.Height > 0)
                {
                    //Seleciona a ROI
                    e.Graphics.SetClip(Rect, System.Drawing.Drawing2D.CombineMode.Exclude);
                    e.Graphics.FillRectangle(selectionBrush, new Rectangle
            (0, 0, ((PictureBox)sender).Width, ((PictureBox)sender).Height));
                    //e.Graphics.FillRectangle(selectionBrush, Rect);
                }
            }
        }

        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            //Define ROI. Valida altura e largura para evitar index range exception.
            if (RealImageRect.Width > 0 && RealImageRect.Height > 0)
            {
                imgEntrada.ROI = RealImageRect;
                imageBox2.Image = imgEntrada;
            }
        }

        private void Homography_Load(object sender, EventArgs e)
        {
            imgEntrada = new Image<Bgr, byte>(@"C:\Users\ahmed nady\Pictures\modelImage.bmp");
            imageBox2.Image = imageBox1.Image = imgEntrada;
        }

        
        
    }
}
