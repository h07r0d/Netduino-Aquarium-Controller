using System;
using Microsoft.SPOT;

namespace Plugins
{
    public interface IPlugin
    {
        IPluginData[] GetData();
        bool Save(string Path);
    }
}
