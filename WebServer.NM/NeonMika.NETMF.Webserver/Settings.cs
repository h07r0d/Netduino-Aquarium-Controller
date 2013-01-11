
namespace NeonMika.Webserver
{
    /// <summary>
    /// Static settings for the webserver
    /// </summary>
    static class Settings
    {
        /// <summary>
        /// Maximum byte size for a HTTP request sent to the server
        /// POST packages will get split up into smaller packages this size
        /// </summary>
        public const int MAX_REQUESTSIZE = 1024;

        /// <summary>
        /// Buffersize for response file sending 
        /// </summary>
        public const int FILE_BUFFERSIZE = 512;

        /// <summary>
        /// Path to save POST arguments temporarly
        /// </summary>
        public const string POST_TEMP_PATH = "\\SD\\lastPOST";

        /// <summary>
        /// Root path for webserver on the SD card.
        /// </summary>
        public const string ROOT_SD_PATH = "\\SD\\";

        /// <summary>
        /// Default page for when root is requested.
        /// </summary>
        public const string DEFAULT_PAGE = "index.html";
    }
}
