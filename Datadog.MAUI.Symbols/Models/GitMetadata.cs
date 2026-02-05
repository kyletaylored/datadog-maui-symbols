namespace Datadog.MAUI.Symbols.Models
{
    /// <summary>
    /// Git repository metadata for symbol upload.
    /// Maps to the optional 'repository' JSON field in the multipart upload.
    /// </summary>
    public class GitMetadata
    {
        /// <summary>
        /// Git commit SHA (full hash).
        /// </summary>
        public string Hash { get; set; } = null!;

        /// <summary>
        /// Git remote URL (typically origin).
        /// </summary>
        public string Remote { get; set; } = null!;

        /// <summary>
        /// List of tracked files in the repository.
        /// </summary>
        public string[] TrackedFiles { get; set; } = System.Array.Empty<string>();

        /// <summary>
        /// Creates the repository payload structure for the API.
        /// </summary>
        public object ToRepositoryPayload()
        {
            return new
            {
                data = new[]
                {
                    new
                    {
                        files = TrackedFiles,
                        hash = Hash,
                        repository_url = Remote
                    }
                },
                version = 1
            };
        }
    }
}
