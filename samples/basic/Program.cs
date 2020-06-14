// Copyright (c) David Federman. All rights reserved.

namespace BasicSampleTool
{
    using System;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using DotNetCoreToolUpdater;

    internal sealed class Program
    {
        private static async Task Main()
        {
            // Start the updater as soon as possible
            var updater = new Updater("BasicSampleTool", @"C:\Users\David\Code\DotNetCoreToolUpdater\artifacts\samples\basic");

            // Do not immediately await the update task. Let it update in the background while this tool does what it's supposed to do.
            var updateTask = updater.UpdateAsync(CancellationToken.None);

            // Do the work the tool is intended for.
            // In this sample, that's just to print its own version out to the console and then sleep for a bit to fake doing work.
            // In a real tool, this would do somethign non-trivial.
            string versionString = Assembly.GetEntryAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                .InformationalVersion
                .ToString();
            Console.WriteLine($"Basic Sample Tool v{versionString}");
            Thread.Sleep(1000);

            // Let the update finish if it hasn't already.
            if (!updateTask.IsCompleted)
            {
                Console.WriteLine("Waiting for updater to complete...");
            }

            var updateResult = await updateTask;
            Console.WriteLine($"UpdateResult.IsSuccessful {updateResult.IsSuccessful}");
        }
    }
}
