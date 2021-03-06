using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.SPOT.Hardware;
using System.Collections;

namespace Extensions
{
	public static class Converter
	{
		public static Hashtable ToHashtable(string[] lines, string seperator, int startAtLine = 0)
		{
			Hashtable toReturn = new Hashtable();
			string[] line;
			for (int i = startAtLine; i < lines.Length; i++)
			{
				line = lines[i].EasySplit(seperator);
				if (line.Length > 1)
					toReturn.Add(line[0], line[1]);

			}
			return toReturn;
		}
	}


	/// <summary>
	/// Extensions for String Class
	/// </summary>
	public static class StringExtensions
	{
		public static string[] EasySplit(this string s, string seperator)
		{
			int pos = s.IndexOf(seperator);
			if (pos != -1)
			{
				return new string[] { 
					s.Substring(0, pos).Trim(new char[] { ' ', '\n', '\r' }), 
					s.Substring(pos + seperator.Length, 
					s.Length - pos - seperator.Length).Trim(new char[] { ' ', '\n', '\r' }) 
				};
			}
			else
				return new string[] { s.Trim(new char[] { ' ', '\n', '\r' }) };
		}


		/// <summary>
		/// Replace all occurances of the 'find' string with the 'replace' string.
		/// </summary>
		/// <param name="content">Original string to operate on</param>
		/// <param name="find">String to find within the original string</param>
		/// <param name="replace">String to be used in place of the find string</param>
		/// <returns>Final string after all instances have been replaced.</returns>
		public static string Replace(this String content, string find, string replace)
		{
			int startFrom = 0;
			int findItemLength = find.Length;

			int firstFound = content.IndexOf(find, startFrom);
			StringBuilder returning = new StringBuilder();

			string workingString = content;

			while ((firstFound = workingString.IndexOf(find, startFrom)) >= 0)
			{
				returning.Append(workingString.Substring(0, firstFound));
				returning.Append(replace);

				// the remaining part of the string.
				workingString = workingString.Substring(firstFound + findItemLength, workingString.Length - (firstFound + findItemLength));
			}
			returning.Append(workingString);
			return returning.ToString();
		}

		/// <summary>
		/// Does the string contain the text being searched for?
		/// </summary>
		/// <param name="_src">string to search for text in</param>
		/// <param name="_search">text to find</param>
		/// <returns>true if _search is found in _src</returns>
		public static bool Contains(this String _src, string _search)
		{
			for (int i = _src.Length-1; i >= 0; --i)
			{
				if (_src.IndexOf(_search) >= 0) { return true; }
			}
			return false;
		}

		/// <summary>
		/// Does the string contain the character being searched for?
		/// </summary>
		/// <param name="_src">string to search</param>
		/// <param name="_search">character to find</param>
		/// <returns>true if _search is found in _src</returns>
		public static bool Contains(this String _src, char _search)
		{
			for (int i = _src.Length-1; i >= 0; --i)
			{
				if (_src.IndexOf(_search) >= 0) { return true; }
			}
			return false;
		}

		/// <summary>
		/// Pad the left side of the string with a given number of a specific character
		/// </summary>
		/// <param name="_src">string to pad</param>
		/// <param name="_count">number of characters to add</param>
		/// <param name="_pad">character to use as padding</param>
		/// <returns>new string with padding added</returns>
		public static string PadLeft(this String _src, int _count, char _pad)
		{
			if (_src.Length >= _count)
				return _src;

			StringBuilder newString = new StringBuilder();
			for (int i = 0, length=_src.Length; i < _count-length; i++)
			{
				newString.Append(_pad);
			}
			newString.Append(_src);
			return newString.ToString();
		}
	}

	public static class StringBuilderExtensions
	{
		/// <summary>
		/// Get byte array directly from a StringBuilder
		/// </summary>
		/// <param name="_sb">StringBuilder to encode</param>
		/// <returns>UTF8 Encoded byte array</returns>
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

	public static class FileStreamExtensions
	{		
		public static void CopyTo(this FileStream _input, FileStream _output)
		{			
			byte[] buffer = new byte[1024];
			int read;
			while ((read = _input.Read(buffer, 0, 1024)) > 0)
				_output.Write(buffer, 0, read);
			
		}		
    }

	public static class Math
	{
		public static double Log(double x, double newBase)
		{
			// Based on Python sourcecode from:
			// http://en.literateprograms.org/Logarithm_Function_%28Python%29

			double partial = 0.5F;
			double integer = 0F;
			double fractional = 0.0F;

			double epsilon = 2.22045e-16;

			if (x == 0.0F) return double.NegativeInfinity;
			if ((x < 1.0F) & (newBase < 1.0F)) throw new ArgumentOutOfRangeException("can't compute Log");

			while (x < 1.0F)
			{
				integer -= 1F;
				x *= newBase;
			}

			while (x >= newBase)
			{
				integer += 1F;
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
				partial *= 0.5F;
				x *= x;
			}

			return (integer + fractional);
		}

		/// <summary>
		/// Returns the base 10 logarithm of a specified number. 
		/// </summary>
		/// <param name="x">a Number </param>
		/// <returns>Logaritmic of x</returns>
		public static double Log(double x)
		{
			return Log(x, 10F);
		}

		/// <summary>
		/// Performs Log calculations with float variables
		/// </summary>
		/// <param name="x">Number</param>
		/// <returns>Float Log of x</returns>
		public static float Log(float x)
		{
			double cast = (double)x;
			cast = Log(x, 10F);
			return (float)cast;
		}
	}


	public static class DateTimeExtensions
	{
		public static void SetFromNetwork(this DateTime dateTime, TimeSpan TimeZoneOffset)
		{
			// Based on http://weblogs.asp.net/mschwarz/archive/2008/03/09/wrong-datetime-on-net-micro-framework-devices.aspx
			// And http://nickstips.wordpress.com/2010/02/12/c-get-nist-internet-time/
			// Time server list: http://tf.nist.gov/tf-cgi/servers.cgi

			var ran = new Random(DateTime.Now.Millisecond);
			var servers = new string[] { "time-a.nist.gov", "time-b.nist.gov", "nist1-la.ustiming.org", "nist1-chi.ustiming.org", "nist1-ny.ustiming.org", "time-nw.nist.gov" };

			// Try each server in random order to avoid blocked requests due to too frequent request  
			for (int i = 0; i < servers.Length; i++)
			{
				try
				{
					// Open a Socket to a random time server  
					var ep = new IPEndPoint(Dns.GetHostEntry(servers[ran.Next(servers.Length)]).AddressList[0], 123);

					var s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
					//s.Connect(ep);

					byte[] ntpData = new byte[48]; // RFC 2030
					ntpData[0] = 0x1B;
					for (int x = 1; x < 48; x++)
						ntpData[x] = 0;

					//s.Send(ntpData);
					s.SendTo(ntpData, ep);
					s.Receive(ntpData);

					byte offsetTransmitTime = 40;
					ulong intpart = 0;
					ulong fractpart = 0;
					for (int x = 0; x <= 3; x++)
						intpart = 256 * intpart + ntpData[offsetTransmitTime + x];

					for (int x = 4; x <= 7; x++)
						fractpart = 256 * fractpart + ntpData[offsetTransmitTime + x];

					ulong milliseconds = (intpart * 1000 + (fractpart * 1000) / 0x100000000L);

					s.Close();

					if (ntpData[47] != 0)
					{
						TimeSpan timeSpan = TimeSpan.FromTicks((long)milliseconds * TimeSpan.TicksPerMillisecond);
						DateTime tempDateTime = new DateTime(1900, 1, 1);
						tempDateTime += timeSpan;
						DateTime networkDateTime = (tempDateTime + TimeZoneOffset);
						Utility.SetLocalTime(networkDateTime);
						break;
					}
				}
				catch (Exception) { /* Do Nothing...try the next server */ }			
			}
		}
	}
}
