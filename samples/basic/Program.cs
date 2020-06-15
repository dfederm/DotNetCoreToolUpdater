// Copyright (c) David Federman. All rights reserved.

namespace BasicSampleTool
{
    using System;
    using System.IO;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using DotNetCoreToolUpdater;

    internal sealed class Program
    {
        private static async Task Main()
        {
            // Start the updater as soon as possible
            var updater = new Updater();

            // In a real tool, this would likely not be needed.
            // In this sample, we go find the directory where this project places its packages.
            string nugetSource = FindNugetSource();

            // Do not immediately await the update task. Let it update in the background while this tool does what it's supposed to do.
            var updateTask = updater.UpdateCurrentToolAsync(nugetSource);

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

        private static string FindNugetSource()
        {
            // Find the git root
            var dir = Directory.GetCurrentDirectory();
            while (!Directory.Exists(Path.Combine(dir, ".git")))
            {
                dir = Path.GetDirectoryName(dir);
            }

            // From the git root, find the package output path.
            return Path.Combine(dir, "artifacts", "samples", "basic");
        }
    }
}
