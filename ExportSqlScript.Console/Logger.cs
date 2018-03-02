// <copyright file="Logger.cs" company="Ivan Hamilton, Rami Abughazaleh, and contributors">
//   Copyright (c) Ivan Hamilton, Rami Abughazaleh, and contributors. All rights reserved.
// </copyright>

namespace ExportSqlScript.Console
{
    using System;
    using System.IO;

    /// <summary>
    /// Logger.
    /// </summary>
    /// <seealso cref="ExportSqlScript.ILog" />
    internal class Logger : ILog
    {
        /// <summary>Standard Output StreamWriter</summary>
        private readonly StreamWriter stdout;

        /// <summary>Standard Error StreamWriter</summary>
        private readonly StreamWriter stderr;

        /// <summary>
        /// Initializes a new instance of the <see cref="Logger"/> class.
        /// </summary>
        public Logger()
        {
            this.stdout = new StreamWriter(System.Console.OpenStandardOutput());
            this.stderr = new StreamWriter(System.Console.OpenStandardError());
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="Logger"/> class.
        /// </summary>
        ~Logger()
        {
            this.stdout.Close();
            this.stderr.Close();
        }

        /// <summary>
        /// Writes the message to the console output.
        /// </summary>
        /// <param name="message">The message.</param>
        public void Information(string message)
        {
            this.Output(message);
        }

        /// <summary>
        /// Writes the message to the console output.
        /// </summary>
        /// <param name="message">The message.</param>
        public void Debug(string message)
        {
            this.Output(message);
        }

        /// <summary>
        /// Write the message to the console output.
        /// </summary>
        /// <param name="message">The message.</param>
        public void Warning(string message)
        {
            this.Output(message);
        }

        /// <summary>
        /// Writes the exception to the console error.
        /// </summary>
        /// <param name="ex">The exception.</param>
        internal void Error(Exception ex)
        {
            this.Error(ex.ToString());
        }

        private void Output(string message)
        {
            this.stdout.WriteLine(message);
            this.stdout.Flush();
        }

        private void Error(string message)
        {
            this.stderr.WriteLine(message);
            this.stderr.Flush();
        }
    }
}