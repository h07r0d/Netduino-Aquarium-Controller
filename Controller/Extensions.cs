using System;
using System.Text;

namespace Controller
{
	public static class StringExtensions
	{
		public static bool Contains(this String _src, string _search)
		{
			for (int i = 0; i < _src.Length; i++)
			{
				if (_src.IndexOf(_search) >= 0) { return true; }
			}
			return false;
		}

		public static bool Contains(this String _src, char _search)
		{
			for (int i = 0; i < _src.Length; i++)
			{
				if (_src.IndexOf(_search) >= 0) { return true; }
			}
			return false;
		}
	}

	public static class StringBuilderExtensions
	{
		public static byte[] ToBytes(this StringBuilder _sb)
		{
			return Encoding.UTF8.GetBytes(_sb.ToString());
		}
	}

	
	public static class IntExtensions
	{
		/// <summary>
		/// Takes a Decimal value and converts it into a Binary-Coded-Decimal value
		/// </summary>
		/// <param name="val">Value to be converted</param>
		/// <returns>A BCD-encoded value</returns>
		public static byte ToBcd(this int _val)
		{
			byte lowOrder = (byte)(_val % 10);
			byte highOrder = (byte)((_val / 10) << 4);
			return (byte)(highOrder | lowOrder);
		}
	}

	public static class ByteExtensions
	{
		/// <summary>
		/// Takes a Binary-Coded-Decimal value and returns it as an integer value
		/// </summary>
		/// <param name="val">BCD encoded value</param>
		/// <returns>An integer value</returns>
		public static int ToDecimal(this byte _val)
		{
			int ones = (_val & 0x0f);
			int tens = (_val & 0xf0) >> 4;
			return (tens * 10) + ones;
		}
	}
}
