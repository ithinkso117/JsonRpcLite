namespace JsonRpcLite.Log
{
    /// <summary>
    /// Write log to target place, replace the Writer to redirect the write place.
    /// </summary>
    public class Logger
    {
        /// <summary>
        /// Gets or sets the debug mode.
        /// </summary>
        public static bool DebugMode { get; set; }

        /// <summary>
        /// The writer implementation.
        /// </summary>
        public static ILogWriter Writer { get; set; }


        /// <summary>
        /// User default ConsoleLogWriter for write the log.
        /// </summary>
        public static void UseDefaultWriter()
        {
            Writer = new ConsoleLogWriter();
        }

        /// <summary>
        /// Write the warn log into target place.
        /// </summary>
        /// <param name="message">The log message to be written.</param>
        public static void WriteWarning(string message)
        {
            Writer?.WriteWarning(message);
        }


        /// <summary>
        /// Write the error log into target place.
        /// </summary>
        /// <param name="message">The log message to be written.</param>
        public static void WriteError(string message)
        {
            Writer?.WriteError(message);
        }


        /// <summary>
        /// Write the info log into target place.
        /// </summary>
        /// <param name="message">The log message to be written.</param>
        public static void WriteInfo(string message)
        {
            Writer?.WriteInfo(message);
        }


        /// <summary>
        /// Write the verbose log into target place.
        /// </summary>
        /// <param name="message">The log message to be written.</param>
        public static void WriteVerbose(string message)
        {
            Writer?.WriteVerbose(message);
        }


        /// <summary>
        /// Write the debug log into target place.
        /// </summary>
        /// <param name="message">The log message to be written.</param>
        public static void WriteDebug(string message)
        {
            if (DebugMode)
            {
                Writer?.WriteDebug(message);
            }
        }
    }
}
