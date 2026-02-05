namespace Datadog.MAUI.Symbols.Models
{
    /// <summary>
    /// Result of a symbol upload operation.
    /// </summary>
    public class UploadResult
    {
        /// <summary>
        /// Whether the upload was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Error message if upload failed.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// HTTP status code from the API response.
        /// </summary>
        public int? StatusCode { get; set; }

        /// <summary>
        /// Creates a successful upload result.
        /// </summary>
        public static UploadResult CreateSuccess()
        {
            return new UploadResult { Success = true };
        }

        /// <summary>
        /// Creates a failed upload result with an error message.
        /// </summary>
        public static UploadResult CreateFailure(string errorMessage, int? statusCode = null)
        {
            return new UploadResult
            {
                Success = false,
                ErrorMessage = errorMessage,
                StatusCode = statusCode
            };
        }
    }
}
