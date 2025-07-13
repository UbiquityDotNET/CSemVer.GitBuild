// -----------------------------------------------------------------------
// <copyright file="CreateVersionInfo.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

// NOTE: Due to constraints, limitations and general issues with MSBUILD Tasks and dependency resolution
//       This must NOT have any dependencies and therefore, does it all locally. This is not as complete
//       as what is offered in the Ubiquity.NET.Versioning library but enables the tasks to function with
//       the least of changes to the consumer (Update to package name and version, gets updated/corrected
//       version)
//
// For gory details of the problems of creating a task with dependencies
// See: https://natemcmaster.com/blog/2017/11/11/msbuild-task-with-dependencies/
namespace Ubiquity.NET.Versioning.Build.Tasks
{
    public class CreateVersionInfo
        : Task
    {
        [Required]
        public int BuildMajor { get; private set; }

        [Required]
        public int BuildMinor { get; private set; }

        [Required]
        public int BuildPatch { get; private set; }

        public string? PreReleaseName { get; private set; }

        public int PreReleaseNumber { get; private set; }

        public int PreReleaseFix { get; private set; }

        public string? CiBuildIndex { get; set; }

        public string? CiBuildName { get; set; }

        public string? BuildMeta { get; set; }

        [Output]
        public string? CSemVer { get; set; }

        [Output]
        public ushort? FileVersionMajor { get; set; }

        [Output]
        public ushort? FileVersionMinor { get; set; }

        [Output]
        public ushort? FileVersionBuild { get; set; }

        [Output]
        public ushort? FileVersionRevision { get; set; }

        public bool IsCIBuild => !string.IsNullOrWhiteSpace( CiBuildIndex )
                              && !string.IsNullOrWhiteSpace( CiBuildName );

        [SuppressMessage( "Design", "CA1031:Do not catch general exception types", Justification = "Caught exceptions are logged as errors" )]
        public override bool Execute( )
        {
            try
            {
                if(PreReleaseName is not null && string.Compare( PreReleaseName, "prerelease", StringComparison.OrdinalIgnoreCase ) == 0)
                {
                    PreReleaseName = "pre";
                }

                Log.LogMessage( MessageImportance.Low, $"+{nameof( CreateVersionInfo )} Task" );

                if(!ValidateInput())
                {
                    return false;
                }

                Log.LogMessage( MessageImportance.Low, "CiBuildIndex={0}", CiBuildIndex ?? string.Empty );
                Log.LogMessage( MessageImportance.Low, "CiBuildName={0}", CiBuildName ?? string.Empty );
                Log.LogMessage( MessageImportance.Low, "BuildMeta={0}", BuildMeta ?? string.Empty );
                Log.LogMessage( MessageImportance.Low, "PreReleaseName={0}", PreReleaseName ?? string.Empty );
                Log.LogMessage( MessageImportance.Low, "PreReleaseNumber={0}", PreReleaseNumber );
                Log.LogMessage( MessageImportance.Low, "PreReleaseFix={0}", PreReleaseFix );

                int preRelIndex = ComputePreReleaseIndex( PreReleaseName);
                Log.LogMessage( MessageImportance.Low, "PreRelIndex={0}", preRelIndex );

                SetFileVersion( preRelIndex );

                CSemVer = CreateSemVerString( preRelIndex, alwaysIncludeZero: IsCIBuild );
                if(string.IsNullOrWhiteSpace( CSemVer ))
                {
                    return false;
                }

                Log.LogMessage( MessageImportance.Low, "CSemVer={0}", CSemVer ?? string.Empty );

                return true;
            }
            catch(Exception ex)
            {
                Log.LogErrorFromException( ex, showStackTrace: true );
                return false;
            }
            finally
            {
                Log.LogMessage( MessageImportance.Low, $"-{nameof( CreateVersionInfo )} Task" );
            }
        }

        /// <summary>Creates a formatted CSemver from the properties of this instance</summary>
        /// <param name="preRelIndex">Numeric form of the build index [(-1)-7] where -1 indicates not a pre-release</param>
        /// <param name="alwaysIncludeZero">Flag to indicate if a 0 pre-release is ALWAYs included [see remarks]</param>
        /// <param name="includeMetadata">Flag to indicate if the metadata is included</param>
        /// <returns>Formatted CSemVer string</returns>
        /// <remarks>
        /// <para>The <paramref name="alwaysIncludeZero"/> is for legacy behavior and should generally be left at the default.
        /// In the current version based on CSemVer v1.0.0-rc.1 the behavior is the same as for a full version. (The Number
        /// is omitted unless it is > 0 OR Fix >0).</para>
        /// </remarks>
        private string? CreateSemVerString( int preRelIndex, bool alwaysIncludeZero = false, bool includeMetadata = true )
        {
            if(IsCIBuild)
            {
                Int64 patchPlus1 = MakeOrderedVersion(preRelIndex) + (Int64)MulPatch;
                if(patchPlus1 > MaxOrderedVersion)
                {
                    LogError( "CSM109", "base version is too large to represent a valid CSemVer-CI version" );
                    return null;
                }

                UpdateFromOrderedVersion( patchPlus1 );
            }

            var bldr = new StringBuilder()
                          .AppendFormat(CultureInfo.InvariantCulture, "{0}.{1}.{2}", BuildMajor, BuildMinor, BuildPatch);

            bool isPreRelease = preRelIndex >= 0;
            if(isPreRelease)
            {
                bldr.Append( '-' )
                    .Append( PreReleaseNames[ preRelIndex ] );

                if(PreReleaseNumber > 0 || PreReleaseFix > 0 || alwaysIncludeZero)
                {
                    bldr.AppendFormat( CultureInfo.InvariantCulture, ".{0}", PreReleaseNumber );
                    if(PreReleaseFix > 0 || alwaysIncludeZero)
                    {
                        bldr.AppendFormat( CultureInfo.InvariantCulture, ".{0}", PreReleaseFix );
                    }
                }
            }

            if(!string.IsNullOrWhiteSpace( CiBuildIndex ) && !string.IsNullOrWhiteSpace( CiBuildName ))
            {
                bldr.AppendFormat( CultureInfo.InvariantCulture, isPreRelease ? ".ci.{0}.{1}" : "--ci.{0}.{1}", CiBuildIndex, CiBuildName );
            }

            if(!string.IsNullOrWhiteSpace( BuildMeta ) && includeMetadata)
            {
                bldr.AppendFormat( CultureInfo.InvariantCulture, $"+{BuildMeta}" );
            }

            return bldr.ToString();
        }

        private Int64 MakeOrderedVersion( int preRelIndex )
        {
            UInt64 orderedVersion = ((ulong)BuildMajor * MulMajor) + ((ulong)BuildMinor * MulMinor) + (((ulong)BuildPatch + 1) * MulPatch);

            if(preRelIndex >= 0)
            {
                orderedVersion -= MulPatch - 1; // Remove the fix+1 multiplier
                orderedVersion += (ulong)preRelIndex * MulName;
                orderedVersion += ((ulong)PreReleaseNumber) * MulNum;
                orderedVersion += (ulong)PreReleaseFix;
            }

            return (Int64)orderedVersion;
        }

        private void UpdateFromOrderedVersion( Int64 orderedVersion )
        {
            // This effectively reverses the math used in computing the ordered version.
            UInt64 accumulator = (UInt64)orderedVersion;
            UInt64 preRelPart = accumulator % MulPatch;

            // skipping pre-release info as it is used AS-IS
            if(preRelPart == 0)
            {
                accumulator -= MulPatch;
            }

            BuildMajor = (Int32)(accumulator / MulMajor);
            accumulator %= MulMajor;

            BuildMinor = (Int32)(accumulator / MulMinor);
            accumulator %= MulMinor;

            BuildPatch = (Int32)(accumulator / MulPatch);
        }

        private void SetFileVersion( int preRelIndex )
        {
            Int64 orderedVersion = MakeOrderedVersion(preRelIndex );
            Log.LogMessage( MessageImportance.Low, "orderedVersion[For FileVersion]={0}", orderedVersion );

            // CI Builds are POST release numbers so they are always ODD
            UInt64 fileVersion64 = (((UInt64)orderedVersion) << 1) + (IsCIBuild ? 1ul : 0ul);
            FileVersionRevision = (UInt16)(fileVersion64 % 65536);

            UInt64 rem = (fileVersion64 - FileVersionRevision.Value) / 65536;
            FileVersionBuild = (UInt16)(rem % 65536);

            rem = (rem - FileVersionBuild.Value) / 65536;
            FileVersionMinor = (UInt16)(rem % 65536);

            rem = (rem - FileVersionMinor.Value) / 65536;
            FileVersionMajor = (UInt16)(rem % 65536);

            Log.LogMessage( MessageImportance.Low, "FileVersionMajor={0}", FileVersionMajor );
            Log.LogMessage( MessageImportance.Low, "FileVersionMinor={0}", FileVersionMinor );
            Log.LogMessage( MessageImportance.Low, "FileVersionBuild={0}", FileVersionBuild );
            Log.LogMessage( MessageImportance.Low, "FileVersionRevision={0}", FileVersionRevision );
        }

        private bool ValidateInput( )
        {
            // Try to report as many input errors at once as is possible
            // That is, don't stop at first one - so all possible errors are logged.
            bool hasInputError = false;
            if(BuildMajor < 0 || BuildMajor > 99999)
            {
                LogError( "CSM100", "BuildMajor value must be in range [0-99999]" );
                hasInputError = true;
            }

            if(BuildMinor < 0 || BuildMinor > 49999)
            {
                LogError( "CSM101", "BuildMinor value must be in range [0-49999]" );
                hasInputError = true;
            }

            if(BuildPatch < 0 || BuildPatch > 9999)
            {
                LogError( "CSM102", "BuildPatch value must be in range [0-9999]" );
                hasInputError = true;
            }

            if(!string.IsNullOrWhiteSpace( PreReleaseName ))
            {
                if(!PreReleaseNames.Contains( PreReleaseName, StringComparer.InvariantCultureIgnoreCase ))
                {
                    LogError( "CSM103", "PreRelease Name is unknown" );
                    hasInputError = true;
                }

                if(PreReleaseNumber < 0 || PreReleaseNumber > 99)
                {
                    LogError( "CSM104", "PreReleaseNumber value must be in range [0-99]" );
                    hasInputError = true;
                }

                if(PreReleaseNumber != 0 && (PreReleaseFix < 0 || PreReleaseFix > 99))
                {
                    LogError( "CSM105", "PreReleaseFix value must be in range [0-99]" );
                    hasInputError = true;
                }
            }

            if(string.IsNullOrWhiteSpace( CiBuildIndex ) != string.IsNullOrWhiteSpace( CiBuildName ))
            {
                LogError( "CSM106", "If CiBuildIndex is set then CiBuildName must also be set; If CiBuildIndex is NOT set then CiBuildName must not be set." );
                hasInputError = true;
            }

            if(CiBuildIndex != null && !CiBuildIdRegEx.IsMatch( CiBuildIndex ))
            {
                LogError( "CSM107", "CiBuildIndex does not match syntax defined by CSemVer" );
                hasInputError = true;
            }

            if(CiBuildName != null && !CiBuildIdRegEx.IsMatch( CiBuildName ))
            {
                LogError( "CSM108", "CiBuildName does not match syntax defined by CSemVer" );
                hasInputError = true;
            }

            // CSM109 tested later as it requires computing of the ordered value.

            return !hasInputError;
        }

        private void LogError(
            string code,
            /*[StringSyntax(StringSyntaxAttribute.CompositeFormat)]*/ string message,
            params object[] messageArgs
            )
        {
            Log.LogError( $"{nameof( CreateVersionInfo )} Task", code, null, null, 0, 0, 0, 0, message, messageArgs );
        }

        private static int ComputePreReleaseIndex( string? preRelName )
        {
            return Find( PreReleaseNames, preRelName ).Index;
        }

        private static (string Value, int Index) Find( string[] values, string? value )
        {
            if(value is null)
            {
                return (string.Empty, -1);
            }

            var q = from element in values.Select( ( v, i ) => (Value: v, Index: i ) )
                    where string.Equals( element.Value, value, StringComparison.OrdinalIgnoreCase )
                    select element;

            var result = q.FirstOrDefault();
            return result == default ? (string.Empty, -1) : result;
        }

        /// <summary>Maximum value of an ordered version number</summary>
        /// <remarks>
        /// This represents a version of v99999.49999.9999. No CSemVer greater than
        /// this value is possible. Thus, no CI build is based on this version either
        /// as ALL CI builds are POST-RELEASE (pre-release of next, but there is no next!).
        /// </remarks>
        private const Int64 MaxOrderedVersion = 4000050000000000000L;

        private const ulong MulNum = 100;
        private const ulong MulName = MulNum * 100;
        private const ulong MulPatch = (MulName * 8) + 1;
        private const ulong MulMinor = MulPatch * 10000;
        private const ulong MulMajor = MulMinor * 50000;

        private static readonly string[] PreReleaseNames = ["alpha", "beta", "delta", "epsilon", "gamma", "kappa", "pre", "rc"];
        private static readonly Regex CiBuildIdRegEx = new(@"\A[0-9a-zA-Z\-]+\Z");
    }
}
