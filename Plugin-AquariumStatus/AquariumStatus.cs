using System;
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
            try
            {
                // Load status.js and update necessary variables	
                Hashtable status = (Hashtable)JSON.JsonDecodeFromVar(m_statusFileName);

                foreach (PluginData _pd in _data.GetData())
                {
                    if (_pd.LastReadSuccess)
                        status[_pd.Name] = _pd.Value.ToString("F");
                }
                status["time"] = DateTime.Now.ToString("s");

                // write status.js back down to fs, including the var declaration
                string statusString = JSON.JsonEncode(status);
                statusString = "var aq=" + statusString;
                byte[] statusBytes = Encoding.UTF8.GetBytes(statusString);
                using (FileStream fs = new FileStream(m_statusFileName, FileMode.Create))
                {
                    fs.Write(statusBytes, 0, statusBytes.Length);
                }
            }
            catch (Exception e)
            {
                Debug.Print(e.Message);
            }

		}
	}
}
