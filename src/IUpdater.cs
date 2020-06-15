// Copyright (c) David Federman. All rights reserved.

namespace DotNetCoreToolUpdater
{
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents an interface for updating a tool.
    /// </summary>
    public interface IUpdater
    {
        /// <summary>
        /// Update the currently running tool.
        /// </summary>
        /// <param name="nugetSource">An additional NuGet package source to use.</param>
        /// <param name="cancellationToken">A token to signal the update shoudl be cancelled.</param>
        /// <returns>A task that represents the asynchronous update operation. The task's result is the result of the update.</returns>
        public Task<UpdateResult> UpdateCurrentToolAsync(string nugetSource = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Update a global tool.
        /// </summary>
        /// <param name="packageName">Name/ID of the NuGet package that contains the .NET Core global tool to update.</param>
        /// <param name="toolPath">Specifies the location where the global tool is installed. If null, the default path will be used.</param>
        /// <param name="nugetSource">An additional NuGet package source to use.</param>
        /// <param name="cancellationToken">A token to signal the update shoudl be cancelled.</param>
        /// <returns>A task that represents the asynchronous update operation. The task's result is the result of the update.</returns>
        public Task<UpdateResult> UpdateGlobalToolAsync(string packageName, string toolPath = null, string nugetSource = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Update a local tool.
        /// </summary>
        /// <param name="packageName">Name/ID of the NuGet package that contains the .NET Core global tool to update.</param>
        /// <param name="nugetSource">An additional NuGet package source to use.</param>
        /// <param name="cancellationToken">A token to signal the update shoudl be cancelled.</param>
        /// <returns>A task that represents the asynchronous update operation. The task's result is the result of the update.</returns>
        public Task<UpdateResult> UpdateLocalToolAsync(string packageName, string nugetSource = null, CancellationToken cancellationToken = default);
    }
}