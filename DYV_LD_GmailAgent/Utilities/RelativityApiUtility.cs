using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using DYV_Linked_Document_Management.Logging;
using Relativity.API;

namespace DYV_Linked_Document_Management.Utilities
{
    /// <summary>
    /// Utility methods for interacting with Relativity APIs
    /// </summary>
    public static class RelativityApiUtility
    {
        /// <summary>
        /// Creates and configures an HTTP client for communicating with Relativity APIs using token authentication
        /// </summary>
        /// <param name="ldLogger">Logger for logging messages</param>
        /// <param name="helper">Relativity API helper</param>
        /// <returns>A configured HttpClient instance</returns>
        public static async Task<HttpClient> CreateHttpClientWithTokenAsync(ILDLogger ldLogger, IHelper helper)
        {
            // Get credentials from instance settings
            var (clientId, clientSecret, instanceUrl, _) = await GetCredentialsAsync(helper, ldLogger);

            // Get access token
            string accessToken = await GetAccessTokenAsync(clientId, clientSecret, instanceUrl, ldLogger);
            if (string.IsNullOrEmpty(accessToken))
            {
                throw new Exception("Failed to obtain access token for Relativity API");
            }

            // Set base address for your Relativity instance
            var client = new HttpClient
            {
                BaseAddress = new Uri(instanceUrl),
                Timeout = TimeSpan.FromMinutes(5) // Increase timeout for larger payloads
            };

            // Using Bearer token authentication
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            // Add required headers
            client.DefaultRequestHeaders.Add("X-CSRF-Header", "-");

            return client;
        }

        /// <summary>
        /// Retrieves required credentials from instance settings
        /// </summary>
        /// <param name="helper">Relativity API helper</param>
        /// <param name="ldLogger">Logger for logging messages</param>
        /// <returns>Tuple containing credentials</returns>
        private static async Task<(string clientId, string clientSecret, string instanceUrl, string instanceId)> GetCredentialsAsync(IHelper helper, ILDLogger ldLogger)
        {
            ldLogger.LogInformation("Retrieving credentials from instance settings");
            try
            {
                var instanceSettings = helper.GetInstanceSettingBundle();
                var clientId = await instanceSettings.GetStringAsync("LTAS Billing Management", "SecurityClientId");
                var clientSecret = await instanceSettings.GetStringAsync("LTAS Billing Management", "SecurityClientSecret");
                var instanceUrl = await instanceSettings.GetStringAsync("Relativity.Core", "RelativityInstanceURL");
                var instanceId = await instanceSettings.GetStringAsync("Relativity.Core", "InstanceIdentifier");

                if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret) ||
                    string.IsNullOrEmpty(instanceUrl) || string.IsNullOrEmpty(instanceId))
                {
                    ldLogger.LogError("One or more required credentials are empty: " +
                        $"ClientId={!string.IsNullOrEmpty(clientId)}, " +
                        $"ClientSecret={!string.IsNullOrEmpty(clientSecret)}, " +
                        $"InstanceUrl={!string.IsNullOrEmpty(instanceUrl)}, " +
                        $"InstanceId={!string.IsNullOrEmpty(instanceId)}");

                    return (string.Empty, string.Empty, string.Empty, string.Empty);
                }

                ldLogger.LogInformation("Successfully retrieved all required credentials");
                return (clientId, clientSecret, instanceUrl, instanceId);
            }
            catch (Exception ex)
            {
                ldLogger.LogError(ex, "Error retrieving credentials from instance settings");
                return (string.Empty, string.Empty, string.Empty, string.Empty);
            }
        }

        /// <summary>
        /// Gets an access token for the Relativity API
        /// </summary>
        /// <param name="clientId">OAuth client ID</param>
        /// <param name="clientSecret">OAuth client secret</param>
        /// <param name="instanceUrl">Relativity instance URL</param>
        /// <param name="ldLogger">Logger for logging messages</param>
        /// <returns>Access token string</returns>
        private static async Task<string> GetAccessTokenAsync(string clientId, string clientSecret, string instanceUrl, ILDLogger ldLogger)
        {
            ldLogger.LogInformation("Attempting to get access token");
            try
            {
                var tokenUrl = $"{instanceUrl}/Identity/connect/token";
                ldLogger.LogInformation($"Requesting new token from {tokenUrl}");

                var tokenRequest = new Dictionary<string, string>
                {
                    { "client_id", clientId },
                    { "client_secret", clientSecret },
                    { "scope", "SystemUserInfo" },
                    { "grant_type", "client_credentials" }
                };

                using (var httpClient = new HttpClient())
                {
                    var content = new FormUrlEncodedContent(tokenRequest);
                    var response = await httpClient.PostAsync(tokenUrl, content);

                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        ldLogger.LogError($"Failed to get token. Status: {response.StatusCode}, Error: {errorContent}");
                        return string.Empty;
                    }

                    var responseContent = await response.Content.ReadAsStringAsync();

                    // Configure JsonSerializerOptions to handle property name casing
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };

                    var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(responseContent, options);

                    if (string.IsNullOrEmpty(tokenResponse?.AccessToken))
                    {
                        ldLogger.LogError("Received empty access token");
                        return string.Empty;
                    }

                    ldLogger.LogInformation("Successfully obtained access token");
                    return tokenResponse.AccessToken;
                }
            }
            catch (Exception ex)
            {
                ldLogger.LogError(ex, "Error getting access token");
                return string.Empty;
            }
        }

        /// <summary>
        /// Ensures that an HTTP response from the Relativity API is successful
        /// </summary>
        /// <param name="response">The HTTP response to check</param>
        /// <param name="errorMessage">The error message prefix to use if the response is not successful</param>
        /// <param name="ldLogger">The logger to use for logging errors</param>
        public static async Task EnsureSuccessResponse(HttpResponseMessage response, string errorMessage, ILDLogger ldLogger)
        {
            if (!response.IsSuccessStatusCode)
            {
                string content = await response.Content.ReadAsStringAsync();
                ldLogger.LogError($"{errorMessage}: {response.StatusCode} - {content}");
                throw new Exception($"{errorMessage}: {response.StatusCode} - {content}");
            }

            // For successful responses, deserialize and check if IsSuccess is true
            try
            {
                string json = await response.Content.ReadAsStringAsync();

                // Configure JsonSerializerOptions to handle property name casing
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var responseObj = JsonSerializer.Deserialize<Response>(json, options);

                if (responseObj != null && !responseObj.IsSuccess)
                {
                    ldLogger.LogError($"{errorMessage}: API returned IsSuccess=false, ErrorCode={responseObj.ErrorCode}, ErrorMessage={responseObj.ErrorMessage}");
                    throw new Exception($"{errorMessage}: API returned IsSuccess=false, ErrorCode={responseObj.ErrorCode}, ErrorMessage={responseObj.ErrorMessage}");
                }
            }
            catch (JsonException)
            {
                // If deserialization fails, just log it but don't throw since the HTTP status was successful
                ldLogger.LogWarning("Response could not be deserialized to Response object, but HTTP status was successful");
            }
        }

        /// <summary>
        /// Class representing the token response from the Relativity Identity API
        /// </summary>
        private class TokenResponse
        {
            [JsonPropertyName("access_token")]
            public string AccessToken { get; set; }

            [JsonPropertyName("expires_in")]
            public int ExpiresIn { get; set; }

            [JsonPropertyName("token_type")]
            public string TokenType { get; set; }

            [JsonPropertyName("scope")]
            public string Scope { get; set; }
        }

        /// <summary>
        /// Response object for Relativity APIs
        /// </summary>
        private class Response
        {
            public bool IsSuccess { get; set; }
            public string ErrorCode { get; set; }
            public string ErrorMessage { get; set; }
        }
    }
}