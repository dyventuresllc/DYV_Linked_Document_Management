using CsvHelper.Configuration;
using CsvHelper;
using DYV_Linked_Document_Management.Logging;
using DYV_Linked_Document_Management.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;

namespace DYV_Linked_Document_Management.Handlers
{
    public class CsvHandler
    {
        public DataTable ProcessGmailCsvMetdataFile(string filePath, ILDLogger ldLogger, Dictionary<string, FieldStats> fieldStats)
        {
            ldLogger.LogInformation($"Processing GMAIL Metadata CSV file with CsvHelper: {filePath}");

            // Create DataTable with all the expected columns
            DataTable dt = new DataTable();
            dt.Columns.Add("Rfc822MessageId", typeof(string));
            dt.Columns.Add("GmailMessageId", typeof(string));
            dt.Columns.Add("FileName", typeof(string));
            dt.Columns.Add("Account", typeof(string));
            dt.Columns.Add("Labels", typeof(string));
            dt.Columns.Add("From", typeof(string));
            dt.Columns.Add("Subject", typeof(string));
            dt.Columns.Add("To", typeof(string));
            dt.Columns.Add("CC", typeof(string));
            dt.Columns.Add("BCC", typeof(string));
            dt.Columns.Add("DateSent", typeof(string));
            dt.Columns.Add("DateReceived", typeof(string));
            dt.Columns.Add("SubjectAtStart", typeof(string));
            dt.Columns.Add("SubjectAtEnd", typeof(string));
            dt.Columns.Add("DateFirstMessageSent", typeof(string));
            dt.Columns.Add("DateLastMessageSent", typeof(string));
            dt.Columns.Add("DateFirstMessageReceived", typeof(string));
            dt.Columns.Add("DateLastMessageReceived", typeof(string));
            dt.Columns.Add("ThreadedMessageCount", typeof(string));

            // Initialize field stats for each column
            foreach (DataColumn column in dt.Columns)
            {
                fieldStats[column.ColumnName] = new FieldStats { FieldName = column.ColumnName };
            }

            try
            {
                // Configure CsvHelper
                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true,
                    MissingFieldFound = null,
                    BadDataFound = null,
                    IgnoreBlankLines = true,
                    TrimOptions = TrimOptions.Trim,
                    Mode = CsvMode.RFC4180,  // Standard CSV mode                    
                    ShouldQuote = args => args.Field.Contains(",") || args.Field.Contains("\"") || args.Field.Contains("\r") || args.Field.Contains("\n")
                };

                using (var reader = new StreamReader(filePath))
                using (var csv = new CsvReader(reader, config))
                {
                    // Read the header row
                    csv.Read();
                    csv.ReadHeader();

                    // Get all column indices
                    var columnIndices = new Dictionary<string, int>();

                    // Define all expected columns
                    var expectedColumns = new List<string>
                    {
                        "Rfc822MessageId", "GmailMessageId", "FileName", "Account", "Labels",
                        "From", "Subject", "To", "CC", "BCC", "DateSent", "DateReceived",
                        "SubjectAtStart", "SubjectAtEnd", "DateFirstMessageSent", "DateLastMessageSent",
                        "DateFirstMessageReceived", "DateLastMessageReceived", "ThreadedMessageCount"
                    };

                    // Log available headers
                    string[] headers = csv.HeaderRecord;
                    ldLogger.LogInformation($"CSV Headers found: {string.Join(", ", headers)}");

                    // Try to find each column index and log whether found
                    foreach (var column in expectedColumns)
                    {
                        int index = csv.GetFieldIndex(column);
                        columnIndices[column] = index;
                        ldLogger.LogInformation($"Column {column}: Index = {(index >= 0 ? index.ToString() : "Not Found")}");
                    }

                    // Read all records
                    int rowCount = 0;
                    while (csv.Read())
                    {
                        try
                        {
                            rowCount++;
                            DataRow row = dt.NewRow();

                            // Process each expected column
                            foreach (var column in expectedColumns)
                            {
                                int index = columnIndices[column];
                                string value = string.Empty;

                                if (index >= 0)
                                {
                                    try
                                    {
                                        value = csv.GetField(index) ?? string.Empty;
                                    }
                                    catch
                                    {
                                        value = string.Empty;
                                    }
                                }

                                // Update field statistics
                                fieldStats[column].AddValue(value);

                                // Add value to the data row
                                row[column] = value;
                            }

                            dt.Rows.Add(row);
                        }
                        catch (Exception ex)
                        {
                            ldLogger.LogWarning($"Error parsing row {rowCount}: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ldLogger.LogError(ex, $"Error processing CSV file: {filePath}");
                throw;
            }

            return dt;
        }

        public DataTable ProcessGmailCsvDriveLinksFile(string filePath, ILDLogger ldLogger, Dictionary<string, FieldStats> fieldStats)
        {
            ldLogger.LogInformation($"Processing Gmail Drive Links CSV file with CsvHelper: {filePath}");

            // Create DataTable with the expected columns for the drive links file
            DataTable dt = new DataTable();
            dt.Columns.Add("Account", typeof(string));
            dt.Columns.Add("Rfc822MessageId", typeof(string));
            dt.Columns.Add("GmailMessageId", typeof(string));
            dt.Columns.Add("DriveUrl", typeof(string));
            dt.Columns.Add("DriveItemId", typeof(string));

            // Initialize field stats for each column
            foreach (DataColumn column in dt.Columns)
            {
                fieldStats[column.ColumnName] = new FieldStats { FieldName = column.ColumnName };
            }

            try
            {
                // Configure CsvHelper
                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true,
                    MissingFieldFound = null,
                    BadDataFound = null,
                    IgnoreBlankLines = true,
                    TrimOptions = TrimOptions.Trim,
                    Mode = CsvMode.RFC4180,
                    ShouldQuote = args => args.Field.Contains(",") || args.Field.Contains("\"") || args.Field.Contains("\r") || args.Field.Contains("\n")
                };

                using (var reader = new StreamReader(filePath))
                using (var csv = new CsvReader(reader, config))
                {
                    // Read the header row
                    csv.Read();
                    csv.ReadHeader();

                    // Get all column indices
                    var columnIndices = new Dictionary<string, int>();

                    // Define all expected columns
                    var expectedColumns = new List<string>
                    {
                        "Account", "Rfc822MessageId", "GmailMessageId", "DriveUrl", "DriveItemId"
                    };

                    // Log available headers
                    string[] headers = csv.HeaderRecord;
                    ldLogger.LogInformation($"Drive Links CSV Headers found: {string.Join(", ", headers)}");

                    // Try to find each column index and log whether found
                    foreach (var column in expectedColumns)
                    {
                        int index = csv.GetFieldIndex(column);
                        columnIndices[column] = index;
                        ldLogger.LogInformation($"Column {column}: Index = {(index >= 0 ? index.ToString() : "Not Found")}");
                    }

                    // Read all records
                    int rowCount = 0;
                    while (csv.Read())
                    {
                        try
                        {
                            rowCount++;
                            DataRow row = dt.NewRow();

                            // Process each expected column
                            foreach (var column in expectedColumns)
                            {
                                int index = columnIndices[column];
                                string value = string.Empty;

                                if (index >= 0)
                                {
                                    try
                                    {
                                        value = csv.GetField(index) ?? string.Empty;
                                    }
                                    catch
                                    {
                                        value = string.Empty;
                                    }
                                }

                                // Update field statistics
                                fieldStats[column].AddValue(value);

                                // Add value to the data row
                                row[column] = value;
                            }

                            dt.Rows.Add(row);
                        }
                        catch (Exception ex)
                        {
                            ldLogger.LogWarning($"Error parsing row {rowCount}: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ldLogger.LogError(ex, $"Error processing Drive Links CSV file: {filePath}");
                throw;
            }

            return dt;
        }

        public string CreateModifiedGmailMetadataCsvFile(DataTable gmailMetadata, string originalFilePath, string fileLinkedDocumentValue, ILDLogger ldLogger)
        {
            ldLogger.LogInformation("Creating modified Gmail Metadata CSV file with identifiers");

            // Create a file in the same directory as the original but with a different name
            string directory = Path.GetDirectoryName(originalFilePath);
            string fileName = Path.GetFileNameWithoutExtension(originalFilePath);
            string newFilePath = Path.Combine(directory, $"{fileName}_with_identifiers.csv");

            try
            {
                // Configure writing to new CSV
                var writeConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true,
                    Mode = CsvMode.RFC4180,
                    ShouldQuote = args => args.Field.Contains(",") || args.Field.Contains("\"") || args.Field.Contains("\r") || args.Field.Contains("\n")
                };

                // Process all records and add required fields
                var records = new List<GmailCsvMetadata>();

                foreach (DataRow row in gmailMetadata.Rows)
                {
                    var gmailRecord = new GmailCsvMetadata
                    {
                        Rfc822MessageId = row["Rfc822MessageId"].ToString(),
                        GmailMessageId = row["GmailMessageId"].ToString(),
                        FileName = row["FileName"].ToString(),
                        Account = row["Account"].ToString(),
                        Labels = row["Labels"].ToString(),
                        From = row["From"].ToString(),
                        Subject = row["Subject"].ToString(),
                        To = row["To"].ToString(),
                        CC = row["CC"].ToString(),
                        BCC = row["BCC"].ToString(),
                        DateSent = row["DateSent"].ToString(),
                        DateReceived = row["DateReceived"].ToString(),
                        SubjectAtStart = row["SubjectAtStart"].ToString(),
                        SubjectAtEnd = row["SubjectAtEnd"].ToString(),
                        DateFirstMessageSent = row["DateFirstMessageSent"].ToString(),
                        DateLastMessageSent = row["DateLastMessageSent"].ToString(),
                        DateFirstMessageReceived = row["DateFirstMessageReceived"].ToString(),
                        DateLastMessageReceived = row["DateLastMessageReceived"].ToString(),
                        ThreadedMessageCount = row["ThreadedMessageCount"].ToString(),
                        Identifier = Guid.NewGuid().ToString(),
                        FileLinkedDocument = fileLinkedDocumentValue
                    };

                    records.Add(gmailRecord);
                }

                //Log the static value for FileLinkedDocument
                ldLogger.LogInformation($"Added 'FileLinkedDocument' column with static value: {fileLinkedDocumentValue} for all {records.Count} records");

                // Write all records to the new file
                using (var writer = new StreamWriter(newFilePath))
                using (var csvWriter = new CsvWriter(writer, writeConfig))
                {
                    // Write the header
                    csvWriter.WriteHeader<GmailCsvMetadata>();
                    csvWriter.NextRecord();

                    // Write all records
                    foreach (var record in records)
                    {
                        csvWriter.WriteRecord(record);
                        csvWriter.NextRecord();
                    }
                }

                ldLogger.LogInformation($"Created modified Gmail Metadata CSV file at: {newFilePath}");
                return newFilePath;
            }
            catch (Exception ex)
            {
                ldLogger.LogError(ex, $"Error creating modified Gmail Metadata CSV file: {ex.Message}");
                throw;
            }
        }

        public string CreateModifiedDriveLinksCsvFile(DataTable gmailDriveLink, string originalFilePath, string fileLinkedDocumentValue, ILDLogger ldLogger)
        {
            ldLogger.LogInformation("Creating modified Drive Links CSV file with identifiers");

            // Create a file in the same directory as the original but with a different name
            string directory = Path.GetDirectoryName(originalFilePath);
            string fileName = Path.GetFileNameWithoutExtension(originalFilePath);
            string newFilePath = Path.Combine(directory, $"{fileName}_with_identifiers.csv");

            try
            {
                // Configure writing to new CSV
                var writeConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true,
                    Mode = CsvMode.RFC4180,
                    ShouldQuote = args => args.Field.Contains(",") || args.Field.Contains("\"") || args.Field.Contains("\r") || args.Field.Contains("\n")
                };

                // Process all records and add required fields
                var records = new List<GmailCsvDriveLink>();

                foreach (DataRow row in gmailDriveLink.Rows)
                {
                    var driveRecord = new GmailCsvDriveLink
                    {
                        Account = row["Account"].ToString(),
                        Rfc822MessageId = row["Rfc822MessageId"].ToString(),
                        GmailMessageId = row["GmailMessageId"].ToString(),
                        DriveUrl = row["DriveUrl"].ToString(),
                        DriveItemId = row["DriveItemId"].ToString(),
                        Identifier = Guid.NewGuid().ToString(),
                        FileLinkedDocument = fileLinkedDocumentValue
                    };

                    records.Add(driveRecord);
                }

                // Write all records to the new file
                using (var writer = new StreamWriter(newFilePath))
                using (var csvWriter = new CsvWriter(writer, writeConfig))
                {
                    // Write the header
                    csvWriter.WriteHeader<GmailCsvDriveLink>();
                    csvWriter.NextRecord();

                    // Write all records
                    foreach (var record in records)
                    {
                        csvWriter.WriteRecord(record);
                        csvWriter.NextRecord();
                    }
                }

                ldLogger.LogInformation($"Created modified Drive Links CSV file at: {newFilePath}");
                return newFilePath;
            }
            catch (Exception ex)
            {
                ldLogger.LogError(ex, $"Error creating modified Drive Links CSV file: {ex.Message}");
                throw;
            }
        }
    }
}
