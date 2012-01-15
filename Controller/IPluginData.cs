using System;
using Microsoft.SPOT;

namespace Controller
{
    public interface IPluginData
    {
		ThingSpeakFields DataType();
		string DataUnits();
		float GetValue();
    }
}
