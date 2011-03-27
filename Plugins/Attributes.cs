using System;
using Microsoft.SPOT;

namespace Plugins
{
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
}
