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

// Added pressure to PointR.

using System;
using System.Drawing;

namespace Recognizer.NDollar
{
	public struct PointR
	{
        public static readonly PointR Empty;
        public double X, Y;
        public long T;
        public double P;

		public PointR(double x, double y) : this(x, y, 0)
		{
		}

        public PointR(double x, double y, long t)
        {
            X = x;
            Y = y;
            T = t;
            P = -1;
        }

        public PointR(double x, double y, long t, double p)
        {
            X = x;
            Y = y;
            T = t;
            P = p;
        }

		// copy constructor
		public PointR(PointR p)
		{
            X = p.X;
            Y = p.Y;
            T = p.T;
            P = p.P;
        }

		public static explicit operator PointF(PointR p)
		{
			return new PointF((float) p.X, (float) p.Y);
		}

		public static bool operator==(PointR p1, PointR p2)
		{
			return (p1.X == p2.X && p1.Y == p2.Y);
		}

		public static bool operator!=(PointR p1, PointR p2)
		{
			return (p1.X != p2.X || p1.Y != p2.Y);
		}

		public override bool Equals(object obj)
		{
			if (obj is PointR)
			{
				PointR p = (PointR) obj;
				return (X == p.X && Y == p.Y);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return ((PointF) this).GetHashCode();
		}
	}
}
