using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Datadog.MAUI.Symbols.Models;

namespace Datadog.MAUI.Symbols.Core
{
    /// <summary>
    /// Collects Git repository metadata for symbol uploads.
    /// </summary>
    internal class GitMetadataCollector
    {
        private readonly string _workingDirectory;
        private readonly string? _repositoryUrlOverride;

        public GitMetadataCollector(string workingDirectory, string? repositoryUrlOverride = null)
        {
            _workingDirectory = workingDirectory;
            _repositoryUrlOverride = repositoryUrlOverride;
        }

        /// <summary>
        /// Attempts to collect git metadata from the current repository.
        /// Returns null if git is not available or not a git repository.
        /// </summary>
        public GitMetadata? CollectMetadata()
        {
            try
            {
                if (!IsGitRepository())
                    return null;

                var hash = GetCommitHash();
                var remote = GetRemoteUrl();
                var trackedFiles = GetTrackedFiles();

                if (string.IsNullOrEmpty(hash) || string.IsNullOrEmpty(remote))
                    return null;

                return new GitMetadata
                {
                    Hash = hash!,
                    Remote = remote!,
                    TrackedFiles = trackedFiles
                };
            }
            catch (Exception)
            {
                // If any git operation fails, return null
                return null;
            }
        }

        /// <summary>
        /// Checks if the working directory is a git repository.
        /// </summary>
        private bool IsGitRepository()
        {
            var gitDir = Path.Combine(_workingDirectory, ".git");
            if (Directory.Exists(gitDir) || File.Exists(gitDir))
                return true;

            // Check if we're in a subdirectory of a git repo
            var result = RunGitCommand("rev-parse --git-dir");
            return !string.IsNullOrEmpty(result);
        }

        /// <summary>
        /// Gets the current git commit hash (full SHA).
        /// </summary>
        private string? GetCommitHash()
        {
            return RunGitCommand("rev-parse HEAD")?.Trim();
        }

        /// <summary>
        /// Gets the git remote URL (typically origin).
        /// Returns the override URL if provided, otherwise queries git.
        /// </summary>
        private string? GetRemoteUrl()
        {
            // Use override if provided
            if (!string.IsNullOrEmpty(_repositoryUrlOverride))
            {
                return _repositoryUrlOverride;
            }

            // Try to get origin remote URL
            var url = RunGitCommand("config --get remote.origin.url")?.Trim();

            if (string.IsNullOrEmpty(url))
            {
                // Fallback: try to get any remote URL
                var remotes = RunGitCommand("remote");
                if (!string.IsNullOrEmpty(remotes))
                {
                    var firstRemote = remotes!.Split('\n').FirstOrDefault()?.Trim();
                    if (!string.IsNullOrEmpty(firstRemote))
                    {
                        url = RunGitCommand($"config --get remote.{firstRemote}.url")?.Trim();
                    }
                }
            }

            return url;
        }

        /// <summary>
        /// Gets the list of tracked files in the repository.
        /// </summary>
        private string[] GetTrackedFiles()
        {
            var output = RunGitCommand("ls-files");

            if (string.IsNullOrEmpty(output))
                return Array.Empty<string>();

            return output!
                .Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(f => f.Trim())
                .Where(f => !string.IsNullOrEmpty(f))
                .ToArray();
        }

        /// <summary>
        /// Runs a git command and returns the output.
        /// </summary>
        private string? RunGitCommand(string arguments)
        {
            try
            {
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = arguments,
                    WorkingDirectory = _workingDirectory,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(processStartInfo))
                {
                    if (process == null)
                        return null;

                    var output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();

                    return process.ExitCode == 0 ? output : null;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
