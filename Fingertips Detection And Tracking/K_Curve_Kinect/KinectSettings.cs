using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

 

namespace FingerTipsDetectionTracking.K_Curve_Kinect
{
    public class KinectSettings
    {
        

        public float marginLeftPerc { get; set; }
        public float marginRightPerc { get; set; }
        public float marginTopPerc { get; set; }
        public float marginBotPerc { get; set; }

        public float nearSpacePerc { get; set; }
        public int absoluteSpace { get; set; }

        public int findCenterContourJump { get; set; }
        public int findCenterInsideJump { get; set; }
        public double fingertipFindJumpPerc { get; set; }

        public int findFingertipsJump { get; set; }
        public double theta { get; set; }
        public int k { get; set; }

        public int maxTrackedHands { get; set; }

        public int smoothingIterations { get; set; }

        public float containerBoxReduction { get; set; }

        

        public KinectSettings()
        {
            setDefault();
        }

        private void setDefault()
        {
             
            // Margins applied to the original size image (Percentaje)
            marginLeftPerc = 0;
            marginRightPerc = 0;
            marginTopPerc = 0;
            marginBotPerc = 0;

            // Percentage of valid space after the closest point, based on the furthest point
            nearSpacePerc = 0.05f;

            // Size of the valid space after the closest point
            absoluteSpace = -1; // 700 optimal

            // Size of the jumps of the contour and inside points used in the calculation of the center
            findCenterContourJump = 8;
            findCenterInsideJump = 8;

            // Size of the jump after check a possible fingertip
            findFingertipsJump = 2;

            // Size of the jump after find a valid fingertips (Percentage over the total)
            fingertipFindJumpPerc = 0.10f; // Size of the jump

            // Variables used in the K-curvature algorithm for the calcultion of the fingertips
            theta = 40 * (Math.PI / 180); // In radians
            k = 22;

            // Maximum number of tracked hands or figures
            maxTrackedHands = 2;

            // Erode and Dilation iterations
            smoothingIterations = 1;

            // Container box reduction
            containerBoxReduction = 0.5f;
        }
    }
}
