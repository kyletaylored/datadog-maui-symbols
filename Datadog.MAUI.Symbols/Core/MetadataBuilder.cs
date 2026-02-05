using System.Collections.Generic;
using Datadog.MAUI.Symbols.Models;

namespace Datadog.MAUI.Symbols.Core
{
    /// <summary>
    /// Builds metadata payloads for symbol upload API requests.
    /// </summary>
    internal class MetadataBuilder
    {
        /// <summary>
        /// Builds the event metadata object from SymbolMetadata.
        /// This matches the structure expected by the Datadog API.
        /// </summary>
        public Dictionary<string, object> BuildEventMetadata(SymbolMetadata metadata)
        {
            var result = new Dictionary<string, object>
            {
                ["cli_version"] = metadata.CliVersion,
                ["type"] = metadata.Type
            };

            // Add optional fields only if they have values
            if (!string.IsNullOrEmpty(metadata.Arch))
                result["arch"] = metadata.Arch!;

            if (!string.IsNullOrEmpty(metadata.BuildId))
                result["build_id"] = metadata.BuildId!;

            if (!string.IsNullOrEmpty(metadata.GitCommitSha))
                result["git_commit_sha"] = metadata.GitCommitSha!;

            if (!string.IsNullOrEmpty(metadata.GitRepositoryUrl))
                result["git_repository_url"] = metadata.GitRepositoryUrl!;

            if (!string.IsNullOrEmpty(metadata.Platform))
                result["platform"] = metadata.Platform!;

            // Always include service/version/variant when available
            // build_id is supplemental and does not replace these fields
            if (!string.IsNullOrEmpty(metadata.Service))
                result["service"] = metadata.Service!;

            if (!string.IsNullOrEmpty(metadata.Variant))
                result["variant"] = metadata.Variant!;

            if (!string.IsNullOrEmpty(metadata.Version))
                result["version"] = metadata.Version!;

            return result;
        }

        /// <summary>
        /// Validates that required metadata fields are present.
        /// </summary>
        public bool ValidateMetadata(SymbolMetadata metadata, out string? errorMessage)
        {
            if (string.IsNullOrEmpty(metadata.CliVersion))
            {
                errorMessage = "CliVersion is required";
                return false;
            }

            if (string.IsNullOrEmpty(metadata.Type))
            {
                errorMessage = "Type is required";
                return false;
            }

            // Service and version are always required
            if (string.IsNullOrEmpty(metadata.Service))
            {
                errorMessage = "Service is required";
                return false;
            }

            if (string.IsNullOrEmpty(metadata.Version))
            {
                errorMessage = "Version is required";
                return false;
            }

            errorMessage = null;
            return true;
        }
    }
}
