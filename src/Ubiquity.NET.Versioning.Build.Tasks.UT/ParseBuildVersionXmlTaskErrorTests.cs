// -----------------------------------------------------------------------
// <copyright file="ParseBuildVersionXmlTaskErrorTests.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

using Microsoft.Build.Evaluation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Ubiquity.NET.Versioning.Build.Tasks.UT.Support;

namespace Ubiquity.NET.Versioning.Build.Tasks.UT
{
    [TestClass]
    [TestCategory("Error Validation")]
    public class ParseBuildVersionXmlTaskErrorTests
    {
        public ParseBuildVersionXmlTaskErrorTests( TestContext ctx )
        {
            ArgumentNullException.ThrowIfNull(ctx);
            ArgumentException.ThrowIfNullOrWhiteSpace(ctx.TestResultsDirectory);

            Context = ctx;
        }

        public TestContext Context { get; }

        [TestMethod]
        public void CSM200_Empty_BuildVersionXml_path_fails( )
        {
            var globalProperties = new Dictionary<string, string>
            {
                ["BuildVersionXml"] = " " // Non-null/empty, but all WS
            };

            using var collection = new ProjectCollection(globalProperties);
            using var fullResults = Context.CreateTestProjectAndInvokeTestedPackage("net8.0", collection);
            var (buildResults, props) = fullResults;
            Assert.IsFalse(buildResults.Success);
            var errors = buildResults.Output.ErrorEvents.Where(evt=>evt.Code == "CSM200").ToList();
            Assert.AreEqual(1, errors.Count);
        }

        [TestMethod]
        public void CSM201_non_existent_BuildVersionXml_file_fails( )
        {
            var globalProperties = new Dictionary<string, string>
            {
                ["BuildVersionXml"] = Context.CreateRandomFilePath()
            };

            using var collection = new ProjectCollection(globalProperties);
            using var fullResults = Context.CreateTestProjectAndInvokeTestedPackage("net8.0", collection);
            var (buildResults, props) = fullResults;
            Assert.IsFalse(buildResults.Success);
            var errors = buildResults.Output.ErrorEvents.Where(evt=>evt.Code == "CSM201").ToList();
            Assert.AreEqual(1, errors.Count);
        }

        [TestMethod]
        public void CSM202_existing_BuildVersionXml_file_with_missing_BuildVersionData_element_fails( )
        {
            string buildVersionXmlPath = Context.CreateRandomFilePath();
            var globalProperties = new Dictionary<string, string>
            {
                ["BuildVersionXml"] = buildVersionXmlPath
            };

            using(var strm = File.Open(buildVersionXmlPath, FileMode.CreateNew))
            {
                var element = new XElement("RootElement");
                element.Save( strm );
                Context.WriteLine( $"BuildVersionXML written to: '{buildVersionXmlPath}'" );
            }

            using var collection = new ProjectCollection(globalProperties);
            using var fullResults = Context.CreateTestProjectAndInvokeTestedPackage("net8.0", collection);
            var (buildResults, props) = fullResults;
            Assert.IsFalse(buildResults.Success);
            var errors = buildResults.Output.ErrorEvents.Where(evt=>evt.Code == "CSM202").ToList();
            Assert.AreEqual(1, errors.Count);
        }

        [TestMethod]
        public void CSM203_existing_BuildVersionXml_file_with_unknown_attribute_warns( )
        {
            string buildVersionXmlPath = Context.CreateRandomFilePath();
            var globalProperties = new Dictionary<string, string>
            {
                ["BuildVersionXml"] = buildVersionXmlPath
            };

            using(var strm = File.Open(buildVersionXmlPath, FileMode.CreateNew))
            {
                var element = new XElement("BuildVersionData",
                                           new XAttribute(PropertyNames.BuildMajor, "1"),
                                           new XAttribute(PropertyNames.BuildMinor, "2"),
                                           new XAttribute(PropertyNames.BuildPatch, "3"),
                                           new XAttribute("Unknown", "Uh-oh!")
                                          );
                element.Save( strm );
                Context.WriteLine( $"BuildVersionXML written to: '{buildVersionXmlPath}'" );
            }

            using var collection = new ProjectCollection(globalProperties);
            using var fullResults = Context.CreateTestProjectAndInvokeTestedPackage("net8.0", collection);
            var (buildResults, props) = fullResults;
            Assert.IsTrue(buildResults.Success);
            var warnings = buildResults.Output.WarningEvents.Where(evt=>evt.Code == "CSM203").ToList();
            Assert.AreEqual(1, warnings.Count);
        }

        [TestMethod]
        public void CSM204_existing_BuildVersionXml_file_with_invalid_xml_fails( )
        {
            var globalProperties = new Dictionary<string, string>
            {
                ["BuildVersionXml"] = Context.CreateRandomFile() // random empty file is NOT valid XML
            };

            using var collection = new ProjectCollection(globalProperties);
            using var fullResults = Context.CreateTestProjectAndInvokeTestedPackage("net8.0", collection);
            var (buildResults, props) = fullResults;
            Assert.IsFalse(buildResults.Success);
            var errors = buildResults.Output.ErrorEvents.Where(evt=>evt.Code == "CSM204").ToList();
            Assert.AreEqual(1, errors.Count);
        }
    }
}
