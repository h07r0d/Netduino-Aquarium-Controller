using System;
using Microsoft.SPOT;
using System.Threading;
using System.Collections;

namespace Controller
{
	/// <summary>
	/// Delegate that is called when a PluginTask hits it's scheduled interval
	/// </summary>
	/// <param name="_state">Any extra data that is required for the callback to process correctly</param>
	public delegate void PluginEventHandler(object _state);

	public class PluginScheduler : IDisposable
	{
		/// <summary>
		/// Required information to track Plugin Task execution
		/// Not used outside the PluginScheduler, hence protected
		/// </summary>
		protected class PluginTask
		{
			public Delegate CallBack { get; set; }
			public Object State { get; set; }
			public Boolean Reschedule { get; set; }
			public TimeSpan Interval { get; set; }
			public PluginTask(Delegate _callback, object _state, bool _reschedule, TimeSpan _interval)
			{
				CallBack = (PluginEventHandler)Delegate.Combine(CallBack, _callback);
				State = _state;
				Reschedule = _reschedule;
				Interval = _interval;
			}
		}

		/// <summary>
		/// Timer that does all the heavy lifting
		/// </summary>
		private Timer m_Poller;

		/// <summary>
		/// Interval to poll plugin list for execution
		/// </summary>
		private TimeSpan m_pollInterval = new TimeSpan(0, 0, 10);

		/// <summary>
		/// Timespan formatted string with coarser resolution than the Timer.
		/// This ensures mismatches on intervals are still hit.  i.e. Minute test with missed minutes
		/// The time format also acts as the task ID.  Note, duplicate entries can exist
		/// </summary>
		private string m_timerFormat = "MMddHHmm";

		/// <summary>
		/// All tasks attached to the scheduler
		/// </summary>
		private ArrayList m_Tasks;

		/// <summary>
		/// Object for thread locking
		/// </summary>
		private object m_Locker = new Object();

		~PluginScheduler() { Dispose(); }
		public void Dispose() { m_Poller.Dispose(); m_Tasks = null; }

		/// <summary>
		/// Constructor
		/// </summary>
		public PluginScheduler()
		{
			m_Poller = new Timer(PollTasks, null, -1, -1);			
			m_Tasks = new ArrayList();
		}

		/// <summary>
		/// Start the scheduler polling
		/// </summary>
		public void Start()
		{
			lock (m_Locker) m_Poller.Change(m_pollInterval, m_pollInterval);			
		}

		/// <summary>
		/// Stop the Scheduler from Polling
		/// </summary>
		public void Stop()
		{
			lock (m_Locker) m_Poller.Change(-1, -1); 
		}

		/// <summary>
		/// Adds a task to the scheduler
		/// </summary>
		/// <param name="_delegate">Callback to execute on matching time</param>
		/// <param name="_state">State object to pass to the Callback</param>
		/// <param name="_execute">Timespan when to execute the Callback</param>
		/// <param name="_interval">Interval when to re-execute Callback, if any</param>
		/// <param name="_continuous">Should this Task be continuously rescheduled</param>
		public void AddTask(Delegate _delegate, object _state, TimeSpan _execute, TimeSpan _interval, bool _continuous)
		{
			lock (m_Locker)
			{
				PluginTask task = new PluginTask(_delegate, _state, _continuous, _interval);
				DateTime now = DateTime.Now;
				now += _execute;
				DictionaryEntry entry = new DictionaryEntry(now.ToString(m_timerFormat), task);
				m_Tasks.Add(entry);
			}
		}		

		/// <summary>
		/// Main polling thread
		/// </summary>
		/// <param name="_state">Any required state information</param>
		private void PollTasks(object _state)
		{
			DateTime now = DateTime.Now;
			int s_now = int.Parse(now.ToString(m_timerFormat));
			Debug.Print("Polling plugin list at: " + now.ToString(m_timerFormat));
			foreach (DictionaryEntry item in m_Tasks)
			{
				//Debug.Print("Checking Item Key: " + item.Key.ToString());
				if (int.Parse(item.Key.ToString()) <= s_now)
				{
					Debug.Print("Executing task");
					// Execute task in Value
					PluginTask task = (PluginTask)item.Value;
					PluginEventHandler callback = (PluginEventHandler)task.CallBack;
					if(callback != null) callback(task.State);
					
					if (task.Reschedule)
					{
                        now = DateTime.Now + task.Interval;
                        item.Key = now.ToString(m_timerFormat);
						Debug.Print("Rescheduling task for " + now.ToString());
					}
					else
					{
						lock (m_Locker)
						{
							m_Tasks.Remove(item);
						}
					}
				}
			}
		}
	}
}