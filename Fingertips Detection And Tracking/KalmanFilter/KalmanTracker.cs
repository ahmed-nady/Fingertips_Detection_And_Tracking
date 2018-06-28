using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

//system
using System.Drawing;

//EMGU
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.UI;
using Emgu.CV.Util;

class KalmanTracker
{
    #region Variables
    float px, py, cx, cy, ix, iy;
    float ax, ay;

    PointF[] oup = new PointF[2];
    Kalman kal;
    Kalman_Filter.SyntheticData syntheticData;
    
    private List<PointF> kalmanPoints;
    #endregion
    #region Constructors
    public KalmanTracker()
    {
         
        kalmanPoints = new List<PointF>();
        kal = new Kalman(4, 2, 0);
        syntheticData = new Kalman_Filter.SyntheticData();
        Matrix<float> state = new Matrix<float>(new float[]
                {
                    0.0f, 0.0f, 0.0f, 0.0f
                });
        kal.CorrectedState = state;
        kal.TransitionMatrix = syntheticData.transitionMatrix;
        kal.MeasurementNoiseCovariance = syntheticData.measurementNoise;
        kal.ProcessNoiseCovariance = syntheticData.processNoise;
        kal.ErrorCovariancePost = syntheticData.errorCovariancePost;
        kal.MeasurementMatrix = syntheticData.measurementMatrix;
    }


    #endregion

    #region Public
    public void UpdateTarget(Point Location)
    {
        ix = Location.X;
        iy = Location.Y;
        PointF inp = new PointF(ix, iy);
        oup = new PointF[2];
        oup = filterPoints(inp);
        PointF[] pts = oup;
    }

    public void UpdateTarget(PointF Location)
    {
        ix = Location.X;
        iy = Location.Y;
        PointF inp = new PointF(ix, iy);
        oup = new PointF[2];
        oup = filterPoints(inp);
        PointF[] pts = oup;
    }

    public PointF GetRawData()
    {
        return new PointF(ix,iy);
    }

    public PointF GetPredicted()
    {
        return new PointF(px, py);
    }
    public void SetPredicted(PointF pt)
    {
        px = pt.X;
        py = pt.Y;
    }

    public PointF GetEstimate()
    {
        return new PointF(cx, cy);
    }
    #endregion

    #region Private
    private PointF[] filterPoints(PointF pt)
    {
        syntheticData.state[0, 0] = pt.X;
        syntheticData.state[1, 0] = pt.Y;
        Matrix<float> prediction = kal.Predict();
        PointF predictPoint = new PointF(prediction[0, 0], prediction[1, 0]);
        PointF measurePoint = new PointF(syntheticData.GetMeasurement()[0, 0],
            syntheticData.GetMeasurement()[1, 0]);
        Matrix<float> estimated = kal.Correct(syntheticData.GetMeasurement());
        PointF estimatedPoint = new PointF(estimated[0, 0], estimated[1, 0]);
        syntheticData.GoToNextState();
        PointF[] results = new PointF[2];
        results[0] = predictPoint;
        results[1] = estimatedPoint;
        px = predictPoint.X;
        py = predictPoint.Y;
        cx = estimatedPoint.X;
        cy = estimatedPoint.Y;
        return results;
    }
    #endregion
}

