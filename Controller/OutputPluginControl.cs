using System;
using Microsoft.SPOT;
using System.Runtime.CompilerServices;

namespace Controller
{
	public sealed class OutputPluginControl
	{
		// Holds all the output delegates
		private OutputPluginEventHandler m_eventHandler;

		public void ProcessInputData(IPluginData _data)
		{
			Debug.Print("Process INput Data = " + _data.GetValue().ToString());
			OutputPluginEventHandler ope = m_eventHandler;
			Debug.Print(ope.Method.ToString());
			// walk through all available output plugins
			if (ope != null) ope(this, _data);
		}

		public event OutputPluginEventHandler DataEvent
		{
			[MethodImpl(MethodImplOptions.Synchronized)]
			add
			{
				m_eventHandler = (OutputPluginEventHandler)Delegate.Combine(m_eventHandler, value);
			}

			[MethodImpl(MethodImplOptions.Synchronized)]
			remove
			{
				m_eventHandler = (OutputPluginEventHandler)Delegate.Remove(m_eventHandler, value);
			}
		}
	}
}
