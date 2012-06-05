using System;
using Microsoft.SPOT;
using System.Text;
using System.Collections;
using System.IO;

namespace Controller
{
	/// <summary>
	/// Class used to generate the web front-end homepage.
	/// Uses a combination of files hosted externally from the Netduino,
	/// some templating fragments, and snippets for each active plugin
	/// </summary>
	class HtmlBuilder : IDisposable
	{
		~HtmlBuilder() { Dispose(); }
		public void Dispose() { }

		private byte[] m_closeDiv;
		private readonly string m_hostUrl = @"http://fishfornerds.com/files/";
		private readonly string m_headerScriptFileName = "headerScripts.tmp";
		private readonly string m_scriptCallFileName = "scriptCall.tmp";
		private ArrayList m_controlPlugins;
		private ArrayList m_inputPlugins;
		private ArrayList m_outputPlugins;


		public HtmlBuilder()
		{
			m_controlPlugins = new ArrayList();
			m_inputPlugins = new ArrayList();
			m_outputPlugins = new ArrayList();
			m_closeDiv = Encoding.UTF8.GetBytes("</div>");
		}

		public void AddPlugin(string _scriptName, PluginType _type, bool _local)
		{
			// Add header js block
			StringBuilder script = new StringBuilder();
			script.Append("<script src=\"");
			if (!_local)
				script.Append(m_hostUrl);
			script.Append(_scriptName);
			script.Append(".min.js\" type=\"text/javascript\"></script>");
			using (FileStream fs = new FileStream(Program.FragmentFolder + m_headerScriptFileName, FileMode.Append))
			{
				byte[] text = script.ToBytes();
				fs.Write(text, 0, text.Length);
				fs.Flush();				
			}			

			// Add footer JS Calls
			script = new StringBuilder();
			script.Append("$(");
			script.Append(_scriptName);
			script.Append("Init);");
			using (FileStream fs = new FileStream(Program.FragmentFolder + m_scriptCallFileName, FileMode.Append))
			{
				byte[] text = script.ToBytes();
				fs.Write(text, 0, text.Length);
				fs.Flush();
			}

			// keep plugin name to pull html fragment later
			switch (_type)
			{
				case PluginType.Input:
					m_inputPlugins.Add(_scriptName);
					break;
				case PluginType.Output:
					m_outputPlugins.Add(_scriptName);
					break;
				case PluginType.Control:
					m_controlPlugins.Add(_scriptName);
					break;
				default:
					break;
			}
			Debug.GC(true);
		}

		public void GenerateIndex()
		{
			byte[] stringBytes;
			FileStream index = new FileStream(@"\SD\index.html", FileMode.Create);
			
			FileStream fragment = new FileStream(Program.FragmentFolder+"header.htm", FileMode.Open);
			fragment.CopyTo(index);
			fragment.Close();
			index.Flush();

			// Header written, add JS Script links
			fragment = new FileStream(Program.FragmentFolder + m_headerScriptFileName, FileMode.Open);
			fragment.CopyTo(index);
			fragment.Close();
			index.Flush();
			
			fragment = new FileStream(Program.FragmentFolder+"body-start.htm", FileMode.Open);
			fragment.CopyTo(index);
			fragment.Close();
			index.Flush();

			//body start written, output plugin groups
			stringBytes = Encoding.UTF8.GetBytes("<div id=\"control\">");
			index.Write(stringBytes, 0, stringBytes.Length);
			if (!WritePlugins(ref index, PluginType.Control))
				return;

			// input plugins
			stringBytes = Encoding.UTF8.GetBytes("<div id=\"input\">");
			index.Write(stringBytes, 0, stringBytes.Length);
			if (!WritePlugins(ref index, PluginType.Input))
				return;

			// output plugins
			stringBytes = Encoding.UTF8.GetBytes("<div id=\"output\">");
			index.Write(stringBytes, 0, stringBytes.Length);
			if (!WritePlugins(ref index, PluginType.Output))
				return;
			
			// close body
			fragment = new FileStream(Program.FragmentFolder+"body-end.htm", FileMode.Open);
			fragment.CopyTo(index);
			fragment.Close();
				
			// add JS calls to initiate front end
			fragment = new FileStream(Program.FragmentFolder + m_scriptCallFileName, FileMode.Open);
			fragment.CopyTo(index);
			fragment.Close();			

			// close document
			fragment = new FileStream(Program.FragmentFolder+"footer.htm", FileMode.Open);
			fragment.CopyTo(index);
			fragment.Close();

			// finished building index
			index.Flush();
			index.Close();					

			// delete temp files used in building index
			File.Delete(Program.FragmentFolder + m_headerScriptFileName);
			File.Delete(Program.FragmentFolder + m_scriptCallFileName);

		}

		private bool WritePlugins(ref FileStream _index, PluginType _type)
		{
			IList currArray;
			FileStream fragment;
			switch (_type)
			{
				case PluginType.Input:
					currArray = m_inputPlugins;
					break;
				case PluginType.Output:
					currArray = m_outputPlugins;
					break;
				case PluginType.Control:
					currArray = m_controlPlugins;
					break;
				default:
					currArray = new ArrayList();
					break;
			}

			try
			{
				foreach (string item in currArray)
				{
					fragment = new FileStream(Program.PluginFolder + item + ".htm", FileMode.Open);
					fragment.CopyTo(_index);
					fragment.Close();
				}
				_index.Write(m_closeDiv, 0, m_closeDiv.Length);
				_index.Flush();
			}
			catch (IOException ioe)
			{
				Debug.Print(ioe.StackTrace);
				return false;
			}
			return true;
		}
	}
}