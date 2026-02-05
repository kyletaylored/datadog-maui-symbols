using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Datadog.MAUI.SymbolsUpload.Models;

namespace Datadog.MAUI.SymbolsUpload.Core
{
    /// <summary>
    /// HTTP client for uploading symbols to the Datadog API.
    /// </summary>
    internal class DatadogApiClient : IDisposable
    {
        private const string ApiPath = "/api/v2/srcmap";
        private readonly HttpClient _httpClient;
        private readonly MetadataBuilder _metadataBuilder;
        private bool _disposed;

        public DatadogApiClient()
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(5)
            };
            _metadataBuilder = new MetadataBuilder();
        }

        /// <summary>
        /// Uploads a symbol file to Datadog with the provided metadata.
        /// </summary>
        public async Task<UploadResult> UploadAsync(UploadRequest request)
        {
            try
            {
                // Validate metadata
                if (!_metadataBuilder.ValidateMetadata(request.Metadata, out var validationError))
                {
                    return UploadResult.CreateFailure($"Metadata validation failed: {validationError}");
                }

                // Validate file exists
                if (!File.Exists(request.FilePath))
                {
                    return UploadResult.CreateFailure($"Symbol file not found: {request.FilePath}");
                }

                // Build the request URI
                // The sourcemap intake endpoint is sourcemap-intake.<site>
                var intakeHost = $"sourcemap-intake.{request.Site}";
                var requestUri = $"https://{intakeHost}{ApiPath}";
                using (var multipartContent = BuildMultipartContent(request))
                using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, requestUri))
                {
                    // Compress the multipart content with gzip
                    var compressedContent = await CompressContentAsync(multipartContent).ConfigureAwait(false);
                    requestMessage.Content = compressedContent;

                    // Add required headers
                    requestMessage.Headers.Add("DD-API-KEY", request.ApiKey);
                    requestMessage.Headers.Add("DD-EVP-ORIGIN", "datadog-maui-symbols-upload");
                    requestMessage.Headers.Add("DD-EVP-ORIGIN-VERSION", GetVersion());

                    // Verbose logging of complete HTTP request
                    if (request.Verbose)
                    {
                        Console.WriteLine("[Datadog.Symbols] === HTTP Request Debug ===");
                        Console.WriteLine($"[Datadog.Symbols] Method: {requestMessage.Method}");
                        Console.WriteLine($"[Datadog.Symbols] URL: {requestUri}");
                        Console.WriteLine($"[Datadog.Symbols] ");
                        Console.WriteLine($"[Datadog.Symbols] Request Headers:");
                        foreach (var header in requestMessage.Headers)
                        {
                            var value = header.Key == "DD-API-KEY" ? "***masked***" : string.Join(", ", header.Value);
                            Console.WriteLine($"[Datadog.Symbols]   {header.Key}: {value}");
                        }
                        Console.WriteLine($"[Datadog.Symbols] ");
                        Console.WriteLine($"[Datadog.Symbols] Content Headers:");
                        foreach (var header in requestMessage.Content.Headers)
                        {
                            Console.WriteLine($"[Datadog.Symbols]   {header.Key}: {string.Join(", ", header.Value)}");
                        }
                    }

                    // Send the request
                    var response = await _httpClient.SendAsync(requestMessage).ConfigureAwait(false);

                    // Verbose logging of response
                    if (request.Verbose)
                    {
                        Console.WriteLine($"[Datadog.Symbols] ");
                        Console.WriteLine($"[Datadog.Symbols] === HTTP Response ===");
                        Console.WriteLine($"[Datadog.Symbols] Status: {(int)response.StatusCode} {response.ReasonPhrase}");
                        Console.WriteLine($"[Datadog.Symbols] ");
                        Console.WriteLine($"[Datadog.Symbols] Response Headers:");
                        foreach (var header in response.Headers)
                        {
                            Console.WriteLine($"[Datadog.Symbols]   {header.Key}: {string.Join(", ", header.Value)}");
                        }
                    }

                    // Accept both 200 OK and 202 Accepted as success
                    if (response.IsSuccessStatusCode)
                    {
                        if (request.Verbose)
                        {
                            var successBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                            if (!string.IsNullOrEmpty(successBody))
                            {
                                Console.WriteLine($"[Datadog.Symbols] ");
                                Console.WriteLine($"[Datadog.Symbols] Response Body:");
                                Console.WriteLine(successBody);
                            }
                        }
                        return UploadResult.CreateSuccess();
                    }
                    else
                    {
                        var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        if (request.Verbose && !string.IsNullOrEmpty(responseBody))
                        {
                            Console.WriteLine($"[Datadog.Symbols] ");
                            Console.WriteLine($"[Datadog.Symbols] Response Body:");
                            Console.WriteLine(responseBody);
                        }
                        return UploadResult.CreateFailure(
                            $"Upload failed: {response.ReasonPhrase}. Response: {responseBody}",
                            (int)response.StatusCode
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                return UploadResult.CreateFailure($"Upload exception: {ex.Message}");
            }
        }

        /// <summary>
        /// Builds the multipart form-data content for the upload request.
        /// </summary>
        private MultipartFormDataContent BuildMultipartContent(UploadRequest request)
        {
            var content = new MultipartFormDataContent();

            if (request.Verbose)
            {
                Console.WriteLine("[Datadog.Symbols] === Multipart Form Data ===");
                Console.WriteLine($"[Datadog.Symbols] ");
            }

            // Add metadata as individual form fields (not wrapped in JSON)
            var metadata = request.Metadata;

            // Add required fields
            content.Add(new StringContent(metadata.Type), "type");
            if (request.Verbose)
                Console.WriteLine($"[Datadog.Symbols] Field: 'type' = '{metadata.Type}'");

            if (!string.IsNullOrEmpty(metadata.Service))
            {
                content.Add(new StringContent(metadata.Service), "service");
                if (request.Verbose)
                    Console.WriteLine($"[Datadog.Symbols] Field: 'service' = '{metadata.Service}'");
            }

            if (!string.IsNullOrEmpty(metadata.Version))
            {
                content.Add(new StringContent(metadata.Version), "version");
                if (request.Verbose)
                    Console.WriteLine($"[Datadog.Symbols] Field: 'version' = '{metadata.Version}'");
            }

            // Add optional fields
            if (!string.IsNullOrEmpty(metadata.Platform))
            {
                content.Add(new StringContent(metadata.Platform), "platform");
                if (request.Verbose)
                    Console.WriteLine($"[Datadog.Symbols] Field: 'platform' = '{metadata.Platform}'");
            }

            if (!string.IsNullOrEmpty(metadata.Variant))
            {
                content.Add(new StringContent(metadata.Variant), "variant");
                if (request.Verbose)
                    Console.WriteLine($"[Datadog.Symbols] Field: 'variant' = '{metadata.Variant}'");
            }

            if (!string.IsNullOrEmpty(metadata.BuildId))
            {
                content.Add(new StringContent(metadata.BuildId), "build_id");
                if (request.Verbose)
                    Console.WriteLine($"[Datadog.Symbols] Field: 'build_id' = '{metadata.BuildId}'");
            }

            if (!string.IsNullOrEmpty(metadata.Arch))
            {
                content.Add(new StringContent(metadata.Arch), "arch");
                if (request.Verbose)
                    Console.WriteLine($"[Datadog.Symbols] Field: 'arch' = '{metadata.Arch}'");
            }

            if (!string.IsNullOrEmpty(metadata.GitCommitSha))
            {
                content.Add(new StringContent(metadata.GitCommitSha), "git_commit_sha");
                if (request.Verbose)
                    Console.WriteLine($"[Datadog.Symbols] Field: 'git_commit_sha' = '{metadata.GitCommitSha}'");
            }

            if (!string.IsNullOrEmpty(metadata.GitRepositoryUrl))
            {
                content.Add(new StringContent(metadata.GitRepositoryUrl), "git_repository_url");
                if (request.Verbose)
                    Console.WriteLine($"[Datadog.Symbols] Field: 'git_repository_url' = '{metadata.GitRepositoryUrl}'");
            }

            // Add symbol file
            var fileName = Path.GetFileName(request.FilePath);
            var fileBytes = File.ReadAllBytes(request.FilePath);
            var fileContent = new ByteArrayContent(fileBytes);
            var symbolFieldName = GetSymbolFileFieldName(request.Metadata.Type);

            if (request.Verbose)
            {
                Console.WriteLine($"[Datadog.Symbols] ");
                Console.WriteLine($"[Datadog.Symbols] Field: '{symbolFieldName}' (file)");
                Console.WriteLine($"[Datadog.Symbols]   Filename: {fileName}");
                Console.WriteLine($"[Datadog.Symbols]   Size: {fileBytes.Length:N0} bytes");
            }

            content.Add(fileContent, symbolFieldName, fileName);

            // Add repository metadata if available (as JSON)
            if (request.GitMetadata != null)
            {
                var repositoryPayload = request.GitMetadata.ToRepositoryPayload();
                var repositoryJson = System.Text.Json.JsonSerializer.Serialize(repositoryPayload, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = request.Verbose
                });

                if (request.Verbose)
                {
                    Console.WriteLine($"[Datadog.Symbols] ");
                    Console.WriteLine($"[Datadog.Symbols] Field: 'repository' (application/json)");
                    Console.WriteLine(repositoryJson);
                }

                content.Add(new StringContent(repositoryJson, Encoding.UTF8, "application/json"), "repository");
            }

            return content;
        }

        /// <summary>
        /// Gets the multipart field name for the symbol file based on its type.
        /// </summary>
        private string GetSymbolFileFieldName(string symbolType)
        {
            return symbolType switch
            {
                "jvm_mapping_file" => "jvm_mapping_file",
                "flutter_symbol_file" => "flutter_symbol_file",
                "ios_dsym" => "dsym",
                _ => "symbol_file"
            };
        }

        /// <summary>
        /// Compresses the HTTP content using gzip compression.
        /// </summary>
        private async Task<HttpContent> CompressContentAsync(HttpContent content)
        {
            // Read the original content
            var originalBytes = await content.ReadAsByteArrayAsync().ConfigureAwait(false);

            // Compress with gzip
            using (var compressedStream = new MemoryStream())
            {
                using (var gzipStream = new GZipStream(compressedStream, CompressionLevel.Optimal, true))
                {
                    await gzipStream.WriteAsync(originalBytes, 0, originalBytes.Length).ConfigureAwait(false);
                }

                var compressedBytes = compressedStream.ToArray();
                var compressedContent = new ByteArrayContent(compressedBytes);

                // Copy important headers from original content
                if (content.Headers.ContentType != null)
                {
                    compressedContent.Headers.ContentType = content.Headers.ContentType;
                }

                // Add Content-Encoding header
                compressedContent.Headers.ContentEncoding.Add("gzip");

                return compressedContent;
            }
        }

        /// <summary>
        /// Gets the version of this assembly.
        /// </summary>
        private string GetVersion()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var version = assembly.GetName().Version;
                return version != null ? version.ToString() : "0.1.0";
            }
            catch
            {
                return "0.1.0";
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _httpClient?.Dispose();
                _disposed = true;
            }
        }
    }
}
