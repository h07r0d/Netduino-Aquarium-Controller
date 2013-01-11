using System;
using System.Text;

namespace Webserver
{
    /// <summary>
    /// Static settings for the webserver
    /// </summary>
    static class Settings
    {
        /// <summary>
        /// "" for complete access, otherwise @"[drive]:\[folder]\[folder]\[...]\"
        /// Watch the '\' at the end of the path
        /// </summary>
        public const string ROOT_PATH = "\\SD\\";

        /// <summary>
        /// Maximum byte size for a HTTP request sent to the server
        /// </summary>
        public const int MAX_REQUESTSIZE = 2048;

        /// <summary>
        /// Buffersize for response file sending 
        /// </summary>
        public const int FILE_BUFFERSIZE = 1024;

        /// <summary>
        /// Temp Post Data File 
        /// </summary>
        public const string LAST_POST = "\\SD\\LastPost.txt";
    }
}
