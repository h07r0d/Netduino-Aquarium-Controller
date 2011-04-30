using System;
using Microsoft.SPOT;

namespace Controller
{
	

	/// <summary>
	/// Display name of this Plugin
	/// </summary>
	[AttributeUsage(AttributeTargets.Class)]
	public class PluginDisplayNameAttribute : System.Attribute
	{
		private string _displayName;

		public PluginDisplayNameAttribute(string DisplayName)
			: base()
		{
			_displayName = DisplayName;
			return;
		}

		public override string ToString()
		{
			return _displayName;
		}
	}

	/// <summary>
	/// Short Description of this plugin
	/// </summary>
	[AttributeUsage(AttributeTargets.Class)]
	public class PluginDescriptionAttribute : System.Attribute
	{
		private string _description;

		public PluginDescriptionAttribute(string Description)
			: base()
		{
			_description = Description;
			return;
		}

		public override string ToString()
		{
			return _description;
		}
	}

	/// <summary>
	/// Type of the plugin to help with invokes
	/// </summary>
	[AttributeUsage(AttributeTargets.Class)]
	public class PluginTypeAttribute : System.Attribute
	{
		private string _typeName;

		public PluginTypeAttribute(string TypeName)
			: base()
		{
			_typeName = TypeName;
		}

		public override string ToString()
		{
			return _typeName;
		}
	}

	public class PluginCategoryAttribute : System.Attribute
	{
		private Category _category;

		public PluginCategoryAttribute(Category Category) : base()
		{
			_category = Category;
		}

		public Category Category { get { return _category; } }
	}

	/// <summary>
	/// The time interval for scheduling this Plugin
	/// <remarks>Represents minutes</remarks>
	/// </summary>
	[AttributeUsage(AttributeTargets.Class)]
	public class PluginTimingInterval : System.Attribute
	{
		private int _interval;

		public PluginTimingInterval(int Interval)
			: base()
		{
			_interval = Interval;
		}

		public int Interval { get { return _interval; } }
	}
}
