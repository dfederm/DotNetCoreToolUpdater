// Copyright (c) David Federman. All rights reserved.

namespace DotNetCoreToolUpdater
{
    /// <summary>
    /// Represents the result of the update.
    /// </summary>
    public sealed class UpdateResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateResult"/> class.
        /// </summary>
        /// <param name="isSuccessful">Whether the update was successful.</param>
        public UpdateResult(bool isSuccessful)
        {
            IsSuccessful = isSuccessful;
        }

        /// <summary>
        /// Gets a value indicating whether the update was successful.
        /// </summary>
        public bool IsSuccessful { get; }
    }
}
