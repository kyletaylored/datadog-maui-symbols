using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Datadog.MAUI.Symbols.Tasks
{
    /// <summary>
    /// MSBuild task that generates a build ID for Android builds.
    /// The build ID is computed from the APK file hash or build configuration.
    /// </summary>
    public class GenerateBuildIdTask : Task
    {
        /// <summary>
        /// Path to the APK file. If specified, build ID will be based on APK hash.
        /// </summary>
        public string? ApkPath { get; set; }

        /// <summary>
        /// App version to include in build ID generation. Optional.
        /// </summary>
        public string? Version { get; set; }

        /// <summary>
        /// Build variant/flavor (e.g., release, debug). Optional.
        /// </summary>
        public string? Variant { get; set; }

        /// <summary>
        /// Version code (Android). Optional.
        /// </summary>
        public string? VersionCode { get; set; }

        /// <summary>
        /// Output parameter: The generated build ID.
        /// </summary>
        [Output]
        public string BuildId { get; set; } = null!;

        public override bool Execute()
        {
            try
            {
                // Strategy 1: Use APK file hash if available
                if (!string.IsNullOrEmpty(ApkPath) && File.Exists(ApkPath))
                {
                    BuildId = GenerateBuildIdFromApk(ApkPath!);
                    Log.LogMessage(MessageImportance.Normal, $"Generated build ID from APK: {BuildId}");
                    return true;
                }

                // Strategy 2: Use build configuration hash
                BuildId = GenerateBuildIdFromConfiguration();
                Log.LogMessage(MessageImportance.Normal, $"Generated build ID from configuration: {BuildId}");
                return true;
            }
            catch (Exception ex)
            {
                Log.LogError($"Failed to generate build ID: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Generates a build ID based on the APK file hash.
        /// Uses SHA256 of the APK file and takes the first 16 characters.
        /// </summary>
        private string GenerateBuildIdFromApk(string apkPath)
        {
            using (var stream = File.OpenRead(apkPath))
            using (var sha256 = SHA256.Create())
            {
                var hashBytes = sha256.ComputeHash(stream);
                var hashString = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                return hashString.Substring(0, 16);
            }
        }

        /// <summary>
        /// Generates a build ID based on build configuration.
        /// Creates a hash from version, variant, and version code.
        /// </summary>
        private string GenerateBuildIdFromConfiguration()
        {
            var configString = $"{Version ?? "unknown"}-{Variant ?? "unknown"}-{VersionCode ?? "0"}-{DateTime.UtcNow:yyyyMMddHHmmss}";

            using (var sha256 = SHA256.Create())
            {
                var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(configString));
                var hashString = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                return hashString.Substring(0, 16);
            }
        }
    }
}
