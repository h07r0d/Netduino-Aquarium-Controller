using System;
using Microsoft.SPOT;
using System.Collections;

namespace Controller
{
	/// <summary>
	/// Collection to hold a list of keyed Delegates.  Allows registering
	/// multiple Handlers against one type of Event
	/// </summary>
	public sealed class EventHandlerList : IDisposable
	{
		private Hashtable m_table;
		private Object m_lock;
		public EventHandlerList() { m_lock = new Object(); }
		public void Dispose() { m_table = null; }

		public Delegate this[object _key]
		{
			get
			{
				if (m_table == null)
					return null;
				return m_table[_key] as Delegate;
			}
			set
			{
				AddHandler(_key, value);
			}
		}

		/// <summary>
		/// Adds a delegate to the list
		/// </summary>
		/// <param name="_key">Key to identify the Handler</param>
		/// <param name="_value">Delegate to store</param>
		public void AddHandler(object _key, Delegate _value)
		{
			lock (m_lock)
			{
				if (m_table == null)
					m_table = new Hashtable();

				Delegate prev = m_table[_key] as Delegate;
				prev = Delegate.Combine(prev, _value);
				m_table[_key] = prev;
			}
		}

		/// <summary>
		/// Removes a delegate from the list
		/// </summary>
		/// <param name="_key">Key of handler to remove</param>
		/// <param name="_value">Delegate to remove from list</param>
		public void RemoveHandler(object _key, Delegate _value)
		{
			if (m_table == null)
				return;
			lock (m_lock)
			{
				Delegate prev = m_table[_key] as Delegate;
				prev = Delegate.Remove(prev, _value);
				m_table[_key] = prev;
			}
		}
	}
}