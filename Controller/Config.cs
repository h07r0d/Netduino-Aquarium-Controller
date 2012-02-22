using System;
using System.IO;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using System.Collections;
using System.Text;

/**
 * Parts taken from ini4net.codeplex.com
 * Full code not pulled, as most functionality is not needed
 */

namespace Controller
{
	#region Syntax Class
	internal class Syntax
	{
		public const char Comment = '#';
		public const char Seperator = '=';
		public const char SectionStart = '[';
		public const char SectionEnd = '[';
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

	#region KeyPair hashtable wrapper
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

		public ICollection GetKeys()
		{
			return m_list.Keys;
		}

		public IEnumerator GetEnumerator()
		{
			return m_list.GetEnumerator();
		}
	}
	#endregion

	public class Config : IEnumerable
	{
		internal Hashtable m_sections = new Hashtable();
		internal Syntax m_syntax = new Syntax();

		private bool IsHeader(string _header)
		{
			return _header.Contains(Syntax.SectionStart) &&
				_header.Contains(Syntax.SectionEnd);
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

		private ICollection GetSectionsByType(string _type)
		{
			Hashtable inputs = new Hashtable();
			foreach (Section section in m_sections)
			{
				if (section.Keys["type"] == _type)
					inputs.Add(section.Name, section);
			}
			return inputs;
		}

		public ICollection InputSections
		{
			get { return GetSectionsByType("input"); }
		}

		public ICollection OutputSections
		{
			get { return GetSectionsByType("output"); }
		}

		public ICollection ControlSections
		{
			get { return GetSectionsByType("control"); }
		}

		public IEnumerator GetEnumerator()
		{
			return this.Sections.GetEnumerator();
		}

		private StringBuilder GetStringBuilder()
		{
			StringBuilder sb = new StringBuilder();
			foreach (Section item in Sections)
			{
				sb.AppendLine("[" + item.Name + "]");
				foreach (string key in item.Keys.GetKeys())
				{
					sb.AppendLine(key + "=" + item.Keys[key]);
				}
				sb.AppendLine();
			}
			sb.AppendLine("");
			return sb;
		}		

		public override string ToString()
		{
			return GetStringBuilder().ToString();
		}
		
		/// <summary>
		/// Write Config out to ini file
		/// </summary>
		/// <param name="_fileName">Name of file to write</param>
		/// <param name="_overwrite">Overwrite the file if it already exists</param>
		public void Save(string _fileName, bool _overwrite)
		{
			if (File.Exists(_fileName) && !_overwrite)
				throw new Exception("File exists");

			File.WriteAllBytes(_fileName, GetStringBuilder().ToBytes());
		}

		/// <summary>
		/// Load text file into memory
		/// </summary>
		/// <param name="_fileName">Name of ini file to Load</param>
		/// <returns>Config object containing all the sections and parameters</returns>
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
							section.Name = line.Contains(Syntax.SectionEnd)
								? line.Substring(1, line.Length - 2)
								: line.Substring(1, line.Length - 1);
							continue;							
						}

						// working with a section already, add values
						if (section != null)
						{
							int pos = line.IndexOf(Syntax.Seperator);
							if (pos > 0)
								section.Keys.Add(line.Substring(0, pos), line.Substring(pos + 1));
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
