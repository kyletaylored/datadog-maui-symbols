namespace Datadog.MAUI.Symbols.Models
{
    /// <summary>
    /// Metadata for symbol file upload to Datadog.
    /// Maps to the 'event' JSON field in the multipart upload.
    /// </summary>
    public class SymbolMetadata
    {
        /// <summary>
        /// Architecture (e.g., "arm64", "x64"). Optional for Android, required for Dart symbols.
        /// </summary>
        public string? Arch { get; set; }

        /// <summary>
        /// Build ID - alternative to service/version/variant for Android.
        /// When provided, service/version/variant are not included.
        /// </summary>
        public string? BuildId { get; set; }

        /// <summary>
        /// CLI version making the upload. Always required.
        /// </summary>
        public string CliVersion { get; set; } = null!;

        /// <summary>
        /// Git commit SHA. Optional - included if git metadata is available.
        /// </summary>
        public string? GitCommitSha { get; set; }

        /// <summary>
        /// Git repository URL. Optional - included if git metadata is available.
        /// </summary>
        public string? GitRepositoryUrl { get; set; }

        /// <summary>
        /// Platform (e.g., "android", "ios"). Optional for Android, required for Dart symbols.
        /// </summary>
        public string? Platform { get; set; }

        /// <summary>
        /// Service name. Always required.
        /// </summary>
        public string? Service { get; set; }

        /// <summary>
        /// Symbol type (e.g., "jvm_mapping_file", "flutter_symbol_file", "ios_dsym").
        /// Always required.
        /// </summary>
        public string Type { get; set; } = null!;

        /// <summary>
        /// Build variant/flavor (e.g., "release", "debug"). Optional.
        /// </summary>
        public string? Variant { get; set; }

        /// <summary>
        /// App version. Always required.
        /// Build metadata (+ characters) should be replaced with dashes before setting.
        /// </summary>
        public string? Version { get; set; }
    }
}
