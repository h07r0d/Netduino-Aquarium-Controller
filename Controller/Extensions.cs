using System;
using System.IO;
using System.Text;
using System.Net;
using System.Net.Sockets;
using Microsoft.SPOT.Hardware;
using Microsoft.SPOT;

namespace Controller
{
	public static class StringExtensions
	{
		public static bool Contains(this String _src, string _search)
		{
			for (int i = _src.Length-1; i >= 0; --i)
			{
				if (_src.IndexOf(_search) >= 0) { return true; }
			}
			return false;
		}

		public static bool Contains(this String _src, char _search)
		{
			for (int i = _src.Length-1; i >= 0; --i)
			{
				if (_src.IndexOf(_search) >= 0) { return true; }
			}
			return false;
		}

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
