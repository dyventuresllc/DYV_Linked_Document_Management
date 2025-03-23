using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DYV_Linked_Document_Management.Handlers;
using DYV_Linked_Document_Management.Logging;
using DYV_Linked_Document_Management.Models;
using Relativity.API;

namespace DYV_Linked_Document_Management.Managers
{
    /// <summary>
    /// Manager class that handles different types of import and overlay jobs
    /// </summary>
    public class JobManager
    {
        private readonly IHelper _helper;
        private readonly ILDLogger _logger;
        private readonly QueueHandler _queueHandler;
        private readonly CsvHandler _csvHandler;
        private readonly ImportHandler _importHandler;

        // Define the expected headers for each file type
        private static readonly HashSet<string> ExpectedGmailMetadataHeaders = new HashSet<string>
        {
            "Rfc822MessageId", "GmailMessageId", "FileName", "Account", "Labels",
            "From", "Subject", "To", "CC", "BCC", "DateSent", "DateReceived",
            "SubjectAtStart", "SubjectAtEnd", "DateFirstMessageSent", "DateLastMessageSent",
            "DateFirstMessageReceived", "DateLastMessageReceived", "ThreadedMessageCount"
        };

        /// <summary>
        /// Initializes a new instance of the JobManager class
        /// </summary>
        /// <param name="helper">Relativity helper</param>
        /// <param name="logger">Logger for operations</param>
        /// <param name="queueHandler">Queue handler for managing job status</param>
        public JobManager(IHelper helper, ILDLogger logger, QueueHandler queueHandler)
        {
            _helper = helper ?? throw new ArgumentNullException(nameof(helper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _queueHandler = queueHandler ?? throw new ArgumentNullException(nameof(queueHandler));

            // Initialize handlers
            _csvHandler = new CsvHandler();
            _importHandler = new ImportHandler(helper, logger);
        }

        /// <summary>
        /// Processes an import job based on its file type
        /// </summary>
        /// <param name="importJob">The import job to process</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task ProcessImportJob(ImportQueueItem importJob)
        {
            if (importJob == null)
            {
                throw new ArgumentNullException(nameof(importJob));
            }

            // Set job GUID if available for all logging for this job
            if (Guid.TryParse(importJob.ImportIdentifier, out Guid jobGuid))
            {
                _logger.SetJobGuid(jobGuid);
            }
            else
            {
                // Create a new GUID for logging if the ImportIdentifier isn't a valid GUID
                _logger.SetJobGuid(Guid.NewGuid());
            }

            try
            {
                // Include more details in the log message
                _logger.LogInformation($"Processing import job: {importJob.ImportIdentifier} (ID: {importJob.ImportQueueId}) for workspace: {importJob.ImportWorkspaceArtifactId}, file type: {importJob.FileType}");

                // Process based on file type
                switch (importJob.FileType)
                {
                    case "G - Email Metadata (.csv)":
                        await ProcessEmailMetadataCsv(importJob);
                        break;

                    // Add additional file types as needed

                    default:
                        _logger.LogWarning($"Unsupported file type: {importJob.FileType} for job: {importJob.ImportIdentifier} (ID: {importJob.ImportQueueId})");
                        _queueHandler.LogImportJobStatus(importJob.ImportQueueId, $"Unsupported file type: {importJob.FileType}");
                        throw new NotSupportedException($"File type '{importJob.FileType}' is not supported for job: {importJob.ImportIdentifier}");
                }

                // Mark job as completed
                _queueHandler.MarkImportJobAsCompleted(importJob.ImportQueueId, true);
                _logger.LogInformation($"Import job: {importJob.ImportIdentifier} (ID: {importJob.ImportQueueId}) completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing import job: {importJob.ImportIdentifier} (ID: {importJob.ImportQueueId}) for workspace: {importJob.ImportWorkspaceArtifactId}");

                // Log the error status (instead of updating the queue table)
                _queueHandler.LogImportJobStatus(importJob.ImportQueueId, $"Error: {ex.Message}");

                // Mark job as failed
                _queueHandler.MarkImportJobAsCompleted(importJob.ImportQueueId, false);

                throw; // Re-throw to let the agent handle it
            }
            finally
            {
                // Clear the job GUID when done
                _logger.ClearJobGuid();
            }
        }

        /// <summary>
        /// Validates that the CSV file has the expected headers
        /// </summary>
        /// <param name="foundHeaders">Headers found in the CSV file</param>
        /// <param name="expectedHeaders">The expected headers</param>
        /// <param name="errorMessage">Error message if validation fails</param>
        /// <returns>True if validation passes, false otherwise</returns>
        private bool ValidateCsvHeaders(string[] foundHeaders, HashSet<string> expectedHeaders, out string errorMessage)
        {
            errorMessage = null;

            // Convert headers to HashSet for easier comparison
            var foundHeaderSet = new HashSet<string>(foundHeaders);

            // Check for missing expected headers
            var missingHeaders = expectedHeaders.Except(foundHeaderSet).ToList();
            if (missingHeaders.Any())
            {
                errorMessage = $"CSV file is missing required headers: {string.Join(", ", missingHeaders)}";
                return false;
            }

            // Check for unexpected headers
            var unexpectedHeaders = foundHeaderSet.Except(expectedHeaders).ToList();
            if (unexpectedHeaders.Any())
            {
                errorMessage = $"CSV file contains unexpected headers: {string.Join(", ", unexpectedHeaders)}. Please notify the developer as changes may be required to accommodate this file format.";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Processes a Gmail metadata CSV file
        /// </summary>
        /// <param name="importJob">The import job containing Gmail metadata</param>
        /// <returns>Task representing the asynchronous operation</returns>
        private async Task ProcessEmailMetadataCsv(ImportQueueItem importJob)
        {
            try
            {
                _logger.LogInformation($"Processing Email Metadata file from: {Path.GetFileName(importJob.ImportFilePath)}");

                // 1. Verify the file exists
                if (!File.Exists(importJob.ImportFilePath))
                {
                    throw new FileNotFoundException($"CSV file not found at path: {importJob.ImportFilePath} for job: {importJob.ImportIdentifier}");
                }

                // Read the CSV headers first for validation
                string[] headers = null;
                try
                {
                    using (var reader = new StreamReader(importJob.ImportFilePath))
                    {
                        // Read the first line which should be the header
                        string headerLine = reader.ReadLine();
                        if (string.IsNullOrEmpty(headerLine))
                        {
                            throw new InvalidDataException("CSV file is empty or contains no headers");
                        }

                        // Split by comma and trim whitespace
                        headers = headerLine.Split(',').Select(h => h.Trim().Trim('"')).ToArray();
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidDataException($"Error reading CSV headers: {ex.Message}", ex);
                }

                // Validate that all expected headers are present and no unexpected headers exist
                if (!ValidateCsvHeaders(headers, ExpectedGmailMetadataHeaders, out string validationError))
                {
                    _logger.LogError($"CSV header validation failed: {validationError}");
                    _queueHandler.LogImportJobStatus(importJob.ImportQueueId, $"CSV header validation failed: {validationError}");
                    throw new InvalidDataException(validationError);
                }

                _logger.LogInformation("CSV header validation passed successfully");

                // 2. Process CSV file and analyze field lengths
                var fieldStats = new Dictionary<string, FieldStats>();
                DataTable gmailData = _csvHandler.ProcessGmailCsvMetdataFile(importJob.ImportFilePath, _logger, fieldStats);

                _logger.LogInformation($"Processed {gmailData.Rows.Count} records from CSV for job: {importJob.ImportIdentifier} (ID: {importJob.ImportQueueId})");

                // 3. Log field length statistics
                foreach (var stat in fieldStats.Values)
                {
                    _logger.LogInformation(stat.ToString());
                }

                // 4. Create a modified CSV file with added identifiers
                _queueHandler.LogImportJobStatus(importJob.ImportQueueId, "Creating modified CSV with identifiers");

                string fileLinkedDocumentValue = importJob.ImportIdentifier;
                string modifiedCsvFilePath = _csvHandler.CreateModifiedGmailMetadataCsvFile(gmailData, importJob.ImportFilePath, fileLinkedDocumentValue, _logger);

                // 5. Import the data into Relativity using the modified CSV file
                _queueHandler.LogImportJobStatus(importJob.ImportQueueId, "Importing data into Relativity");

                _logger.LogInformation($"Starting Relativity Import process with the modified CSV file");
                await _importHandler.ImportGmailMetadataToRelativity(
                    modifiedCsvFilePath,
                    importJob.ImportWorkspaceArtifactId,
                    importJob.ImportObjectTypArtifactId);

                _queueHandler.LogImportJobStatus(importJob.ImportQueueId, "Import completed successfully");
                _logger.LogInformation($"Email Metadata CSV processing completed successfully for job: {importJob.ImportIdentifier} (ID: {importJob.ImportQueueId})");
            }
            catch (Exception ex)
            {
                _queueHandler.LogImportJobStatus(importJob.ImportQueueId, $"Error: {ex.Message}");
                _logger.LogError(ex, $"Error processing Email Metadata CSV for job: {importJob.ImportIdentifier} (ID: {importJob.ImportQueueId})");
                throw;
            }
        }
    }
}