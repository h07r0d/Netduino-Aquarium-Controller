using System;
using System.Text;
using Microsoft.SPOT;
using System.Collections;

public class HttpGeneral
{
    //HTTP/1.1 200 OK
    //Date: Sun, 07 Aug 2011 06:01:05 GMT
    //Server: Apache/2
    //Last-Modified: Wed, 01 Sep 2004 13:24:52 GMT
    //ETag: ""805b-3e3073913b100""
    //Accept-Ranges: bytes
    //Cache-Control: max-age=21600
    //Expires: Sun, 07 Aug 2011 12:01:05 GMT
    //Vary: Accept-Encoding
    //Content-Encoding: gzip
    //P3P: policyref=""http://www.w3.org/2001/05/P3P/p3p.xml""
    //Content-Length: 5605
    //Connection: close
    //Content-Type: text/html; charset=iso-8859-1

    public static string GetHttpHeader(long length, string type, int maxAge)
    {
        string header = "HTTP/1.1 200 OK\r\nContent-Type: " + type + "; charset=utf-8\r\nAccess-Control-Allow-Origin: *\r\nCache-Control: max-age=" + maxAge.ToString() + ", must-revalidate\r\nContent-Length: " + length.ToString() + "\r\nConnection: close\r\n\r\n";
        return header;
    }

    
    /// <summary>
    /// Send a Not Found response
    /// </summary>
    public static string Get404Header(long contentLength)
    {
        Debug.Print("Sending 404 Not Found");
        string headerString = "HTTP/1.1 404 Not Found\r\nConnection: close\r\n\r\n";
        return headerString;
    }

    /// <summary>
    /// Send a Server Error 500 response
    /// </summary>
    public static string Get500Header(long contentLength)
    {
        Debug.Print("Sending 500 server error.");
        string headerString = "HTTP/1.1 500 server error.\r\nContent-Length: " + contentLength.ToString() + "\r\nConnection: close\r\n\r\n";
        return headerString;
    }


	public static Hashtable ParseQuerystring(string _uri, out string _baseUri)
	{
		_baseUri = _uri;
		Hashtable result = new Hashtable();
		if (_uri != null)
		{
			string[] split = _uri.Split('?');
			if (split.Length > 1)
			{
				_baseUri = split[0];
				string partQueryString = split[1];
				result = GetEncodedVariables(partQueryString);
			}
		}
		return result;
	}

	private static Hashtable GetEncodedVariables(string variableString)
	{
		Hashtable result = new Hashtable();
		if (variableString != null)
		{
			string[] split = variableString.Split('&');
			if (split.Length > 0)
			{// remove querystring ?
				split[0] = split[0].TrimStart(new char[] { '?', ' ' });
			}
			for (int i = 0; i < split.Length; i++)
			{
				string[] parameterArray = split[i].Split('=');

				string value = "";
				if (parameterArray.Length > 1)
				{
					value = parameterArray[1].Trim();
					value = UriDecode(value);
				}
				string key = parameterArray[0].Trim();
				if (!result.Contains(key))
				{
					result.Add(key, value);
				}
			}
		}
		return result;
	}

    private static string UriDecode(string s)
    {
        if (s == null) return null;
        if (s.Length < 1) return s;

        char[] chars = s.ToCharArray();
        byte[] bytes = new byte[chars.Length * 2];
        int count = chars.Length;
        int dstIndex = 0;
        int srcIndex = 0;

        while (true)
        {
            if (srcIndex >= count)
            {
                if (dstIndex < srcIndex)
                {
                    byte[] sizedBytes = new byte[dstIndex];
                    Array.Copy(bytes, 0, sizedBytes, 0, dstIndex);
                    bytes = sizedBytes;
                }
                return new string(Encoding.UTF8.GetChars(bytes));
            }

            if (chars[srcIndex] == '+')
            {
                bytes[dstIndex++] = (byte)' ';
                srcIndex += 1;
            }
            else if (chars[srcIndex] == '%' && srcIndex < count - 2)
                if (chars[srcIndex + 1] == 'u' && srcIndex < count - 5)
                {
                    int ch1 = HexToInt(chars[srcIndex + 2]);
                    int ch2 = HexToInt(chars[srcIndex + 3]);
                    int ch3 = HexToInt(chars[srcIndex + 4]);
                    int ch4 = HexToInt(chars[srcIndex + 5]);

                    if (ch1 >= 0 && ch2 >= 0 && ch3 >= 0 && ch4 >= 0)
                    {
                        bytes[dstIndex++] = (byte)((ch1 << 4) | ch2);
                        bytes[dstIndex++] = (byte)((ch3 << 4) | ch4);
                        srcIndex += 6;
                        continue;
                    }
                }
                else
                {
                    int ch1 = HexToInt(chars[srcIndex + 1]);
                    int ch2 = HexToInt(chars[srcIndex + 2]);

                    if (ch1 >= 0 && ch2 >= 0)
                    {
                        bytes[dstIndex++] = (byte)((ch1 << 4) | ch2);
                        srcIndex += 3;
                        continue;
                    }
                }
            else
            {
                byte[] charBytes = Encoding.UTF8.GetBytes(chars[srcIndex++].ToString());
                charBytes.CopyTo(bytes, dstIndex);
                dstIndex += charBytes.Length;
            }
        }
    }

    private static int HexToInt(char ch)
    {
        return
        (ch >= '0' && ch <= '9') ? ch - '0' :
        (ch >= 'a' && ch <= 'f') ? ch - 'a' + 10 :
        (ch >= 'A' && ch <= 'F') ? ch - 'A' + 10 :
        -1;
    }
}
