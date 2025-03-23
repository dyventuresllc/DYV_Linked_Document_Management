using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using DYV_Linked_Document_Management.Logging;
using DYV_Linked_Document_Management.Models;
using Relativity.API;

namespace DYV_Linked_Document_Management.Handlers
{
    /// <summary>
    /// Handles operations related to the LinkedDocument import and processing queue
    /// </summary>
    public class QueueHandler
    {
        private readonly IDBContext _eddsDbContext;
        private readonly ILDLogger _logger;

        /// <summary>
        /// Initializes a new instance of the QueueHandler class
        /// </summary>
        /// <param name="eddsDbContext">EDDS database context</param>
        /// <param name="logger">Logger for recording operations</param>
        public QueueHandler(IDBContext eddsDbContext, ILDLogger logger)
        {
            _eddsDbContext = eddsDbContext ?? throw new ArgumentNullException(nameof(eddsDbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets the next available import job from the queue
        /// </summary>
        /// <returns>Import queue item or null if no jobs are available</returns>
        public ImportQueueItem GetNextImportJob()
        {
            try
            {
                string sql = @"
                    SELECT TOP 1 
                        ImportQueueId, ImportIdentifier, ImportFilePath, LDFObjectArtifactId, 
                        ImportWorkspaceArtifactId, ImportObjectTypArtifactId, FileType
                    FROM QE.LinkedDocumentImportQueue
                    WHERE SubmittedDateTime IS NOT NULL 
                    AND ImportStartedDateTime IS NULL
                    AND ImportAgentID IS NULL
                    ORDER BY SubmittedDateTime";

                DataTable result = _eddsDbContext.ExecuteSqlStatementAsDataTable(sql);

                if (result == null || result.Rows.Count == 0)
                {
                    _logger.LogInformation("No pending import jobs found in the queue");
                    return null;
                }

                DataRow row = result.Rows[0];

                var importQueueItem = new ImportQueueItem
                {
                    ImportQueueId = row["ImportQueueId"] != DBNull.Value ? Convert.ToInt32(row["ImportQueueId"]) : 0,
                    ImportIdentifier = row["ImportIdentifier"] != DBNull.Value ? row["ImportIdentifier"].ToString() : string.Empty,
                    ImportFilePath = row["ImportFilePath"] != DBNull.Value ? row["ImportFilePath"].ToString() : string.Empty,
                    LDFObjectArtifactId = row["LDFObjectArtifactId"] != DBNull.Value ? Convert.ToInt32(row["LDFObjectArtifactId"]) : 0,
                    ImportWorkspaceArtifactId = row["ImportWorkspaceArtifactId"] != DBNull.Value ? Convert.ToInt32(row["ImportWorkspaceArtifactId"]) : 0,
                    ImportObjectTypArtifactId = row["ImportObjectTypArtifactId"] != DBNull.Value ? Convert.ToInt32(row["ImportObjectTypArtifactId"]) : 0,
                    FileType = row["FileType"] != DBNull.Value ? row["FileType"].ToString() : string.Empty
                };

                _logger.LogInformation($"Found import job {importQueueItem.ImportQueueId} for file type: {importQueueItem.FileType}");
                return importQueueItem;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving next import job from queue");
                throw;
            }
        }

        /// <summary>
        /// Marks an import job as in progress
        /// </summary>
        /// <param name="importQueueId">The ID of the import job</param>
        /// <param name="agentId">The ID of the agent processing the job</param>
        public void MarkImportJobAsInProgress(int importQueueId, string agentId)
        {
            try
            {
                if (importQueueId <= 0)
                {
                    throw new ArgumentException("Import queue ID must be greater than zero", nameof(importQueueId));
                }

                if (string.IsNullOrEmpty(agentId))
                {
                    throw new ArgumentException("Agent ID cannot be null or empty", nameof(agentId));
                }

                var parameters = new List<SqlParameter>
                {
                    new SqlParameter("@ImportQueueId", importQueueId),
                    new SqlParameter("@AgentId", agentId)
                };

                string sql = @"
                    UPDATE QE.LinkedDocumentImportQueue
                    SET ImportAgentId = @AgentId, 
                        ImportStartedDateTime = GETUTCDATE()
                    WHERE ImportQueueId = @ImportQueueId
                    AND ImportStartedDateTime IS NULL";

                int rowsAffected = _eddsDbContext.ExecuteNonQuerySQLStatement(sql, parameters.ToArray());

                if (rowsAffected <= 0)
                {
                    _logger.LogWarning($"Import job {importQueueId} could not be marked as in progress - it may have been picked up by another agent");
                    throw new InvalidOperationException($"Could not mark import job {importQueueId} as in progress - it may have been picked up by another agent");
                }

                _logger.LogInformation($"Marked import job {importQueueId} as in progress with agent ID {agentId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error marking import job {importQueueId} as in progress");
                throw;
            }
        }

        /// <summary>
        /// Marks an import job as completed
        /// </summary>
        /// <param name="importQueueId">The ID of the import job</param>
        /// <param name="success">Whether the job completed successfully</param>
        public void MarkImportJobAsCompleted(int importQueueId, bool success)
        {
            try
            {
                if (importQueueId <= 0)
                {
                    throw new ArgumentException("Import queue ID must be greater than zero", nameof(importQueueId));
                }

                var parameters = new List<SqlParameter>
                {
                    new SqlParameter("@ImportQueueId", importQueueId)
                };

                // Log the success/failure but don't try to update a non-existent column
                _logger.LogInformation($"Job {importQueueId} completed with success={success}");

                string sql = @"
                    UPDATE QE.LinkedDocumentImportQueue
                    SET ImportCompletedDateTime = GETUTCDATE()
                    WHERE ImportQueueId = @ImportQueueId";

                int rowsAffected = _eddsDbContext.ExecuteNonQuerySQLStatement(sql, parameters.ToArray());

                if (rowsAffected <= 0)
                {
                    _logger.LogWarning($"Import job {importQueueId} could not be marked as completed - it may not exist");
                }
                else
                {
                    _logger.LogInformation($"Marked import job {importQueueId} as completed");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error marking import job {importQueueId} as completed");
                throw;
            }
        }

        /// <summary>
        /// Logs a status update for an import job (without updating the database)
        /// </summary>
        /// <param name="importQueueId">The ID of the import job</param>
        /// <param name="statusMessage">Status message to log</param>
        public void LogImportJobStatus(int importQueueId, string statusMessage)
        {
            if (importQueueId <= 0)
            {
                throw new ArgumentException("Import queue ID must be greater than zero", nameof(importQueueId));
            }

            if (string.IsNullOrEmpty(statusMessage))
            {
                throw new ArgumentException("Status message cannot be null or empty", nameof(statusMessage));
            }

            _logger.LogInformation($"Job {importQueueId} Status: {statusMessage}");
        }
    }
}