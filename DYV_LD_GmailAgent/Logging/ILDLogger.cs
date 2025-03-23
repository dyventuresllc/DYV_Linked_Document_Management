using System;

namespace DYV_Linked_Document_Management.Logging
{
    public interface ILDLogger
    {
        /// <summary>
        /// Sets the current job GUID for all subsequent log entries
        /// </summary>
        /// <param name="jobGuid">The GUID of the job being processed</param>
        void SetJobGuid(Guid jobGuid);

        /// <summary>
        /// Clears the current job GUID
        /// </summary>
        void ClearJobGuid();

        /// <summary>
        /// Logs an informational message
        /// </summary>
        /// <param name="message">The message to log</param>
        void LogInformation(string message);

        /// <summary>
        /// Logs a warning message
        /// </summary>
        /// <param name="message">The message to log</param>
        void LogWarning(string message);

        /// <summary>
        /// Logs an error message
        /// </summary>
        /// <param name="message">The message to log</param>
        void LogError(string message);

        /// <summary>
        /// Logs an error message with exception details
        /// </summary>
        /// <param name="ex">The exception to log</param>
        /// <param name="message">The message to log</param>
        void LogError(Exception ex, string message);

        /// <summary>
        /// Logs a debug message
        /// </summary>
        /// <param name="message">The message to log</param>
        void LogDebug(string message);
    }
}