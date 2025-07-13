// -----------------------------------------------------------------------
// <copyright file="TestModuleFixtures.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;

using Microsoft.Build.Utilities.ProjectCreation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ubiquity.NET.Versioning.Build.Tasks.UT.Support
{
    // Provides common location for one time initialization for all tests in this assembly
    // Doing the package repo construction here, allows tests to run in parallel without
    // hitting access denied errors due to the use of the location in some other test.
    [TestClass]
    public static class TestModuleFixtures
    {
        [AssemblyInitialize]
        public static void AssemblyInitialize( TestContext ctx )
        {
            ArgumentNullException.ThrowIfNull(ctx);
            ArgumentException.ThrowIfNullOrWhiteSpace(ctx.TestRunDirectory);

            // This assumes the solutions '.runsettings' file has re-directed the run output
            // to a well known location. There's no way to "pass" in the location to the build
            // of this test.
            BuildOutputPath = Path.GetFullPath(Path.Combine(ctx.TestRunDirectory, "..", ".."));
            RepoRoot = Path.GetFullPath(Path.Combine(BuildOutputPath, ".."));

            // Generate fake directory.build.[props|targets] in the test run directory to prevent MSBUILD
            // from searching beyond these to find the REPO one in the root (It contains things that will
            // interfere with deterministic testing).
            ProjectCreator.Create()
                          .Save(Path.Combine(ctx.TestRunDirectory, "Directory.Build.props"));

            ProjectCreator.Create()
                          .Save(Path.Combine(ctx.TestRunDirectory, "Directory.Build.targets"));

            // Ensure environment is clear of any overrides to ensure tests are validating the correct behavior
            // Individual tests MAY set these again. To prevent interference with other tests, any such test
            // needing to set these must restore them (even on exceptional exit) via a try/finally or using pattern.
            foreach (string envVar in TaskInputPropertyNames)
            {
                string? value = Environment.GetEnvironmentVariable(envVar);
                if (value is not null)
                {
                    // Save the environment var value for use with task assembly build verification
                    // as these are needed to verify the assembly build details
                    OriginalInputVars.Add(envVar, value);
                    Environment.SetEnvironmentVariable(envVar, null);
                }
            }

            // Nuget.Config is written to the root path, so this folder must be a parent of the
            // test project files or the settings will NOT apply!
            PackageRepo = PackageRepository.Create(
                ctx.TestRunDirectory,                            // '.nuget/packages' repo folder goes here
                new Uri(Path.Combine(BuildOutputPath, "NuGet")), // Local feed (Contains location of the build of the package under test)
                new Uri("https://api.nuget.org/v3/index.json") // standard NuGet Feed
            ).SourceMapping("Local1", "Ubiquity.NET.Versioning*") // force all fetches of built package to locally built source only
             .SourceMapping("api.nuget.org", "*"); // Everything else goes to NuGet public feed.
        }

        [AssemblyCleanup]
        public static void AssemblyCleanup( )
        {
            PackageRepo?.Dispose();
        }

        internal static string BuildOutputPath { get; private set; } = string.Empty;

        internal static string RepoRoot { get; private set; } = string.Empty;

        internal static readonly Dictionary<string, string> OriginalInputVars = [];

        private static void CopyFile( string srcDir, string fileName, string dstDir )
        {
            File.Copy(Path.Combine(srcDir, fileName), Path.Combine(dstDir, fileName), overwrite: true);
        }

        private static PackageRepository? PackageRepo;

        private static readonly ImmutableArray<string> TaskInputPropertyNames = [
            PropertyNames.BuildTime,
            EnvVarNames.IsPullRequestBuild,
            EnvVarNames.IsAutomatedBuild,
            EnvVarNames.IsReleaseBuild,
            PropertyNames.CiBuildIndex,
            PropertyNames.CiBuildName,
            "BuildVersionXml",
            PropertyNames.BuildMajor,
            PropertyNames.FullBuildNumber,
            PropertyNames.FileVersion,
            PropertyNames.AssemblyVersion,
            PropertyNames.InformationalVersion
        ];
    }
}
