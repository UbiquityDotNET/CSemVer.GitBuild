// -----------------------------------------------------------------------
// <copyright file="ParseBuildVersionXml.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Xml.Linq;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Ubiquity.NET.Versioning.Build.Tasks
{
    public class ParseBuildVersionXml
        : Task
    {
        [Required]
        public string? BuildVersionXml { get; set; }

        [Output]
        public string? BuildMajor { get; private set; }

        [Output]
        public string? BuildMinor { get; private set; }

        [Output]
        public string? BuildPatch { get; private set; }

        [Output]
        public string? PreReleaseName { get; private set; }

        [Output]
        public string? PreReleaseNumber { get; private set; }

        [Output]
        public string? PreReleaseFix { get; private set; }

        [SuppressMessage( "Design", "CA1031:Do not catch general exception types", Justification = "Caught exceptions are logged as errors" )]
        public override bool Execute( )
        {
            try
            {
                Log.LogMessage(MessageImportance.Low, $"+{nameof(ParseBuildVersionXml)} Task");
                if (string.IsNullOrWhiteSpace(BuildVersionXml))
                {
                    LogError("CSM200", "BuildVersionXml is required and must not be all whitespace");
                    return false;
                }

                if (!File.Exists(BuildVersionXml))
                {
                    LogError("CSM201", $"Specified BuildVersionXml does not exist '{BuildVersionXml}'");
                    return false;
                }

                using var stream = File.OpenText( BuildVersionXml );
                var xdoc = XDocument.Load( stream, System.Xml.Linq.LoadOptions.None );
                var data = xdoc.Element( "BuildVersionData" );
                if (data is null)
                {
                    LogError("CSM202", $"BuildVersionData element does not exist in '{BuildVersionXml}'");
                    return false;
                }

                foreach( var attrib in data.Attributes( ) )
                {
                    switch( attrib.Name.LocalName )
                    {
                    case "BuildMajor":
                        Log.LogMessage(MessageImportance.Low, $"BuildMajor: {attrib.Value}");
                        BuildMajor = attrib.Value;
                        break;

                    case "BuildMinor":
                        Log.LogMessage(MessageImportance.Low, $"BuildMinor: {attrib.Value}");
                        BuildMinor = attrib.Value;
                        break;

                    case "BuildPatch":
                        Log.LogMessage(MessageImportance.Low, $"BuildPatch: {attrib.Value}");
                        BuildPatch = attrib.Value;
                        break;

                    case "PreReleaseName":
                        Log.LogMessage(MessageImportance.Low, $"PreReleaseName: {attrib.Value}");
                        PreReleaseName = attrib.Value;
                        break;

                    case "PreReleaseNumber":
                        Log.LogMessage(MessageImportance.Low, $"PreReleaseNumber: {attrib.Value}");
                        PreReleaseNumber = attrib.Value;
                        break;

                    case "PreReleaseFix":
                        Log.LogMessage(MessageImportance.Low, $"PreReleaseFix: {attrib.Value}");
                        PreReleaseFix = attrib.Value;
                        break;

                    default:
                        LogWarning( "CSM203", "Unexpected attribute {0}", attrib.Name.LocalName );
                        break;
                    }
                }

                // correct malformed values
                if( string.IsNullOrWhiteSpace( PreReleaseName ) )
                {
                    Log.LogMessage(MessageImportance.Low, "PreReleaseName not provided, forcing PreReleaseNumber and PreReleaseFix == 0");
                    PreReleaseNumber = "0";
                    PreReleaseFix = "0";
                }

                Log.LogMessage(MessageImportance.Low, $"-{nameof(ParseBuildVersionXml)} Task");
                return true;
            }
            catch(System.Xml.XmlException)
            {
                LogError("CSM204", "XML format of '{0}' is invalid", BuildVersionXml!);
                return false;
            }
            catch(Exception ex)
            {
                Log.LogErrorFromException(ex, showStackTrace: true);
                return false;
            }
        }

        private void LogError(
            string code,
            /*[StringSyntax(StringSyntaxAttribute.CompositeFormat)]*/ string message,
            params object[] messageArgs
            )
        {
            Log.LogError($"{nameof(ParseBuildVersionXml)} Task", code, null, null, 0, 0, 0, 0, message, messageArgs);
        }

        private void LogWarning(
            string code,
            /*[StringSyntax(StringSyntaxAttribute.CompositeFormat)]*/ string message,
            params object[] messageArgs
            )
        {
            Log.LogWarning($"{nameof(ParseBuildVersionXml)} Task", code, null, null, 0, 0, 0, 0, message, messageArgs);
        }
    }
}
