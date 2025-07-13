// -----------------------------------------------------------------------
// <copyright file="DateTimeExtensions.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Globalization;

namespace Ubiquity.NET.Versioning.Build.Tasks.UT.Support
{
    /// <summary>Utility Class for <see cref="DateTime"/> extensions</summary>
    /// <remarks>
    /// <para>This is a clone of the code in the `Ubiquity.NET.Versioning` and implements the
    /// algorithm for converting a time stamp into a numerical value and returning a
    /// string form of that. This is intended for use in defining the CI index value.</para>
    /// <para>Cloning this for the tests, means they are independent of the versioning library,
    /// which makes it a candidate for isolation in a different repo/build that depends on the
    /// TASK package for generation of versions</para>
    /// <note type="note">
    /// Using just the commit ID, or even the GIT height, for a GIT repo, does NOT differentiate
    /// one local build from the next. Use of the time stamp for all does. Normally, the automated
    /// builds will use the time stamp of the head commit for this so that they remain consistent
    /// no matter how many times the automated build is re-run. Locally, the values are generated/set
    /// as env vars, so a developer can chose to force a re-init or not etc...
    /// </note>
    /// </remarks>
    internal static class DateTimeExtensions
    {
        /// <summary>Gets a build index based on a time stamp</summary>
        /// <param name="timeStamp">Time stamp to use to create the build index</param>
        /// <returns>
        /// Build index as a string. The time stamp is converted to UTC (if not already in UTC form)
        /// so that the resulting index is consistent across builds on different machines/locales.
        /// </returns>
        /// <remarks>
        /// Since the resulting build index is based on the number of seconds since midnight and needs
        /// to fit in a limited string output. There is a narrow window of 2 seconds where two distinct
        /// builds (Local or PR) might generate the same matching build number. However, since this number
        /// is normally only used for local builds that's not realistically a problem. (Automated builds
        /// usually use the time stamp of the HEAD commit of the repo as the build index)
        /// </remarks>
        public static string ToBuildIndex( this DateTime timeStamp )
        {
            // establish an increasing build index based on the number of seconds from a common UTC date
            timeStamp = timeStamp.ToUniversalTime( );
            var midnightUtc = new DateTime( timeStamp.Year, timeStamp.Month, timeStamp.Day, 0, 0, 0, DateTimeKind.Utc );

            // Upper 16 bits of the build index is the number of days since the common base value
            // Lower 16 bits is the number of seconds (divided by 2) since midnight (on the date of the time stamp)
            uint buildIndex = (uint)(timeStamp - CommonBaseDate).Days << 16;
            buildIndex += (ushort)((timeStamp - midnightUtc).TotalSeconds / 2);

            return buildIndex.ToString( CultureInfo.InvariantCulture );
        }

        // Fixed point in time to use as reference for a build index.
        // Build index value is a string form of the number of days since this point in time + the number of seconds
        // since midnight of that time stamp.
        private static readonly DateTime CommonBaseDate = new( 2000, 1, 1, 0, 0, 0, DateTimeKind.Utc );
    }
}
