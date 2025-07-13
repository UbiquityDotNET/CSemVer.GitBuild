// -----------------------------------------------------------------------
// <copyright file="BuildTaskErrorTests.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Build.Evaluation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Ubiquity.NET.Versioning.Build.Tasks.UT.Support;

namespace Ubiquity.NET.Versioning.Build.Tasks.UT
{
    [TestClass]
    [TestCategory("Error Validation")]
    public class BuildTaskErrorTests
    {
        public BuildTaskErrorTests( TestContext ctx )
        {
            ArgumentNullException.ThrowIfNull(ctx);
            ArgumentException.ThrowIfNullOrWhiteSpace(ctx.TestResultsDirectory);

            Context = ctx;
        }

        public TestContext Context { get; }

        [TestMethod]
        public void CSM001_Missing_BuildMajor_should_fail( )
        {
            // use a build version XML that has no attributes to get the expected error
            // Otherwise, MSBUILD kicks in and complains about missing required param
            string buildVersionXml = Context.CreateEmptyBuildVersionXmlWithRandomName();
            var globalProperties = new Dictionary<string, string>
            {
                ["BuildVersionXml"] = buildVersionXml
            };

            using var collection = new ProjectCollection(globalProperties);
            using var fullResults = Context.CreateTestProjectAndInvokeTestedPackage("net8.0", collection);
            var (buildResults, props) = fullResults;
            Assert.IsFalse(buildResults.Success);
            var errors = buildResults.Output.ErrorEvents.Where(evt=>evt.Code == "CSM001").ToList();
            Assert.AreEqual(1, errors.Count);
        }

        [TestMethod]
        public void CSM002_Missing_BuildMinor_should_fail( )
        {
            var globalProperties = new Dictionary<string, string>
            {
                [PropertyNames.BuildMajor] = "10"
            };

            using var collection = new ProjectCollection(globalProperties);
            using var fullResults = Context.CreateTestProjectAndInvokeTestedPackage("net8.0", collection);
            var (buildResults, props) = fullResults;
            Assert.IsFalse(buildResults.Success);
            var errors = buildResults.Output.ErrorEvents.Where(evt=>evt.Code == "CSM002").ToList();
            Assert.AreEqual(1, errors.Count);
        }

        [TestMethod]
        public void CSM003_Missing_BuildPatch_should_fail( )
        {
            var globalProperties = new Dictionary<string, string>
            {
                [PropertyNames.BuildMajor] = "10",
                [PropertyNames.BuildMinor] = "1",
            };

            using var collection = new ProjectCollection(globalProperties);
            using var fullResults = Context.CreateTestProjectAndInvokeTestedPackage("net8.0", collection);
            var (buildResults, props) = fullResults;
            Assert.IsFalse(buildResults.Success);
            var errors = buildResults.Output.ErrorEvents.Where(evt=>evt.Code == "CSM003").ToList();
            Assert.AreEqual(1, errors.Count);
        }

        [TestMethod]
        public void CSM004_Missing_FileVersionMajor_should_fail( )
        {
            var globalProperties = new Dictionary<string, string>
            {
                [PropertyNames.BuildMajor] = "10",
                [PropertyNames.BuildMinor] = "1",
                [PropertyNames.BuildPatch] = "2",
                [PropertyNames.FullBuildNumber] = "\t", // Present but all whitespace; Presence of this skips the CreateVersionInfoTask
            };

            using var collection = new ProjectCollection(globalProperties);
            using var fullResults = Context.CreateTestProjectAndInvokeTestedPackage("net8.0", collection);
            var (buildResults, props) = fullResults;
            Assert.IsFalse(buildResults.Success);
            var errors = buildResults.Output.ErrorEvents.Where(evt=>evt.Code == "CSM004").ToList();
            Assert.AreEqual(1, errors.Count);
        }

        [TestMethod]
        public void CSM006_Missing_FileVersionMinor_should_fail( )
        {
            var globalProperties = new Dictionary<string, string>
            {
                [PropertyNames.BuildMajor] = "10",
                [PropertyNames.BuildMinor] = "1",
                [PropertyNames.BuildPatch] = "2",
                [PropertyNames.FullBuildNumber] = "\t", // Present but all whitespace; Presence of this skips the CreateVersionInfoTask
                [PropertyNames.FileVersionMajor] = "1", // avoid CSM004 to allow testing of next field requirement
            };

            using var collection = new ProjectCollection(globalProperties);
            using var fullResults = Context.CreateTestProjectAndInvokeTestedPackage("net8.0", collection);
            var (buildResults, props) = fullResults;
            Assert.IsFalse(buildResults.Success);
            var errors = buildResults.Output.ErrorEvents.Where(evt=>evt.Code == "CSM005").ToList();
            Assert.AreEqual(1, errors.Count);
        }

        [TestMethod]
        public void CSM006_Missing_FileVersionBuild_should_fail( )
        {
            var globalProperties = new Dictionary<string, string>
            {
                [PropertyNames.BuildMajor] = "10",
                [PropertyNames.BuildMinor] = "1",
                [PropertyNames.BuildPatch] = "2",
                [PropertyNames.FullBuildNumber] = "\t", // Present but all whitespace; Presence of this skips the CreateVersionInfoTask
                [PropertyNames.FileVersionMajor] = "1", // avoid CSM004 to allow testing of next field requirement
                [PropertyNames.FileVersionMinor] = "2", // avoid CSM005 to allow testing of next field requirement
            };

            using var collection = new ProjectCollection(globalProperties);
            using var fullResults = Context.CreateTestProjectAndInvokeTestedPackage("net8.0", collection);
            var (buildResults, props) = fullResults;
            Assert.IsFalse(buildResults.Success);
            var errors = buildResults.Output.ErrorEvents.Where(evt=>evt.Code == "CSM006").ToList();
            Assert.AreEqual(1, errors.Count);
        }

        [TestMethod]
        public void CSM007_Missing_FileVersionRevision_should_fail( )
        {
            var globalProperties = new Dictionary<string, string>
            {
                [PropertyNames.BuildMajor] = "10",
                [PropertyNames.BuildMinor] = "1",
                [PropertyNames.BuildPatch] = "2",
                [PropertyNames.FullBuildNumber] = "\t", // Present but all whitespace; Presence of this skips the CreateVersionInfoTask
                [PropertyNames.FileVersionMajor] = "1", // avoid CSM004 to allow testing of next field requirement
                [PropertyNames.FileVersionMinor] = "2", // avoid CSM005 to allow testing of next field requirement
                [PropertyNames.FileVersionBuild] = "3", // avoid CSM006 to allow testing of next field requirement
            };

            using var collection = new ProjectCollection(globalProperties);
            using var fullResults = Context.CreateTestProjectAndInvokeTestedPackage("net8.0", collection);
            var (buildResults, props) = fullResults;
            Assert.IsFalse(buildResults.Success);
            var errors = buildResults.Output.ErrorEvents.Where(evt=>evt.Code == "CSM007").ToList();
            Assert.AreEqual(1, errors.Count);
        }
    }
}
