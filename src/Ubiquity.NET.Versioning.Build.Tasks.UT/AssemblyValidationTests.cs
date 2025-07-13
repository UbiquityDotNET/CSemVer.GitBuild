// -----------------------------------------------------------------------
// <copyright file="AssemblyValidationTests.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Loader;

using Microsoft.Build.Evaluation;
using Microsoft.Build.Utilities.ProjectCreation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Ubiquity.NET.Versioning.Build.Tasks.UT.Support;

namespace Ubiquity.NET.Versioning.Build.Tasks.UT
{
    [TestClass]
    [TestCategory("Build Task Assembly")]
    public class AssemblyValidationTests
    {
        public AssemblyValidationTests( TestContext ctx )
        {
            ArgumentNullException.ThrowIfNull(ctx);
            ArgumentException.ThrowIfNullOrWhiteSpace( ctx.TestResultsDirectory );

            Context = ctx;
        }

        public TestContext Context { get; }

        // Repo assembly versions are imported from `GeneratedVersion.props` as the build task is not usable for itself.
        // This verifies the build versioning used in the scripts/build matches what is expected for an end-consumer by
        // testing the assembly's versioning information against the output from the task assembly. This should be identical
        // for IDE builds AND command line builds. Basically this verifies the manual IDE properties as well as the automated
        // build scripting matches what the task itself produces. If anything is out of whack, this will complain.
        [TestMethod]
        [DataRow("netstandard2.0")]
        [DataRow("net48")]
        [DataRow("net8.0")]
        public void ValidateRepoAssemblyVersion( string targetFramework)
        {
            var globalProperties = new Dictionary<string,string>
            {
                // for this test use the official repo-root build version to compare against
                ["BuildVersionXml"] = Path.Combine(Context.GetRepoRoot(), "BuildVersion.xml"),
            };

            // For a CI build load the ciBuildIndex and ciBuildName from the generatedversion.props file
            // so the test knows what to expect. If this test is run locally, as a PR buddy build, a CI build
            // or a release, the results are different and the test needs to account for those differences.
            var (ciBuildIndex, ciBuildName, buildTime, envControl) = TestUtils.GetGeneratedBuildInfo();
            using(envControl)
            {
                if(!string.IsNullOrWhiteSpace(ciBuildIndex))
                {
                    globalProperties[PropertyNames.CiBuildIndex] = ciBuildIndex;
                }

                // Build name depends on context of the build (Local, PR, CI, Release)
                // and therefore is NOT hard-coded in the tests.
                if(!string.IsNullOrWhiteSpace(ciBuildName))
                {
                    globalProperties[PropertyNames.CiBuildName] = ciBuildName;
                }

                if(!string.IsNullOrWhiteSpace(buildTime))
                {
                    // NOT using `exact` parsing as that's 'flaky' at best and doesn't actually handle all ISO-8601 formats
                    // Also, NOT using assumption of UTC as commit dates from repo are local time based. ToBuildIndex() will
                    // convert to UTC so that the resulting index is still consistent.
                    var parsedBuildTime = DateTime.Parse(buildTime, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
                    string indexFromLib = parsedBuildTime.ToBuildIndex();
                    Assert.AreEqual(indexFromLib, ciBuildIndex, "Index computed with versioning library should match the index computed by scripts");

                    globalProperties[PropertyNames.BuildTime] = buildTime;
                }

                using var collection = new ProjectCollection(globalProperties);

                using var fullResults = Context.CreateTestProjectAndInvokeTestedPackage(targetFramework, collection);
                var (buildResults, props) = fullResults;
                LogBuildMessages(buildResults.Output);
                Assert.IsTrue(buildResults.Success);

                string? taskAssembly = buildResults.Creator.ProjectInstance.GetOptionalProperty("_Ubiquity_NET_Versioning_Build_Tasks");
                Assert.IsNotNull( taskAssembly, "Task assembly property should contain full path to the task DLL (Not NULL)" );
                Context.WriteLine( $"Task Assembly: '{taskAssembly}'" );

                Assert.IsFalse( string.IsNullOrWhiteSpace( taskAssembly ), "Task assembly property should contain full path to the task DLL (Not Whitespace)" );
                Assert.IsNotNull( props.FileVersion, "Generated properties should have a 'FileVersion'" );
                Context.WriteLine( $"Generated FileVersion: {props.FileVersion}" );

                var alc = new AssemblyLoadContext("TestALC", isCollectible: true);
                try
                {
                    var asm = alc.LoadFromAssemblyPath(taskAssembly);
                    Assert.IsNotNull( asm, "should be able to load task assembly" );
                    var asmName = asm.GetName();
                    Version? asmVer = asmName.Version;
                    Assert.IsNotNull( asmVer, "Task assembly should have a version" );
                    Context.WriteLine( $"TaskAssemblyVersion: {asmVer}" );
                    Context.WriteLine( $"AssemblyName: {asmName}" );

                    Assert.IsNotNull( props.FileVersionMajor, "Property value for Major should exist" );
                    Assert.AreEqual( (int)props.FileVersionMajor, asmVer.Major, "Major value of assembly version should match" );

                    Assert.IsNotNull( props.FileVersionMinor, "Property value for Minor should exist" );
                    Assert.AreEqual( (int)props.FileVersionMinor, asmVer.Minor, "Minor value of assembly version should match" );

                    Assert.IsNotNull( props.FileVersionBuild, "Property value for Build should exist" );
                    Assert.AreEqual( (int)props.FileVersionBuild, asmVer.Build, "Build value of assembly version should match" );

                    Assert.IsNotNull( props.FileVersionRevision, "Property value for Revision should exist" );
                    Assert.AreEqual( (int)props.FileVersionRevision, asmVer.Revision, "Revision value of assembly version should match" );

                    // Release builds won't have a CI component by definition so nothing to validate for those
                    // Should get local, PR and CI builds before that to hit this case though.
                    if(!string.IsNullOrWhiteSpace(ciBuildIndex))
                    {
                        Assert.AreEqual( ciBuildIndex, props.CiBuildIndex, "BuildIndex computed in scripts should match computed value from task");
                        Assert.AreEqual( 1, props.FileVersionRevision & 1, "CI builds should have an ODD numbered revision!");
                    }

                    // Test that AssemblyFileVersion on the task assembly matches expected value
                    string fileVersion = ( from attr in asm.CustomAttributes
                                           where attr.AttributeType.FullName == "System.Reflection.AssemblyFileVersionAttribute"
                                           let val = attr.ConstructorArguments.Single().Value as string
                                           where val is not null
                                           select val
                                         ).Single();

                    Assert.AreEqual(props.FileVersion, fileVersion);

                    // Test that AssemblyInformationalVersion on the task assembly matches expected value
                    string informationalVersion = ( from attr in asm.CustomAttributes
                                                    where attr.AttributeType.FullName == "System.Reflection.AssemblyInformationalVersionAttribute"
                                                    let val = attr.ConstructorArguments.Single().Value as string
                                                    where val is not null
                                                    select val
                                                  ).Single();

                    Assert.AreEqual(props.InformationalVersion, informationalVersion);
                }
                finally
                {
                    alc.Unload();
                }
            }
        }

        private void LogBuildMessages( BuildOutput output )
        {
            foreach(string msg in output.Messages.Low)
            {
                Context.WriteLine("MSBUILD: {0}", msg);
            }
        }
    }
}
