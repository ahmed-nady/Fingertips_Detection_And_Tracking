/**
 * The $N Multistroke Recognizer (C# version)
 *
 *	    Lisa Anthony, Ph.D.
 *		UMBC
 *		Information Systems Department
 * 		1000 Hilltop Circle
 *		Baltimore, MD 21250
 * 		lanthony@umbc.edu
 * 
 *      Jacob O. Wobbrock, Ph.D.
 * 		The Information School
 *		University of Washington
 *		Mary Gates Hall, Box 352840
 *		Seattle, WA 98195-2840
 *		wobbrock@u.washington.edu
 *
 * This software is distributed under the "New BSD License" agreement:
 * 
 * Copyright (c) 2007-2012, Lisa Anthony and Jacob O. Wobbrock
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *    * Redistributions of source code must retain the above copyright
 *      notice, this list of conditions and the following disclaimer.
 *    * Redistributions in binary form must reproduce the above copyright
 *      notice, this list of conditions and the following disclaimer in the
 *      documentation and/or other materials provided with the distribution.
 *    * Neither the name of the University of Washington or UMBC,
 *      nor the names of its contributors may be used to endorse or promote 
 *      products derived from this software without specific prior written
 *      permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS
 * IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO,
 * THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR
 * PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL Jacob O. Wobbrock OR Lisa Anthony 
 * BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR 
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE 
 * GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) 
 * HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT
 * LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY
 * OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF
 * SUCH DAMAGE.
 *
 */

using System;
using System.IO;
using System.Xml;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using Microsoft.Ink;

namespace Recognizer.NDollar
{
	public class GeometricRecognizer
	{
		#region Members

        //public const int NumResamplePoints = 64;
        private const double DX = 250.0;
        public static readonly SizeR ResampleScale = new SizeR(DX, DX);
        public static readonly double Diagonal = Math.Sqrt(DX * DX + DX * DX);
        public static readonly double HalfDiagonal = 0.5 * Diagonal;
        public static readonly PointR ResampleOrigin = new PointR(0, 0);
        private static readonly double Phi = 0.5 * (-1 + Math.Sqrt(5)); // Golden Ratio

        private static readonly double _RotationBound = 45.0; // Lisa 1/2/2008; could also try 15.0; changed from 45.0 so we're no longer fully rotation-invariant
        public static readonly double _1DThreshold = 0.30; // threshold for the ratio between short-side and long-side of a gesture, Lisa 1/2/2008; empirically determined
        
        // Note that the configurable recognition parameters have now been moved to a singleton 
        // class NDollarParameters. Access this by NDollarParameters.Instance.<parameter>.
        // Lisa 8/16/2009

        // batch testing
        private const int NumRandomTests = 100;
        private int[] numberOfTrainingSamples = new int[] { 1, 2, 4, 8 }; 
        public event ProgressEventHandler ProgressChangedEvent;

		private Hashtable _gestures;

        // added for debugging, Lisa 8/9/2009
        public static readonly bool _debug = false; // NOTE: DO NOT SET TO TRUE WHEN RUNNING MAKE IMAGES, COMPUTE STATS, OR TEST BATCH ON BIG DATASET
        private DebugForm _debugFrm = null;

		#endregion

		#region Constructor
	
		public GeometricRecognizer()
		{
            _gestures = new Hashtable(256);
		}

		#endregion

		#region Recognition

        public NBestList Recognize(List<PointR> points, int numStrokes) // candidate points
        {
            // removed the candidate transformations by creating a Gesture here
            // of the input points
            // this helps keep the transformations done to templates and candidates the same
            // and we won't have to edit code in two places
            // Lisa, 5/12/2008
            Gesture candidate = new Gesture(points);
            
            NBestList nbest = new NBestList();
            
            // added to check how much savings we are getting out of the Utils.AngleBetwenVUnitVectors() check
            // Lisa 8/9/2009
            int totalComparisons = 0;
            int actualComparisons = 0;

            // we have to compare the current gesture to all candidates,
            // each subgesture in our set of Multistrokes
            // Lisa 12/22/2007
            foreach (Multistroke ms in _gestures.Values) 
            {
                // added as of 8/9/2009
                if (!NDollarParameters.Instance.MatchOnlyIfSameNumberOfStrokes || numStrokes == ms.NumStrokes) // optional -- only attempt match when number of strokes is same
                {
                    NBestList thisMSnbest = new NBestList(); // store the best list for just this MS
                    foreach (Gesture p in ms.Gestures)
                    {
                        totalComparisons++;
                        // added as of 8/9/2009
                        if (!NDollarParameters.Instance.DoStartAngleComparison ||
                            (NDollarParameters.Instance.DoStartAngleComparison && Utils.AngleBetweenUnitVectors(candidate.StartUnitVector, p.StartUnitVector) <= NDollarParameters.Instance.StartAngleThreshold))
                        {
                            actualComparisons++;

                            double score = -1;
                            double[] best = new double[3] { -1, -1, -1 };

                            if (NDollarParameters.Instance.SearchMethod == NDollarParameters.PossibleSearchMethods.GSS)
                            {
                                best = GoldenSectionSearch(
                                    candidate.Points,                 // to rotate
                                    p.Points,                         // to match
                                    Utils.Deg2Rad(-_RotationBound),   // lbound, Lisa 1/2/2008 
                                    Utils.Deg2Rad(+_RotationBound),   // ubound, Lisa 1/2/2008 
                                    Utils.Deg2Rad(2.0)                // threshold
                                );    

                                score = 1d - best[0] / HalfDiagonal;
                            }
                            else if (NDollarParameters.Instance.SearchMethod == NDollarParameters.PossibleSearchMethods.Protractor)
                            {
                                best = OptimalCosineDistance(p.VectorVersion, candidate.VectorVersion); //candidate.Points, p.Points);
                                score = 1 / best[0]; // distance
                            }

                            // display the candidate and template at best match rotations
                            if (GeometricRecognizer._debug)
                            {
                                _debugFrm = new DebugForm();
                                _debugFrm.Owner = null;
                                _debugFrm.Show();

                                // send template & candidate
                                _debugFrm.SendPoints(p.Points, Utils.RotateByDegrees(candidate.Points, best[1]),
                                    String.Format("Template name: {0}; Score = {1}", p.Name, Math.Round(score, 2)));
                            }

                            // keep track of what subgesture was best match for this multistroke
                            // and only add that particular template's score to the nbest list
                            // Lisa 12/22/2007
                            thisMSnbest.AddResult(p.Name, score, best[0], best[1]); // name, score, distance, angle
                        }
                    }
                    thisMSnbest.SortDescending();
                    // add the one that was best of those subgestures
                    // these properties return the property of the top result
                    // Lisa 12/22/2007
                    nbest.AddResult(thisMSnbest.Name, thisMSnbest.Score, thisMSnbest.Distance, thisMSnbest.Angle); // name, score, distance, angle
                }
            }
            nbest.SortDescending(); // sort so that nbest[0] is best result
            nbest.setTotalComparisons(totalComparisons);
            nbest.setActualComparisons(actualComparisons);
            return nbest;
        }

        // From http://yangl.org/protractor/Protractor%20Gesture%20Recognizer.pdf
        private double[] OptimalCosineDistance(List<Double> v1, List<Double> v2)
        {
            double a = 0;
            double b = 0;
            
            for (int i = 0; i < v1.Count; i = i + 2)
            {
                a = a + v1[i] * v2[i] + v1[i + 1] * v2[i + 1];
                b = b + v1[i] * v2[i + 1] - v1[i + 1] * v2[i];
            }

            double angle = Math.Atan(b / a);
            return new double[3] { Math.Acos(a * Math.Cos(angle) + b * Math.Sin(angle)), Utils.Rad2Deg(angle), 0d }; // distance, angle, calls to path dist (n/a)
        }

        // From http://www.math.uic.edu/~jan/mcs471/Lec9/gss.pdf
        private double[] GoldenSectionSearch(List<PointR> pts1, List<PointR> pts2, double a, double b, double threshold)
        {
            double x1 = Phi * a + (1 - Phi) * b;
            List<PointR> newPoints = Utils.RotateByRadians(pts1, x1);
            double fx1 = Utils.PathDistance(newPoints, pts2);

            double x2 = (1 - Phi) * a + Phi * b;
            newPoints = Utils.RotateByRadians(pts1, x2);
            double fx2 = Utils.PathDistance(newPoints, pts2);

            double i = 2.0; // calls
            while (Math.Abs(b - a) > threshold)
            {
                if (fx1 < fx2)
                {
                    b = x2;
                    x2 = x1;
                    fx2 = fx1;
                    x1 = Phi * a + (1 - Phi) * b;
                    newPoints = Utils.RotateByRadians(pts1, x1);
                    fx1 = Utils.PathDistance(newPoints, pts2);
                }
                else
                {
                    a = x1;
                    x1 = x2;
                    fx1 = fx2;
                    x2 = (1 - Phi) * a + Phi * b;
                    newPoints = Utils.RotateByRadians(pts1, x2);
                    fx2 = Utils.PathDistance(newPoints, pts2);
                }
                i++;
            }
            return new double[3] { Math.Min(fx1, fx2), Utils.Rad2Deg((b + a) / 2.0), i }; // distance, angle, calls to pathdist
        }

        // continues to rotate 'pts1' by 'step' degrees as long as points become ever-closer 
        // in path-distance to pts2. the initial distance is given by D. the best distance
        // is returned in array[0], while the angle at which it was achieved is in array[1].
        // array[3] contains the number of calls to PathDistance.
        private double[] HillClimbSearch(List<PointR> pts1, List<PointR> pts2, double D, double step)
        {
            double i = 0.0;
            double theta = 0.0;
            double d = D;
            do
            {
                D = d; // the last angle tried was better still
                theta += step;
                List<PointR> newPoints = Utils.RotateByDegrees(pts1, theta);
                d = Utils.PathDistance(newPoints, pts2);
                i++;
            }
            while (d <= D);
            return new double[3] { D, theta - step, i }; // distance, angle, calls to pathdist
        }

        private double[] FullSearch(List<PointR> pts1, List<PointR> pts2, StreamWriter writer)
        {
            double bestA = 0d;
            double bestD = Utils.PathDistance(pts1, pts2);

            for (int i = -180; i <= +180; i++)
            {
                List<PointR> newPoints = Utils.RotateByDegrees(pts1, i);
                double d = Utils.PathDistance(newPoints, pts2);
                if (writer != null)
                {
                    writer.WriteLine("{0}\t{1:F3}", i, Math.Round(d, 3));
                }
                if (d < bestD)
                {
                    bestD = d;
                    bestA = i;
                }
            }
            writer.WriteLine("\nFull Search (360 rotations)\n{0:F2}{1}\t{2:F3} px", Math.Round(bestA, 2), (char) 176, Math.Round(bestD, 3)); // calls, angle, distance
            return new double[3] { bestD, bestA, 360.0 }; // distance, angle, calls to pathdist
        }

        #endregion

        #region Gestures & Xml

        public int NumGestures
		{
			get
			{
                return _gestures.Count;
			}
		}

        public ArrayList Gestures
        {
            get
            {
                ArrayList list = new ArrayList(_gestures.Values);
                list.Sort();
                return list;
            }
        }

		public void ClearGestures()
		{
            _gestures.Clear();
		}

        // added the numPtsInStroke argument so we can read and write the gestures we draw ourselves for testing
        // Lisa 1/2/2008
		public bool SaveGesture(string filename, List<List<PointR>> strokes, List<int> numPtsInStroke)
		{
			// add the new prototype with the name extracted from the filename.
            string name = Gesture.ParseName(filename);
            if (_gestures.ContainsKey(name))
                _gestures.Remove(name);

            // Lisa 1/2/2008
			Multistroke newPrototype = new Multistroke(name, "test", "test", strokes); //points, numPtsInStroke);
            
            _gestures.Add(name, newPrototype);

            List<PointR> points = newPrototype.OriginalGesture.RawPoints;
            // figure out the duration of the gesture
            PointR p0 = points[0];
            PointR pn = points[points.Count - 1];

			// do the xml writing (of the raw points)
			bool success = true;
			XmlTextWriter writer = null;
			try
			{
				// save the prototype as an Xml file
				writer = new XmlTextWriter(filename, Encoding.UTF8);
				writer.Formatting = Formatting.Indented;
				writer.WriteStartDocument(true);
				writer.WriteStartElement("Gesture");
				writer.WriteAttributeString("Name", name);
                writer.WriteAttributeString("Subject", "test");
                writer.WriteAttributeString("Speed", "test");
				writer.WriteAttributeString("NumPts", XmlConvert.ToString(points.Count));
                writer.WriteAttributeString("Milliseconds", XmlConvert.ToString(pn.T - p0.T));
                writer.WriteAttributeString("AppName", Assembly.GetExecutingAssembly().GetName().Name);
				writer.WriteAttributeString("AppVer", Assembly.GetExecutingAssembly().GetName().Version.ToString());
				writer.WriteAttributeString("Date", DateTime.Now.ToLongDateString());
				writer.WriteAttributeString("TimeOfDay", DateTime.Now.ToLongTimeString());

                // write out the Stroke tags, Lisa 1/2/2008
                int numStrokesWritten = 0;
                // write out the raw individual points
                // fixed to work with strokes, Lisa 8/8/2009
                foreach (List<PointR> pts in strokes)
                {
                    writer.WriteStartElement("Stroke");
                    writer.WriteAttributeString("index", XmlConvert.ToString(numStrokesWritten + 1));
                    numStrokesWritten++;
                    foreach (PointR p in pts)
                    {
                        writer.WriteStartElement("Point");
                        writer.WriteAttributeString("X", XmlConvert.ToString(p.X));
                        writer.WriteAttributeString("Y", XmlConvert.ToString(p.Y));
                        writer.WriteAttributeString("T", XmlConvert.ToString(p.T));
                        writer.WriteEndElement(); // <Point />
                    }
                    // write the Stroke tags, Lisa 1/2/2008
                    writer.WriteEndElement(); // </Stroke>, I hope
                }
                writer.WriteEndDocument(); // </Gesture>
			}
			catch (XmlException xex)
			{
				Console.WriteLine(xex.Message);
                Console.Write(xex.StackTrace);
                Console.WriteLine();
                success = false;
			}
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.Write(ex.StackTrace);
                Console.WriteLine();
                success = false;
            }
			finally
			{
				if (writer != null)
					writer.Close();
			}
			return success; // Xml file successfully written (or not)
		}

		public bool LoadGesture(string filename)
		{
			bool success = true;
			XmlTextReader reader = null;
            try
            {
                reader = new XmlTextReader(filename);
                reader.WhitespaceHandling = WhitespaceHandling.None;
                reader.MoveToContent();

                Multistroke p = ReadGesture(reader); // Lisa 1/2/2008

                // remove any with the same name and add the prototype gesture
                if (_gestures.ContainsKey(p.Name))
                    _gestures.Remove(p.Name);

                // _gestures now contains Multistrokes, not just Gestures
                // Lisa 12/21/2007
                _gestures.Add(p.Name, p);
            }
            catch (XmlException xex)
            {
                Console.WriteLine(xex.Message);
                Console.Write(xex.StackTrace);
                Console.WriteLine();
                success = false;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.Write(ex.StackTrace);
                Console.WriteLine();
                success = false;
            }
			finally
			{
				if (reader != null)
					reader.Close();
			}
			return success;
		}

        // assumes the reader has been just moved to the head of the content.
        // changed this to return a Multistroke so we can change the order of pre-processing.
        // Lisa 1/2/2008
        public static Gesture ReadGestureRaw(XmlTextReader reader, out int numStrokes, out List<List<PointR>> strokes)
        {
            Debug.Assert(reader.LocalName == "Gesture");
            string name = reader.GetAttribute("Name");
            string user = reader.GetAttribute("Subject");
            string speed = reader.GetAttribute("Speed");

            // add speed to name to allow for unique testing
            if (speed != null)
                user += "-" + speed;

            List<PointR> points = new List<PointR>();
            // added by Lisa 12/21/2007, revised 8/8/2009
            //List<List<PointR>>
            strokes = new List<List<PointR>>();

            reader.Read(); // advance to the first Point
            // added by Lisa 12/21/2007 to advance past the <Stroke> tags
            if (!(reader.LocalName == "Point"))
                reader.Read();
            else Debug.Assert(reader.LocalName == "Point");
            // updated 5/11/2008 to be conditional

            // change the end condition to specifically reference the end tag Gesture by name
            // Lisa 12/21/2007
            while (reader.LocalName != "Gesture") // && (reader.NodeType != XmlNodeType.EndElement))
            {
                if (reader.LocalName == "Point")
                {
                    PointR p = PointR.Empty;
                    p.X = XmlConvert.ToDouble(reader.GetAttribute("X"));
                    p.Y = XmlConvert.ToDouble(reader.GetAttribute("Y"));
                    p.T = XmlConvert.ToInt64(reader.GetAttribute("T")); // get last 6 digits
                    string pressure = reader.GetAttribute("P"); // if no pressure in this dataset, should be null
                    if (pressure == null)
                        p.P = -1.0;
                    else p.P = XmlConvert.ToDouble(pressure);
                    points.Add(p);
                    reader.ReadStartElement("Point");
                }
                else if ((reader.LocalName == "Stroke") && (reader.NodeType != XmlNodeType.EndElement))
                {
                    // set up stroke index for the beginning of this stroke
                    strokes.Add(new List<PointR>(points));
                    points = new List<PointR>();
                    reader.Read();
                }
                else
                {
                    reader.Read(); // read start Stroke tags and do nothing
                }
            }
            // add last stroke size
            strokes.Add (new List<PointR>(points));
            List<PointR> all_points = new List<PointR>();
            foreach (List<PointR> pts in strokes)
            {
                all_points.AddRange(pts);
            }
            numStrokes = strokes.Count;
            return new Gesture(name, all_points);
        }

        public static Multistroke ReadGesture(XmlTextReader reader)
        {
            Debug.Assert(reader.LocalName == "Gesture");
            string name = reader.GetAttribute("Name");
            string user = reader.GetAttribute("Subject");
            string speed = reader.GetAttribute("Speed");

            // add speed to name to allow for unique testing
            if (speed != null)
                user += "-" + speed;

            List<PointR> points = new List<PointR>();
            // added by Lisa 12/21/2007, revised 8/8/2009
            List<List<PointR>> strokes = new List<List<PointR>>();

            reader.Read(); // advance to the first Point
            // added by Lisa 12/21/2007 to advance past the <Stroke> tags
            if (!(reader.LocalName == "Point"))
                reader.Read();
            else Debug.Assert(reader.LocalName == "Point");
            // updated 5/11/2008 to be conditional

            // change the end condition to specifically reference the end tag Gesture by name
            // Lisa 12/21/2007
            while (reader.LocalName != "Gesture") // && (reader.NodeType != XmlNodeType.EndElement))
            {
                if (reader.LocalName == "Point")
                {
                    PointR p = PointR.Empty;
                    p.X = XmlConvert.ToDouble(reader.GetAttribute("X"));
                    p.Y = XmlConvert.ToDouble(reader.GetAttribute("Y"));
                    p.T = XmlConvert.ToInt64(reader.GetAttribute("T")); // get last 6 digits
                    string pressure = reader.GetAttribute("P"); // if no pressure in this dataset, should be null
                    if (pressure == null)
                        p.P = -1.0;
                    else p.P = XmlConvert.ToDouble(pressure);
                    points.Add(p);
                    reader.ReadStartElement("Point");
                }
                else if ((reader.LocalName == "Stroke") && (reader.NodeType != XmlNodeType.EndElement))
                {
                    // set up stroke index for the beginning of this stroke
                    strokes.Add(new List<PointR>(points));
                    points = new List<PointR>();
                    reader.Read();
                }
                else
                {
                    reader.Read(); // read start Stroke tags and do nothing
                }
            }
            // add last stroke size
            strokes.Add(new List<PointR>(points));
            return new Multistroke(name, user, speed, strokes); //user+"~"+name, user, speed, strokes); // keep each stroke separate until we're done pre-processing
            // TODO: make the gesture naming convention more general again
        }

        #endregion

        #region Batch Processing

        /// <summary>
        /// Assemble the gesture filenames into categories that contain 
        /// potentially multiple examples of the same gesture.
        /// </summary>
        /// <param name="filenames"></param>
        /// <returns>A Hashtable keyed by user # of category instances, 
        /// or <b>null</b> if an error occurs.</returns>
        /// <remarks>
        /// See the comments above MainForm.BatchProcess_Click.
        /// </remarks>
        public SamplesCollection AssembleBatch(string[] filenames, bool include1D, bool include2D)
        {
            // organize these by user: each user's categories are added at the index=user #
            // Lisa 1/5/2008
            SamplesCollection categoriesByUser = new SamplesCollection();

            Console.Write("Assembling batch from files");
            for (int i = 0; i < filenames.Length; i++)
            {
                string filename = filenames[i];

                XmlTextReader reader = null;
                try
                {
                    reader = new XmlTextReader(filename);
                    reader.WhitespaceHandling = WhitespaceHandling.None;
                    reader.MoveToContent();
                    
                    Multistroke p = ReadGesture(reader); // Lisa 1/2/2008
                    // only include the kinds of gestures we say to (via threshold), Lisa 5/13/2008
                    if (p.OriginalGesture.Is1D && include1D)
                        categoriesByUser.AddExample(p);
                    else if (!p.OriginalGesture.Is1D && include2D)
                        categoriesByUser.AddExample(p);
                    Console.Write(".");
                }
                catch (XmlException xex)
                {
                    Console.WriteLine(xex.Message);
                    Console.Write(xex.StackTrace);
                    Console.WriteLine();
                    categoriesByUser.Clear();
                    categoriesByUser = null;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.Write(ex.StackTrace);
                    Console.WriteLine();
                    categoriesByUser.Clear();
                    categoriesByUser = null;
                }
                finally
                {
                    if (reader != null)
                        reader.Close();
                }
            }
            Console.WriteLine();

            // now make sure that each category has the same number of elements in it.
            // actually, we don't need this constraint anymore, so we just return the
            // Hashtable, Lisa 1/5/2008
            foreach (KeyValuePair<string, Dictionary<string, Category>> user in categoriesByUser)
            {
                if (!categoriesByUser.AreNumExamplesEqualForUser((string)user.Key))
                    Console.WriteLine("Warning: in case you were not expecting it, there is a different number of samples across categories for user " + (string)user.Key + ".");
                /*List<string> catsToRemove = new List<string>();
                foreach (KeyValuePair<string, Category> cat in user.Value)
                {
                    // Console.WriteLine("user: " + user.Key + ", category: " + cat.Key + ", num ex = " + categoriesByUser.GetCategoryByUser((string)user.Key, (string)cat.Key).NumExamples);
                    if (categoriesByUser.GetCategoryByUser((string)user.Key, (string)cat.Key).NumExamples < _MinExamples)
                    {
                        // remove any user/symbol pairs with fewer than a certain number of examples
                        catsToRemove.Add((string)cat.Key);
                    }
                }
                foreach (string s in catsToRemove)
                {
                    categoriesByUser.RemoveSamples((string)user.Key, s);
                }*/
            }
            Console.WriteLine("Done assembling batch.");
            return categoriesByUser; // list;
        }

        /// <summary>
        /// Tests an entire batch of files. See comments atop MainForm.TestBatch_Click().
        /// 
        /// This was adapted from the original TestBatch() method used by $1 to do multistroke testing.
        /// (Lisa 1/5/2008)
        /// </summary>
        /// <param name="categories">A hashtable keyed by user of gesture categories 
        /// that each contain lists of prototypes (examples) within that gesture category.</param>
        /// <param name="dir">The directory into which to write the output files.</param>
        /// <returns>True if successful; false otherwise.</returns>
        public bool TestBatch(SamplesCollection categoriesByUser, string dir)
        {
            if (NDollarParameters.Instance.TestMethod == NDollarParameters.PossibleTestMethods.CROSSVAL)
            {
                return TestBatch_CrossValidate(categoriesByUser, dir);
            }
            else if (NDollarParameters.Instance.TestMethod == NDollarParameters.PossibleTestMethods.RANDOM100)
            {
                return TestBatch_100Random(categoriesByUser, dir);
            }
            else if (NDollarParameters.Instance.TestMethod == NDollarParameters.PossibleTestMethods.USERINDEP)
            {
                return TestBatch_UserIndependent_100Random(categoriesByUser, dir);
            }
            else {
                return false;
            }
        }

        public bool TestBatch_100Random(SamplesCollection categoriesByUser, string dir)
        {
            Console.Write("Testing batch (one tick per user)");
            bool success = true;
            StreamWriter mainWriter = null;
            StreamWriter recWriter = null;
            try
            {
                //
                // set up a main results file and detailed recognition results file
                //
                int start = Environment.TickCount;
                string mainFile = String.Format("{0}\\ndollar_main_{1}.csv", dir, start);
                string recFile = String.Format("{0}\\ndollar_data_{1}.csv", dir, start);

                mainWriter = new StreamWriter(mainFile, false);//, Encoding.UTF8);
                mainWriter.WriteLine("Recognizer:,{0}, StartTime(ms):,{1}\n", NDollarParameters.Instance.RecognizerName, start);
                mainWriter.WriteLine("Testing:,within-user,Matching method:,{0},Rotation invariance:,{1},Rotation bound:,{2},1D Threshold:,{3},Do start angle comparison:,{4},Start angle index:,{5},Start angle threshold:,{6},Do match only same number of strokes:,{7},Test for 1D gestures:,{8},UseUniformScaling:,{9}\n",
                    (NDollarParameters.Instance.SearchMethod == NDollarParameters.PossibleSearchMethods.GSS) ? "GSS" : "Protractor",
                    NDollarParameters.Instance.RotationInvariant,
                    _RotationBound,
                    _1DThreshold,
                    NDollarParameters.Instance.DoStartAngleComparison,
                    NDollarParameters.Instance.StartAngleIndex,
                    Utils.Rad2Deg(NDollarParameters.Instance.StartAngleThreshold),
                    NDollarParameters.Instance.MatchOnlyIfSameNumberOfStrokes,
                    NDollarParameters.Instance.TestFor1D,
                    NDollarParameters.Instance.UseUniformScaling);
                mainWriter.WriteLine("Recognizer,Subject,Speed,NumTraining,GestureType,RecognitionRate");

                recWriter = new StreamWriter(recFile, false);//, Encoding.UTF8);
                recWriter.WriteLine("Recognizer:,{0}, StartTime(ms):,{1}\n", NDollarParameters.Instance.RecognizerName, start);
                //recWriter.WriteLine("Testing:,within-user,Rotation invariance:,{0},Rotation bound:,{1},1D Threshold:,{2},Do start angle comparison:,{3},Start angle index:,{4},Start angle threshold:,{5},Do match only same number of strokes:,{6},Test for 1D gestures:,{7},UseUniformScaling:,{8}\n",
                recWriter.WriteLine("Testing:,within-user,Matching method:,{0},Rotation invariance:,{1},Rotation bound:,{2},1D Threshold:,{3},Do start angle comparison:,{4},Start angle index:,{5},Start angle threshold:,{6},Do match only same number of strokes:,{7},Test for 1D gestures:,{8},UseUniformScaling:,{9}\n",
                    (NDollarParameters.Instance.SearchMethod == NDollarParameters.PossibleSearchMethods.GSS) ? "GSS" : "Protractor",
                    NDollarParameters.Instance.RotationInvariant,
                    _RotationBound,
                    _1DThreshold,
                    NDollarParameters.Instance.DoStartAngleComparison,
                    NDollarParameters.Instance.StartAngleIndex,
                    Utils.Rad2Deg(NDollarParameters.Instance.StartAngleThreshold),
                    NDollarParameters.Instance.MatchOnlyIfSameNumberOfStrokes,
                    NDollarParameters.Instance.TestFor1D,
                    NDollarParameters.Instance.UseUniformScaling);
                recWriter.WriteLine("Subject,Speed,Correct?,NumTrain,Tested,Character,ActualComparisons,TotalComparisons,Is1D,1stCorrect,Pts,Ms,NumStrokes,RecoDuration,Angle,:,(NBestNames),[NBestScores]");

                // PER-USER-TESTING:
                // for each user
                //      for i = 1 to max number of training templates per symbol
                //          choose i samples randomly
                //          load those templates
                //          test on 1 remaining sample per symbol, randomly chosen
                //      repeat 100 times

                // new outermost loop: does the whole thing once for each user
                // Lisa, 5/12/2008
                foreach (KeyValuePair<string, Dictionary<string, Category>> user in categoriesByUser)
                {
                    Console.Write(".");
                    string speed = "unknown"; // TODO: get this from the new object later

                    //
                    // determine the maximum number of gesture categories and the 
                    // minimum number of examples per category for this specific user
                    //
                    int minNumExamples = categoriesByUser.GetMinNumExamplesForUser(user.Key);
                    double totalTests = (minNumExamples - 1) * NumRandomTests;

                    //
                    // next loop: trains on N=1..9, tests on 10-N (for e.g., numExamples = 10)
                    //
                    for (int n = 1; n <= minNumExamples - 1; n++)
                    {
                        // storage for the final avg results for each category for this N
                        Hashtable results = new Hashtable();

                        //
                        // run a number of tests at this particular N number of training examples
                        //
                        for (int r = 0; r < NumRandomTests; r++)
                        {
                            _gestures.Clear(); // clear any (old) loaded prototypes

                            // load (train on) N randomly selected gestures in each category
                            // do this for this user only
                            foreach (KeyValuePair<string, Category> cat in user.Value)
                            {
                                Category c = (Category)cat.Value;  // the category to load N examples for
                                // choose over the whole range of examples for this user/symbol pair, Lisa 1/5/2008
                                int[] chosen = Utils.Random(0, c.NumExamples - 1, n); // select N unique indices
                                for (int j = 0; j < chosen.Length; j++)
                                {
                                    Multistroke p = c[chosen[j]]; // get the prototype from this category at chosen[j], Lisa 1/5/2008
                                    _gestures.Add(p.Name, p); // load the randomly selected test gestures into the recognizer
                                }
                            }
                        
                            //
                            // testing loop on all unloaded gestures in each category. creates a recognition
                            // rate (%) by averaging the binary outcomes (correct, incorrect) for each test.
                            //
                            // do this for this user only
                            foreach (KeyValuePair<string, Category> cat in user.Value)
                            {
                                // pick a random unloaded gesture in this category for testing
                                // instead of dumbly picking, first find out what indices aren't
                                // loaded, and then randomly pick from those.
                                Category c = (Category)cat.Value;
                                int[] notLoaded = new int[c.NumExamples - n];
                                for (int j = 0, k = 0; j < c.NumExamples; j++)
                                {
                                    Multistroke g = c[j]; // Lisa 1/5/2008
                                    if (!_gestures.ContainsKey(g.Name))
                                        notLoaded[k++] = j; // jth gesture in c is not loaded
                                }
                                int chosen = Utils.Random(0, notLoaded.Length - 1); // index
                                Multistroke ms = c[notLoaded[chosen]]; // gesture to test
                                Gesture p = ms.OriginalGesture; // we only test on the original Gesture in the Multistroke, Lisa 1/5/2008
                                Debug.Assert(!_gestures.ContainsKey(p.Name));

                                // do the recognition!
                                // record time before reco
                                int recoStart = Environment.TickCount;
                                Console.WriteLine("file we are about to recognize: " + p.Name);
                                NBestList result = null;
                                if (NDollarParameters.Instance.RecognizerName == NDollarParameters.PossibleRecognizers.NDOLLAR)
                                    result = this.Recognize(p.RawPoints, ms.NumStrokes);
                                else if (NDollarParameters.Instance.RecognizerName == NDollarParameters.PossibleRecognizers.TABLETPC)
                                    result = this.RecognizeInk(p.RawPoints, ms.NumStrokes);
                                else throw new Exception("Bad Recognizer Name in config.xml");
                                // record time after reco
                                int recoEnd = Environment.TickCount;
                                int recoDuration = recoEnd - recoStart; // in milliseconds
                                string category = Category.ParseName(result.Name);
                                int correct = (c.Name == category) ? 1 : 0;

                                speed = ms.Speed;
                                recWriter.WriteLine("{0},{1},{2},{3},'{4},'{5},{6},{7},{8},{9},{10},{11},{12},{13},{14:F1}{15},:,({16}),[{17}]",
                                    ms.User,                            // 0 Subject
                                    ms.Speed,                           // 1 Speed
                                    correct,                            // 2 Correct?
                                    n,                                  // 3 NumTrain 
                                    p.Name,                             // 4 Tested 
                                    Category.ParseName(p.Name),         // 5 Character
                                    result.getActualComparisons(),      // 6 ActualComparisons
                                    result.getTotalComparisons(),       // 7 TotalComparisons
                                    p.Is1D,                             // 8 Is1D
                                    FirstCorrect(p.Name, result.Names), // 9 1stCorrect
                                    p.RawPoints.Count,                  // 10 Pts
                                    p.Duration,                         // 11 Ms 
                                    ms.NumStrokes,                      // 12 number of strokes
                                    recoDuration,                       // 13 recognition duration (millis)
                                    Math.Round(result.Angle, 1), (char)176, // 14/15 Angle tweaking :
                                    result.NamesString,                 // 16 (NBestNames)
                                    result.ScoresString);               // 17 [NBestScores]

                                // c is a Category object, unique to user/category pair
                                // use the category NAME to store the results
                                // Lisa 1/6/2008
                                if (results.ContainsKey(cat.Key))
                                {
                                    double temp = (double)results[cat.Key] + correct;
                                    results[cat.Key] = temp;
                                }
                                else
                                {
                                    results.Add(cat.Key, (double)correct);
                                }
                            }
                       
                            // provide feedback as to how many tests have been performed thus far.
                            double testsSoFar = ((n - 1) * NumRandomTests) + r;
                            if (ProgressChangedEvent != null)
                                ProgressChangedEvent(this, new ProgressEventArgs(testsSoFar / totalTests)); // callback
                        }

                        //
                        // now create the final results for this user and this N and write them to a file
                        //
                        foreach (KeyValuePair<string, Category> cat in user.Value)
                        {
                            double temp = (double)results[cat.Key] / ((double)NumRandomTests * 1); // normalize by the number of tests at this N, Lisa 1/5/2008
                            results[cat.Key] = temp;
                            // Subject Recognizer Speed NumTraining GestureType RecognitionRate
                            mainWriter.WriteLine("ndollar,{0},{1},{2},{3},{4:F3}", user.Key, speed, n, cat.Key, Math.Round((double)results[cat.Key], 3));
                        }
                    }
                }
                // time-stamp the end of the processing when it's allll done
                int end = Environment.TickCount;
                mainWriter.WriteLine("\nEndTime(ms):,{0}, Minutes:,{1:F2}", end, Math.Round((end - start) / 60000.0, 2));
                recWriter.WriteLine("\nEndTime(ms):,{0}, Minutes:,{1:F2}", end, Math.Round((end - start) / 60000.0, 2));
                Console.WriteLine();
                Console.WriteLine("Done testing batch.");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.Write(ex.StackTrace);
                Console.WriteLine();
                success = false;
            }
            finally
            {
                if (mainWriter != null)
                    mainWriter.Close();
                if (recWriter != null)
                    recWriter.Close();
            }
            return success;
        }

        private NBestList RecognizeInk(List<PointR> points, int numStrokes) {
            RecognizerContext recognizerCtxt = new RecognizerContext();
            recognizerCtxt.Strokes = null;
            recognizerCtxt.WordList = new WordList();

            // Note: there should be no strokes in the Strokes property of the RecognizerContext
            // when setting the WordList property
            recognizerCtxt.WordList = NDollarParameters.Instance.TabletRecognizerWordList; // recoWordList;
            recognizerCtxt.Factoid = Microsoft.Ink.Factoid.WordList;
            recognizerCtxt.RecognitionFlags = RecognitionModes.WordMode | RecognitionModes.Coerce;
             
            Ink myInk = new Ink();
            Point[] pts = new Point[points.Count];

            int c = 0;
            foreach (PointR p in points) {
                pts[c] = new Point((int)(p.X), (int)(p.Y));
                c++;
            }
            Stroke stroke = myInk.CreateStroke(pts);
            
            recognizerCtxt.Strokes = myInk.CreateStrokes();
            recognizerCtxt.Strokes.Add(stroke);
            RecognitionStatus recoStatus = new RecognitionStatus();
            Microsoft.Ink.RecognitionResult recoResult = recognizerCtxt.Recognize(out recoStatus);
            string id = GetMappedIDFromCharacter(recoResult.TopString);
            Console.WriteLine("***** Tablet PC recognition result: " + recoResult.TopString + "\t Symbol: " + id);
            recognizerCtxt.Strokes.Clear();

            // make NBextList and return it
            NBestList nbest = new NBestList();
            nbest.AddResult(id, 0, 0, 0); // name, score, distance, angle
            //nbest.SortDescending(); // sort so that nbest[0] is best result
            //nbest.setTotalComparisons(totalComparisons);
            //nbest.setActualComparisons(actualComparisons);
            recognizerCtxt.Dispose();
            myInk.Dispose();
            return nbest;
        }

        private string GetMappedIDFromCharacter(string character)
        {
            switch (character) {
                case "o": return "circle"; 
                case "O": return "circle"; 
                case "+": return "plus";
                case "-": return "line";
                default: return character;
            }
        }

        private int FirstCorrect(string name, string[] names)
        {
            string category = Category.ParseName(name);
            for (int i = 0; i < names.Length; i++)
            {
                string c = Category.ParseName(names[i]);
                if (category == c)
                {
                    return i + 1;
                }
            }
            return -1;
        }

        // Cross-Validation Test Batch. Assumes 5 folds.
        public bool TestBatch_CrossValidate(SamplesCollection categoriesByUser, string dir)
        {
            Console.Write("Testing batch (one tick per user)");
            bool success = true;
            StreamWriter mainWriter = null;
            StreamWriter recWriter = null;
            try
            {
                //
                // set up a main results file and detailed recognition results file
                //
                int start = Environment.TickCount;
                string mainFile = String.Format("{0}\\ndollar_main_{1}.csv", dir, start);
                string recFile = String.Format("{0}\\ndollar_data_{1}.csv", dir, start);

                mainWriter = new StreamWriter(mainFile, false);//, Encoding.UTF8);
                mainWriter.WriteLine("Recognizer:,{0}, StartTime(ms):,{1}\n", NDollarParameters.Instance.RecognizerName, start);
                mainWriter.WriteLine("Testing:,within-user,Matching method:,{0},Rotation invariance:,{1},Rotation bound:,{2},1D Threshold:,{3},Do start angle comparison:,{4},Start angle index:,{5},Start angle threshold:,{6},Do match only same number of strokes:,{7},Test for 1D gestures:,{8},UseUniformScaling:,{9}\n",
                    (NDollarParameters.Instance.SearchMethod == NDollarParameters.PossibleSearchMethods.GSS) ? "GSS" : "Protractor",
                    NDollarParameters.Instance.RotationInvariant,
                    _RotationBound,
                    _1DThreshold,
                    NDollarParameters.Instance.DoStartAngleComparison,
                    NDollarParameters.Instance.StartAngleIndex,
                    Utils.Rad2Deg(NDollarParameters.Instance.StartAngleThreshold),
                    NDollarParameters.Instance.MatchOnlyIfSameNumberOfStrokes,
                    NDollarParameters.Instance.TestFor1D,
                    NDollarParameters.Instance.UseUniformScaling);
                mainWriter.WriteLine("Recognizer,Subject,Speed,Fold,NumTraining,GestureType,RecognitionRate");

                recWriter = new StreamWriter(recFile, false);//, Encoding.UTF8);
                recWriter.WriteLine("Recognizer:,{0}, StartTime(ms):,{1}\n", NDollarParameters.Instance.RecognizerName, start);
                //recWriter.WriteLine("Testing:,within-user,Rotation invariance:,{0},Rotation bound:,{1},1D Threshold:,{2},Do start angle comparison:,{3},Start angle index:,{4},Start angle threshold:,{5},Do match only same number of strokes:,{6},Test for 1D gestures:,{7},UseUniformScaling:,{8}\n",
                recWriter.WriteLine("Testing:,within-user,Matching method:,{0},Rotation invariance:,{1},Rotation bound:,{2},1D Threshold:,{3},Do start angle comparison:,{4},Start angle index:,{5},Start angle threshold:,{6},Do match only same number of strokes:,{7},Test for 1D gestures:,{8},UseUniformScaling:,{9}\n",
                    (NDollarParameters.Instance.SearchMethod == NDollarParameters.PossibleSearchMethods.GSS) ? "GSS" : "Protractor",
                    NDollarParameters.Instance.RotationInvariant,
                    _RotationBound,
                    _1DThreshold,
                    NDollarParameters.Instance.DoStartAngleComparison,
                    NDollarParameters.Instance.StartAngleIndex,
                    Utils.Rad2Deg(NDollarParameters.Instance.StartAngleThreshold),
                    NDollarParameters.Instance.MatchOnlyIfSameNumberOfStrokes,
                    NDollarParameters.Instance.TestFor1D,
                    NDollarParameters.Instance.UseUniformScaling);
                recWriter.WriteLine("Subject,Speed,Fold,Correct?,NumTrain,Tested,Character,ActualComparisons,TotalComparisons,Is1D,1stCorrect,Pts,Ms,NumStrokes,RecoDuration,Angle,:,(NBestNames),[NBestScores]");

                // Cross-Validation Procedure:
                // for 1 to n folds (divide data into folds)
                //   for each category c
                //     choose n / c.numExamples (preserving equal numbers of samples per symbol per user in each fold)
                //     add all to fold f
                // for each fold f in folds
                //   train $N with iterative numbers of training examples per symbol randomly chosen from all folds != f
                //   test with all samples in fold f
                //   write out test results for each test to recWriter (add column for fold)
                // write out cumulative results for all tests to mainWriter

                // for now do this within-user:
                foreach (KeyValuePair<string, Dictionary<string, Category>> user in categoriesByUser)
                {
                    Console.Write(".");
                    string speed = "unknown"; // TODO: get this from the new object later

                    int numFolds = 5;
                    List<SamplesCollection> allFolds = new List<SamplesCollection>(numFolds);
                    //List<List<Multistroke>> allFolds = new List<List<Multistroke>>(numFolds);

                    // division of data into folds
                    // divide the user's samples by 5, evenly from each symbol (category)
                    foreach (KeyValuePair<string, Category> cat in user.Value)
                    {
                        Category c = (Category)cat.Value;
                        List<int> usedIndices = new List<int>(); //int[c.NumExamples - 1];
                        for (int f = 0; f < numFolds; f++)
                        {
                            allFolds.Add(new SamplesCollection());

                            // choose over the whole range of not chosen examples for this user/symbol pair
                            int n = c.NumExamples / numFolds;
                            for (int i = 0; i < n; i++)
                            {
                                int chosen = Utils.Random(0, c.NumExamples - 1); // select a number between 0 and c.NumExamples
                                while (usedIndices.Contains(chosen))
                                {
                                    chosen = Utils.Random(0, c.NumExamples - 1); // select a number between 0 and c.NumExamples
                                }
                                Multistroke p = c[chosen]; // get the prototype from this category at chosen-th index
                                allFolds[f].AddExample(p); // add this example to the f fold
                                usedIndices.Add(chosen);
                            }
                        }
                    }  

                    // training / testing
                    for (int f = 0; f < numFolds; f++)
                    {
                        // train iteratively on all data in folds minus f
                        int minNumExamples = allFolds[f].GetMinNumExamplesForUser(user.Key) * (numFolds - 1);
                        double totalTests = minNumExamples * allFolds[f].GetTotalNumExamples() * numFolds;

                        //
                        // next loop: trains on all remaining data in other folds
                        //
                        for (int n = 1; n <= minNumExamples; n++)
                        {
                            // storage for the final avg results for each category for this N
                            Hashtable results = new Hashtable();
                            _gestures.Clear(); // clear any (old) loaded prototypes

                            // load (train on) N randomly selected gestures in each category
                            // do this for this user only
                            // make a temporary list of all possible training examples in folds minus f
                            SamplesCollection trainingPool = new SamplesCollection();
                            for (int p = 0; p < allFolds.Count; p++)
                            {
                                if (p != f)
                                {
                                    foreach (KeyValuePair<string, Dictionary<string, Category>> u1 in allFolds[p])
                                    {
                                        foreach (KeyValuePair<string, Category> cat in u1.Value)
                                        {
                                            Category c = (Category)cat.Value;  // the category to load N examples for
                                            for (int q = 0; q < c.NumExamples; q++)
                                            {
                                                trainingPool.AddExample(c[q]);
                                            }
                                        }
                                    }
                                }
                            }

                            // get n random examples of each category
                            foreach (KeyValuePair<string, Dictionary<string, Category>> u1 in trainingPool)
                            {
                                foreach (KeyValuePair<string, Category> cat in u1.Value)
                                {
                                    Category c = (Category)cat.Value;  // the category to load N examples for
                                    // choose over the whole range of examples for this user/symbol pair, Lisa 1/5/2008
                                    int[] chosen = Utils.Random(0, c.NumExamples - 1, n); // select N unique indices
                                    for (int j = 0; j < chosen.Length; j++)
                                    {
                                        Multistroke ms = c[chosen[j]]; // get the prototype from this category at chosen[j], Lisa 1/5/2008
                                        _gestures.Add(ms.Name, ms); // load the randomly selected test gestures into the recognizer
                                    }
                                }
                            }
                            
                            // testing loop: for all samples in allFolds[f]
                            int numTests = 0;
                            foreach (KeyValuePair<string, Dictionary<string, Category>> u1 in allFolds[f])
                            {
                                foreach (KeyValuePair<string, Category> cat in u1.Value)
                                {
                                    Category c = (Category)cat.Value;  // the category to load N examples for
                                    for (int q = 0; q < c.NumExamples; q++)
                                    {
                                        Multistroke ms = c[q];
                                        Gesture p = ms.OriginalGesture; // we only test on the original Gesture in the Multistroke, Lisa 1/5/2008
                                        Debug.Assert(!_gestures.ContainsKey(p.Name));

                                        // do the recognition!
                                        // record time before reco
                                        int recoStart = Environment.TickCount;
                                        NBestList result = null;
                                        if (NDollarParameters.Instance.RecognizerName == NDollarParameters.PossibleRecognizers.NDOLLAR)
                                            result = this.Recognize(p.RawPoints, ms.NumStrokes);
                                        else if (NDollarParameters.Instance.RecognizerName == NDollarParameters.PossibleRecognizers.TABLETPC)
                                            result = this.RecognizeInk(p.RawPoints, ms.NumStrokes);
                                        else throw new Exception("Bad Recognizer Name in config.xml");
                                        // record time after reco
                                        int recoEnd = Environment.TickCount;
                                        int recoDuration = recoEnd - recoStart; // in milliseconds
                                        string category = Category.ParseName(result.Name);
                                        int correct = (Category.ParseName(p.Name) == category) ? 1 : 0;

                                        speed = ms.Speed;
                                        recWriter.WriteLine("{0},{1},{2},{3},{4},'{5},'{6},{7},{8},{9},{10},{11},{12},{13},{14},{15:F1}{16},:,({17}),[{18}]",
                                            ms.User,                            // 0 Subject
                                            ms.Speed,                           // 1 Speed
                                            f,                                  // 2 Fold
                                            correct,                            // 3 Correct?
                                            n,                                  // 4 NumTrain 
                                            p.Name,                             // 5 Tested 
                                            Category.ParseName(p.Name),         // 6 Character
                                            result.getActualComparisons(),      // 7 ActualComparisons
                                            result.getTotalComparisons(),       // 8 TotalComparisons
                                            p.Is1D,                             // 9 Is1D
                                            FirstCorrect(p.Name, result.Names), // 10 1stCorrect
                                            p.RawPoints.Count,                  // 11 Pts
                                            p.Duration,                         // 12 Ms 
                                            ms.NumStrokes,                      // 13 number of strokes
                                            recoDuration,                       // 14 recognition duration (millis)
                                            Math.Round(result.Angle, 1), (char)176, // 15/16 Angle tweaking :
                                            result.NamesString,                 // 17 (NBestNames)
                                            result.ScoresString);               // 18 [NBestScores]

                                        // c is a Category object, unique to user/category pair
                                        // use the category NAME to store the results
                                        // Lisa 1/6/2008
                                        if (results.ContainsKey(Category.ParseName(p.Name)))
                                        {
                                            double temp = (double)results[Category.ParseName(p.Name)] + (correct / c.NumExamples); // keep the normalized version around from the beginning
                                            results[Category.ParseName(p.Name)] = temp;
                                        }
                                        else
                                        {
                                            results.Add(Category.ParseName(p.Name), (double)correct);
                                        }

                                        // provide feedback as to how many tests have been performed thus far.                            
                                        numTests++;
                                        double testsSoFar = (f * minNumExamples * allFolds[f].GetTotalNumExamples()) + ((n - 1) * allFolds[f].GetTotalNumExamples()) + numTests;
                                        if (ProgressChangedEvent != null)
                                            ProgressChangedEvent(this, new ProgressEventArgs(testsSoFar / totalTests)); // callback
                                    }
                                }
                            }

                            //
                            // now create the final results for this user and this N and write them to a file
                            //
                            foreach (KeyValuePair<string, Category> cat in user.Value)
                            {
                                // Subject Recognizer Speed NumTraining GestureType RecognitionRate
                                mainWriter.WriteLine("ndollar,{0},{1},{2},{3},{4},{5:F3}", user.Key, speed, f, n, cat.Key, Math.Round((double)results[cat.Key], 3));
                            }
                        }
                    }
                }
                // time-stamp the end of the processing when it's allll done
                int end = Environment.TickCount;
                mainWriter.WriteLine("\nEndTime(ms):,{0}, Minutes:,{1:F2}", end, Math.Round((end - start) / 60000.0, 2));
                recWriter.WriteLine("\nEndTime(ms):,{0}, Minutes:,{1:F2}", end, Math.Round((end - start) / 60000.0, 2));
                Console.WriteLine();
                Console.WriteLine("Done testing batch.");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.Write(ex.StackTrace);
                Console.WriteLine();
                success = false;
            }
            finally
            {
                if (mainWriter != null)
                    mainWriter.Close();
                if (recWriter != null)
                    recWriter.Close();
            }
            return success;
        }

        public bool TestBatch_UserIndependent_100Random(SamplesCollection categoriesByUser, string dir)
        {
            Console.Write("Testing batch (one tick per user)");
            bool success = true;
            StreamWriter mainWriter = null;
            StreamWriter recWriter = null;
            try
            {
                //
                // set up a main results file and detailed recognition results file
                //
                int start = Environment.TickCount;
                string mainFile = String.Format("{0}\\ndollar_main_{1}.csv", dir, start);
                string recFile = String.Format("{0}\\ndollar_data_{1}.csv", dir, start);

                mainWriter = new StreamWriter(mainFile, false);//, Encoding.UTF8);
                mainWriter.WriteLine("Recognizer:,{0}, StartTime(ms):,{1}\n", NDollarParameters.Instance.RecognizerName, start);
                mainWriter.WriteLine("Testing:,within-user,Matching method:,{0},Rotation invariance:,{1},Rotation bound:,{2},1D Threshold:,{3},Do start angle comparison:,{4},Start angle index:,{5},Start angle threshold:,{6},Do match only same number of strokes:,{7},Test for 1D gestures:,{8},UseUniformScaling:,{9}\n",
                    (NDollarParameters.Instance.SearchMethod == NDollarParameters.PossibleSearchMethods.GSS) ? "GSS" : "Protractor",
                    NDollarParameters.Instance.RotationInvariant,
                    _RotationBound,
                    _1DThreshold,
                    NDollarParameters.Instance.DoStartAngleComparison,
                    NDollarParameters.Instance.StartAngleIndex,
                    Utils.Rad2Deg(NDollarParameters.Instance.StartAngleThreshold),
                    NDollarParameters.Instance.MatchOnlyIfSameNumberOfStrokes,
                    NDollarParameters.Instance.TestFor1D,
                    NDollarParameters.Instance.UseUniformScaling);
                mainWriter.WriteLine("Recognizer,NumTrainingUsers,Speed,NumTrainingSamples,GestureType,RecognitionRate");

                recWriter = new StreamWriter(recFile, false);//, Encoding.UTF8);
                recWriter.WriteLine("Recognizer:,{0}, StartTime(ms):,{1}\n", NDollarParameters.Instance.RecognizerName, start);
                //recWriter.WriteLine("Testing:,within-user,Rotation invariance:,{0},Rotation bound:,{1},1D Threshold:,{2},Do start angle comparison:,{3},Start angle index:,{4},Start angle threshold:,{5},Do match only same number of strokes:,{6},Test for 1D gestures:,{7},UseUniformScaling:,{8}\n",
                recWriter.WriteLine("Testing:,within-user,Matching method:,{0},Rotation invariance:,{1},Rotation bound:,{2},1D Threshold:,{3},Do start angle comparison:,{4},Start angle index:,{5},Start angle threshold:,{6},Do match only same number of strokes:,{7},Test for 1D gestures:,{8},UseUniformScaling:,{9}\n",
                    (NDollarParameters.Instance.SearchMethod == NDollarParameters.PossibleSearchMethods.GSS) ? "GSS" : "Protractor",
                    NDollarParameters.Instance.RotationInvariant,
                    _RotationBound,
                    _1DThreshold,
                    NDollarParameters.Instance.DoStartAngleComparison,
                    NDollarParameters.Instance.StartAngleIndex,
                    Utils.Rad2Deg(NDollarParameters.Instance.StartAngleThreshold),
                    NDollarParameters.Instance.MatchOnlyIfSameNumberOfStrokes,
                    NDollarParameters.Instance.TestFor1D,
                    NDollarParameters.Instance.UseUniformScaling);
                recWriter.WriteLine("Subject,Speed,NumTrainingUsers,Correct?,NumTrainingSamples,Tested,Character,ActualComparisons,TotalComparisons,Is1D,1stCorrect,Pts,Ms,NumStrokes,RecoDuration,Angle,:,(NBestNames),[NBestScores]");

                // procedure for user-independent testing
                // copies from Radu's $P procedure and adapted for $N SamplesCollection representation
                // Vary NT from 1..9 (NUM_PARTICIPANTS)
                //   - Repeat the following 100 times: (REPETITIONS)
                //     - Random select NT participants for training
                //     - Random select 1 participant different from NT for testing
                //     - Vary T from 1..9 (numberOfTrainingSamples)
                //       - Repeat the following 100 times: (REPETITIONS)
                //         - Build training set (random select T samples from each NT)
                //         - Build testing set (random select 1 sample from testing participant)
                //         - Compute recognition rate for each metric

                int NUM_PARTICIPANTS = categoriesByUser.GetUsersList().Count;
                int REPETITIONS = 10;

                for (int numParticipantsUsedForTraining = 1; numParticipantsUsedForTraining <= NUM_PARTICIPANTS - 1; numParticipantsUsedForTraining++)
                {
                    Console.Write(".");
                    string speed = "unknown"; // TODO: get this from the new object later

                    //
                    // determine the maximum number of gesture categories and the 
                    // minimum number of examples per category for this specific user
                    //
                    double totalTestsThisRoundNT = (numberOfTrainingSamples[numberOfTrainingSamples.Length - 1] - 1) * REPETITIONS;

                    // repeat selection of participants for training REPETITIONS times
                    for (int repetitionSelection = 0; repetitionSelection < REPETITIONS; repetitionSelection++)
                    {
                        // random select training participants and 1 testing participant
                        List<int> trainingParticipants = new List<int>();
                        int testingParticipant = -1;
                        SelectParticipantsForTrainingAndTesting(NUM_PARTICIPANTS, numParticipantsUsedForTraining, ref trainingParticipants, ref testingParticipant);

                        // test on different sizes for the training set
                        foreach (int T in numberOfTrainingSamples) // TODO: make this general for datasets without equal #s of samples and do all 1..T
                        {
                            // storage for the final avg results for each category for this NT, T
                            Hashtable results = new Hashtable();

                            // repeat classification REPETITIONS times
                            for (int repetitionClassification = 0; repetitionClassification < REPETITIONS; repetitionClassification++)
                            {
                                _gestures.Clear(); // clear any (old) loaded prototypes

                                // build training and testing sets
                                List<string> allUsers = categoriesByUser.GetUsersList();
                                // select T samples per category (symbol) for each user in the training set and load them
                                foreach (int indexParticipant in trainingParticipants)
                                {
                                    foreach (KeyValuePair<string, Category> cat in categoriesByUser[allUsers[indexParticipant]])
                                    {
                                        Category c = (Category)cat.Value;  // the category to load N examples for
                                        // choose over the whole range of examples for this user/symbol pair, Lisa 1/5/2008
                                        int[] chosen = Utils.Random(0, c.NumExamples - 1, T); // select N unique indices
                                        for (int j = 0; j < chosen.Length; j++)
                                        {
                                            Multistroke p = c[chosen[j]]; // get the prototype from this category at chosen[j], Lisa 1/5/2008
                                            _gestures.Add(p.Name, p); // load the randomly selected test gestures into the recognizer
                                        }
                                    }
                                }
                                // training set is now made and loaded

                                // select 1 sample per category (symbol) for the user in the testing set
                                foreach (KeyValuePair<string, Category> cat in categoriesByUser[allUsers[testingParticipant]])
                                {
                                    //Console.WriteLine("c = " + cat.Value.Name);
                                    Category c = (Category)cat.Value;  // the category to load N examples for
                                    // choose over the whole range of examples for this user/symbol pair, Lisa 1/5/2008
                                    int chosen = Utils.Random(0, c.NumExamples - 1); // select 1 unique indices to test
                                    Multistroke ms = c[chosen]; // gesture to test
                                    Gesture p = ms.OriginalGesture; // we only test on the original Gesture in the Multistroke, Lisa 1/5/2008
                                    //Debug.Assert(!_gestures.ContainsKey(p.Name)); // TODO: re-add assert but add username to name of gesture to keep them unique

                                    // test this gesture and write the output
                                    // do the recognition!
                                    // record time before reco
                                    int recoStart = Environment.TickCount;
                                    //Console.WriteLine("file we are about to recognize: " + p.Name);
                                    NBestList result = null;
                                    if (NDollarParameters.Instance.RecognizerName == NDollarParameters.PossibleRecognizers.NDOLLAR)
                                        result = this.Recognize(p.RawPoints, ms.NumStrokes);
                                    else if (NDollarParameters.Instance.RecognizerName == NDollarParameters.PossibleRecognizers.TABLETPC)
                                        result = this.RecognizeInk(p.RawPoints, ms.NumStrokes);
                                    else throw new Exception("Bad Recognizer Name in config.xml");
                                    // record time after reco
                                    int recoEnd = Environment.TickCount;
                                    int recoDuration = recoEnd - recoStart; // in milliseconds
                                    string category = Category.ParseName(result.Name);
                                    int correct = (c.Name == category) ? 1 : 0;

                                    speed = ms.Speed;
                                    recWriter.WriteLine("{0},{1},{2},{3},{4},'{5},'{6},{7},{8},{9},{10},{11},{12},{13},{14},{15:F1}{16},:,({17}),[{18}]",
                                        ms.User,                            // 0 Subject
                                        ms.Speed,                           // 1 Speed
                                        numParticipantsUsedForTraining,     // 2 NumTrainingUsers
                                        correct,                            // 3 Correct?
                                        T,                                  // 4 NumTrainingSamples
                                        p.Name,                             // 5 Tested 
                                        Category.ParseName(p.Name),         // 6 Character
                                        result.getActualComparisons(),      // 7 ActualComparisons
                                        result.getTotalComparisons(),       // 8 TotalComparisons
                                        p.Is1D,                             // 9 Is1D
                                        FirstCorrect(p.Name, result.Names), // 10 1stCorrect
                                        p.RawPoints.Count,                  // 11 Pts
                                        p.Duration,                         // 12 Ms 
                                        ms.NumStrokes,                      // 13 number of strokes
                                        recoDuration,                       // 14 recognition duration (millis)
                                        Math.Round(result.Angle, 1), (char)176, // 15/16 Angle tweaking :
                                        result.NamesString,                 // 17 (NBestNames)
                                        result.ScoresString);               // 18 [NBestScores]

                                    // c is a Category object, unique to user/category pair
                                    // use the category NAME to store the results
                                    // Lisa 1/6/2008
                                    if (results.ContainsKey(cat.Key))
                                    {
                                        double temp = (double)results[cat.Key] + correct;
                                        results[cat.Key] = temp;
                                    }
                                    else
                                    {
                                        results.Add(cat.Key, (double)correct);
                                    }

                                }
                                // testing set has now been recognized

                                // provide feedback as to how many tests have been performed thus far.
                                double testsSoFar = ((T - 1) * REPETITIONS) + repetitionClassification;
                                if (ProgressChangedEvent != null)
                                    ProgressChangedEvent(this, new ProgressEventArgs(testsSoFar / totalTestsThisRoundNT)); // callback

                            }
                            //
                            // now create the final results for this NT and this N and write them to a file
                            //
                            List<string> tmp = categoriesByUser.GetCategoryNames();
                            foreach (string cat in categoriesByUser.GetCategoryNames())
                            {
                                double temp = (double)results[cat] / ((double)REPETITIONS * 1); // normalize by the number of tests at this N, Lisa 1/5/2008
                                results[cat] = temp;
                                // Subject Recognizer Speed NumTraining GestureType RecognitionRate
                                mainWriter.WriteLine("ndollar,{0},{1},{2},{3},{4:F3}", numParticipantsUsedForTraining, speed, T, cat, Math.Round((double)results[cat], 3));
                            }
                        }
                    }
                }

                // time-stamp the end of the processing when it's allll done
                int end = Environment.TickCount;
                mainWriter.WriteLine("\nEndTime(ms):,{0}, Minutes:,{1:F2}", end, Math.Round((end - start) / 60000.0, 2));
                recWriter.WriteLine("\nEndTime(ms):,{0}, Minutes:,{1:F2}", end, Math.Round((end - start) / 60000.0, 2));
                Console.WriteLine();
                Console.WriteLine("Done testing batch.");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.Write(ex.StackTrace);
                Console.WriteLine();
                Console.ReadLine();
                success = false;
            }
            finally
            {
                if (mainWriter != null)
                    mainWriter.Close();
                if (recWriter != null)
                    recWriter.Close();
            }
            return success;
        }

        /// <summary>
        /// Splits all the participants (allParticipants) into training and testing such that
        /// T participants will be used for training and one participant for testing
        /// </summary>
        private void SelectParticipantsForTrainingAndTesting(int numParticipants, int T, ref List<int> trainingParticipants, ref int testingParticipant)
        {
            // random select participants for training
            int[] indexes = Utils.Random(0, numParticipants - 1, T + 1); // does same thing as Radu's SelectRandomIntegers?
            trainingParticipants = new List<int>();
            for (int i = 0; i < indexes.Length - 1; i++)
                trainingParticipants.Add(indexes[i]);

            // random select one participant for testing
            testingParticipant = indexes[indexes.Length - 1];
        }

        #endregion

        #region Rotation Graph

        public bool CreateRotationGraph(string file1, string file2, string dir, bool similar)
        {
            bool success = true;
            StreamWriter writer = null;
            XmlTextReader reader = null;
            try
            {
                // read gesture file #1
                reader = new XmlTextReader(file1);
                reader.WhitespaceHandling = WhitespaceHandling.None;
                reader.MoveToContent();
                Multistroke g1 = ReadGesture(reader); // Lisa 1/2/2008
                reader.Close();

                // read gesture file #2
                reader = new XmlTextReader(file2);
                reader.WhitespaceHandling = WhitespaceHandling.None;
                reader.MoveToContent();
                Multistroke g2 = ReadGesture(reader); // Lisa 1/2/2008

                // create output file for results
                string outfile = String.Format("{0}\\{1}({2}, {3})_{4}.txt", dir, similar ? "o" : "x", g1.Name, g2.Name, Environment.TickCount);
                writer = new StreamWriter(outfile, false, Encoding.UTF8);
                writer.WriteLine("Rotated: {0} --> {1}. {2}, {3}\n", g1.Name, g2.Name, DateTime.Now.ToLongDateString(), DateTime.Now.ToLongTimeString());

                // do the full 360 degree rotations
                double[] full = FullSearch(g1.OriginalGesture.Points, g2.OriginalGesture.Points, writer); // Lisa 1/2/2008

                // use bidirectional hill climbing to do it again
                double init = Utils.PathDistance(g1.OriginalGesture.Points, g2.OriginalGesture.Points); // initial distance  // Lisa 1/2/2008
                double[] pos = HillClimbSearch(g1.OriginalGesture.Points, g2.OriginalGesture.Points, init, 1d); // Lisa 1/2/2008
                double[] neg = HillClimbSearch(g1.OriginalGesture.Points, g2.OriginalGesture.Points, init, -1d); // Lisa 1/2/2008
                double[] best = new double[3];
                best = (neg[0] < pos[0]) ? neg : pos; // min distance
                writer.WriteLine("\nHill Climb Search ({0} rotations)\n{1:F2}{2}\t{3:F3} px", pos[2] + neg[2] + 1, Math.Round(best[1], 2), (char) 176, Math.Round(best[0], 3)); // calls, angle, distance

                // use golden section search to do it yet again
                double[] gold = GoldenSectionSearch(
                    g1.OriginalGesture.Points,              // to rotate   // Lisa 1/2/2008
                    g2.OriginalGesture.Points,              // to match    // Lisa 1/2/2008
                    Utils.Deg2Rad(-_RotationBound),   // lbound   // Lisa 1/2/2008
                    Utils.Deg2Rad(+_RotationBound),   // ubound   // Lisa 1/2/2008
                    Utils.Deg2Rad(2.0));    // threshold
                writer.WriteLine("\nGolden Section Search ({0} rotations)\n{1:F2}{2}\t{3:F3} px", gold[2], Math.Round(gold[1], 2), (char) 176, Math.Round(gold[0], 3)); // calls, angle, distance

                // for pasting into Excel
                writer.WriteLine("\n{0} {1} {2:F2} {3:F2} {4:F3} {5:F3} {6} {7:F2} {8:F2} {9:F3} {10} {11:F2} {12:F2} {13:F3} {14}",
                    g1.Name,                    // rotated
                    g2.Name,                    // into
                    Math.Abs(Math.Round(full[1], 2)), // |angle|
                    Math.Round(full[1], 2),     // Full Search angle
                    Math.Round(full[0], 3),     // Full Search distance
                    Math.Round(init, 3),        // Initial distance w/o any search
                    full[2],                    // Full Search iterations
                    Math.Abs(Math.Round(best[1], 2)), // |angle|
                    Math.Round(best[1], 2),     // Bidirectional Hill Climb Search angle
                    Math.Round(best[0], 3),     // Bidirectional Hill Climb Search distance
                    pos[2] + neg[2] + 1,        // Bidirectional Hill Climb Search iterations
                    Math.Abs(Math.Round(gold[1], 2)), // |angle|
                    Math.Round(gold[1], 2),     // Golden Section Search angle
                    Math.Round(gold[0], 3),     // Golden Section Search distance
                    gold[2]);                   // Golden Section Search iterations
            }
            catch (XmlException xml)
            {
                Console.WriteLine(xml.Message);
                Console.Write(xml.StackTrace);
                Console.WriteLine();
                success = false;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.Write(ex.StackTrace);
                Console.WriteLine();
                success = false;
            }
            finally
            {
                if (reader != null)
                    reader.Close();
                if (writer != null)
                    writer.Close();
            }
            return success;
        }

        #endregion
    }
}
