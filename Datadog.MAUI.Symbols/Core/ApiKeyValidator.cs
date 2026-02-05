using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Datadog.MAUI.Symbols.Core
{
    /// <summary>
    /// Validates Datadog API keys before attempting symbol uploads.
    /// Provides fast-fail behavior for invalid credentials.
    /// </summary>
    internal class ApiKeyValidator : IDisposable
    {
        private readonly HttpClient _httpClient;

        public ApiKeyValidator()
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(10)
            };
        }

        /// <summary>
        /// Validates an API key by making a request to the Datadog validation endpoint.
        /// </summary>
        /// <param name="apiKey">The API key to validate</param>
        /// <param name="site">The Datadog site (e.g., datadoghq.com, us3.datadoghq.com)</param>
        /// <returns>True if the API key is valid, false otherwise</returns>
        public async Task<bool> ValidateApiKeyAsync(string apiKey, string site)
        {
            if (string.IsNullOrEmpty(apiKey))
            {
                return false;
            }

            try
            {
                var validationUrl = GetValidationUrl(site);
                var request = new HttpRequestMessage(HttpMethod.Get, validationUrl);
                request.Headers.Add("DD-API-KEY", apiKey);

                var response = await _httpClient.SendAsync(request);

                // 200 OK means valid key
                // 403 Forbidden means invalid key
                return response.IsSuccessStatusCode;
            }
            catch (HttpRequestException)
            {
                // Network errors - assume key could be valid but network is down
                // Let the upload attempt proceed and fail there if needed
                return true;
            }
            catch (TaskCanceledException)
            {
                // Timeout - assume key could be valid but network is slow
                return true;
            }
            catch (Exception)
            {
                // Other errors - assume key could be valid
                return true;
            }
        }

        /// <summary>
        /// Gets the API key validation URL for the specified Datadog site.
        /// </summary>
        private string GetValidationUrl(string site)
        {
            return $"https://api.{site}/api/v1/validate";
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
