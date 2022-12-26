﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace APKInstaller.Helpers
{
    public class CommandHelper
    {
        /// <summary>
        /// Executes a command.
        /// </summary>
        /// <param name="command">The command to execute.</param>
        /// <returns>The list of results.</returns>
        public static async Task<List<string>> ExecuteShellCommand(string command)
        {
            try
            {
                return await ExecuteShellCommandAsync(command, CancellationToken.None);
            }
            catch (AggregateException ex)
            {
                if (ex.InnerExceptions.Count == 1)
                {
                    throw ex.InnerException;
                }
                else
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// Executes a command.
        /// </summary>
        /// <param name="command">The command to execute.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> which can be used to cancel the asynchronous operation.</param>
        /// <returns>A <see cref="Task"/> which represents the asynchronous operation.</returns>
        public static async Task<List<string>> ExecuteShellCommandAsync(string command, CancellationToken cancellationToken)
        {
            ProcessStartInfo start = new()
            {
                FileName = "powershell.exe",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                Arguments = command,
                CreateNoWindow = true
            };

            using Process process = Process.Start(start);

            process.EnableRaisingEvents = true;

            List<string> lines = new();

            try
            {
                using StreamReader reader = process.StandardOutput;
                // Previously, we would loop while reader.Peek() >= 0. Turns out that this would
                // break too soon in certain cases (about every 10 loops, so it appears to be a timing
                // issue). Checking for reader.ReadLine() to return null appears to be much more robust
                // -- one of the integration test fetches output 1000 times and found no truncations.
                while (!cancellationToken.IsCancellationRequested)
                {
                    string line = await reader.ReadLineAsync().ConfigureAwait(false);

                    if (line == null)
                    {
                        process.Kill();
                        process.Close();
                        break;
                    }

                    lines.Add(line);
                }
            }
            catch (Exception e)
            {
                SettingsHelper.LogManager.GetLogger(nameof(CommandHelper)).Error(e.ExceptionToMessage(), e);
#if DEBUG
                Debug.WriteLine(e.Message);
#endif
            }
            return lines;
        }
    }
}
