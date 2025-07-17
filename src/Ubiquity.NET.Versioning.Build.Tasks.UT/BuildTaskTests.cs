// -----------------------------------------------------------------------
// <copyright file="BuildTaskTests.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

using Microsoft.Build.Evaluation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Ubiquity.NET.Versioning.Build.Tasks.UT.Support;

namespace Ubiquity.NET.Versioning.Build.Tasks.UT
{
    [TestClass]
    public class BuildTaskTests
    {
        public BuildTaskTests( TestContext ctx )
        {
            ArgumentNullException.ThrowIfNull(ctx);
            ArgumentException.ThrowIfNullOrWhiteSpace(ctx.TestResultsDirectory);

            Context = ctx;
        }

        public TestContext Context { get; }

        [TestMethod]
        [DataRow("netstandard2.0")]
        [DataRow("net48")]
        [DataRow("net8.0")]
        public void GoldenPathTest( string targetFramework )
        {
            var globalProperties = new Dictionary<string, string>
            {
                [PropertyNames.BuildMajor] = "20",
                [PropertyNames.BuildMinor] = "1",
                [PropertyNames.BuildPatch] = "4",
                [PropertyNames.PreReleaseName] = "alpha",
            };

            using var collection = new ProjectCollection(globalProperties);
            using var fullResults = Context.CreateTestProjectAndInvokeTestedPackage(targetFramework, collection);
            var (buildResults, props) = fullResults;
            Assert.IsTrue(buildResults.Success);

            // v20.1.4-alpha => 5.44854.3875.59946 [see: https://csemver.org/playground/site/#/]
            // NOTE: CI build is +1 (FileVersionRevision)!
            //
            // NOTE: Since build index is based on time which is captured during build it
            // is not possible to know 'a priori' what the value will be..., additionally, the
            // CiBuildName is dependent on the environment. Other, tests validate the behavior of
            // those with an explicit setting...
            string expectedFullBuildNumber = $"20.1.5-alpha.0.0.ci.{props.CiBuildIndex}.{props.CiBuildName}";
            string expectedFileVersion = "5.44854.3875.59947"; // CI build

            Assert.IsNotNull(props.BuildMajor, "should have a value set for 'BuildMajor'");
            Assert.AreEqual(20u, props.BuildMajor.Value);

            Assert.IsNotNull(props.BuildMinor, "should have a value set for 'BuildMinor'");
            Assert.AreEqual(1u, props.BuildMinor.Value);

            Assert.IsNotNull(props.BuildPatch, "should have a value set for 'BuildPatch'");
            Assert.AreEqual(4, props.BuildPatch.Value);

            Assert.IsNotNull(props.PreReleaseName, "should have a value set for 'PreReleaseName'");
            Assert.AreEqual("alpha", props.PreReleaseName);

            Assert.IsNull(props.PreReleaseNumber, "Should NOT have a value set for 'PreReleaseNumber'");
            Assert.IsNull(props.PreReleaseFix, "Should NOT have a value set for 'PreReleaseFix'");

            Assert.AreEqual(expectedFullBuildNumber, props.FullBuildNumber);
            Assert.AreEqual(expectedFullBuildNumber, props.PackageVersion);

            // TODO: Test that time is in ISO-8601 format and within a few seconds of "now"
            // For now, just make sure they aren't null or empty
            Assert.IsFalse(string.IsNullOrWhiteSpace(props.BuildTime));
            Assert.IsFalse(string.IsNullOrWhiteSpace(props.CiBuildIndex));

            Assert.AreEqual("ZZZ", props.CiBuildName);

            Assert.IsNotNull(props.FileVersionMajor);
            Assert.AreEqual(5, props.FileVersionMajor.Value);

            Assert.IsNotNull(props.FileVersionMinor);
            Assert.AreEqual(44854, props.FileVersionMinor.Value);

            Assert.IsNotNull(props.FileVersionBuild);
            Assert.AreEqual(3875, props.FileVersionBuild.Value);

            Assert.IsNotNull(props.FileVersionRevision);
            Assert.AreEqual(59947, props.FileVersionRevision.Value);

            Assert.AreEqual(expectedFileVersion, props.FileVersion);
            Assert.AreEqual(expectedFileVersion, props.AssemblyVersion);
            Assert.AreEqual(expectedFullBuildNumber, props.InformationalVersion);
        }

        [TestMethod]
        [DataRow("netstandard2.0")]
        [DataRow("net48")]
        [DataRow("net8.0")]
        public void BuildVersionXmlIsUsed( string targetFramework )
        {
            string buildVersionXml = Context.CreateBuildVersionXmlWithRandomName(20, 1, 5);
            string buildTime = DateTime.UtcNow.ToString("o");
            const string buildIndex = "ABCDEF12";
            var globalProperties = new Dictionary<string, string>
            {
                [PropertyNames.BuildTime] = buildTime, // should be ignored as presence of explicit CiBuildIndex overrides it
                [PropertyNames.CiBuildIndex] = buildIndex,
                ["BuildVersionXml"] = buildVersionXml
            };

            using var collection = new ProjectCollection(globalProperties);

            using var fullResults = Context.CreateTestProjectAndInvokeTestedPackage(targetFramework, collection);
            var (buildResults, props) = fullResults;
            Assert.IsTrue(buildResults.Success);

            // v20.1.5 => 5.44854.3880.52268 [see: https://csemver.org/playground/site/#/]
            // NOTE: CI build is Patch+1 for the string form
            // and for a FileVersion, it is baseBuild + CI bit.
            // Non-prerelease double dash.
            string expectedFullBuildNumber = $"20.1.6--ci.ABCDEF12.ZZZ";
            string expectedFileVersion = "5.44854.3880.52269";

            Assert.IsNotNull(props.BuildMajor);
            Assert.AreEqual(20u, props.BuildMajor.Value);

            Assert.IsNotNull(props.BuildMinor);
            Assert.AreEqual(1u, props.BuildMinor.Value);

            Assert.IsNotNull(props.BuildPatch);
            Assert.AreEqual(5u, props.BuildPatch.Value);

            Assert.IsNull(props.PreReleaseName);

            Assert.IsNotNull(props.PreReleaseNumber);
            Assert.AreEqual(0, props.PreReleaseNumber.Value);

            Assert.IsNotNull(props.PreReleaseFix);
            Assert.AreEqual(0, props.PreReleaseFix.Value);

            Assert.AreEqual(expectedFullBuildNumber, props.FullBuildNumber);
            Assert.AreEqual(expectedFullBuildNumber, props.PackageVersion);

            // Test for expected global properties (Should not change values)
            Assert.AreEqual(buildTime, props.BuildTime);
            Assert.AreEqual(buildIndex, props.CiBuildIndex);

            Assert.AreEqual("ZZZ", props.CiBuildName);

            Assert.IsNotNull(props.FileVersionMajor);
            Assert.AreEqual(5, props.FileVersionMajor.Value);

            Assert.IsNotNull(props.FileVersionMinor);
            Assert.AreEqual(44854, props.FileVersionMinor.Value);

            Assert.IsNotNull(props.FileVersionBuild);
            Assert.AreEqual(3880, props.FileVersionBuild.Value);

            Assert.IsNotNull(props.FileVersionRevision);
            Assert.AreEqual(52269, props.FileVersionRevision.Value);

            Assert.AreEqual(expectedFileVersion, props.FileVersion);
            Assert.AreEqual(expectedFileVersion, props.AssemblyVersion);
            Assert.AreEqual(expectedFullBuildNumber, props.InformationalVersion);
        }

        [TestMethod]
        [DataRow("netstandard2.0")]
        [DataRow("net48")]
        [DataRow("net8.0")]
        public void CiBuildInfoIsProcessedCorrectly( string targetFramework )
        {
            // NOT using BuildVersion.xml, all values set as globals to test handling of that
            var globalProperties = new Dictionary<string, string>
            {
                [PropertyNames.BuildMajor] = "20",
                [PropertyNames.BuildMinor] = "1",
                [PropertyNames.BuildPatch] = "5",
                [PropertyNames.PreReleaseName] = "delta",
                [PropertyNames.PreReleaseNumber] = "0",
                [PropertyNames.PreReleaseFix] = "1",
                [PropertyNames.BuildTime] = "2025-06-02T10:15:48-07:00", // Format typical of commit date time stamp
                [PropertyNames.CiBuildName] = "QRP", // Intentionally, not a standard value
            };

            // compute build index from the time stamp to get the expected value of the index
            // Technically, this is const as the time stamp itself is a const, but this saves on
            // "magic numbers" and allows easier updates to validate a different time stamp.
            var parsedBuildTime = DateTime.Parse(globalProperties[PropertyNames.BuildTime], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
            string expectedIndex = parsedBuildTime.ToBuildIndex();

            using var collection = new ProjectCollection(globalProperties);
            using var fullResults = Context.CreateTestProjectAndInvokeTestedPackage(targetFramework, collection);
            var (buildResults, props) = fullResults;
            Assert.IsTrue(buildResults.Success);

            // v20.1.5-delta.0.1 => 5.44854.3878.63342 [see: https://csemver.org/playground/site/#/]
            // NOTE: CI build is +1 (FileVersionRevision)!
            //
            // NOTE: Since build index is based on time which is captured during build it
            // is not possible to know 'a priori' what the value will be..., additionally, the
            // CiBuildName is dependent on the environment. Other, tests validate the behavior of
            // those with an explicit setting...
            string expectedFullBuildNumber = $"20.1.6-delta.0.1.ci.{expectedIndex}.QRP";
            string expectedFileVersion = "5.44854.3878.63343"; // CI Build (+1)

            Assert.IsNotNull(props.BuildMajor, "should have a value set for 'BuildMajor'");
            Assert.AreEqual(20u, props.BuildMajor.Value);

            Assert.IsNotNull(props.BuildMinor, "should have a value set for 'BuildMinor'");
            Assert.AreEqual(1u, props.BuildMinor.Value);

            Assert.IsNotNull(props.BuildPatch, "should have a value set for 'BuildPatch'");
            Assert.AreEqual(5, props.BuildPatch.Value);

            Assert.IsNotNull(props.PreReleaseName, "should have a value set for 'PreReleaseName'");
            Assert.AreEqual("delta", props.PreReleaseName);

            Assert.IsNotNull(props.PreReleaseNumber, "Should have a value set for 'PreReleaseNumber'");
            Assert.AreEqual((ushort)0u, props.PreReleaseNumber);

            Assert.IsNotNull(props.PreReleaseFix, "Should have a value set for 'PreReleaseFix'");
            Assert.AreEqual((ushort)1u, props.PreReleaseFix);

            Assert.AreEqual(expectedFullBuildNumber, props.FullBuildNumber);
            Assert.AreEqual(expectedFullBuildNumber, props.PackageVersion);

            Assert.AreEqual(globalProperties[PropertyNames.BuildTime], props.BuildTime);
            Assert.AreEqual(expectedIndex, props.CiBuildIndex);

            Assert.AreEqual("QRP", props.CiBuildName);

            Assert.IsNotNull(props.FileVersionMajor);
            Assert.AreEqual(5, props.FileVersionMajor.Value);

            Assert.IsNotNull(props.FileVersionMinor);
            Assert.AreEqual(44854, props.FileVersionMinor.Value);

            Assert.IsNotNull(props.FileVersionBuild);
            Assert.AreEqual(3878, props.FileVersionBuild.Value);

            Assert.IsNotNull(props.FileVersionRevision);
            Assert.AreEqual(63343, props.FileVersionRevision.Value);

            Assert.AreEqual(expectedFileVersion, props.FileVersion);
            Assert.AreEqual(expectedFileVersion, props.AssemblyVersion);
            Assert.AreEqual(expectedFullBuildNumber, props.InformationalVersion);
        }

        [TestMethod]
        [DynamicData(nameof(GetPrereleaseTestData))]
        public void PreReleaseFixShownCorrectly( PrereleaseTestData data )
        {
            // This test validates how pre-release number and fix are shown.
            // For a CSemVer-CI these are always shown, even if 0. For a
            // CSemVer, however, they are not shown if zero except the Number which
            // is shown as 0 IFF Fix > 0.

            // NOT using BuildVersion.xml, all values set as globals to test handling of that
            var globalProperties = new Dictionary<string, string>
            {
                [PropertyNames.BuildMajor] = "20",
                [PropertyNames.BuildMinor] = "1",
                [PropertyNames.BuildPatch] = "5",
                [PropertyNames.PreReleaseName] = "delta",
                [PropertyNames.PreReleaseNumber] = data.Number.ToString(CultureInfo.InvariantCulture),
                [PropertyNames.PreReleaseFix] = data.Fix.ToString(CultureInfo.InvariantCulture),
                [EnvVarNames.IsAutomatedBuild] = "true", // Not a local build (only relevant for a CI build)
            };

            string? expectedCiIndex = string.Empty;
            string? expectedCiName = string.Empty;
            string versionBase = "20.1.5-delta";

            if(data.IsCI)
            {
                globalProperties[PropertyNames.BuildTime] = "2025-06-02T10:15:48-07:00"; // Format typical of commit date time stamp
                globalProperties[PropertyNames.CiBuildName] = "QRP"; // Intentionally, not a standard value

                // compute build index from the time stamp to get the expected value of the index
                // Technically, this is const as the time stamp itself is a const, but this saves on
                // "magic numbers" and allows easier updates to validate a different time stamp.
                var parsedBuildTime = DateTime.Parse(globalProperties[PropertyNames.BuildTime], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
                expectedCiIndex = parsedBuildTime.ToBuildIndex();
                expectedCiName = globalProperties[PropertyNames.CiBuildName];

                // CI version strings are Patch+1!
                versionBase = "20.1.6-delta";
            }
            else
            {
                // ensure generation is based on the test input and does NOT create
                // a CI build version if it isn't supposed to.
                globalProperties[EnvVarNames.IsReleaseBuild] = "true";
            }

            using var collection = new ProjectCollection(globalProperties);
            using var fullResults = Context.CreateTestProjectAndInvokeTestedPackage(data.Tfm, collection);
            var (buildResults, props) = fullResults;
            Assert.IsTrue(buildResults.Success);

            // v20.1.5-delta.1 => 5.44854.3878.63540 [see: https://csemver.org/playground/site/#/]
            // NOTE: CI build is +1 (FileVersionRevision)!
            //
            // NOTE: Since build index is based on time which is captured during build it
            // is not possible to know 'a priori' what the value will be..., additionally, the
            // CiBuildName is dependent on the environment. Other, tests validate the behavior of
            // those with an explicit setting...
            var expectedVersionbldr = new StringBuilder(versionBase);
            string expectedPrerelString = data.Expected;
            if(!string.IsNullOrWhiteSpace(expectedPrerelString))
            {
                expectedVersionbldr.Append('.')
                                   .Append(expectedPrerelString);
            }

            if( data.IsCI )
            {
                expectedVersionbldr.Append(".ci")
                                   .Append('.')
                                   .Append(expectedCiIndex)
                                   .Append('.')
                                   .Append(expectedCiName);
            }

            string expectedFullBuildNumber = expectedVersionbldr.ToString();
            FileVersionQuad expectedFileVersion = ExpectedFileVersion(data);

            Assert.IsNotNull(props.BuildMajor, "should have a value set for 'BuildMajor'");
            Assert.AreEqual(20u, props.BuildMajor.Value);

            Assert.IsNotNull(props.BuildMinor, "should have a value set for 'BuildMinor'");
            Assert.AreEqual(1u, props.BuildMinor.Value);

            Assert.IsNotNull(props.BuildPatch, "should have a value set for 'BuildPatch'");
            Assert.AreEqual(5, props.BuildPatch.Value);

            Assert.IsNotNull(props.PreReleaseName, "should have a value set for 'PreReleaseName'");
            Assert.AreEqual("delta", props.PreReleaseName);

            Assert.IsNotNull(props.PreReleaseNumber, "Should have a value set for 'PreReleaseNumber'");
            Assert.AreEqual(data.Number, props.PreReleaseNumber);

            Assert.IsNotNull(props.PreReleaseFix, "Should have a value set for 'PreReleaseFix'");
            Assert.AreEqual(data.Fix, props.PreReleaseFix);

            Assert.AreEqual(expectedFullBuildNumber, props.FullBuildNumber);
            Assert.AreEqual(expectedFullBuildNumber, props.PackageVersion);

            if( data.IsCI )
            {
                Assert.AreEqual(globalProperties[PropertyNames.BuildTime], props.BuildTime);
            }

            // Default for these is NULL (not specified) so these should match independent of CI
            Assert.AreEqual(expectedCiIndex, props.CiBuildIndex);
            Assert.AreEqual(expectedCiName, props.CiBuildName);

            Assert.IsNotNull(props.FileVersionMajor);
            Assert.AreEqual(expectedFileVersion.Major, props.FileVersionMajor.Value);

            Assert.IsNotNull(props.FileVersionMinor);
            Assert.AreEqual(expectedFileVersion.Minor, props.FileVersionMinor.Value);

            Assert.IsNotNull(props.FileVersionBuild);
            Assert.AreEqual(expectedFileVersion.Build, props.FileVersionBuild.Value);

            Assert.IsNotNull(props.FileVersionRevision);
            Assert.AreEqual(expectedFileVersion.Revision, props.FileVersionRevision.Value);

            string expectedFileVersionString = expectedFileVersion.ToString();
            Assert.AreEqual(expectedFileVersionString, props.FileVersion);
            Assert.AreEqual(expectedFileVersionString, props.AssemblyVersion);
            Assert.AreEqual(expectedFullBuildNumber, props.InformationalVersion);

            // Inline function to support simpler conversion of parameters to
            // expected FileVersionQuad
            //
            // v20.1.5-delta => 5.44854.3878.63340 [see: https://csemver.org/playground/site/#/]
            // NOTE: CI build is Patch+1
            static FileVersionQuad ExpectedFileVersion( PrereleaseTestData d )
            {
                FileVersionQuad retVal = d.PrereleaseIndex switch
                {
                    0 => new(5, 44854, 3878, 63340), // v20.1.5-delta[0.0]
                    1 => new(5, 44854, 3878, 63342), // v20.1.5-delta.0.1
                    2 => new(5, 44854, 3878, 63540), // v20.1.5-delta.1[.0]
                    3 => new(5, 44854, 3878, 63542), // v20.1.5-delta.1.1
                    _ => throw new InvalidOperationException("Unknown pre-release index")
                };

                if(d.IsCI)
                {
                     retVal = retVal with { Revision = (ushort)(retVal.Revision + 1u) };
                }

                return retVal;
            }
        }

        [TestMethod]
        /* IsPreRelease, IsCiBuild, includeMetadata */
        [DataRow(false, false, false)]
        [DataRow(false, true, false)]
        [DataRow(true, false, false)]
        [DataRow(true, true, false)]
        [DataRow(false, false, true)]
        [DataRow(false, true, true)]
        [DataRow(true, false, true)]
        [DataRow(true, true, true)]
        public void ValidateVersionFormatting( bool isPreRelease, bool isCiBuild, bool includeMeta )
        {
            // NOT using BuildVersion.xml, all values set as globals to test handling of that
            var globalProperties = new Dictionary<string, string>
            {
                [PropertyNames.BuildMajor] = "20",
                [PropertyNames.BuildMinor] = "1",
                [PropertyNames.BuildPatch] = "4",
            };

            // CI Builds use a pre-release of the "next" release (POST build version number)
            string expectedFullBuildNumber = isCiBuild ? "20.1.5" : "20.1.4";

            if (isPreRelease)
            {
                globalProperties[PropertyNames.PreReleaseName] = "delta";
                globalProperties[PropertyNames.PreReleaseNumber] = "1";
                globalProperties[PropertyNames.PreReleaseFix] = "0";
                expectedFullBuildNumber += "-delta.1";
            }

            if (isCiBuild)
            {
                globalProperties[PropertyNames.CiBuildIndex] = "MyIndex"; // Intentionally not a standard value
                globalProperties[PropertyNames.CiBuildName] = "QRP"; // Intentionally, not a standard value
                string ciSuffix = isPreRelease ? ".ci.MyIndex.QRP" : "--ci.MyIndex.QRP";

                if (isPreRelease)
                {
                    // CI Builds Always include both parts
                    expectedFullBuildNumber += $".{globalProperties[PropertyNames.PreReleaseFix]}";
                }

                expectedFullBuildNumber += ciSuffix;
            }
            else
            {
                // if this isn't set, the targets will assume it is a CI build and provide defaults
                globalProperties[EnvVarNames.IsReleaseBuild] = "true";
            }

            if (includeMeta)
            {
                globalProperties[PropertyNames.BuildMeta] = "MyMeta";
                expectedFullBuildNumber += "+MyMeta";
            }

            using var collection = new ProjectCollection(globalProperties);
            using var fullResults = Context.CreateTestProjectAndInvokeTestedPackage("net8.0", collection);
            var (buildResults, props) = fullResults;
            Assert.IsTrue(buildResults.Success);

            FileVersionQuad expectedFileVersion = ExpectedFileVersion(isPreRelease, isCiBuild);

            Assert.IsNotNull(props.BuildMajor, "should have a value set for 'BuildMajor'");
            Assert.AreEqual(20u, props.BuildMajor.Value);

            Assert.IsNotNull(props.BuildMinor, "should have a value set for 'BuildMinor'");
            Assert.AreEqual(1u, props.BuildMinor.Value);

            Assert.IsNotNull(props.BuildPatch, "should have a value set for 'BuildPatch'");
            Assert.AreEqual(4, props.BuildPatch.Value);

            if (isPreRelease)
            {
                Assert.IsNotNull(props.PreReleaseName, "should have a value set for 'PreReleaseName'");
                Assert.AreEqual("delta", props.PreReleaseName);

                Assert.IsNotNull(props.PreReleaseNumber, "Should have a value set for 'PreReleaseNumber'");
                Assert.AreEqual((ushort)1u, props.PreReleaseNumber);

                Assert.IsNotNull(props.PreReleaseFix, "Should have a value set for 'PreReleaseFix'");
                Assert.AreEqual((ushort)0u, props.PreReleaseFix);
            }

            if (isCiBuild)
            {
                Assert.AreEqual("MyIndex", props.CiBuildIndex);
                Assert.AreEqual("QRP", props.CiBuildName);
            }

            Assert.AreEqual(isCiBuild ? 1 : 0, props.FileVersionRevision & 1, "CI builds should have ODD numbered revision");

            if (includeMeta)
            {
                Assert.AreEqual("MyMeta", props.BuildMeta);
            }

            Assert.AreEqual(expectedFullBuildNumber, props.FullBuildNumber);
            Assert.AreEqual(expectedFullBuildNumber, props.PackageVersion);

            Assert.IsNotNull(props.FileVersionMajor);
            Assert.AreEqual(expectedFileVersion.Major, props.FileVersionMajor.Value);

            Assert.IsNotNull(props.FileVersionMinor);
            Assert.AreEqual(expectedFileVersion.Minor, props.FileVersionMinor.Value);

            Assert.IsNotNull(props.FileVersionBuild);
            Assert.AreEqual(expectedFileVersion.Build, props.FileVersionBuild.Value);

            Assert.IsNotNull(props.FileVersionRevision);
            Assert.AreEqual(expectedFileVersion.Revision, props.FileVersionRevision.Value);

            string expectedFileVersionString = expectedFileVersion.ToString();
            Assert.AreEqual(expectedFileVersionString, props.FileVersion);
            Assert.AreEqual(expectedFileVersionString, props.AssemblyVersion);
            Assert.AreEqual(expectedFullBuildNumber, props.InformationalVersion);

            // Inline function to support simpler conversion of parameters to
            // expected FileVersionQuad
            //
            // v20.1.4-delta.1 => 5.44854.3876.34610 [see: https://csemver.org/playground/site/#/]
            // v20.1.4 => 5.44854.3878.23338 [see: https://csemver.org/playground/site/#/]
            // NOTE: CI build is +1 (FileVersionRevision)!
            static FileVersionQuad ExpectedFileVersion( bool isPreRelease, bool isCiBuild )
            {
                FileVersionQuad retVal = isPreRelease
                                       ? new(5, 44854, 3876, 34610)
                                       : new(5, 44854, 3878, 23338);

                // NOTE: ODD numbered revisions are for CI builds.
                return isCiBuild
                     ? retVal with { Revision = (ushort)(retVal.Revision + 1u) }
                     : retVal;
            }
        }

        [TestMethod]
        [DataRow("netstandard2.0")]
        [DataRow("net48")]
        [DataRow("net8.0")]
        public void Projects_with_project_references_create_proper_dependencies_in_nuspec( string targetFramework )
        {
            // This tests for a fix to https://github.com/UbiquityDotNET/CSemVer.GitBuild/issues/79
            // Currently this is a bit of a hack to move things along elsewhere.
            // for now, hack this and specifically look for the solution that is at least known to work at this point.
            var globalProperties = new Dictionary<string, string>
            {
                [PropertyNames.BuildMajor] = "1",
                [PropertyNames.BuildMinor] = "2",
                [PropertyNames.BuildPatch] = "3",
                [PropertyNames.PreReleaseName] = "delta",
            };

            using var collection = new ProjectCollection(globalProperties);
            using var fullResults = Context.CreateTestProjectAndInvokeTestedPackage(targetFramework, collection);
            var prop = fullResults.BuildResults.Creator.Project.GetProperty("GetPackageVersionDependsOn");
            Assert.IsTrue(prop.EvaluatedValue.Contains("PrepareVersioningForBuild", StringComparison.Ordinal));

            // A full solution would:
            // Create a project (dependentProj)
            //     - Should generate a NuGetPackage
            // Create a project (testProj) that has a project reference for 'dependentProj'
            // pack testProj
            // look into generated nupkg to ensure dependency for dependentProj has correct version (and NOT the default 1.0.0)
        }

        private static IEnumerable<PrereleaseTestData> GetPrereleaseTestData()
        {
            return from tfm in TargetFrameworks
                   from num in NumberOrFixArray
                   from fix in NumberOrFixArray
                   from isCI in BooleanValue
                   select new PrereleaseTestData(tfm, num, fix, isCI);
        }

        private static IEnumerable<string> TargetFrameworks => ["netstandard2.0", "net48", "net8.0"];

        private static readonly bool[] BooleanValue = [true, false];

        private static readonly byte[] NumberOrFixArray = [0, 1];
    }

    public readonly record struct PrereleaseTestData(string Tfm, byte Number, byte Fix, bool IsCI)
    {
        internal string Expected
        {
            get
            {
                // CSemVer-CI ***ALWAYS*** includes the build numbers for pre-release versions
                // this ensures correct sort ordering of CI builds (which are POST-RELEASE)
                if(IsCI)
                {
                    return $"{Number}.{Fix}";
                }

                // Non CI Might include the release number of zero (IFF Fix > 0)
                var bldr = new StringBuilder();
                if(Number > 0 || Fix > 0)
                {
                    bldr.Append(Number);
                    if(Fix > 0)
                    {
                        bldr.Append('.')
                            .Append(Fix);
                    }
                }

                return bldr.ToString();
            }
        }

        internal int PrereleaseIndex => (Number << 1) + Fix;
    }
}
