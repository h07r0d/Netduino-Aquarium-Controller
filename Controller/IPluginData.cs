using System;
using Microsoft.SPOT;

namespace Controller
{
    public interface IPluginData
    {
		string DataType();
		string DataUnits();
		float GetValue();
    }
}
