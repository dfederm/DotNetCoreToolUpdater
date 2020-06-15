// Copyright (c) David Federman. All rights reserved.

namespace DotNetCoreToolUpdater
{
    /// <summary>
    /// Represents the result of the update.
    /// </summary>
    public sealed class UpdateResult
    {
        /// <summary>
        /// Gets or sets a value indicating whether the update was successful.
        /// </summary>
        public bool IsSuccessful { get; set; }

        /// <summary>
        /// Gets or sets the current version of the tool.
        /// </summary>
        public string CurrentVersion { get; set; }
    }
}
