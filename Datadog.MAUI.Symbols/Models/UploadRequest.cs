namespace Datadog.MAUI.Symbols.Models
{
    /// <summary>
    /// Request configuration for uploading symbols to Datadog.
    /// </summary>
    public class UploadRequest
    {
        /// <summary>
        /// Symbol metadata (event field).
        /// </summary>
        public SymbolMetadata Metadata { get; set; } = null!;

        /// <summary>
        /// Path to the symbol file on disk.
        /// </summary>
        public string FilePath { get; set; } = null!;

        /// <summary>
        /// Optional git metadata (repository field).
        /// </summary>
        public GitMetadata? GitMetadata { get; set; }

        /// <summary>
        /// Datadog API key for authentication.
        /// </summary>
        public string ApiKey { get; set; } = null!;

        /// <summary>
        /// Datadog site (e.g., "datadoghq.com", "us5.datadoghq.com").
        /// </summary>
        public string Site { get; set; } = "datadoghq.com";

        /// <summary>
        /// Enable verbose logging for debugging.
        /// </summary>
        public bool Verbose { get; set; } = false;
    }
}
