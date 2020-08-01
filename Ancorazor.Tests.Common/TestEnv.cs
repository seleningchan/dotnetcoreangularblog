﻿using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Ancorazor.Tests.Common {

    public static class TestEnv
    {
        public static string SolutionPath()
        {
            const string webProjectSubPath = "src/Discussion.Web/Discussion.Web.csproj";
            var currentPath = Directory.GetCurrentDirectory();

            do
            {
                var webProjectPath = Path.Combine(currentPath, webProjectSubPath).NormalizeToAbsolutePath();
                if (File.Exists(webProjectPath))
                {
                    return currentPath;
                }

                var parent = Directory.GetParent(currentPath);
                currentPath = parent?.FullName;
            } while (currentPath != null);

            throw new InvalidOperationException("Cannot find the project path based on current directory.");
        }

        public static string WebProjectPath()
        {
            return Path.Combine(SolutionPath(), "src/Discussion.Web").NormalizeToAbsolutePath();
        }

        public static string RuntimeLauncherPath()
        {
            var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            var envVarSeparateChar = isWindows ? ';' : ':';
            var commandName = isWindows ? "dotnet.exe" : "dotnet";

            return FindFileThoughEnvironmentVariables(commandName, envVarSeparateChar);
        }

        private static string FindFileThoughEnvironmentVariables(string executableName, char envVarSeparateChar)
        {
            foreach (string envPath in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(envVarSeparateChar))
            {
                var path = envPath.Trim();
                var fullPath = Path.Combine(path, executableName);
                if (!string.IsNullOrWhiteSpace(path) && File.Exists(fullPath))
                {
                    return Path.GetFullPath(fullPath);
                }
            }

            throw new Exception("Runtime not detected on the machine.");
        }

        private static string NormalizeToAbsolutePath(this string relativePath)
        {
            return Path.GetFullPath(relativePath.NormalizeSeparatorChars());
        }

        public static string NormalizeSeparatorChars(this string path)
        {
            return path.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
        }
    }
}
