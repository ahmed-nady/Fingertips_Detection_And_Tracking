using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

using Emgu.CV;
using Emgu.CV.Structure;
using HandGestureRecognition.SkinDetector;
using Emgu.CV.VideoSurveillance;


namespace FingerTipsDetectionTracking
{
    class FingerTip
    {
        
        Hsv hsv_min;
        Hsv hsv_max;
        Ycc YCrCb_min;
        Ycc YCrCb_max;

        List<Point> tips;
       public FingerTip()
        {
          tips = new List<Point>();

            hsv_min = new Hsv(0, 45, 0);
            hsv_max = new Hsv(20, 255, 255);
            YCrCb_min = new Ycc(0, 131, 80);
            YCrCb_max = new Ycc(255, 185, 135);
        }
       public Contour<Point> ExtractBiggestContour(Contour<Point> contours)
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
       //filter fingertips obtained by convexity defects using k-curvature
       public List<Point> filtering_tips(Seq<MCvConvexityDefect> defects, Contour<Point> contour)
       {
           tips.Clear();
           List<Point> fingerTips = find_fingerTips(defects, contour.BoundingRectangle);
          // Seq<MCvConvexityDefect> defects = eleminateDefects(convexityDefects, contour.BoundingRectangle);
           Point p = new Point();
           Point q = new Point();
           Point r = new Point();
           int k = 16;


           for (int i = 0; i < fingerTips.Count; i++)
           {
               if (contour.Total < k)
                   break;

               p = fingerTips[i];
               if (i >= k)
                   q = contour.ElementAt(i - k);
               else
                   q = contour.ElementAt(contour.Total - 1 - i);
               if (i < contour.Total - k)
                   r = contour.ElementAt(i + k);
               else
                   r = contour.ElementAt(k - (contour.Total - i));



               double angle = getAngle(q, p, r);

               if (angle < 40)
               {

                   int cross = ((q.X - p.X) * (r.Y - p.Y) - (q.Y - p.Y) * (r.X - p.X));
                   if (cross > 0)
                   {
                       tips.Add(p);
                   }

               }
           }
           return tips;

       }
       public List<Point> filtering_tips_of_templateMatch(List<Point> fingerTips, Contour<Point> contour)
       {
           tips.Clear();
          
           Point p = new Point();
           Point q = new Point();
           Point r = new Point();
           int k = 16;


           for (int i = 0; i < fingerTips.Count; i++)
           {
               if (contour.Total < k)
                   break;

               p = fingerTips[i];
               if (i >= k)
                   q = contour.ElementAt(i - k);
               else
                   q = contour.ElementAt(contour.Total - 1 - i);
               if (i < contour.Total - k)
                   r = contour.ElementAt(i + k);
               else
                   r = contour.ElementAt(k - (contour.Total - i));



               double angle = getAngle(q, p, r);

               if (angle < 40)
               {

                   int cross = ((q.X - p.X) * (r.Y - p.Y) - (q.Y - p.Y) * (r.X - p.X));
                   if (cross > 0)
                   {
                       tips.Add(p);
                   }

               }
           }
           return tips;

       }
       
        public float distanceP2P(Point a, Point b)
        {
            float d = (float)Math.Sqrt(Math.Abs(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2)));
            return d;
        }
        public float distanceP2P(PointF a, PointF b)
        {
            float d = (float)Math.Sqrt(Math.Abs(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2)));
            return d;
        }
        public float getAngle(Point s, Point f, Point e)
        {
            float l1 = distanceP2P(f, s);
            float l2 = distanceP2P(f, e);
            float dot = (s.X - f.X) * (e.X - f.X) + (s.Y - f.Y) * (e.Y - f.Y);

            //  float angle = (float)Math.Acos(dot / (l1 * l2));
            float angle = (dot / (l1 * l2));
            angle = (float)angle * 180 / (float)Math.PI;
            return angle;
        }

        //K-curvature Method
        public List<Point> findFingerTips(Contour<Point> handContour)
        {
            tips.Clear();
           ////use hand contour
           // Point p = new Point();
           // Point q = new Point();
           // Point r = new Point();
           // int k = 16;

           // Contour<Point> MaxContour = handContour;
           // for (int i = 0; i < MaxContour.Total; i++)
           // {
           //     if (MaxContour.Total < k)
           //         break;
           //     //q,p,r points for defining vectors
           //     p = MaxContour.ElementAt(i);
           //     if (i >= k)
           //         q = MaxContour.ElementAt(i - k);
           //     else
           //         q = MaxContour.ElementAt(MaxContour.Total - 1 - i);
           //     if (i < MaxContour.Total - k)
           //         r = MaxContour.ElementAt(i + k);
           //     else
           //         r = MaxContour.ElementAt(k - (MaxContour.Total - i));



           //     double angle = getAngle(q, p, r);

           //     if (angle < 40 && angle > 15)
           //     {

           //         int cross = ((q.X - p.X) * (r.Y - p.Y) - (q.Y - p.Y) * (r.X - p.X));
           //         if (cross > 0)
           //         {
           //             tips.Add(p);
           //         }
           //         //int contourRadius = 15;
           //         //Seq<Point> hull = MaxContour.GetConvexHull(Emgu.CV.CvEnum.ORIENTATION.CV_CLOCKWISE);
           //         //for (int j = 0; j < hull.Total; j++)
           //         //{
           //         //    Point pointHull = hull.ElementAt(j);

           //         //    if (pointHull.Y - contourRadius <= p.Y && pointHull.Y + contourRadius >= p.Y && pointHull.X - contourRadius <= p.X && pointHull.X + contourRadius >= p.X)
           //         //    {
           //         //        j = hull.Total;
           //         //        tips.Add(p);
           //         //    }
           //         //}
           //     }
           // }
           //// return tipsCulstering(tips);
           // return tips;
            //using hull +k-curvature
         //   Seq<Point> hull = handContour.GetConvexHull(Emgu.CV.CvEnum.ORIENTATION.CV_CLOCKWISE);
            // Seq<Point> handContourApproximate = hull.ApproxPoly(hull.Perimeter * 0.001);
         //  //getting index of corner points of hull on contour
            //int[] index = new int[hull.Total]; 
            //for (int i = 0; i < hull.Total; i++)
            //{
            //    for(int j=0;j<handContour.Total;j++)
            //    {
            //        if (hull.ElementAt(i) == handContour.ElementAt(j))
            //            index[i] = j;
            //    }
            //}
           // Contour<Point> handContour = handContour1.ApproxPoly(handContour1.Perimeter * 0.01);
            Matrix<int> indeces = new Matrix<int>(handContour.Total, 1);
            CvInvoke.cvConvexHull2(handContour.Ptr, indeces.Ptr, Emgu.CV.CvEnum.ORIENTATION.CV_CLOCKWISE, -1);
            /* cluster indeces*/
            ////Matrix<int> indeces1 = new Matrix<int>(handContour.Total, 1);
            ////List<int> index = new List<int>();
            ////for (int i = 0; i < indeces.Rows; i++)
            ////{
            ////    if (indeces.Data[i + 1, 0] - indeces.Data[i, 0] <= 10)
            ////        index.Add(indeces.Data[i, 0]);
                    
            ////    else
            ////    {
            ////        index.Add(indeces.Data[i, 0]);
            ////        indeces1.Data[i, 0] =  index.ElementAt(index.Count/2);
            ////        index.Clear();
            ////    }

            ////}

            Point p = new Point();
            Point q = new Point();
            Point r = new Point();
            int k = 16;


            for (int i = 0; i < indeces.Rows; i++)
            {
                if (handContour.Total < k)
                    break;

                p = handContour.ElementAt(indeces.Data[i,0]);
                if (indeces.Data[i, 0] >= k)
                    q = handContour.ElementAt(indeces.Data[i, 0] - k);
                else
                    q = handContour.ElementAt(handContour.Total - 1 - indeces.Data[i, 0]);
                if (indeces.Data[i, 0] < handContour.Total - k)
                    r = handContour.ElementAt(indeces.Data[i, 0] + k);
                else
                    r = handContour.ElementAt(k - (handContour.Total - indeces.Data[i, 0]));



                double angle = getAngle(q, p, r);

                if (angle < 40 )
                {

                    int cross = ((q.X - p.X) * (r.Y - p.Y) - (q.Y - p.Y) * (r.X - p.X));
                    if (cross > 0)
                    {
                        tips.Add(p);
                    }

                }
            }
            //return tips;
            return Cluster(tips);

        }

        public List<Point> findFingerTipsUsingK_Curvature(Contour<Point> handContour)
        {
            tips.Clear();
            //use hand contour
             Point p = new Point();
             Point q = new Point();
             Point r = new Point();
             int k = 16;

             Contour<Point> MaxContour = handContour;
             for (int i = 0; i < MaxContour.Total; i++)
             {
                 if (MaxContour.Total < k)
                     break;
                 //q,p,r points for defining vectors
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

                     int cross = ((q.X - p.X) * (r.Y - p.Y) - (q.Y - p.Y) * (r.X - p.X));
                     if (cross > 0)
                     {
                         //   tips.Add(p);
                         // }
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
           //  return tipsCulstering(tips);
             return Cluster(tips);
             //return tips;
                    

        }
        public List<Point> findFingerTips_and_fittingEllipse(Contour<Point> handContour)
        {
            tips.Clear();
            //use hand contour
            Point p = new Point();
            Point q = new Point();
            Point r = new Point();
            int k = 16;

            Contour<Point> MaxContour = handContour;
            for (int i = 0; i < MaxContour.Total; i++)
            {
                if (MaxContour.Total < k)
                    break;
                //q,p,r points for defining vectors
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

                    int cross = ((q.X - p.X) * (r.Y - p.Y) - (q.Y - p.Y) * (r.X - p.X));
                    if (cross > 0)
                    {
                        tips.Add(p);
                    }
                    //int contourRadius = 15;
                    //Seq<Point> hull = MaxContour.GetConvexHull(Emgu.CV.CvEnum.ORIENTATION.CV_CLOCKWISE);
                    //for (int j = 0; j < hull.Total; j++)
                    //{
                    //    Point pointHull = hull.ElementAt(j);

                    //    if (pointHull.Y - contourRadius <= p.Y && pointHull.Y + contourRadius >= p.Y && pointHull.X - contourRadius <= p.X && pointHull.X + contourRadius >= p.X)
                    //    {
                    //        j = hull.Total;
                    //        tips.Add(p);
                    //    }
                    //}
                }
            }
            // return tipsCulstering(tips);
           
                return (tips);
          

        }
        public Seq<MCvConvexityDefect> eleminateDefects(Seq<MCvConvexityDefect> defects, Rectangle rec)
        {
            Seq<MCvConvexityDefect> newDefects  = new Seq<MCvConvexityDefect>(new MemStorage());
            int tolerance = rec.Height / 5;
            float angleTol = 95;
           Point ptStart = new Point();
           Point ptEnd = new Point();
           Point ptFar = new Point();
            
            for(int i=0;i<defects.Total;i++)
            {
                ptStart = defects[i].StartPoint;
                ptEnd = defects[i].EndPoint;
                ptFar = defects[i].DepthPoint;

                if(distanceP2P(ptStart,ptFar) > tolerance && distanceP2P(ptEnd,ptFar) > tolerance && getAngle(ptStart,ptFar,ptEnd) < angleTol)
                {
                    if(ptEnd.Y > (rec.Y+rec.Height-rec.Height/4))
                    {

                    }
                    else if(ptStart.Y > (rec.Y+rec.Height-rec.Height/4))
                    {

                    }
                    else
                    {
                        newDefects.Push(defects[i]);
                    }
                }

            }
            removeRedundantEndPoints(newDefects, rec);
            return newDefects;
        }

        // remove endpoint of convexity defects if they are at the same fingertip
        public void removeRedundantEndPoints(Seq<MCvConvexityDefect> newDefects,Rectangle rec)
        {
            float tolerance = rec.Width / 6;
           Point ptStart = new Point();
           Point ptEnd = new Point();
           Point ptStart2 = new Point();
           Point ptEnd2 = new Point();

            for(int i=0;i<newDefects.Total;i++)
            {
                for(int j =i;j<newDefects.Total;j++)
                {
                    ptStart = newDefects[i].StartPoint;
                    ptEnd = newDefects[i].EndPoint;
                    ptStart2 = newDefects[j].StartPoint;
                    ptEnd2 = newDefects[j].EndPoint;

                    if(distanceP2P(ptStart,ptEnd2) <tolerance )
                    {
                        newDefects.RemoveAt(i);
                        break;
                    }
                    if(distanceP2P(ptEnd,ptStart2) < tolerance)
                    {
                        newDefects.RemoveAt(j);
                    }
                }
            }
        }
        public List<Point> find_fingerTips(Seq<MCvConvexityDefect> defects, Rectangle rec)
        {
          Seq<MCvConvexityDefect> newDefects=  eleminateDefects(defects, rec);
           // removeRedundantEndPoints(newDefects, rec);
          tips.Clear();
            for (int k = 0; k < newDefects.Total; k++)
            {
                if (distanceP2P(newDefects[k].StartPoint, newDefects[k].EndPoint) < 10)
                {
                    tips.Add(newDefects[k].StartPoint);
                }
                else
                {
                    tips.Add(newDefects[k].StartPoint);
                    tips.Add(newDefects[k].EndPoint);
                }
            }
            return removeRedundantFingerTips();
        }
        // remove fingertips that are too close to eachother
        List<Point> removeRedundantFingerTips(){
            List<Point> newTips = new List<Point>();
            for (int i = 0; i < tips.Count; i++)
            {
                for (int j = i + 1; j < tips.Count(); j++)
                {
                    if (distanceP2P(tips[i], tips[j]) < 30)
                    {
                    }
                    else
                    {
                        newTips.Add(tips[i]);
                        break;
                    }
                }
            }
            return newTips;
        }

        public List<Point> tipsCulstering(List<Point> fingertips)
        {
            List<Point> newFingers = new List<Point>();


            List<Point> closePoints = new List<Point>();
            int postPoint = 0;

            for (int i = 0; i < fingertips.Count; i++)
            {
                for (int j = (i + 1); j < fingertips.Count; j++)
                {
                    postPoint = j;
                    if (i == fingertips.Count - 1 && j == fingertips.Count - 1)
                    {
                        postPoint = 0;
                    }
                    // closePoints.Add(fingertips[i]);

                    if (distanceP2P(fingertips[i], fingertips[postPoint]) < 100)
                    {

                        //if (postPoint == 0)
                        //    break;
                        //else
                        closePoints.Add(fingertips[postPoint]);

                    }
                    else
                    {
                        i = j - 1;

                        if (closePoints.Count >= 20)
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
        public List<Point> Cluster(List<Point> fingertips)
        {
            List<Point> newFingers = new List<Point>();
            List<Point> closePoints = new List<Point>();
            int postPoint = 0;

            for (int i = 0; i < fingertips.Count; i++)
            {
                 
                    postPoint = i+1;
                    if (i == fingertips.Count - 1)
                    {
                        postPoint = 0;
                    }
                    // closePoints.Add(fingertips[i]);

                    if (distanceP2P(fingertips[i], fingertips[postPoint]) < 50)
                    {
                        closePoints.Add(fingertips[i]);
                    }
                    else
                    {
                        if (closePoints.Count >= 0)
                        {
                            closePoints.Add(fingertips[i]);
                            newFingers.Add(closePoints[(int)closePoints.Count / 2]);
                            closePoints.Clear();
                        }
                        else
                            newFingers.Add(fingertips[i]);
                         
                    }
 
            }
            
            return newFingers;

        }
	 

    }
}
