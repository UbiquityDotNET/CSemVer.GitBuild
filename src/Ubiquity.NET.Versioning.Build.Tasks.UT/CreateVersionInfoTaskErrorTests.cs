// -----------------------------------------------------------------------
// <copyright file="CreateVersionInfoTaskErrorTests.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using Microsoft.Build.Evaluation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Ubiquity.NET.Versioning.Build.Tasks.UT.Support;

namespace Ubiquity.NET.Versioning.Build.Tasks.UT
{
    [TestClass]
    [TestCategory("Error Validation")]
    public class CreateVersionInfoTaskErrorTests
    {
        public CreateVersionInfoTaskErrorTests( TestContext ctx )
        {
            ArgumentNullException.ThrowIfNull( ctx );
            ArgumentException.ThrowIfNullOrWhiteSpace( ctx.TestResultsDirectory );

            Context = ctx;
        }

        public TestContext Context { get; }

        [TestMethod]
        public void CSM100_BuildMajor_Negative_should_fail( )
        {
            var globalProperties = new Dictionary<string, string>
            {
                [PropertyNames.BuildMajor] = "-1",
                [PropertyNames.BuildMinor] = "1",
                [PropertyNames.BuildPatch] = "2",
            };

            using var collection = new ProjectCollection(globalProperties);
            using var fullResults = Context.CreateTestProjectAndInvokeTestedPackage("net8.0", collection);
            var (buildResults, props) = fullResults;
            Assert.IsFalse( buildResults.Success );
            var errors = buildResults.Output.ErrorEvents.Where(evt=>evt.Code == "CSM100").ToList();
            Assert.AreEqual( 1, errors.Count );
        }

        [TestMethod]
        public void CSM100_BuildMajor_too_large_should_fail( )
        {
            var globalProperties = new Dictionary<string, string>
            {
                [PropertyNames.BuildMajor] = "100000",
                [PropertyNames.BuildMinor] = "1",
                [PropertyNames.BuildPatch] = "2",
            };

            using var collection = new ProjectCollection(globalProperties);
            using var fullResults = Context.CreateTestProjectAndInvokeTestedPackage("net8.0", collection);
            var (buildResults, props) = fullResults;
            Assert.IsFalse( buildResults.Success );
            var errors = buildResults.Output.ErrorEvents.Where(evt=>evt.Code == "CSM100").ToList();
            Assert.AreEqual( 1, errors.Count );
        }

        [TestMethod]
        public void CSM101_BuildMinor_Negative_should_fail( )
        {
            var globalProperties = new Dictionary<string, string>
            {
                [PropertyNames.BuildMajor] = "1",
                [PropertyNames.BuildMinor] = "-1",
                [PropertyNames.BuildPatch] = "2",
            };

            using var collection = new ProjectCollection(globalProperties);
            using var fullResults = Context.CreateTestProjectAndInvokeTestedPackage("net8.0", collection);
            var (buildResults, props) = fullResults;
            Assert.IsFalse( buildResults.Success );
            var errors = buildResults.Output.ErrorEvents.Where(evt=>evt.Code == "CSM101").ToList();
            Assert.AreEqual( 1, errors.Count );
        }

        [TestMethod]
        public void CSM101_BuildMinor_too_large_should_fail( )
        {
            var globalProperties = new Dictionary<string, string>
            {
                [PropertyNames.BuildMajor] = "1",
                [PropertyNames.BuildMinor] = "50000",
                [PropertyNames.BuildPatch] = "2",
            };

            using var collection = new ProjectCollection(globalProperties);
            using var fullResults = Context.CreateTestProjectAndInvokeTestedPackage("net8.0", collection);
            var (buildResults, props) = fullResults;
            Assert.IsFalse( buildResults.Success );
            var errors = buildResults.Output.ErrorEvents.Where(evt=>evt.Code == "CSM101").ToList();
            Assert.AreEqual( 1, errors.Count );
        }

        [TestMethod]
        public void CSM102_BuildPatch_Negative_should_fail( )
        {
            var globalProperties = new Dictionary<string, string>
            {
                [PropertyNames.BuildMajor] = "1",
                [PropertyNames.BuildMinor] = "1",
                [PropertyNames.BuildPatch] = "-1",
            };

            using var collection = new ProjectCollection(globalProperties);
            using var fullResults = Context.CreateTestProjectAndInvokeTestedPackage("net8.0", collection);
            var (buildResults, props) = fullResults;
            Assert.IsFalse( buildResults.Success );
            var errors = buildResults.Output.ErrorEvents.Where(evt=>evt.Code == "CSM102").ToList();
            Assert.AreEqual( 1, errors.Count );
        }

        [TestMethod]
        public void CSM102_BuildPatch_too_large_should_fail( )
        {
            var globalProperties = new Dictionary<string, string>
            {
                [PropertyNames.BuildMajor] = "1",
                [PropertyNames.BuildMinor] = "2",
                [PropertyNames.BuildPatch] = "10000",
            };

            using var collection = new ProjectCollection(globalProperties);
            using var fullResults = Context.CreateTestProjectAndInvokeTestedPackage("net8.0", collection);
            var (buildResults, props) = fullResults;
            Assert.IsFalse( buildResults.Success );
            var errors = buildResults.Output.ErrorEvents.Where(evt=>evt.Code == "CSM102").ToList();
            Assert.AreEqual( 1, errors.Count );
        }

        [TestMethod]
        public void CSM103_Unknown_PreReleaseName_should_fail( )
        {
            var globalProperties = new Dictionary<string, string>
            {
                [PropertyNames.BuildMajor] = "1",
                [PropertyNames.BuildMinor] = "2",
                [PropertyNames.BuildPatch] = "3",
                [PropertyNames.PreReleaseName] = "invalid" // not one of the 8 supported names...
            };

            using var collection = new ProjectCollection(globalProperties);
            using var fullResults = Context.CreateTestProjectAndInvokeTestedPackage("net8.0", collection);
            var (buildResults, props) = fullResults;
            Assert.IsFalse( buildResults.Success );
            var errors = buildResults.Output.ErrorEvents.Where(evt=>evt.Code == "CSM103").ToList();
            Assert.AreEqual( 1, errors.Count );
        }

        [TestMethod]
        public void CSM104_PreReleaseNumber_Negative_should_fail( )
        {
            var globalProperties = new Dictionary<string, string>
            {
                [PropertyNames.BuildMajor] = "1",
                [PropertyNames.BuildMinor] = "2",
                [PropertyNames.BuildPatch] = "3",
                [PropertyNames.PreReleaseName] = "alpha",
                [PropertyNames.PreReleaseNumber] = "-1"
            };

            using var collection = new ProjectCollection(globalProperties);
            using var fullResults = Context.CreateTestProjectAndInvokeTestedPackage("net8.0", collection);
            var (buildResults, props) = fullResults;
            Assert.IsFalse( buildResults.Success );
            var errors = buildResults.Output.ErrorEvents.Where(evt=>evt.Code == "CSM104").ToList();
            Assert.AreEqual( 1, errors.Count );
        }

        [TestMethod]
        public void CSM104_PreReleaseNumber_too_large_should_fail( )
        {
            var globalProperties = new Dictionary<string, string>
            {
                [PropertyNames.BuildMajor] = "1",
                [PropertyNames.BuildMinor] = "2",
                [PropertyNames.BuildPatch] = "3",
                [PropertyNames.PreReleaseName] = "alpha",
                [PropertyNames.PreReleaseNumber] = "100"
            };

            using var collection = new ProjectCollection(globalProperties);
            using var fullResults = Context.CreateTestProjectAndInvokeTestedPackage("net8.0", collection);
            var (buildResults, props) = fullResults;
            Assert.IsFalse( buildResults.Success );
            var errors = buildResults.Output.ErrorEvents.Where(evt=>evt.Code == "CSM104").ToList();
            Assert.AreEqual( 1, errors.Count );
        }

        [TestMethod]
        public void PreReleaseNumber_ignored_when_no_PreReleaseName( )
        {
            var globalProperties = new Dictionary<string, string>
            {
                [PropertyNames.BuildMajor] = "1",
                [PropertyNames.BuildMinor] = "2",
                [PropertyNames.BuildPatch] = "3",

                // [PropertyNames.PreReleaseName] = "alpha",
                [PropertyNames.PreReleaseNumber] = "100"
            };

            using var collection = new ProjectCollection(globalProperties);
            using var fullResults = Context.CreateTestProjectAndInvokeTestedPackage("net8.0", collection);
            var (buildResults, props) = fullResults;
            Assert.IsTrue( buildResults.Success );
        }

        [TestMethod]
        public void CSM105_PreReleaseFix_Negative_should_fail( )
        {
            var globalProperties = new Dictionary<string, string>
            {
                [PropertyNames.BuildMajor] = "1",
                [PropertyNames.BuildMinor] = "2",
                [PropertyNames.BuildPatch] = "3",
                [PropertyNames.PreReleaseName] = "alpha",
                [PropertyNames.PreReleaseNumber] = "1",
                [PropertyNames.PreReleaseFix] = "-1"
            };

            using var collection = new ProjectCollection(globalProperties);
            using var fullResults = Context.CreateTestProjectAndInvokeTestedPackage("net8.0", collection);
            var (buildResults, props) = fullResults;
            Assert.IsFalse( buildResults.Success );
            var errors = buildResults.Output.ErrorEvents.Where(evt=>evt.Code == "CSM105").ToList();
            Assert.AreEqual( 1, errors.Count );
        }

        [TestMethod]
        public void CSM105_PreReleaseFix_too_large_should_fail( )
        {
            var globalProperties = new Dictionary<string, string>
            {
                [PropertyNames.BuildMajor] = "1",
                [PropertyNames.BuildMinor] = "2",
                [PropertyNames.BuildPatch] = "3",
                [PropertyNames.PreReleaseName] = "alpha",
                [PropertyNames.PreReleaseNumber] = "1",
                [PropertyNames.PreReleaseFix] = "100"
            };

            using var collection = new ProjectCollection(globalProperties);
            using var fullResults = Context.CreateTestProjectAndInvokeTestedPackage("net8.0", collection);
            var (buildResults, props) = fullResults;
            Assert.IsFalse( buildResults.Success );
            var errors = buildResults.Output.ErrorEvents.Where(evt=>evt.Code == "CSM105").ToList();
            Assert.AreEqual( 1, errors.Count );
        }

        [TestMethod]
        public void PreReleaseFix_ignored_when_no_PreReleaseName( )
        {
            var globalProperties = new Dictionary<string, string>
            {
                [PropertyNames.BuildMajor] = "1",
                [PropertyNames.BuildMinor] = "2",
                [PropertyNames.BuildPatch] = "3",

                // [PropertyNames.PreReleaseName] = "alpha",
                [PropertyNames.PreReleaseNumber] = "1",
                [PropertyNames.PreReleaseFix] = "100"
            };

            using var collection = new ProjectCollection(globalProperties);
            using var fullResults = Context.CreateTestProjectAndInvokeTestedPackage("net8.0", collection);
            var (buildResults, props) = fullResults;
            Assert.IsTrue( buildResults.Success );
        }

        [TestMethod]
        public void PreReleaseFix_ignored_when_no_PreReleaseNumber( )
        {
            var globalProperties = new Dictionary<string, string>
            {
                [PropertyNames.BuildMajor] = "1",
                [PropertyNames.BuildMinor] = "2",
                [PropertyNames.BuildPatch] = "3",
                [PropertyNames.PreReleaseName] = "alpha",

                // [PropertyNames.PreReleaseNumber] = "1",
                [PropertyNames.PreReleaseFix] = "100"
            };

            using var collection = new ProjectCollection(globalProperties);
            using var fullResults = Context.CreateTestProjectAndInvokeTestedPackage("net8.0", collection);
            var (buildResults, props) = fullResults;
            Assert.IsTrue( buildResults.Success );
        }

#if CAN_FORCE_CSM106_SOMEHOW // see docs and IsReleaseBuild_forces_CI_properties_empty() test method
        [TestMethod]
        public void CSM106_When_only_CiBuildIndex_is_set( )
        {
            var globalProperties = new Dictionary<string, string>
            {
                [PropertyNames.BuildMajor] = "1",
                [PropertyNames.BuildMinor] = "2",
                [PropertyNames.BuildPatch] = "3",
                [EnvVarNames.IsReleaseBuild] = "true", // Bypasses props file setting of CiBuildName
                [PropertyNames.CiBuildIndex] = "ABC01234"
            };

            using var collection = new ProjectCollection(globalProperties);
            using var fullResults = Context.CreateTestProjectAndInvokeTestedPackage("net8.0", collection);
            var (buildResults, props) = fullResults;
            Assert.IsFalse(buildResults.Success);
            var errors = buildResults.Output.ErrorEvents.Where(evt=>evt.Code == "CSM106").ToList();
            Assert.AreEqual(1, errors.Count);
        }

        [TestMethod]
        public void CSM106_When_only_CiBuildName_is_set( )
        {
            var globalProperties = new Dictionary<string, string>
            {
                [PropertyNames.BuildMajor] = "1",
                [PropertyNames.BuildMinor] = "2",
                [PropertyNames.BuildPatch] = "3",
                [EnvVarNames.IsReleaseBuild] = "true", // Bypasses props file setting of CiBuildName
                [PropertyNames.CiBuildName] = "01234ABC"
            };

            using var collection = new ProjectCollection(globalProperties);
            using var fullResults = Context.CreateTestProjectAndInvokeTestedPackage("net8.0", collection);
            var (buildResults, props) = fullResults;
            Assert.IsFalse(buildResults.Success);
            var errors = buildResults.Output.ErrorEvents.Where(evt=>evt.Code == "CSM106").ToList();
            Assert.AreEqual(1, errors.Count);
        }
#endif

        [TestMethod]
        public void IsReleaseBuild_forces_CI_properties_empty( )
        {
            var globalProperties = new Dictionary<string, string>
            {
                [PropertyNames.BuildMajor] = "1",
                [PropertyNames.BuildMinor] = "2",
                [PropertyNames.BuildPatch] = "3",
                [EnvVarNames.IsReleaseBuild] = "true", // Bypasses props file setting of CiBuildName; and forces it to clear the CI info
                [PropertyNames.CiBuildIndex] = "ABC01234", // targets file uses "TreatAsLocalProperty" to allow override/mutability of these
                [PropertyNames.CiBuildName] = "01234ABC",
                [PropertyNames.BuildTime] = DateTime.UtcNow.ToString("o")
            };

            using var collection = new ProjectCollection(globalProperties);

            using var fullResults = Context.CreateTestProjectAndInvokeTestedPackage("net8.0", collection);
            var (buildResults, props) = fullResults;
            Assert.IsTrue( buildResults.Success );
            Assert.IsTrue( string.IsNullOrWhiteSpace( props.CiBuildIndex ) );
            Assert.IsTrue( string.IsNullOrWhiteSpace( props.CiBuildName ) );
            Assert.IsTrue( string.IsNullOrWhiteSpace( props.BuildTime ) );
        }

        [TestMethod]
        public void CSM107_CiBuildIndex_has_bad_syntax( )
        {
            var globalProperties = new Dictionary<string, string>
            {
                [PropertyNames.BuildMajor] = "1",
                [PropertyNames.BuildMinor] = "2",
                [PropertyNames.BuildPatch] = "3",
                [PropertyNames.CiBuildIndex] = "01234ABC="
            };

            using var collection = new ProjectCollection(globalProperties);
            using var fullResults = Context.CreateTestProjectAndInvokeTestedPackage("net8.0", collection);
            var (buildResults, props) = fullResults;
            Assert.IsFalse( buildResults.Success );
            var errors = buildResults.Output.ErrorEvents.Where(evt=>evt.Code == "CSM107").ToList();
            Assert.AreEqual( 1, errors.Count );
        }

        [TestMethod]
        public void CSM108_CiBuildName_has_bad_syntax( )
        {
            var globalProperties = new Dictionary<string, string>
            {
                [PropertyNames.BuildMajor] = "1",
                [PropertyNames.BuildMinor] = "2",
                [PropertyNames.BuildPatch] = "3",
                [PropertyNames.CiBuildIndex] = "FOO", // valid, but of questionable value. 8^)
                [PropertyNames.CiBuildName] = "01234_ABC" // '_' is not a valid char!
            };

            using var collection = new ProjectCollection(globalProperties);
            using var fullResults = Context.CreateTestProjectAndInvokeTestedPackage("net8.0", collection);
            var (buildResults, props) = fullResults;
            Assert.IsFalse( buildResults.Success );
            var errors = buildResults.Output.ErrorEvents.Where(evt=>evt.Code == "CSM108").ToList();
            Assert.AreEqual( 1, errors.Count );
        }

        [TestMethod]
        public void CSM109_BaseBuild_already_at_max_fails( )
        {
            var globalProperties = new Dictionary<string, string>
            {
                [PropertyNames.BuildMajor] = MaxMajor.ToString(CultureInfo.InvariantCulture),
                [PropertyNames.BuildMinor] = MaxMinor.ToString(CultureInfo.InvariantCulture),
                [PropertyNames.BuildPatch] = MaxPatch.ToString(CultureInfo.InvariantCulture),
                [PropertyNames.CiBuildIndex] = "ABCDEF",
                [PropertyNames.CiBuildName] = "01234-ABC" // '-' is valid!
            };

            using var collection = new ProjectCollection(globalProperties);
            using var fullResults = Context.CreateTestProjectAndInvokeTestedPackage("net8.0", collection);
            var (buildResults, props) = fullResults;
            Assert.IsFalse( buildResults.Success );
            var errors = buildResults.Output.ErrorEvents.Where(evt=>evt.Code == "CSM109").ToList();
            Assert.AreEqual( 1, errors.Count );
        }

        private const int MaxMajor = 99999;
        private const int MaxMinor = 49999;
        private const int MaxPatch = 9999;

        //private const byte MaxPrereleaseNumberOrFix = 99;
    }
}
