namespace Extensions
{
    /// <summary>
    /// Static settings for the webserver
    /// </summary>
    public static class Settings
    {
        /// <summary>
        /// Maximum byte size for a HTTP request sent to the server
        /// POST packages will get split up into smaller packages this size
        /// </summary>
        public const int MaxRequestSize = 512;

        /// <summary>
        /// Buffersize for response file sending 
        /// </summary>
        public const int FileBufferSize = 512;

        /// <summary>
        /// Path to save POST arguments temporarly
        /// </summary>
        public const string PostTempPath = @"\SD\lastPOST";
		public const string RootPath = @"\SD\";
		public const string PluginFolder = @"\SD\plugins\";
		public const string FragmentFolder = @"\SD\fragments\";
		public const string ConfigFile = @"\SD\config.js";
		public const string IndexFile = @"\SD\index.html";
    }
}
