// <copyright file="ILog.cs" company="Ivan Hamilton, Rami Abughazaleh, and contributors">
//   Copyright (c) Ivan Hamilton, Rami Abughazaleh, and contributors. All rights reserved.
// </copyright>

namespace ExportSqlScript
{
    /// <summary>
    /// Log
    /// </summary>
    public interface ILog
    {
        /// <summary>
        /// Logs the specified message for information purposes.
        /// </summary>
        /// <param name="message">The message.</param>
        void Information(string message);

        /// <summary>
        /// Logs the specified message as a warning.
        /// </summary>
        /// <param name="message">The message.</param>
        void Warning(string message);

        /// <summary>
        /// Logs the specified message as an error.
        /// </summary>
        /// <param name="message">The message.</param>
        void Debug(string message);
    }
}
