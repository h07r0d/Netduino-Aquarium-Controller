using System;

namespace Controller
{
	public static class Math
	{
		/// <summary>
		/// Calculate logaritmic value from value with given base
		/// </summary>
		/// <param name="x">a Number</param>
		/// <param name="newBase">Base to use</param>
		/// <returns>Logaritmic of x</returns>
		public static double Log(double x)
		{
			// Based on Python sourcecode from:
			// http://en.literateprograms.org/Logarithm_Function_%28Python%29

			double partial = 0.5;
			double newBase = 10;
			double integer = 0;
			double fractional = 0.0;
			double epsilon = 2.22045e-16;

			if (x == 0.0) return double.NegativeInfinity;
			if ((x < 1.0) & (newBase < 1.0)) throw new ArgumentOutOfRangeException("can't compute Log");

			while (x < 1.0)
			{
				integer -= 1;
				x *= newBase;
			}

			while (x >= newBase)
			{
				integer += 1;
				x /= newBase;
			}

			x *= x;

			while (partial >= epsilon)
			{
				if (x >= newBase)
				{
					fractional += partial;
					x = x / newBase;
				}
				partial *= 0.5;
				x *= x;
			}

			return (integer + fractional);
		}

	}
}
