// -----------------------------------------------------------------------
// <copyright file="TestUtils.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;

using Microsoft.Build.Definition;
using Microsoft.Build.Evaluation;

namespace Ubiquity.NET.Versioning.Build.Tasks.UT.Support
{
    internal static class TestUtils
    {
        /// <summary>Gets information on the build info and sets up environment variable overrides</summary>
        /// <returns>Info and an IDisposable to restore the environment variable state</returns>
        /// <remarks>
        /// Forcing the environment variables is needed as the test infrastructure does NOT inherit the
        /// values from the launching process. Thus the `Is*` variables are set based on the build kind
        /// information and restored in the returned disposable.
        /// </remarks>
        internal static (string CiBuildIndex, string CiBuildName, string BuildTime, IDisposable EnvControl) GetGeneratedBuildInfo( )
        {
            using var dummyCollection = new ProjectCollection();
            var options = new ProjectOptions()
            {
                ProjectCollection = dummyCollection
            };

            var project = Project.FromFile(Path.Combine(TestModuleFixtures.RepoRoot, "GeneratedVersion.props"), options);

            return (
                project.GetPropertyValue( PropertyNames.CiBuildIndex ),
                project.GetPropertyValue( PropertyNames.CiBuildName ),
                project.GetPropertyValue( PropertyNames.BuildTime ),
                project.SetEnvFromGeneratedVersionInfo()
                );
        }

        [SuppressMessage( "Performance", "CA1859:Use concrete types when possible for improved performance", Justification = "Not possible, file scoped type" )]
        private static IDisposable SetEnvFromGeneratedVersionInfo( this Project project )
        {
            // set environment variables for this test process based on the build-kind set by build scripts
            // This is needed as the tests don't inherit the environment of the command that runs them.
            switch(project.GetPropertyValue( "BuildKind" ))
            {
            case "LocalBuild":
                Environment.SetEnvironmentVariable( EnvVarNames.IsAutomatedBuild, "false" );
                Environment.SetEnvironmentVariable( EnvVarNames.IsPullRequestBuild, "false" );
                Environment.SetEnvironmentVariable( EnvVarNames.IsReleaseBuild, "false" );
                break;

            case "PullRequestBuild":
                Environment.SetEnvironmentVariable( EnvVarNames.IsAutomatedBuild, "true" );
                Environment.SetEnvironmentVariable( EnvVarNames.IsPullRequestBuild, "true" );
                Environment.SetEnvironmentVariable( EnvVarNames.IsReleaseBuild, "false" );
                break;

            case "CiBuild":
                Environment.SetEnvironmentVariable( EnvVarNames.IsAutomatedBuild, "true" );
                Environment.SetEnvironmentVariable( EnvVarNames.IsPullRequestBuild, "false" );
                Environment.SetEnvironmentVariable( EnvVarNames.IsReleaseBuild, "false" );
                break;

            case "ReleaseBuild":
                Environment.SetEnvironmentVariable( EnvVarNames.IsAutomatedBuild, "true" );
                Environment.SetEnvironmentVariable( EnvVarNames.IsPullRequestBuild, "false" );
                Environment.SetEnvironmentVariable( EnvVarNames.IsReleaseBuild, "true" );
                break;

            default:
                throw new InvalidOperationException( "Unknown build kind in GeneratedVersion.props" );
            }

            return new ResetEnv();
        }
    }

    [SuppressMessage( "StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "DUH! It's file scoped!" )]
    file sealed class ResetEnv
        : IDisposable
    {
        public void Dispose( )
        {
            Environment.SetEnvironmentVariable( EnvVarNames.IsAutomatedBuild, null );
            Environment.SetEnvironmentVariable( EnvVarNames.IsPullRequestBuild, null );
            Environment.SetEnvironmentVariable( EnvVarNames.IsReleaseBuild, null );
        }
    }
}
