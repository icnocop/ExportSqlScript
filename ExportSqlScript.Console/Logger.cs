using System;
using System.IO;

namespace ExportSqlScript.Console
{
    internal class Logger : ILog
    {
        /// <summary>Standard Output StreamWriter</summary>
        private readonly StreamWriter stdout;

        /// <summary>Standard Error StreamWriter</summary>
        private readonly StreamWriter stderr;

        public Logger()
        {
            stdout = new StreamWriter(System.Console.OpenStandardOutput());
            stderr = new StreamWriter(System.Console.OpenStandardError());
        }

        public void Information(string message)
        {
            stdout.WriteLine(message);
            stdout.Flush();
        }

        public void Debug(string message)
        {
            stdout.WriteLine(message);
            stdout.Flush();
        }

        public void Warning(string message)
        {
            stdout.WriteLine(message);
            stdout.Flush();
        }

        internal void Error(Exception ex)
        {
            stderr.WriteLine(ex.ToString());
            stderr.Flush();
        }

        /// <summary>
        /// Releases unmanaged resources and performs other cleanup operations before the
        /// <see cref="Logger"/> is reclaimed by garbage collection.
        /// </summary>
        ~Logger()
        {
            stdout.Close();
            stderr.Close();
        }
    }
}