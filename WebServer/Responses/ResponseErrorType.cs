using System;
using System.Text;
using System.Diagnostics;
using System.Net.Sockets;
using System.IO;
using System.Collections;
using Webserver.EventArgs;
using Microsoft.SPOT;

namespace Webserver.Responses
{
    public enum ResponseErrorType 
    {
        ParameterMissing,
        ParameterConvertError,
        ParameterRangeException,
        InternalValueNotSet
    }
}