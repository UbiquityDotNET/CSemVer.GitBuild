// -----------------------------------------------------------------------
// <copyright file="TestContextExtensions.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Utilities.ProjectCreation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ubiquity.NET.Versioning.Build.Tasks.UT.Support
{
    internal static class TestContextExtensions
    {
        /// <summary>Determines the repository root from the test context</summary>
        /// <param name="ctx">Test context to use for determining the repository root</param>
        /// <returns>Root of the repository</returns>
        /// <exception cref="InvalidOperationException">If the root is indeterminable from the <paramref name="ctx"/></exception>
        /// <remarks>
        /// The repository root is generally not something the test needs or should care about. However, the test case that validates
        /// the task assembly itself requires access to the BuildVersion XML file used along with the 'generatedversion.props'
        /// file to know what was used to validate against.
        /// </remarks>
        internal static string GetRepoRoot( this TestContext ctx )
        {
            // Currently this assumes a fixed relationship between the "TestRunDirectory" and the actual root
            // If that ever changes, this is the one place to change it.
            return !string.IsNullOrWhiteSpace( ctx.TestRunDirectory )
                   ? Path.GetFullPath( Path.Combine( ctx.TestRunDirectory, "..", "..", ".." ) )
                   : throw new InvalidOperationException( "Context.TestRunDirectory is not available" );
        }

        internal static VersioningProjectBuildResults CreateTestProjectAndInvokeTestedPackage(
            this TestContext ctx,
            string targetFramework,
            ProjectCollection projectCollection
            )
        {
#pragma warning disable CA2000 // Dispose objects before losing scope
            // It doesn't "lose scope", the disposable members are transferred to the return
            ProjectBuildResults buildResults = ctx.CreateAndRestoreTestProject( targetFramework, projectCollection );
#pragma warning restore CA2000 // Dispose objects before losing scope
            try
            {
                if(!buildResults.Success)
                {
                    ctx.LogBuildErrors( buildResults.Output! );
                    return new( buildResults, default );
                }

                // Since this project uses an imported target, it won't even exist until AFTER ResolvePackageDependencies[DesignTime|ForBuild] comes along.
                var result = buildResults.Creator.ProjectInstance.Build(buildResults.Output, "PrepareVersioningForBuild" );
                Assert.IsNotNull( result );
                Assert.IsNotNull( result.ProjectStateAfterBuild );
                if(result.OverallResult != BuildResultCode.Success)
                {
                    ctx.LogBuildErrors( buildResults.Output );
                }

#pragma warning disable CA2000 // Dispose objects before losing scope
                // Newly created instance is returned as a member of a disposable type
                return new(
                    new ProjectBuildResults(buildResults.Creator, buildResults.Output, result.OverallResult == BuildResultCode.Success),
                    result.OverallResult == BuildResultCode.Success ? new BuildProperties( result.ProjectStateAfterBuild) : default
                );
#pragma warning restore CA2000 // Dispose objects before losing scope
            }
            catch
            {
                buildResults.Dispose();
                throw;
            }
        }

        internal static ProjectBuildResults CreateAndRestoreTestProject(
            this TestContext ctx,
            string targetFramework,
            ProjectCollection projectCollection
            )
        {
            if(string.IsNullOrWhiteSpace( ctx.TestResultsDirectory ))
            {
                throw new InvalidOperationException( "TestResultsDirectory is not available!" );
            }

            if(string.IsNullOrWhiteSpace( ctx.TestName ))
            {
                throw new InvalidOperationException( "TestName is not available!" );
            }

            // package version of the tasks should match the informational version of THIS assembly
            // That, is they MUST be built together as they are inherently tightly coupled.
            string? packageVersion = ( from attr in typeof(TestContextExtensions).Assembly.CustomAttributes
                                       where attr.AttributeType.FullName == "System.Reflection.AssemblyInformationalVersionAttribute"
                                       select (string?)attr.ConstructorArguments[0].Value
                                     ).FirstOrDefault();

            if (string.IsNullOrWhiteSpace(packageVersion))
            {
                throw new InvalidOperationException("AssemblyInformationalVersionAttribute is missing, null or whitespace!");
            }

            string projectPath = Path.Combine( ctx.TestResultsDirectory, $"{nameof(BuildTaskTests)}-{ctx.TestName}-{targetFramework}.csproj");
#pragma warning disable CA2000 // Dispose objects before losing scope
            // restoreBuildOutput doesn't lose scope here, it's provided to the return type
            var project = ProjectCreator.Templates
                                        .VersioningProject(targetFramework, packageVersion, projectCollection )
                                        .Save( projectPath )
                                        .TryRestore(out bool restoreResult, out BuildOutput restoreBuildOutput);
#pragma warning restore CA2000 // Dispose objects before losing scope

            return new( project, restoreBuildOutput, restoreResult );
        }

        internal static void LogBuildErrors( this TestContext ctx, BuildOutput buildOutput )
        {
            foreach(var err in buildOutput.ErrorEvents)
            {
                // Log build errors in standard MSBuild format
                // see: https://learn.microsoft.com/en-us/visualstudio/msbuild/msbuild-diagnostic-format-for-tasks?view=vs-2022
                ctx.WriteLine( $"{err.ProjectFile}({err.LineNumber},{err.ColumnNumber}) : {err.Subcategory} error {err.Code} : {err.Message}" );
            }
        }

        internal static string CreateRandomFilePath(this TestContext ctx)
        {
            return !string.IsNullOrWhiteSpace( ctx.DeploymentDirectory )
                ? Path.Combine(ctx.DeploymentDirectory, Path.GetRandomFileName())
                : throw new InvalidOperationException( "DeploymentDirectory is not available!" );
        }

        internal static string CreateRandomFile(this TestContext ctx)
        {
            string retVal = ctx.CreateRandomFilePath();
            using var strm = File.Open(retVal, FileMode.CreateNew);
            return retVal;
        }

        internal static string CreateBuildVersionXmlWithRandomName(
            this TestContext ctx,
            int buildMajor,
            int buildMinor,
            int buildPatch,
            string? preReleaseName = null,
            int? preReleaseNumber = null,
            int? preReleaseFix = null
            )
        {
            string retVal = ctx.CreateRandomFilePath();
            using var strm = File.Open(retVal, FileMode.CreateNew);
            var element = new XElement("BuildVersionData",
                                        new XAttribute(PropertyNames.BuildMajor, buildMajor),
                                        new XAttribute(PropertyNames.BuildMinor, buildMinor),
                                        new XAttribute(PropertyNames.BuildPatch, buildPatch)
                                        );
            if(!string.IsNullOrWhiteSpace( preReleaseName ))
            {
                element.Add( new XAttribute( PropertyNames.PreReleaseName, preReleaseName ) );
            }

            if(preReleaseNumber.HasValue)
            {
                element.Add( new XAttribute( PropertyNames.PreReleaseNumber, preReleaseNumber.Value ) );
            }

            if(preReleaseFix.HasValue)
            {
                element.Add( new XAttribute( PropertyNames.PreReleaseNumber, preReleaseFix.Value ) );
            }

            element.Save( strm );
            ctx.WriteLine( $"BuildVersionXML written to: '{retVal}'" );
            return retVal;
        }

        internal static string CreateEmptyBuildVersionXmlWithRandomName( this TestContext ctx )
        {
            string retVal = ctx.CreateRandomFilePath();
            using var strm = File.Open(retVal, FileMode.CreateNew);
            var element = new XElement("BuildVersionData");
            element.Save( strm );
            ctx.WriteLine( $"BuildVersionXML written to: '{retVal}'" );
            return retVal;
        }
    }
}
