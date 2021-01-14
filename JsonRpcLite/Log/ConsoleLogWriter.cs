using System;

namespace JsonRpcLite.Log
{

    /// <summary>
    /// Write log to console.
    /// </summary>
    internal class ConsoleLogWriter : ILogWriter
    {
        private class ConsoleColorSetter:IDisposable
        {
            private readonly ConsoleColor _oldColor;

            public ConsoleColorSetter(ConsoleColor color)
            {
                _oldColor = Console.ForegroundColor;
                Console.ForegroundColor = color;
            }

            public void Dispose()
            {
                Console.ForegroundColor = _oldColor;
            }
        }

        /// <summary>
        /// Write the log to console.
        /// </summary>
        /// <param name="message">The log message to be written.</param>
        private void Write(string message)
        {
            var now = DateTime.Now.ToString("yyyyMMdd-HH:mm:ss");
            var logMsg = $"[{now}] - {message}";
            Console.WriteLine(logMsg);
        }


        /// <summary>
        /// Write the warn log into target place.
        /// </summary>
        /// <param name="message">The log message to be written.</param>
        public void WriteWarning(string message)
        {
            lock (this)
            {
                using (new ConsoleColorSetter(ConsoleColor.DarkYellow))
                {
                    Write(message);
                }
            }
        }


        /// <summary>
        /// Write the error log into target place.
        /// </summary>
        /// <param name="message">The log message to be written.</param>
        public void WriteError(string message)
        {
            lock (this)
            {
                using (new ConsoleColorSetter(ConsoleColor.Red))
                {
                    Write(message);
                }
            }
        }


        /// <summary>
        /// Write the info log into target place.
        /// </summary>
        /// <param name="message">The log message to be written.</param>
        public void WriteInfo(string message)
        {
            lock (this)
            {
                using (new ConsoleColorSetter(ConsoleColor.Green))
                {
                    Write(message);
                }
            }
        }


        /// <summary>
        /// Write the verbose log into target place.
        /// </summary>
        /// <param name="message">The log message to be written.</param>
        public void WriteVerbose(string message)
        {
            lock (this)
            {
                using (new ConsoleColorSetter(ConsoleColor.White))
                {
                    Write(message);
                }
            }
        }


        /// <summary>
        /// Write the debug log into target place.
        /// </summary>
        /// <param name="message">The log message to be written.</param>
        public void WriteDebug(string message)
        {
            lock (this)
            {
                using (new ConsoleColorSetter(ConsoleColor.Gray))
                {
                    Write(message);
                }
            }
        }
    }
}
