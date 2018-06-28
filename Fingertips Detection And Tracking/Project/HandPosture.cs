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
    public partial class HandPosture : Form
    {
        bool startCapture = false;
        private Emgu.CV.Image<Emgu.CV.Structure.Gray, byte> _binary_template, _binary_template1, _binary_template2, _binary_template3, _binary_template_store;
        Image<Bgr, byte> testImg;
        Image<Gray, byte> template, template1, template2, template3, combiningGesture;
        Hsv hsv_min;
        Hsv hsv_max;
        Ycc YCrCb_min;
        Ycc YCrCb_max;
        IColorSkinDetector skinDetector;
        
        Capture camera;

        int rotation = 10;
        int leftRotation = 350;
        float size = 1;
        float D_size = .5f;
        public HandPosture()
        {
            InitializeComponent();
            skinDetector = new YCrCbSkinDetector();
            camera = new Capture();//@"D:\Working\XNA\STREAM\00005_hand.MTS");
           
            hsv_min = new Hsv(0, 45, 0);
            hsv_max = new Hsv(20, 255, 255);
            YCrCb_min = new Ycc(0, 131, 80);
            YCrCb_max = new Ycc(255, 185, 135);
            template = new Image<Gray, byte>(@"C:\Users\ahmed nady\Desktop\hand1.jpg");
            ////_binary_template = skinDetector.DetectSkin(template, YCrCb_min, YCrCb_max);

            template1 = new Image<Gray, byte>(@"C:\Users\ahmed nady\Desktop\zoomOut.jpg");
           // _binary_template1 = skinDetector.DetectSkin(template1, YCrCb_min, YCrCb_max);

            template2 = new Image<Gray, byte>(@"C:\Users\ahmed nady\Desktop\rotateLeft.jpg");
           // _binary_template2 = skinDetector.DetectSkin(template2, YCrCb_min, YCrCb_max);

            template3 = new Image<Gray, byte>(@"C:\Users\ahmed nady\Desktop\rotateRight.jpg");
           // _binary_template3 = skinDetector.DetectSkin(template3, YCrCb_min, YCrCb_max);

            testImg = new Image<Bgr, byte>(@"C:\Users\Public\Pictures\Sample Pictures\Tulips.jpg");

            combiningGesture = (template.ConcateHorizontal(template1)).ConcateHorizontal(template2.ConcateHorizontal(template3));
            
        }

         
        /**
 * Function to perform fast template matching with image pyramid
 */
 Image<Gray, float> fastMatchTemplate(Image<Gray, byte> srca,  // The reference image
                       Image<Gray,byte> srcb,  // The template image
                       int maxlevel)   // Number of levels
{
    Image<Gray, byte>[] refs, tpls;
    Image<Gray, float>[] results = new Image<Gray, float>[maxlevel+1];
     
    // Build Gaussian pyramid
    refs=srca.BuildPyramid(maxlevel);
    tpls= srcb.BuildPyramid(maxlevel);

    Image<Gray,byte> ref1, tpl,ref11,ref111;
    Image<Gray,float>res=null;
    int level;
    // Process each level
    for (level = maxlevel; level >= 0; level--)
    {
        ref1 = refs[level].Clone();
        tpl = tpls[level].Clone();
      

        if (level == maxlevel)
        {
            // On the smallest level, just perform regular template matching
           res= ref1.MatchTemplate(tpl, Emgu.CV.CvEnum.TM_TYPE.CV_TM_CCOEFF_NORMED);
              
        }
        else
        {
            // On the next layers, template matching is performed on pre-defined 
            // ROI areas.  We define the ROI using the template matching result 
            // from the previous layer.

            Image<Gray, byte> mask = results[maxlevel-level-1].Convert<Gray, byte>().PyrUp();
            // Find matches from previous layer
            // Use the contours to define region of interest and 
            // perform template matching on the areas
            for ( Contour<Point> contours =mask.FindContours(Emgu.CV.CvEnum.CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_NONE,Emgu.CV.CvEnum.RETR_TYPE.CV_RETR_EXTERNAL); contours != null; contours = contours.HNext)
            {
                 ref1.ROI= contours.BoundingRectangle;
                 int h = ref1.ROI.Height, w = ref1.ROI.Width;

                 if (ref1.ROI.Width < tpl.Width)
                     w = tpl.Width;
                 if (ref1.ROI.Height < tpl.Height)
                     h = tpl.Height;
                 
                 ref11= ref1.Copy(new Rectangle(ref1.ROI.X,ref1.ROI.Y,w,h));
               
                res= ref11.MatchTemplate(tpl, Emgu.CV.CvEnum.TM_TYPE.CV_TM_CCOEFF_NORMED);
            }
        }

        // Only keep good matches
        res.ThresholdToZero(new Gray(.94));

        results[maxlevel-level] = res.Clone();
    }

    return res;
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

        void processFrame(object sender, EventArgs e)
        {
            Image<Bgr, byte> skin = camera.QueryFrame();//.Resize(300, 300, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);//line 1
            imageBox1.Image = skin;
            if (skin == null)
                return;

            skin._SmoothGaussian(3);

            //  skin._EqualizeHist();
            //
           Image<Gray, Byte> BinaryHandImage = skinDetector.DetectSkin(skin, YCrCb_min, YCrCb_max);
            skin._Erode(3);
            skin._Dilate(3);
           imageBox2.Image = combiningGesture;

            imageBox3.Image = BinaryHandImage;


            //loop through list of image 
            for (int i = 0; i < 4; i++)
            {

                switch (i)
                {
                    case 0:
                        _binary_template = template.Clone();
                        break;
                    case 1:
                        _binary_template = template1.Clone();
                        break;
                    case 2:
                        _binary_template = template2.Clone();
                        break;
                    case 3:
                        _binary_template = template3.Clone();
                        break;
                }

                using (Image<Gray, float> result = /*fastMatchTemplate(BinaryHandImage, template1, 2))*/BinaryHandImage.MatchTemplate(_binary_template, Emgu.CV.CvEnum.TM_TYPE.CV_TM_CCOEFF_NORMED))
                {
                    
                     
                    double[] minValues, maxValues;
                    Point[] minLocations, maxLocations;
                    result.MinMax(out minValues, out maxValues, out minLocations, out maxLocations);

                    // You can try different values of the threshold. I guess somewhere between 0.75 and 0.95 would be good.
                    if (maxValues[0] > .8)
                    {
                        // This is a match. Do something with it, for example draw a rectangle around it.
                        Rectangle match = new Rectangle(maxLocations[0], (_binary_template).Size);
                        skin.Draw(match, new Bgr(Color.Red), 3);

                      //  check i and perform gesture
                        switch(i)
                        {
                            case 0:
                                size = (size + .03f) % 3;
                               imageBox4.Image = testImg.Resize(size, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);
                               
                               break;
                            case 1:
                               size = (size/.03f)%3;
                                imageBox4.Image =testImg.Resize(size, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);
                                break;
                            case 2:
                               imageBox4.Image = testImg.Rotate(rotation,new Bgr(0,0,0));
                               rotation = (rotation+10)%360;
                                break;
                            case 3:
                               imageBox4.Image = testImg.Rotate(leftRotation,new Bgr(0,0,0));
                               leftRotation = (leftRotation - 10) % 360;
                                break;


                        }
                        break;

                    }
                }
            }
          

            //Contour<Point> handContour = null;
            //using (MemStorage m = new MemStorage())
            //{
            //    Contour<System.Drawing.Point> MaxContour = new Contour<Point>(m);

            //    Contour<System.Drawing.Point> Contours =
            //        BinaryHandImage.FindContours();//Emgu.CV.CvEnum.CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_SIMPLE, Emgu.CV.CvEnum.RETR_TYPE.CV_RETR_TREE);

            //    //Contours.OrderByDescending <Contour ,double)();

            //    handContour = ExtractBiggestContour(Contours);

            //    if (handContour != null)
            //    {
            //        //BinaryHandImage.ROI = handContour.BoundingRectangle;
            //       _warped= BinaryHandImage;//.Copy();
            //      //  Emgu.CV.Contour<System.Drawing.Point> c = handContour.ApproxPoly(handContour.Perimeter * 0.05, m);
            //      //  // Warp content of poly-line as if looking at it from the top
            //      //  System.Drawing.PointF[] warp_source = new System.Drawing.PointF[] { 
            //      //new System.Drawing.PointF(c[0].X, c[0].Y),
            //      //new System.Drawing.PointF(c[1].X, c[1].Y),
            //      //new System.Drawing.PointF(c[2].X, c[2].Y),
            //      //new System.Drawing.PointF(c[3].X, c[3].Y)};

            //      //  Emgu.CV.Matrix<float> warp_matrix = new Emgu.CV.Matrix<float>(3, 3);
            //      //  Emgu.CV.CvInvoke.cvGetPerspectiveTransform(warp_source, _warp_dest, warp_matrix);
            //      //  Emgu.CV.CvInvoke.cvWarpPerspective(
            //      //    BinaryHandImage, _warped, warp_matrix,
            //      //    (int)Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC + (int)Emgu.CV.CvEnum.WARP.CV_WARP_FILL_OUTLIERS,
            //      //    new Emgu.CV.Structure.MCvScalar(0)
            //      //  );

            //        float error;
            //        int orientation;
            //        TemplateMatch(out error, out orientation);

            //        if (error < .4f)
            //        {
            //            MessageBox.Show("Detect Hand with orientation "+orientation);
            //        }
            //    }
            //}
        }
        private void button1_Click(object sender, EventArgs e)
        {
            if (!startCapture)
            {
                // 
                button1.Text = "Stop";
                Application.Idle += processFrame;
            }
            else
            {

                 
                button1.Text = "Resume";
                Application.Idle -= processFrame;
            }
            startCapture = !startCapture;
        }
    }
}
