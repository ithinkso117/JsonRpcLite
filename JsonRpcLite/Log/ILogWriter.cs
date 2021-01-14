namespace JsonRpcLite.Log
{
    /// <summary>
    /// Write interface for writing the log.
    /// </summary>
    public interface ILogWriter
    {
        /// <summary>
        /// Write the warn log into target place.
        /// </summary>
        /// <param name="message">The log message to be written.</param>
        void WriteWarning(string message);

        /// <summary>
        /// Write the error log into target place.
        /// </summary>
        /// <param name="message">The log message to be written.</param>
        void WriteError(string message);


        /// <summary>
        /// Write the info log into target place.
        /// </summary>
        /// <param name="message">The log message to be written.</param>
        void WriteInfo(string message);

        /// <summary>
        /// Write the verbose log into target place.
        /// </summary>
        /// <param name="message">The log message to be written.</param>
        void WriteVerbose(string message);

        /// <summary>
        /// Write the debug log into target place.
        /// </summary>
        /// <param name="message">The log message to be written.</param>
        void WriteDebug(string message);
    }
}
