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
		private Hashtable table;
		public EventHandlerList() { }
		public void Dispose() { table = null; }

		public Delegate this[object _key]
		{
			get
			{
				if (table == null)
					return null;
				return table[_key] as Delegate;
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
			if (table == null)
				table = new Hashtable();

			Delegate prev = table[_key] as Delegate;
			prev = Delegate.Combine(prev, _value);
			table[_key] = prev;
		}

		/// <summary>
		/// Removes a delegate from the list
		/// </summary>
		/// <param name="_key">Key of handler to remove</param>
		/// <param name="_value">Delegate to remove from list</param>
		public void RemoveHandler(object _key, Delegate _value)
		{
			if (table == null)
				return;

			Delegate prev = table[_key] as Delegate;
			prev = Delegate.Remove(prev, _value);
			table[_key] = prev;
		}
	}
}