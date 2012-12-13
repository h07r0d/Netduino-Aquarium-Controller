﻿using System;
using System.Collections;
using System.IO;
using System.Text;
using Controller;
using Microsoft.SPOT;

namespace Plugins
{
	/// <summary>
	/// Output plugin that provides current tank conditions to the web front end via JS
	/// </summary>
	/// <remarks>Will eventually write out to attached LCD, but that's still VERY WIP</remarks>
	public class AquariumStatus : OutputPlugin
	{
		~AquariumStatus() { Dispose(); }
		public override void Dispose() { }

		public AquariumStatus() { }
		public AquariumStatus(object _config) { }

		private const string m_statusFileName = @"\SD\status.js";

		public override void EventHandler(object _sender, IPluginData _data)
		{
			// Load status.js and update necessary variables			
			Hashtable status = (Hashtable)JSON.JsonDecodeFromFile(m_statusFileName);
            foreach (PluginData _PluginData in _data.GetData())
            {
                status[_PluginData.Name] = (float)_PluginData.Value;
            }
			status["time"] = DateTime.Now.ToString("s");

			foreach (DictionaryEntry item in status)
			{
				Debug.Print(item.Key.ToString() + "=" + item.Value.ToString());
			}

			// write status.js back down to fs, including the var declaration
			string statusString = JSON.JsonEncode(status);
			statusString = "var aq=" + statusString;
			byte[] statusBytes = Encoding.UTF8.GetBytes(statusString);
			using (FileStream fs = new FileStream(m_statusFileName, FileMode.Truncate))
			{
				fs.Write(statusBytes, 0, statusBytes.Length);
			}

		}
	}
}
