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
        /// Update the tool.
        /// </summary>
        /// <param name="cancellationToken">A token to signal the update shoudl be cancelled.</param>
        /// <returns>A task that represents the asynchronous update operation. The task's result is the result of the update.</returns>
        public Task<UpdateResult> UpdateAsync(CancellationToken cancellationToken);
    }
}