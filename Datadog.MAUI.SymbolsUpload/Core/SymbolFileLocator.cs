using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Datadog.MAUI.SymbolsUpload.Core
{
    /// <summary>
    /// Locates symbol files in build output directories.
    /// Supports both .NET MAUI and native Android/iOS project structures.
    /// </summary>
    internal class SymbolFileLocator
    {
        /// <summary>
        /// Finds Android ProGuard/R8 mapping files in the build output.
        /// Searches .NET MAUI paths first (bin/*/net*-android/), then falls back to native Android paths (build/outputs/mapping/).
        /// Prioritizes the publish/ subfolder when it exists.
        /// </summary>
        public List<string> FindAndroidMappingFiles(string projectDir, string? variant = null)
        {
            var results = new List<string>();

            // .NET MAUI paths (prioritize these)
            // Search pattern: bin/{Configuration}/net*-android/publish/mapping.txt (highest priority)
            //                 bin/{Configuration}/net*-android/mapping.txt (fallback)
            var binDir = Path.Combine(projectDir, "bin");
            if (Directory.Exists(binDir))
            {
                try
                {
                    // Search recursively for mapping.txt files
                    var mappingFiles = Directory.GetFiles(binDir, "mapping.txt", SearchOption.AllDirectories)
                        .Where(f => f.Contains("net") && f.Contains("-android"))
                        .OrderByDescending(f => f.Contains("publish")) // Prioritize publish/ folder
                        .ThenByDescending(f => f.Contains("Release")) // Then Release builds
                        .ToList();

                    results.AddRange(mappingFiles);
                }
                catch (Exception)
                {
                    // Ignore access denied or other directory enumeration errors
                }
            }

            // Native Android paths (fallback for native projects)
            // build/outputs/mapping/{variant}/mapping.txt
            var mappingDir = Path.Combine(projectDir, "build", "outputs", "mapping");
            if (Directory.Exists(mappingDir))
            {
                if (!string.IsNullOrEmpty(variant))
                {
                    var variantPath = Path.Combine(mappingDir, variant, "mapping.txt");
                    if (File.Exists(variantPath))
                        results.Add(variantPath);
                }
                else
                {
                    // Search all variant directories
                    try
                    {
                        var variantDirs = Directory.GetDirectories(mappingDir);
                        foreach (var dir in variantDirs)
                        {
                            var mappingFile = Path.Combine(dir, "mapping.txt");
                            if (File.Exists(mappingFile))
                                results.Add(mappingFile);
                        }
                    }
                    catch (Exception)
                    {
                        // Ignore errors
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// Finds iOS dSYM bundles in the build output.
        /// dSYM files are directories with .dSYM extension containing DWARF debug symbols.
        /// Searches .NET MAUI paths (bin/{Configuration}/net*-ios/{RuntimeIdentifier}/) and native iOS paths.
        /// </summary>
        public List<string> FindIosDsymBundles(string projectDir)
        {
            var results = new List<string>();

            if (!Directory.Exists(projectDir))
                return results;

            // Search for .dSYM directories recursively
            // .NET MAUI: bin/Release/net9.0-ios/ios-arm64/*.app.dSYM
            // Native iOS: Various locations depending on Xcode project structure
            try
            {
                var dsymBundles = Directory.GetDirectories(projectDir, "*.dSYM", SearchOption.AllDirectories)
                    .OrderByDescending(f => f.Contains("Release")) // Prioritize Release builds
                    .ThenByDescending(f => f.Contains("ios-arm64")) // Then device builds (arm64)
                    .ToList();

                results.AddRange(dsymBundles);
            }
            catch (Exception)
            {
                // Ignore access denied or other directory enumeration errors
            }

            return results;
        }

        /// <summary>
        /// Attempts to locate symbol files based on platform and variant.
        /// Returns the first file found or null if none found.
        /// </summary>
        public string? LocateSymbolFile(string projectDir, string platform, string? variant = null)
        {
            List<string> files;

            switch (platform.ToLowerInvariant())
            {
                case "android":
                    files = FindAndroidMappingFiles(projectDir, variant);
                    break;

                case "ios":
                    files = FindIosDsymBundles(projectDir);
                    break;

                default:
                    return null;
            }

            return files.FirstOrDefault();
        }
    }
}
