// -----------------------------------------------------------------------
// <copyright file="VersioningProjectBuildResults.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace Ubiquity.NET.Versioning.Build.Tasks.UT.Support
{
    [SuppressMessage( "StyleCop.CSharp.DocumentationRules", "SA1649:File name should match first type name", Justification = "Simple record used here" )]
    internal readonly ref struct VersioningProjectBuildResults
    {
        public VersioningProjectBuildResults( ProjectBuildResults buildResults, BuildProperties properties )
        {
            BuildResults = buildResults;
            Properties = properties;
        }

        public ProjectBuildResults BuildResults { get; }

        public BuildProperties Properties { get; }

        public void Deconstruct(out ProjectBuildResults buildResults, out BuildProperties properties)
        {
            buildResults = BuildResults;
            properties = Properties;
        }

        public void Dispose( )
        {
            BuildResults.Dispose();
        }
    }
}
