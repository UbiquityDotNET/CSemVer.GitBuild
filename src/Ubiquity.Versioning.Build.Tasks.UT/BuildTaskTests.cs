﻿// -----------------------------------------------------------------------
// <copyright file="BuildTaskTests.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;

using Microsoft.Build.Evaluation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Ubiquity.NET.Versioning;

namespace Ubiquity.Versioning.Build.Tasks.UT
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
                ["BuildMajor"] = "20",
                ["BuildMinor"] = "1",
                ["BuildPatch"] = "4",
                ["PreReleaseName"] = "alpha",
            };

            using var collection = new ProjectCollection(globalProperties);
            var (_, props) = Context.CreateTestProjectAndInvokeTestedPackage(targetFramework, collection);

            // v20.1.4-alpha => 5.44854.3875.59946 [see: https://csemver.org/playground/site/#/]
            // NOTE: CI build is +1 (FileVersionRevision)!
            //
            // NOTE: Since build index is based on time which is captured during build it
            // is not possible to know 'a priori' what the value will be..., additionally, the
            // CiBuildName is dependent on the environment. Other, tests validate the behavior of
            // those with an explicit setting...
            string expectedFullBuildNumber = $"20.1.4-alpha.ci.{props.CiBuildIndex}.{props.CiBuildName}";
            string expectedShortNumber = $"20.1.4-a.ci.{props.CiBuildIndex}.{props.CiBuildName}";
            string expectedFileVersion = "5.44854.3875.59947";

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
                ["BuildTime"] = buildTime, // should be ignored as presence of explicit CiBuildIndex overrides it
                ["CiBuildIndex"] = buildIndex,
                ["BuildVersionXml"] = buildVersionXml
            };

            using var collection = new ProjectCollection(globalProperties);

            var (_, props) = Context.CreateTestProjectAndInvokeTestedPackage(targetFramework, collection);

            // v20.1.5 => 5.44854.3880.52268 [see: https://csemver.org/playground/site/#/]
            // NOTE: CI build is +1 (FileVersionRevision)!
            string expectedFullBuildNumber = $"20.1.5--ci.ABCDEF12.ZZZ";
            string expectedShortNumber = $"20.1.5--ci.ABCDEF12.ZZZ";
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
            Assert.AreEqual(expectedShortNumber, props.PackageVersion);

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
                ["BuildMajor"] = "20",
                ["BuildMinor"] = "1",
                ["BuildPatch"] = "5",
                ["PreReleaseName"] = "delta",
                ["PreReleaseNumber"] = "0",
                ["PreReleaseFix"] = "1",
                ["BuildTime"] = "2025-06-02T10:15:48-07:00", // Format typical of commit date time stamp
                ["CiBuildName"] = "QRP", // Intentionally, not a standard value
            };

            // compute build index from the time stamp to get the expected value of the index
            // Technically, this is const as the time stamp itself is a const, but this saves on
            // "magic numbers" and allows easier updates to validate a different time stamp.
            var parsedBuildTime = DateTime.Parse(globalProperties["BuildTime"], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
            string expectedIndex = parsedBuildTime.ToBuildIndex();

            using var collection = new ProjectCollection(globalProperties);
            var (_, props) = Context.CreateTestProjectAndInvokeTestedPackage(targetFramework, collection);

            // v20.1.5-delta.0.1 => 5.44854.3878.63342 [see: https://csemver.org/playground/site/#/]
            // NOTE: CI build is +1 (FileVersionRevision)!
            //
            // NOTE: Since build index is based on time which is captured during build it
            // is not possible to know 'a priori' what the value will be..., additionally, the
            // CiBuildName is dependent on the environment. Other, tests validate the behavior of
            // those with an explicit setting...
            string expectedFullBuildNumber = $"20.1.5-delta.0.1.ci.{expectedIndex}.QRP";
            string expectedShortNumber = $"20.1.5-d.0.1.ci.{expectedIndex}.QRP";
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
            Assert.AreEqual((UInt16)0u, props.PreReleaseNumber);

            Assert.IsNotNull(props.PreReleaseFix, "Should have a value set for 'PreReleaseFix'");
            Assert.AreEqual((UInt16)1u, props.PreReleaseFix);

            Assert.AreEqual(expectedFullBuildNumber, props.FullBuildNumber);
            Assert.AreEqual(expectedFullBuildNumber, props.PackageVersion);

            Assert.AreEqual(globalProperties["BuildTime"], props.BuildTime);
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
        [DataRow("netstandard2.0")]
        [DataRow("net48")]
        [DataRow("net8.0")]
        public void PreReleaseFixOfZeroNotShownIfNumber( string targetFramework )
        {
            // NOT using BuildVersion.xml, all values set as globals to test handling of that
            var globalProperties = new Dictionary<string, string>
            {
                ["BuildMajor"] = "20",
                ["BuildMinor"] = "1",
                ["BuildPatch"] = "5",
                ["PreReleaseName"] = "delta",
                ["PreReleaseNumber"] = "1",
                ["PreReleaseFix"] = "0",
                ["BuildTime"] = "2025-06-02T10:15:48-07:00", // Format typical of commit date time stamp
                ["CiBuildName"] = "QRP", // Intentionally, not a standard value
            };

            // compute build index from the time stamp to get the expected value of the index
            // Technically, this is const as the time stamp itself is a const, but this saves on
            // "magic numbers" and allows easier updates to validate a different time stamp.
            var parsedBuildTime = DateTime.Parse(globalProperties["BuildTime"], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
            string expectedIndex = parsedBuildTime.ToBuildIndex();

            using var collection = new ProjectCollection(globalProperties);
            var (_, props) = Context.CreateTestProjectAndInvokeTestedPackage(targetFramework, collection);

            // v20.1.5-delta.1 => 5.44854.3878.63540 [see: https://csemver.org/playground/site/#/]
            // NOTE: CI build is +1 (FileVersionRevision)!
            //
            // NOTE: Since build index is based on time which is captured during build it
            // is not possible to know 'a priori' what the value will be..., additionally, the
            // CiBuildName is dependent on the environment. Other, tests validate the behavior of
            // those with an explicit setting...
            string expectedFullBuildNumber = $"20.1.5-delta.1.ci.{expectedIndex}.QRP";
            string expectedShortNumber = $"20.1.5-d01.ci.{expectedIndex}.QRP";
            string expectedFileVersion = "5.44854.3878.63541"; // CI Build (+1)

            Assert.IsNotNull(props.BuildMajor, "should have a value set for 'BuildMajor'");
            Assert.AreEqual(20u, props.BuildMajor.Value);

            Assert.IsNotNull(props.BuildMinor, "should have a value set for 'BuildMinor'");
            Assert.AreEqual(1u, props.BuildMinor.Value);

            Assert.IsNotNull(props.BuildPatch, "should have a value set for 'BuildPatch'");
            Assert.AreEqual(5, props.BuildPatch.Value);

            Assert.IsNotNull(props.PreReleaseName, "should have a value set for 'PreReleaseName'");
            Assert.AreEqual("delta", props.PreReleaseName);

            Assert.IsNotNull(props.PreReleaseNumber, "Should have a value set for 'PreReleaseNumber'");
            Assert.AreEqual((UInt16)1u, props.PreReleaseNumber);

            Assert.IsNotNull(props.PreReleaseFix, "Should have a value set for 'PreReleaseFix'");
            Assert.AreEqual((UInt16)0u, props.PreReleaseFix);

            Assert.AreEqual(expectedFullBuildNumber, props.FullBuildNumber);
            Assert.AreEqual(expectedFullBuildNumber, props.PackageVersion);

            Assert.AreEqual(globalProperties["BuildTime"], props.BuildTime);
            Assert.AreEqual(expectedIndex, props.CiBuildIndex);

            Assert.AreEqual("QRP", props.CiBuildName);

            Assert.IsNotNull(props.FileVersionMajor);
            Assert.AreEqual(5, props.FileVersionMajor.Value);

            Assert.IsNotNull(props.FileVersionMinor);
            Assert.AreEqual(44854, props.FileVersionMinor.Value);

            Assert.IsNotNull(props.FileVersionBuild);
            Assert.AreEqual(3878, props.FileVersionBuild.Value);

            Assert.IsNotNull(props.FileVersionRevision);
            Assert.AreEqual(63541, props.FileVersionRevision.Value);

            Assert.AreEqual(expectedFileVersion, props.FileVersion);
            Assert.AreEqual(expectedFileVersion, props.AssemblyVersion);
            Assert.AreEqual(expectedFullBuildNumber, props.InformationalVersion);
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
                ["BuildMajor"] = "20",
                ["BuildMinor"] = "1",
                ["BuildPatch"] = "4",
            };

            string expectedFullBuildNumber = "20.1.4";
            string expectedShortNumber = "20.1.4";

            if (isPreRelease)
            {
                globalProperties["PreReleaseName"] = "delta";
                globalProperties["PreReleaseNumber"] = "1";
                globalProperties["PreReleaseFix"] = "0";
                expectedFullBuildNumber += "-delta.1";
                expectedShortNumber += "-d01";
            }

            if (isCiBuild)
            {
                globalProperties["CiBuildIndex"] = "MyIndex"; // Intentionally not a standard value
                globalProperties["CiBuildName"] = "QRP"; // Intentionally, not a standard value
                string ciSuffix = isPreRelease ? ".ci.MyIndex.QRP" : "--ci.MyIndex.QRP";
                expectedFullBuildNumber += ciSuffix;
                expectedShortNumber += ciSuffix;
            }
            else
            {
                // if this isn't set, the targets will assume it is a CI build and provide defaults
                globalProperties["IsReleaseBuild"] = "true";
            }

            if (includeMeta)
            {
                globalProperties["BuildMeta"] = "MyMeta";
                expectedFullBuildNumber += "+MyMeta";
            }

            using var collection = new ProjectCollection(globalProperties);
            var (_, props) = Context.CreateTestProjectAndInvokeTestedPackage("net8.0", collection);

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
                Assert.AreEqual((UInt16)1u, props.PreReleaseNumber);

                Assert.IsNotNull(props.PreReleaseFix, "Should have a value set for 'PreReleaseFix'");
                Assert.AreEqual((UInt16)0u, props.PreReleaseFix);
            }

            if (isCiBuild)
            {
                Assert.AreEqual("MyIndex", props.CiBuildIndex);
                Assert.AreEqual("QRP", props.CiBuildName);
            }

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
                FileVersionQuad retVal = isPreRelease ? new(5, 44854, 3876, 34610) : new(5, 44854, 3878, 23338);
                return isCiBuild ? retVal with { Revision = (UInt16)(retVal.Revision + 1u) } : retVal;
            }
        }
    }
}
