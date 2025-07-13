// -----------------------------------------------------------------------
// <copyright file="FileVersionQuad.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Numerics;

namespace Ubiquity.NET.Versioning.Build.Tasks.UT.Support
{
    /// <summary>File version QUAD used for testing</summary>
    /// <para>This is a clone (and subset) of the code in the `Ubiquity.NET.Versioning`. Cloning this for the tests, means
    /// they are independent of the versioning library, which makes that library a candidate for isolation in
    /// a different repo/build that depends on the TASK package for generation of versions</para>
    internal readonly record struct FileVersionQuad( ushort Major, ushort Minor, ushort Build, ushort Revision )
        : IComparable<FileVersionQuad>
        , IComparisonOperators<FileVersionQuad, FileVersionQuad, bool>
    {
        /// <summary>Gets a value indicating whether this version is a CI build</summary>
        public bool IsCiBuild => (Revision & 1) == 1; // 1 indicates a CI build with a higher sort order.

        /// <inheritdoc/>
        public int CompareTo( FileVersionQuad other )
        {
            return ToUInt64().CompareTo(other.ToUInt64());
        }

        /// <inheritdoc/>
        public static bool operator <( FileVersionQuad left, FileVersionQuad right ) => left.CompareTo( right ) < 0;

        /// <inheritdoc/>
        public static bool operator <=( FileVersionQuad left, FileVersionQuad right ) => left.CompareTo( right ) <= 0;

        /// <inheritdoc/>
        public static bool operator >( FileVersionQuad left, FileVersionQuad right ) => left.CompareTo( right ) > 0;

        /// <inheritdoc/>
        public static bool operator >=( FileVersionQuad left, FileVersionQuad right ) => left.CompareTo( right ) >= 0;

        /// <summary>Gets the UInt64 representation of the version</summary>
        /// <returns>UInt64 version of the version</returns>
        /// <remarks>
        /// The value is independent of system "endianness" and consists of the multiple parts
        /// corresponding to four 16 bit wide fields. In order of significance those values are:
        /// Major, Minor, Build, Revision. Thus, while the actual byte ordering of the data making
        /// up an integral value will depend on the system architecture, it's VALUE does not.
        /// </remarks>
        public ulong ToUInt64( )
        {
            return ((ulong)Major << 48)
                 + ((ulong)Minor << 32)
                 + ((ulong)Build << 16)
                 + Revision;
        }

        /// <summary>Gets the CSemVer defined ordered version number for this FileVersion quad</summary>
        /// <param name="isCIBuild">Indicates if the File Version is for a CI build or not</param>
        /// <returns>Ordered version number of this file version</returns>
        /// <remarks>
        /// The File version form of a CSemVer value includes a bit to indicate if the version is a CI build
        /// or not. This condition is important for ordering
        /// </remarks>
        public ulong ToOrderedVersion(out bool isCIBuild)
        {
            isCIBuild = IsCiBuild;
            return ToUInt64() >> 1;
        }

        /// <inheritdoc cref="ToOrderedVersion(out bool)"/>
        public ulong ToOrderedVersion()
        {
            return ToOrderedVersion(out _);
        }

        /// <summary>Converts this instance to a <see cref="Version"/></summary>
        /// <returns>Values of this instance as a <see cref="Version"/></returns>
        /// <remarks>
        /// <note type="important">
        /// This conversion INCLUDES the CI build information bit and thus ordering
        /// of the result is NOT correct. (In File version Quads an ODD revision
        /// indicates a CI build, but any such build has an ordering that is LESS
        /// then any without it and therefore does NOT match the numeric ordering)
        /// </note>
        /// </remarks>
        public Version ToVersion( )
        {
            return new Version( Major, Minor, Build, Revision );
        }

        /// <summary>Converts this instance to a dotted string form</summary>
        /// <returns>Formatted string</returns>
        public override string ToString( ) => $"{Major}.{Minor}.{Build}.{Revision}";

        /// <summary>Converts a <see cref="Version"/> value to a <see cref="FileVersionQuad"/> if possible</summary>
        /// <param name="v">Version to convert</param>
        /// <returns>Resulting <see cref="FileVersionQuad"/></returns>
        /// <exception cref="ArgumentOutOfRangeException">At least one of the members of <paramref name="v"/> is out of range of a <see cref="ushort"/></exception>
        public static FileVersionQuad From( Version v)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(v.Major, ushort.MaxValue);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(v.Minor, ushort.MaxValue);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(v.Build, ushort.MaxValue);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(v.Revision, ushort.MaxValue);

            return new((ushort)v.Major, (ushort)v.Minor, (ushort)v.Build, (ushort)v.Revision);
        }

        /// <summary>Converts a version integral value into a <see cref="FileVersionQuad"/></summary>
        /// <param name="value">Value to convert</param>
        /// <returns> <see cref="FileVersionQuad"/> variant of <paramref name="value"/></returns>
        public static FileVersionQuad From( ulong value )
        {
            ushort revision = (ushort)(value % 65536);
            ulong rem = (value - revision) / 65536;

            ushort build = (ushort)(rem % 65536);
            rem = (rem - build) / 65536;

            ushort minor = (ushort)(rem % 65536);
            rem = (rem - minor) / 65536;

            ushort major = (ushort)(rem % 65536);
            return new( major, minor, build, revision );
        }
    }
}
