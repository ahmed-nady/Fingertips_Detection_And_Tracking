using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.UI;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Features2D;

namespace FingerTipsDetectionTracking.Project
{
    public partial class Surf : Form
    {
        Capture camera;
        Image<Rgb, byte> modelImage = null;
        Image<Rgb, byte> observedImage = null;

        ImageFeature<float>[] modelFeatures;
        Features2DTracker<float> tracker;
        SURFDetector surfParam;

        bool capture = false;
        public Surf()
        {
            InitializeComponent();
            camera = new Capture(0);
            surfParam = new SURFDetector(500, false);
           modelImage= new Image<Rgb, byte>(@"C:\Users\ahmed nady\Desktop\hand.jpg");
            //extract features from the object image
                modelFeatures = surfParam.DetectFeatures(modelImage.Convert<Gray,byte>(), null);

                //Create a Feature Tracker
                tracker = new Features2DTracker<float>(modelFeatures);
        }

        void ProcessFrame(object sender, EventArgs arg)
        {
              observedImage = camera.QueryFrame().Convert<Rgb,byte>();
          
                  
                 // extract features from the observed image
                 ImageFeature<float>[] imageFeatures = surfParam.DetectFeatures(observedImage.Convert<Gray, byte>(), null);

                 Features2DTracker<float>.MatchedImageFeature[] matchedFeatures = tracker.MatchFeature(imageFeatures, 2);
                 matchedFeatures = Features2DTracker<float>.VoteForUniqueness(matchedFeatures, 0.8);
                 matchedFeatures = Features2DTracker<float>.VoteForSizeAndOrientation(matchedFeatures, 1.5, 20);
                 HomographyMatrix homography = Features2DTracker<float>.GetHomographyMatrixFromMatchedFeatures(matchedFeatures);


                 Image<Rgb, Byte> res = modelImage.ConcateHorizontal(observedImage);

         
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
                 imageBox2.Image = res;
                     imageBox1.Image=observedImage;
      

                }
         
        private void button1_Click(object sender, EventArgs e)
        {
            if (capture)
            {
                button1.Text = "start";
                //extract features from the object image
                modelFeatures = surfParam.DetectFeatures(modelImage.Convert<Gray, byte>(), null);

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
    }
}
