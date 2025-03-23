using DYV_Linked_Document_Management.Utilities;
using Relativity.API;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace DYV_Linked_Document_Management.Logging
{
    public class LDLogger : ILDLogger
    {
        private readonly IDBContext _eddsDbContext;
        private readonly string _applicationName;
        private readonly string _source;
        private readonly DYVLDHelper _ldHelper;
        private Guid? _jobGuid;

        public LDLogger(IDBContext eddsDbContext, IHelper helper, IAPILog logger, string applicationName, string source)
        {
            _eddsDbContext = eddsDbContext ?? throw new ArgumentNullException(nameof(eddsDbContext));
            _applicationName = applicationName ?? throw new ArgumentNullException(nameof(applicationName));
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _ldHelper = new DYVLDHelper(helper, logger.ForContext<LDLogger>());
            _jobGuid = null; // Initially no job is associated
        }

        /// <summary>
        /// Sets the current job GUID for all subsequent log entries
        /// </summary>
        /// <param name="jobGuid">The GUID of the job being processed</param>
        public void SetJobGuid(Guid jobGuid)
        {
            _jobGuid = jobGuid;
        }

        /// <summary>
        /// Clears the current job GUID
        /// </summary>
        public void ClearJobGuid()
        {
            _jobGuid = null;
        }

        public void LogInformation(string message)
        {
            Log("Information", message);
        }

        public void LogWarning(string message)
        {
            Log("Warning", message);
        }

        public void LogError(string message)
        {
            Log("Error", message);
        }

        public void LogError(Exception ex, string message)
        {
            Log("Error", message, ex);
        }

        public void LogDebug(string message)
        {
            Log("Debug", message);
        }

        private void Log(string level, string message, Exception exception = null)
        {
            try
            {
                string sql = @"
                    INSERT INTO QE.ApplicationLog_LDMgmt 
                    (LogDateTime, LogLevel, ApplicationName, Source, Message, 
                     ExceptionMessage, InnerException, StackTrace, JobGuid)
                    VALUES 
                    (@logDateTime, @logLevel, @applicationName, @source, @message,
                     @exceptionMessage, @innerException, @stackTrace, @jobGuid)";

                var parameters = new List<SqlParameter>
                {
                    new SqlParameter("@logDateTime", DateTime.UtcNow),
                    new SqlParameter("@logLevel", level),
                    new SqlParameter("@applicationName", _applicationName),
                    new SqlParameter("@source", _source),
                    new SqlParameter("@message", message),
                    new SqlParameter("@exceptionMessage", (object)exception?.Message ?? DBNull.Value),
                    new SqlParameter("@innerException", (object)exception?.InnerException?.Message ?? DBNull.Value),
                    new SqlParameter("@stackTrace", (object)exception?.StackTrace ?? DBNull.Value),
                    new SqlParameter("@jobGuid", (object)_jobGuid ?? DBNull.Value)
                };

                _eddsDbContext.ExecuteNonQuerySQLStatement(sql, parameters);
            }
            catch (Exception ex)
            {
                _ldHelper.Logger.LogError(ex, $"Failed to write to log: {ex.Message}");
            }
        }
    }
}