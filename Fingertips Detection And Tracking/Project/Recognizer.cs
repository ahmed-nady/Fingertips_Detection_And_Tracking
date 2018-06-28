using Recognizer.NDollar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FingerTipsDetectionTracking.Project
{
    class Recognizer 
    {
        GeometricRecognizer _rec;
       
        public Recognizer()
        {
            _rec = new GeometricRecognizer();
             
            
            _rec.LoadGesture(@"G:\Documents\ZoomIn.xml");
            _rec.LoadGesture(@"G:\Documents\ZoomOut.xml");
        }
      public  String recgnizeGesture(List<PointR> points,int Numstrokes)
        {
             
            // combine the strokes into one unistroke, Lisa 8/8/2009
          //  List<PointR> points = new List<PointR>();
            //foreach (List<PointR> pts in _strokes)
            //{
            //    points.AddRange(pts);
            //}
            NBestList result = _rec.Recognize(points, Numstrokes); // where all the action is!!
            if (result.Score == -1)
            {
               return String.Format("No Match!\n[{0} out of {1} comparisons made]",
                    result.getActualComparisons(),
                    result.getTotalComparisons());
            }
            else
            {
               return String.Format("{0}: {1} ({2}px, {3}{4})\n[{5} out of {6} comparisons made]",
                result.Name,
                Math.Round(result.Score, 2),
                Math.Round(result.Distance, 2),
                Math.Round(result.Angle, 2), (char)176,
                result.getActualComparisons(),
                result.getTotalComparisons());
            }

        }
    }
}
