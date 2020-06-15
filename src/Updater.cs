// Copyright (c) David Federman. All rights reserved.

namespace DotNetCoreToolUpdater
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using NugetSettings = NuGet.Configuration.Settings;
    using NugetSettingsUtility = NuGet.Configuration.SettingsUtility;

    /// <summary>
    /// Manages updates to the tool.
    /// </summary>
    public sealed class Updater : IUpdater
    {
        /// <inheritdoc />
        public Task<UpdateResult> UpdateCurrentToolAsync(
            string nugetSource = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            (bool isLocalTool, string toolPath, string packageName, string packageVersion) = DetectToolContext();

            return UpdateInternalAsync(isLocalTool, toolPath, packageName, packageVersion, nugetSource, cancellationToken);
        }

        /// <inheritdoc />
        public Task<UpdateResult> UpdateGlobalToolAsync(
            string packageName,
            string toolPath = null,
            string nugetSource = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(packageName))
            {
                throw new ArgumentNullException(nameof(packageName));
            }

            cancellationToken.ThrowIfCancellationRequested();

            return UpdateInternalAsync(isLocalTool: false, toolPath, packageName, packageVersion: null, nugetSource, cancellationToken);
        }

        /// <inheritdoc />
        public Task<UpdateResult> UpdateLocalToolAsync(
            string packageName,
            string nugetSource = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(packageName))
            {
                throw new ArgumentNullException(nameof(packageName));
            }

            cancellationToken.ThrowIfCancellationRequested();

            return UpdateInternalAsync(isLocalTool: true, toolPath: null, packageName, packageVersion: null, nugetSource, cancellationToken);
        }

        private static Task<UpdateResult> UpdateInternalAsync(
            bool isLocalTool,
            string toolPath,
            string packageName,
            string packageVersion,
            string nugetSource,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Create a new task as soon as possible to avoid blocking the caller.
            return Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                bool successful = TryInvokeDotNetToolUpdate(isLocalTool, toolPath, packageName, nugetSource, cancellationToken);
                return new UpdateResult
                {
                    IsSuccessful = successful,
                    CurrentVersion = packageVersion,
                };
            });
        }

        private static bool TryInvokeDotNetToolUpdate(
            bool isLocalTool,
            string toolPath,
            string packageName,
            string nugetSource,
            CancellationToken cancellationToken)
        {
            var argumentBuilder = new StringBuilder();
            argumentBuilder.Append("tool update ");
            argumentBuilder.Append(packageName);

            if (!isLocalTool)
            {
                if (string.IsNullOrEmpty(toolPath))
                {
                    argumentBuilder.Append(" --global");
                }
                else
                {
                    argumentBuilder.Append(" --tool-path ");
                    argumentBuilder.Append(toolPath);
                }
            }

            if (!string.IsNullOrEmpty(nugetSource))
            {
                argumentBuilder.Append(" --add-source ");
                argumentBuilder.Append(nugetSource);
            }

            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    Arguments = argumentBuilder.ToString(),
                    CreateNoWindow = true,
                    FileName = "dotnet",
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                },
            };

            // https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet#environment-variables
            process.StartInfo.EnvironmentVariables["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
            process.StartInfo.EnvironmentVariables["DOTNET_CLI_UI_LANGUAGE "] = "en-US";
            process.StartInfo.EnvironmentVariables["DOTNET_MULTILEVEL_LOOKUP "] = "0";
            process.StartInfo.EnvironmentVariables["DOTNET_NOLOGO"] = "1";

            try
            {
                if (!process.Start())
                {
                    return false;
                }
            }
#pragma warning disable CA1031 // Do not catch general exception types. We never want to bubble exceptions up to the calling application.
            catch
#pragma warning restore CA1031 // Do not catch general exception types
            {
                return false;
            }

            using CancellationTokenRegistration registration = cancellationToken.Register(() =>
            {
                try
                {
                    process.Kill();
                }
#pragma warning disable CA1031 // Do not catch general exception types. We never want to bubble exceptions up to the calling application.
                catch
#pragma warning restore CA1031 // Do not catch general exception types
                {
                    // Swallow any exception from attempting to kill the process.
                }
            });

            process.WaitForExit();

            return process.ExitCode == 0;
        }

        private static (bool isLocalTool, string toolPath, string packageName, string packageVersion) DetectToolContext()
        {
            var toolAssemblyPath = Assembly.GetEntryAssembly().Location;

            // Windows is not case-sensitive, so use a case-insensitive regexes
            var regexOptions = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? RegexOptions.IgnoreCase
                : RegexOptions.None;

            var regexEscapedDirectorySeparator = Regex.Escape(Path.DirectorySeparatorChar.ToString());

            // Local tools execute from the nuget global package folder, so check if the entry assembly (the tool)
            // is located there to detect that it's running as a local tool.
            var nugetSettings = NugetSettings.LoadDefaultSettings(Directory.GetCurrentDirectory());
            var nugetGlobalPackageFolder = NugetSettingsUtility.GetGlobalPackagesFolder(nugetSettings);
            if (nugetGlobalPackageFolder[nugetGlobalPackageFolder.Length - 1] != Path.DirectorySeparatorChar)
            {
                nugetGlobalPackageFolder += Path.DirectorySeparatorChar;
            }

            // Example: C:\Users\Foo\.nuget\packages\basicsampletool\1.0.0\tools\netcoreapp3.1\any\BasicSampleTool.dll
            var localToolPathRegex = new Regex(
                $@"^{Regex.Escape(nugetGlobalPackageFolder)}(?<PackageName>[^{regexEscapedDirectorySeparator}]+){regexEscapedDirectorySeparator}(?<PackageVersion>[^{regexEscapedDirectorySeparator}]+)",
                regexOptions);
            Match localToolPathMatch = localToolPathRegex.Match(toolAssemblyPath);
            if (localToolPathMatch.Success)
            {
                string packageName = localToolPathMatch.Groups["PackageName"].Value;
                string packageVersion = localToolPathMatch.Groups["PackageVersion"].Value;
                return (isLocalTool: true, null, packageName, packageVersion);
            }

            // Global tools install to and run from a directory with a ".store" subdirectory which has the actual package content inside of it.
            // Example (default path): C:\Users\David\.dotnet\tools\.store\basicsampletool\1.0.0\basicsampletool\1.0.0\tools\netcoreapp3.1\any\BasicSampleTool.dll
            // Example (custom path): C:\Users\David\Code\DotNetCoreToolUpdater\tmp\.store\basicsampletool\1.0.0\basicsampletool\1.0.0\tools\netcoreapp3.1\any\BasicSampleTool.dll
            var globalToolPathRegex = new Regex(
                $@"^(?<ToolPath>.+){regexEscapedDirectorySeparator}.store{regexEscapedDirectorySeparator}(?<PackageName>[^{regexEscapedDirectorySeparator}]+){regexEscapedDirectorySeparator}(?<PackageVersion>[^{regexEscapedDirectorySeparator}]+)",
                regexOptions);
            Match globalToolPathMatch = globalToolPathRegex.Match(toolAssemblyPath);
            if (globalToolPathMatch.Success)
            {
                string toolPath = globalToolPathMatch.Groups["ToolPath"].Value;
                string packageName = globalToolPathMatch.Groups["PackageName"].Value;
                string packageVersion = globalToolPathMatch.Groups["PackageVersion"].Value;
                return (isLocalTool: false, toolPath, packageName, packageVersion);
            }

            throw new InvalidOperationException("Could not detect the context for the currently running tool.");
        }
    }
}
