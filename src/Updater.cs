// Copyright (c) David Federman. All rights reserved.

namespace DotNetCoreToolUpdater
{
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Manages updates to the tool.
    /// </summary>
    public sealed class Updater : IUpdater
    {
        private readonly string _toolPackageName;

        private readonly string _toolPackageSource;

        /// <summary>
        /// Initializes a new instance of the <see cref="Updater"/> class.
        /// </summary>
        /// <param name="toolPackageName">The name of the tool package to update.</param>
        /// <param name="toolPackageSource">The NuGet source where the tool package is located.</param>
        public Updater(
            string toolPackageName,
            string toolPackageSource)
        {
            _toolPackageName = toolPackageName;
            _toolPackageSource = toolPackageSource;
        }

        /// <inheritdoc />
        public Task<UpdateResult> UpdateAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromResult(new UpdateResult(isSuccessful: false));
            }

            // Create a new task as soon as possible to avoid blocking the caller.
            return Task.Run(() => PerformUpdate(cancellationToken));
        }

        private UpdateResult PerformUpdate(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return new UpdateResult(isSuccessful: false);
            }

            bool successful = TryInvokeDotNetToolUpdate(cancellationToken);
            return new UpdateResult(successful);
        }

        private bool TryInvokeDotNetToolUpdate(CancellationToken cancellationToken)
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    Arguments = $"tool update {_toolPackageName} --add-source {_toolPackageSource}",
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
    }
}
