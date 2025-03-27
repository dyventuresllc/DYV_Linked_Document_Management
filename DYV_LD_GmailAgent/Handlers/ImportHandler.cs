using DYV_Linked_Document_Management.Logging;
using DYV_Linked_Document_Management.Utilities;
using Relativity.API;
using Relativity.Import.V1;
using Relativity.Import.V1.Models;
using Relativity.Import.V1.Models.Settings;
using Relativity.Import.V1.Models.Sources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DYV_Linked_Document_Management.Handlers
{
    public class ImportHandler
    {
        private readonly IHelper _helper;
        private readonly ILDLogger _logger;

        public ImportHandler(IHelper helper, ILDLogger logger)
        {
            _helper = helper;
            _logger = logger;
        }

        public async Task ImportGmailMetadataToRelativity(string csvFilePath, int workspaceId, int customObjectTypeId, int importQueueId)
        {
            try
            {
                // Generate IDs for the import job
                Guid importId = Guid.NewGuid();
                Guid sourceId = Guid.NewGuid();

                // Create HTTP client with token authentication
                var httpClient = await RelativityApiUtility.CreateHttpClientWithTokenAsync(_logger, _helper);
                _logger.LogInformation($"Using base address: {httpClient.BaseAddress}");

                // Configure the import settings
                var fieldMappings = CreateGmailMetadataFieldMappings();
                var importSettings = CreateImportSettings(customObjectTypeId, fieldMappings);
                var dataSourceSettings = CreateDataSourceSettings(csvFilePath);

                // Execute the import process
                await ExecuteGmailMetadataImportProcess(
                    httpClient,
                    workspaceId,
                    importId,
                    sourceId,
                    importSettings,
                    dataSourceSettings,
                    importQueueId);

                // Monitor the import progress
                await MonitorImportProgress(httpClient, workspaceId, importId, sourceId);

                _logger.LogInformation("Import completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Relativity Import");
                throw;
            }
        }

        private List<FieldMapping> CreateGmailMetadataFieldMappings()
        {
            // Added fields indices
            const int identifierColumnIndex = 19;
            const int fileLinkedDocumentColumnIndex = 20;
            const int custodianIdColumnIndex = 21;

            return new List<FieldMapping>
            {
                new FieldMapping { Field = "Rfc822MessageId", ContainsID = false, ColumnIndex = 0, ContainsFilePath = false },
                new FieldMapping { Field = "GmailMessageId", ContainsID = false, ColumnIndex = 1, ContainsFilePath = false },
                new FieldMapping { Field = "FileName", ContainsID = false, ColumnIndex = 2, ContainsFilePath = false },
                new FieldMapping { Field = "Account", ContainsID = false, ColumnIndex = 3, ContainsFilePath = false },
                new FieldMapping { Field = "Labels", ContainsID = false, ColumnIndex = 4, ContainsFilePath = false },
                new FieldMapping { Field = "From", ContainsID = false, ColumnIndex = 5, ContainsFilePath = false },
                new FieldMapping { Field = "Subject", ContainsID = false, ColumnIndex = 6, ContainsFilePath = false },
                new FieldMapping { Field = "To", ContainsID = false, ColumnIndex = 7, ContainsFilePath = false },
                new FieldMapping { Field = "CC", ContainsID = false, ColumnIndex = 8, ContainsFilePath = false },
                new FieldMapping { Field = "BCC", ContainsID = false, ColumnIndex = 9, ContainsFilePath = false },
                new FieldMapping { Field = "DateSent", ContainsID = false, ColumnIndex = 10, ContainsFilePath = false },
                new FieldMapping { Field = "DateReceived", ContainsID = false, ColumnIndex = 11, ContainsFilePath = false },
                new FieldMapping { Field = "SubjectAtStart", ContainsID = false, ColumnIndex = 12, ContainsFilePath = false },
                new FieldMapping { Field = "SubjectAtEnd", ContainsID = false, ColumnIndex = 13, ContainsFilePath = false },
                new FieldMapping { Field = "DateFirstMessageSent", ContainsID = false, ColumnIndex = 14, ContainsFilePath = false },
                new FieldMapping { Field = "DateLastMessageSent", ContainsID = false, ColumnIndex = 15, ContainsFilePath = false },
                new FieldMapping { Field = "DateFirstMessageReceived", ContainsID = false, ColumnIndex = 16, ContainsFilePath = false },
                new FieldMapping { Field = "DateLastMessageReceived", ContainsID = false, ColumnIndex = 17, ContainsFilePath = false },
                new FieldMapping { Field = "ThreadedMessageCount", ContainsID = false, ColumnIndex = 18, ContainsFilePath = false },
                new FieldMapping { Field = "Identifier", ContainsID = false, ColumnIndex = identifierColumnIndex, ContainsFilePath = false },
                new FieldMapping { Field = "File (Linked Document)", ContainsID = false, ColumnIndex = fileLinkedDocumentColumnIndex, ContainsFilePath = false },
                new FieldMapping { Field = "CustodianId", ContainsID = false, ColumnIndex = custodianIdColumnIndex, ContainsFilePath = false },
            };
        }

        private ImportRdoSettings CreateImportSettings(int customObjectTypeId, List<FieldMapping> fieldMappings)
        {
            return new ImportRdoSettings()
            {
                Overlay = null,
                Fields = new FieldsSettings
                {
                    FieldMappings = fieldMappings.ToArray(),
                },
                Rdo = new RdoSettings
                {
                    ArtifactTypeID = customObjectTypeId,
                    ParentColumnIndex = null,
                },
            };
        }

        private DataSourceSettings CreateDataSourceSettings(string csvFilePath)
        {
            return new DataSourceSettings
            {
                Type = DataSourceType.LoadFile,
                Path = csvFilePath,
                NewLineDelimiter = '\n',
                ColumnDelimiter = ',',
                QuoteDelimiter = '"',
                MultiValueDelimiter = ';',
                NestedValueDelimiter = '\\',
                Encoding = "utf-8",
                CultureInfo = "en-US",
                EndOfLine = DataSourceEndOfLine.Windows,
                FirstLineContainsColumnNames = true,
                StartLine = 0,
            };
        }

        private async Task ExecuteGmailMetadataImportProcess(
            HttpClient httpClient,
            int workspaceId,
            Guid importId,
            Guid sourceId,
            ImportRdoSettings importSettings,
            DataSourceSettings dataSourceSettings,
            int importQueueId)
        {
            // Create import job
            var createJobPayload = new
            {
                applicationName = "GmailMetadata-Import",
                correlationID = $"GmailMetadataImport-{Guid.NewGuid()}"
            };

            var createImportJobUri = RelativityImportEndpoints.GetImportJobCreateUri(workspaceId, importId);
            _logger.LogInformation($"Creating import job with URI: {createImportJobUri}");

            string createJobJson = JsonSerializer.Serialize(createJobPayload);
            var createJobContent = new StringContent(createJobJson, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(createImportJobUri, createJobContent);

            // Log detailed information if request fails
            if (!response.IsSuccessStatusCode)
            {
                string content = await response.Content.ReadAsStringAsync();
                string fullUrl = new Uri(httpClient.BaseAddress, createImportJobUri).ToString();
                _logger.LogError($"Request failed: {response.StatusCode} for URL: {fullUrl}");
                _logger.LogError($"Request body: {createJobJson}");
                _logger.LogError($"Response content: {content}");
                throw new Exception($"Failed to create import job: {response.StatusCode} - {content}");
            }

            _logger.LogInformation("Import job created successfully");

            // Add import RDO settings
            var rdosConfigurationUri = RelativityImportEndpoints.GetRdoConfigurationUri(workspaceId, importId);
            var importSettingPayload = new { importSettings };
            string settingsJson = JsonSerializer.Serialize(importSettingPayload);
            var settingsContent = new StringContent(settingsJson, Encoding.UTF8, "application/json");

            response = await httpClient.PostAsync(rdosConfigurationUri, settingsContent);
            await RelativityApiUtility.EnsureSuccessResponse(response, "Failed to configure RDO settings", _logger);

            _logger.LogInformation("RDO configuration added successfully");

            // Add data source settings
            var importSourcesUri = RelativityImportEndpoints.GetImportSourceUri(workspaceId, importId, sourceId);
            var dataSourceSettingsPayload = new { dataSourceSettings };
            string dataSourceJson = JsonSerializer.Serialize(dataSourceSettingsPayload);
            var dataSourceContent = new StringContent(dataSourceJson, Encoding.UTF8, "application/json");

            response = await httpClient.PostAsync(importSourcesUri, dataSourceContent);
            await RelativityApiUtility.EnsureSuccessResponse(response, "Failed to add data source", _logger);

            _logger.LogInformation("Data source added successfully");

            // Start import job
            var beginImportJobUri = RelativityImportEndpoints.GetImportJobBeginUri(workspaceId, importId);
            response = await httpClient.PostAsync(beginImportJobUri, null);
            await RelativityApiUtility.EnsureSuccessResponse(response, "Failed to begin import job", _logger);

            _logger.LogInformation("Import job started successfully");

            // End import job
            var endImportJobUri = RelativityImportEndpoints.GetImportJobEndUri(workspaceId, importId);
            response = await httpClient.PostAsync(endImportJobUri, null);
            await RelativityApiUtility.EnsureSuccessResponse(response, "Failed to end import job", _logger);

            _logger.LogInformation("Import job ended successfully");

            // Log the job details for monitoring
            _logger.LogInformation($"Import job submitted with workspaceId: {workspaceId}, importId: {importId}, sourceId: {sourceId}");

            // Update the queue item with the import and source IDs
            if (importQueueId > 0)
            {
                try
                {
                    // Create an instance of QueueHandler to update the import job
                    var eddsDbContext = _helper.GetDBContext(-1);
                    var queueHandler = new QueueHandler(eddsDbContext, _logger);

                    // Update the import job with the Relativity import and source IDs
                    queueHandler.UpdateImportJobIds(importQueueId, importId, sourceId);
                    _logger.LogInformation($"Updated import queue item {importQueueId} with Import ID and Source ID");
                }
                catch (Exception ex)
                {
                    // Log error but don't fail the import process
                    _logger.LogError(ex, $"Error updating import queue item {importQueueId} with Import ID and Source ID");
                }
            }
        }

        private async Task MonitorImportProgress(HttpClient httpClient, int workspaceId, Guid importId, Guid sourceId)
        {
            // Set up JSON options for enum conversion
            JsonSerializerOptions options = new JsonSerializerOptions
            {
                Converters = { new JsonStringEnumConverter() }
            };

            // Get the import details endpoint
            var importSourceDetailsUri = RelativityImportEndpoints.GetImportSourceDetailsUri(workspaceId, importId, sourceId);
            _logger.LogInformation("Monitoring import progress...");

            bool isCompleted = false;
            int attempts = 0;
            const int maxAttempts = 180; // Increased to 30 minutes at 10 second intervals
            DataSourceState[] completedStates = { DataSourceState.Completed, DataSourceState.CompletedWithItemErrors, DataSourceState.Failed };

            while (!isCompleted && attempts < maxAttempts)
            {
                attempts++;

                try
                {
                    // Get current import source details
                    var detailsResponse = await httpClient.GetAsync(importSourceDetailsUri);

                    if (detailsResponse.IsSuccessStatusCode)
                    {
                        string detailsJson = await detailsResponse.Content.ReadAsStringAsync();
                        var detailsResult = JsonSerializer.Deserialize<ValueResponse<DataSourceDetails>>(detailsJson, options);

                        if (detailsResult?.IsSuccess ?? false)
                        {
                            var state = detailsResult.Value.State;

                            // Log progress every 10 attempts to reduce log spam
                            if (attempts % 10 == 0)
                            {
                                _logger.LogInformation($"Import data source state: {state} (check #{attempts})");

                                // Get progress information
                                var progressUri = RelativityImportEndpoints.GetImportSourceProgressUri(workspaceId, importId, sourceId);
                                var progressResponse = await httpClient.GetAsync(progressUri);

                                if (progressResponse.IsSuccessStatusCode)
                                {
                                    string progressJson = await progressResponse.Content.ReadAsStringAsync();
                                    var progressResult = JsonSerializer.Deserialize<ValueResponse<ImportProgress>>(progressJson, options);

                                    if (progressResult?.IsSuccess ?? false)
                                    {
                                        _logger.LogInformation($"Import progress: Total records: {progressResult.Value.TotalRecords}, " +
                                                             $"Imported records: {progressResult.Value.ImportedRecords}, " +
                                                             $"Records with errors: {progressResult.Value.ErroredRecords}");
                                    }
                                }
                            }

                            // Check if the import is completed or failed
                            if (completedStates.Contains(state))
                            {
                                isCompleted = true;

                                // Get final progress information
                                var progressUri = RelativityImportEndpoints.GetImportSourceProgressUri(workspaceId, importId, sourceId);
                                var progressResponse = await httpClient.GetAsync(progressUri);

                                if (progressResponse.IsSuccessStatusCode)
                                {
                                    string progressJson = await progressResponse.Content.ReadAsStringAsync();
                                    var progressResult = JsonSerializer.Deserialize<ValueResponse<ImportProgress>>(progressJson, options);

                                    if (progressResult?.IsSuccess ?? false)
                                    {
                                        _logger.LogInformation($"Final import progress: Total records: {progressResult.Value.TotalRecords}, " +
                                                             $"Imported records: {progressResult.Value.ImportedRecords}, " +
                                                             $"Records with errors: {progressResult.Value.ErroredRecords}");
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        _logger.LogWarning($"Error response when checking status: {detailsResponse.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Error while checking import status: {ex.Message}");
                }

                if (!isCompleted)
                {
                    // Wait 10 seconds before checking again
                    await Task.Delay(10000);
                }
            }

            if (!isCompleted)
            {
                _logger.LogWarning($"Import monitoring timed out after {attempts} attempts, but import is still running.");
                _logger.LogWarning("The import will continue in Relativity but this agent will not wait for it to complete.");
                _logger.LogWarning($"Check workspaceId: {workspaceId}, importId: {importId}, sourceId: {sourceId} in Relativity for final status.");
            }
        }
    }
}