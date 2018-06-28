/**
 * The $N Multistroke Recognizer (C# version)
 *
 *		Lisa Anthony
 *		Lockheed Martin
 *		Advanced Technology Laboratories
 * 		3 Executive Campus, Suite 600
 *		Cherry Hill, NJ 08002
 * 		lanthony@atl.lmco.com
 * 
 *              Jacob O. Wobbrock
 * 		The Information School
 *		University of Washington
 *		Mary Gates Hall, Box 352840
 *		Seattle, WA 98195-2840
 *		wobbrock@u.washington.edu
 *
 *
 * This software is distributed under the "New BSD License" agreement:
 * 
 * Copyright (c) 2007-2010, Lisa Anthony and Jacob O. Wobbrock
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *    * Redistributions of source code must retain the above copyright
 *      notice, this list of conditions and the following disclaimer.
 *    * Redistributions in binary form must reproduce the above copyright
 *      notice, this list of conditions and the following disclaimer in the
 *      documentation and/or other materials provided with the distribution.
 *    * Neither the name of the University of Washington or Lockheed Martin,
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

// This has not changed at all from $1.

using System;
using System.Drawing;

namespace Recognizer.NDollar
{
	public struct SizeR
	{
		public static readonly SizeR Empty;
        private double _cx;
		private double _cy;

		public SizeR(double cx, double cy)
		{
			_cx = cx;
			_cy = cy;
		}

		// copy constructor
		public SizeR(SizeR sz)
		{
			_cx = sz.Width;
			_cy = sz.Height;
		}

		public double Width
		{
			get
			{
                return _cx;
			}
			set
			{
				_cx = value;
			}
		}

		public double Height
		{
			get
			{
                return _cy;
			}
			set
			{
				_cy = value;
			}
		}

		public static explicit operator SizeF(SizeR sz)
		{
			return new SizeF((float) sz.Width, (float) sz.Height);
		}

		public static bool operator==(SizeR sz1, SizeR sz2)
		{
			return (sz1.Width == sz2.Width && sz1.Height == sz2.Height);
		}

		public static bool operator!=(SizeR sz1, SizeR sz2)
		{
			return (sz1.Width != sz2.Width || sz1.Height != sz2.Height);
		}

		public override bool Equals(object obj)
		{
			if (obj is SizeR)
			{
				SizeR sz = (SizeR) obj;
				return (Width == sz.Width && Height == sz.Height);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return ((SizeR) this).GetHashCode();
		}
	}
}
