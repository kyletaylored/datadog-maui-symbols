using System;
using System.IO;
using System.Reflection;
using Datadog.MAUI.Symbols.Core;
using Datadog.MAUI.Symbols.Models;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Task = Microsoft.Build.Utilities.Task;

namespace Datadog.MAUI.Symbols.Tasks
{
    /// <summary>
    /// MSBuild task that uploads symbol files to Datadog.
    /// </summary>
    public class UploadSymbolsTask : Task
    {
        private const string LogPrefix = "[Datadog.Symbols]";

        /// <summary>
        /// Datadog API key for authentication. Required.
        /// </summary>
        [Required]
        public string ApiKey { get; set; } = null!;

        /// <summary>
        /// Service name. Always required.
        /// </summary>
        [Required]
        public string ServiceName { get; set; } = null!;

        /// <summary>
        /// App version. Always required.
        /// </summary>
        [Required]
        public string Version { get; set; } = null!;

        /// <summary>
        /// Build variant/flavor (e.g., release, debug). Optional.
        /// </summary>
        public string? Variant { get; set; }

        /// <summary>
        /// Build ID - alternative to service/version/variant. Optional.
        /// </summary>
        public string? BuildId { get; set; }

        /// <summary>
        /// Platform (android, ios, flutter). Optional - auto-detected from TargetPlatformIdentifier if not specified.
        /// </summary>
        public string? Platform { get; set; }

        /// <summary>
        /// Architecture (arm64, x64, etc.). Optional - auto-detected from RuntimeIdentifier if not specified.
        /// </summary>
        public string? Arch { get; set; }

        /// <summary>
        /// Symbol type (jvm_mapping_file, flutter_symbol_file, ios_dsym). Optional - auto-detected from Platform if not specified.
        /// </summary>
        public string? SymbolType { get; set; }

        /// <summary>
        /// MSBuild TargetPlatformIdentifier (e.g., "android", "ios"). Used for auto-detecting Platform and SymbolType.
        /// </summary>
        public string? TargetPlatformIdentifier { get; set; }

        /// <summary>
        /// MSBuild RuntimeIdentifier (e.g., "ios-arm64", "iossimulator-arm64"). Used for auto-detecting Arch.
        /// </summary>
        public string? RuntimeIdentifier { get; set; }

        /// <summary>
        /// Path to the symbol file. If not specified, will attempt to auto-locate.
        /// </summary>
        public string? SymbolFilePath { get; set; }

        /// <summary>
        /// Project directory for auto-locating symbol files. Defaults to current directory.
        /// </summary>
        public string? ProjectDirectory { get; set; }

        /// <summary>
        /// Datadog site (e.g., datadoghq.com, us5.datadoghq.com). Defaults to datadoghq.com.
        /// </summary>
        public string Site { get; set; } = "datadoghq.com";

        /// <summary>
        /// Disables git operations and prevents sending repository metadata. Defaults to false.
        /// When true, git will not be invoked and no repository data will be sent.
        /// </summary>
        public bool DisableGit { get; set; } = false;

        /// <summary>
        /// Custom repository URL to override the git-detected remote URL.
        /// Useful in CI/CD environments where git config may not be available.
        /// </summary>
        public string? RepositoryUrl { get; set; }

        /// <summary>
        /// [Deprecated] Use DisableGit instead. Whether to include git metadata in the upload. Defaults to true.
        /// </summary>
        [Obsolete("Use DisableGit instead for CLI consistency")]
        public bool IncludeGitMetadata { get; set; } = true;

        /// <summary>
        /// Skip upload if true. Useful for conditional builds. Defaults to false.
        /// </summary>
        public bool Skip { get; set; } = false;

        /// <summary>
        /// Dry run mode - performs all checks and preparations but skips the actual upload. Defaults to false.
        /// </summary>
        public bool DryRun { get; set; } = false;

        /// <summary>
        /// Skip API key validation. Useful for air-gapped environments or testing. Defaults to false.
        /// </summary>
        public bool SkipApiKeyValidation { get; set; } = false;

        /// <summary>
        /// Enable verbose logging including API payload debugging. Defaults to false.
        /// </summary>
        public bool Verbose { get; set; } = false;

        public override bool Execute()
        {
            if (Skip)
            {
                Console.WriteLine("⚠️  Symbol upload skipped.");
                Console.WriteLine("To enable symbol upload, ensure DatadogApiKey is set via environment variable or project property.");
                return true;
            }

            try
            {
                var result = ExecuteAsync().GetAwaiter().GetResult();
                return result;
            }
            catch (Exception ex)
            {
                Log.LogError($"Symbol upload failed: {ex.Message}");
                Log.LogError($"Exception: {ex.GetType().Name}");
                if (ex.InnerException != null)
                {
                    Log.LogError($"Inner exception: {ex.InnerException.Message}");
                }
                return false;
            }
        }

        private async System.Threading.Tasks.Task<bool> ExecuteAsync()
        {
            var startTime = DateTime.UtcNow;

            // Auto-detect platform properties if not specified
            DetectPlatformProperties();

            // Validate inputs
            if (!ValidateInputs(out var validationError))
            {
                Log.LogError(validationError);
                return false;
            }

            // Validate API key before proceeding (skip in dry run mode or if explicitly disabled)
            if (!DryRun && !SkipApiKeyValidation)
            {
                Console.WriteLine("Validating API key...");
                using (var validator = new ApiKeyValidator())
                {
                    var isValidKey = await validator.ValidateApiKeyAsync(ApiKey, Site);
                    if (!isValidKey)
                    {
                        Log.LogError($"Invalid API key for Datadog site {Site}");
                        Log.LogError("Please verify your DATADOG_API_KEY environment variable or DatadogApiKey property.");
                        Log.LogError("To skip validation, set DatadogSkipApiKeyValidation=true");
                        return false;
                    }
                }
                Console.WriteLine("[Datadog.Symbols] API key validated successfully.");
            }

            // Determine symbol file path
            var symbolFilePath = SymbolFilePath;
            if (string.IsNullOrEmpty(symbolFilePath))
            {
                symbolFilePath = LocateSymbolFile();
                if (string.IsNullOrEmpty(symbolFilePath))
                {
                    Log.LogError($"Could not locate symbol file for platform '{Platform}'. Please specify SymbolFilePath explicitly.");
                    return false;
                }
            }

            if (!File.Exists(symbolFilePath))
            {
                Log.LogError($"Symbol file not found: {symbolFilePath}");
                return false;
            }

            // Build metadata
            var metadata = BuildMetadata();

            // Log upload information
            Console.WriteLine($"{LogPrefix} Starting symbol upload.");
            Console.WriteLine($"{LogPrefix} Uploading {GetSymbolTypeDescription(SymbolType)} at location {symbolFilePath}");

            if (!string.IsNullOrEmpty(BuildId))
            {
                Console.WriteLine($"{LogPrefix}   build_id: {BuildId}");
            }
            else
            {
                Console.WriteLine($"{LogPrefix}   version: {Version} service: {ServiceName}" +
                    (string.IsNullOrEmpty(Variant) ? "" : $" variant: {Variant}"));
                Console.WriteLine($"{LogPrefix} Please ensure you use the same values during SDK initialization to guarantee the success of the symbolication process.");
            }

            // Collect git metadata if enabled
            // DisableGit takes precedence over IncludeGitMetadata (backwards compatibility)
            GitMetadata? gitMetadata = null;
#pragma warning disable CS0618 // IncludeGitMetadata is obsolete but kept for backwards compatibility
            var shouldCollectGit = !DisableGit && IncludeGitMetadata;
#pragma warning restore CS0618

            if (shouldCollectGit)
            {
                var projectDir = ProjectDirectory ?? Directory.GetCurrentDirectory();
                var gitCollector = new GitMetadataCollector(projectDir, RepositoryUrl);
                gitMetadata = gitCollector.CollectMetadata();

                if (gitMetadata != null)
                {
                    Console.WriteLine($"Collected git metadata: commit {gitMetadata.Hash.Substring(0, Math.Min(7, gitMetadata.Hash.Length))}, {gitMetadata.TrackedFiles.Length} tracked files");
                }
                else
                {
                    Console.WriteLine($"{LogPrefix} ⚠️  Warning: Could not collect git metadata.");
                    Console.WriteLine($"{LogPrefix}     Make sure you're running within a git repository to enable commit tracking.");
                    Console.WriteLine($"{LogPrefix}     To disable this warning: set DatadogDisableGit=true");
                }
            }

            // Create upload request
            var request = new UploadRequest
            {
                Metadata = metadata,
                FilePath = symbolFilePath!,
                GitMetadata = gitMetadata,
                ApiKey = ApiKey,
                Site = Site,
                Verbose = Verbose
            };

            // Get file size information
            var fileInfo = new FileInfo(symbolFilePath!);
            var fileSizeMB = fileInfo.Length / (1024.0 * 1024.0);

            // Handle dry-run mode
            if (DryRun)
            {
                Console.WriteLine("");
                Console.WriteLine("🔍 DRY RUN MODE - No files will be uploaded");
                Console.WriteLine("");

                var intakeHost = $"sourcemap-intake.{Site}";
                var requestUri = $"https://{intakeHost}/api/v2/srcmap";

                Console.WriteLine($"File: {Path.GetFileName(symbolFilePath)}");
                Console.WriteLine($"Size: {fileSizeMB:F2} MB");
                Console.WriteLine($"Type: {GetSymbolTypeDescription(SymbolType)}");
                Console.WriteLine($"Endpoint: {requestUri}");
                Console.WriteLine($"Site: {Site}");
                Console.WriteLine($"Service: {ServiceName}");
                Console.WriteLine($"Version: {Version}");

                if (!string.IsNullOrEmpty(Variant))
                {
                    Console.WriteLine($"Variant: {Variant}");
                }

                if (!string.IsNullOrEmpty(BuildId))
                {
                    Console.WriteLine($"Build ID: {BuildId}");
                }

                if (gitMetadata != null)
                {
                    Console.WriteLine($"Git commit: {gitMetadata.Hash.Substring(0, Math.Min(7, gitMetadata.Hash.Length))}");
                    Console.WriteLine($"Git tracked files: {gitMetadata.TrackedFiles.Length}");
                }

                var elapsed = DateTime.UtcNow - startTime;
                Console.WriteLine("");
                Console.WriteLine($"✅ DRY RUN: All checks passed. Upload would succeed. ({elapsed.TotalSeconds:F3}s)");
                return true;
            }

            // Upload
            Console.WriteLine($"{LogPrefix} Uploading {GetSymbolTypeDescription(SymbolType)} {Path.GetFileName(symbolFilePath)} ({fileSizeMB:F2} MB)");

            using (var client = new DatadogApiClient())
            {
                var result = await client.UploadAsync(request);

                if (result.Success)
                {
                    var elapsed = DateTime.UtcNow - startTime;
                    Console.WriteLine("Symbol upload finished");
                    Console.WriteLine("After upload is successful, symbol files will be processed and ready to use within the next 5 minutes.");
                    Console.WriteLine("");
                    Console.WriteLine($"✅ Uploaded symbol 1 file ({fileSizeMB:F2} MB) in {elapsed.TotalSeconds:F3} seconds.");
                    return true;
                }
                else
                {
                    Console.WriteLine("");
                    Console.WriteLine($"{LogPrefix} ❌ Symbol upload failed: {result.ErrorMessage}");
                    if (result.StatusCode.HasValue)
                    {
                        Console.WriteLine($"{LogPrefix}    HTTP Status Code: {result.StatusCode.Value}");
                    }
                    // Log one error so MSBuild knows something failed, but keep it concise
                    Log.LogError("Symbol upload failed. See output above for details.");
                    return false;
                }
            }
        }

        private string GetSymbolTypeDescription(string? symbolType)
        {
            return symbolType switch
            {
                "jvm_mapping_file" => "Android ProGuard/R8 Mapping File",
                "ios_dsym" => "iOS dSYM Bundle",
                "flutter_symbol_file" => "Flutter Symbol File",
                null => "Unknown",
                _ => symbolType
            };
        }

        private bool ValidateInputs(out string? errorMessage)
        {
            // ServiceName and Version are always required
            if (string.IsNullOrEmpty(ServiceName))
            {
                errorMessage = "ServiceName is required";
                return false;
            }

            if (string.IsNullOrEmpty(Version))
            {
                errorMessage = "Version is required";
                return false;
            }

            errorMessage = null;
            return true;
        }

        private string? LocateSymbolFile()
        {
            var projectDir = ProjectDirectory ?? Directory.GetCurrentDirectory();
            var locator = new SymbolFileLocator();
            return locator.LocateSymbolFile(projectDir, Platform ?? string.Empty, Variant);
        }

        private SymbolMetadata BuildMetadata()
        {
            var cliVersion = GetCliVersion();

            return new SymbolMetadata
            {
                Arch = Arch,
                BuildId = BuildId,
                CliVersion = cliVersion,
                Platform = Platform,
                Service = ServiceName,
                Type = SymbolType ?? string.Empty,
                Variant = Variant,
                Version = Version
            };
        }

        private string GetCliVersion()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var version = assembly.GetName().Version;
                return version != null ? $"datadog-maui-symbols-upload/{version}" : "datadog-maui-symbols-upload/unknown";
            }
            catch
            {
                return "datadog-maui-symbols-upload/unknown";
            }
        }

        /// <summary>
        /// Auto-detects platform properties from MSBuild properties if not explicitly set.
        /// </summary>
        private void DetectPlatformProperties()
        {
            // Auto-detect Platform from TargetPlatformIdentifier
            if (string.IsNullOrEmpty(Platform) && !string.IsNullOrEmpty(TargetPlatformIdentifier))
            {
                Platform = TargetPlatformIdentifier!.ToLowerInvariant();
            }

            // Auto-detect SymbolType from Platform
            if (string.IsNullOrEmpty(SymbolType) && !string.IsNullOrEmpty(Platform))
            {
                switch (Platform!.ToLowerInvariant())
                {
                    case "android":
                        SymbolType = "jvm_mapping_file";
                        break;
                    case "ios":
                        SymbolType = "ios_dsym";
                        break;
                    case "flutter":
                        SymbolType = "flutter_symbol_file";
                        break;
                }
            }

            // Auto-detect Arch from RuntimeIdentifier
            if (string.IsNullOrEmpty(Arch) && !string.IsNullOrEmpty(RuntimeIdentifier))
            {
                var rid = RuntimeIdentifier!.ToLowerInvariant();
                if (rid.Contains("arm64"))
                {
                    Arch = "arm64";
                }
                else if (rid.Contains("x64") || rid.Contains("x86_64"))
                {
                    Arch = "x64";
                }
                else if (rid.Contains("x86"))
                {
                    Arch = "x86";
                }
            }
        }
    }
}
