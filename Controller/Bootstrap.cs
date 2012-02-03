using System;
using System.IO;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using System.Collections;

/**
 * Parts taken from ini4net.codeplex.com
 * Full code not pulled, as most functionality is not needed
 */

#region String Extension
public static class Strings
{
	public static bool Contains(string _src, string _search)
	{
		for (int i = 0; i < _src.Length; i++)
		{
			if (_src.IndexOf(_search) >= 0) { return true; }
		}
		return false;
	}
}
#endregion

namespace Controller
{

	#region Syntax Class
	internal class Syntax
	{
		public static readonly char Comment = '#';
		public static readonly char Seperator = '=';
		public static readonly char SectionStart = '[';
		public static readonly char SectionEnd = '[';
	}
	#endregion

	#region Exceptions
	public class KeyNotFoundException : Exception
	{
		public KeyNotFoundException(string _msg) : base(_msg) { }
	}

	public class SectionNotFoundException : Exception
	{
		public SectionNotFoundException(string _msg) : base(_msg) { }
	}
	#endregion

	#region Config Section Class
	public class Section : IEnumerable
	{
		private KeyPairList m_keys = new KeyPairList();
		public KeyPairList	Keys { get { return m_keys; } }
		public string Name;

		public string this[string _keyName]
		{
			get
			{
				if (m_keys.Contains(_keyName))
				{
					try
					{
						return m_keys[_keyName];
					}
					catch (Exception)
					{
						throw new KeyNotFoundException("Key has no values");
					}
				}
				throw new KeyNotFoundException("Key not found");
			}
		}

		public IEnumerator GetEnumerator()
		{
			return m_keys.GetEnumerator();
		}
	}
	#endregion

	public class KeyPairList : IEnumerable
	{
		private Hashtable m_list = new Hashtable();

		public int Count { get { return m_list.Count; } }

		public string this[string _key]
		{
			get
			{
				if (Contains(_key))
				{
					return m_list[_key].ToString();
				}
				throw new KeyNotFoundException("Key not found");
			}
			set
			{
				if (Contains(_key))
				{
					m_list[_key] = value;
				}
				else
				{
					Add(_key, value);
				}
			}
		}

		public bool Contains(string _key)
		{
			return m_list.Contains(_key.Trim());
		}

		public void Add(string _key, string _value)
		{
			m_list.Add(_key.Trim(), _value.Trim());
		}

		public IEnumerator GetEnumerator()
		{
			return m_list.GetEnumerator();
		}
	}

	public class Config : IEnumerable
	{
		internal Hashtable m_sections = new Hashtable();
		internal Syntax m_syntax = new Syntax();

		private bool IsHeader(string _header)
		{
			return Strings.Contains(_header, Syntax.SectionStart.ToString()) &&
				Strings.Contains(_header, Syntax.SectionEnd.ToString());
		}

		public Section this[string sectionName]
		{
			get
			{
				if (m_sections.Contains(sectionName))
				{
					return (Section)m_sections[sectionName];
				}
				throw new SectionNotFoundException("Section not found");
			}
		}

		public ICollection Sections
		{
			get { return m_sections.Values; }
		}

		public ICollection SectionNames
		{
			get { return m_sections.Keys; }
		}

		public IEnumerator GetEnumerator()
		{
			return this.Sections.GetEnumerator();
		}

		public bool Load(string _fileName)
		{
			// Loading file, make sure sections are empty
			m_sections.Clear();
			using (FileStream fs = new FileStream(_fileName, FileMode.Open, FileAccess.Read))
			{
				int ch;
				string line = string.Empty;
				ArrayList lines = new ArrayList();
				// read until a carriage return, then set a line
				while ((ch = fs.ReadByte()) != -1)
				{
					if (ch == '\n')
					{
						lines.Add(line);
						line = string.Empty;
					}
					else
						line += (char)ch;
				}
				return ProcessLines(lines);
			}			
		}

		private bool ProcessLines(ArrayList _lines)
		{
			Section section = null;
			string line;
			for (int i = 0; i < _lines.Count; i++)
			{
				if (_lines[i] != null)
				{
					line = (_lines[i] as string).Trim();
					if ((line.Length > 0) && (line[0] != Syntax.Comment))
					{
						if (IsHeader(line))
						{
							// we've hit a new Section, store original and reset
							if (section != null)
								m_sections.Add(section.Name, section);

							section = new Section();

							// ensure it's a valid section header
							section.Name = Strings.Contains(line, Syntax.SectionEnd.ToString())
								? line.Substring(1, line.Length - 2)
								: line.Substring(1, line.Length - 1);
							continue;							
						}

						// working with a section already, add values
						if (section != null)
						{
							if (Strings.Contains(line, Syntax.Seperator.ToString()))
							{
								int pos = line.IndexOf(Syntax.Seperator);
								section.Keys.Add(line.Substring(0, pos), line.Substring(pos + 1));
							}
							else
								section.Keys.Add(line, line);
						}
					}					
				}
			}
			if (section != null)
			{
				m_sections.Add(section.Name, section);
			}
			return true;
		}
	}
}
