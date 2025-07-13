// -----------------------------------------------------------------------
// <copyright file="ProjectBuildResults.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

using Microsoft.Build.Utilities.ProjectCreation;

namespace Ubiquity.NET.Versioning.Build.Tasks.UT.Support
{
    [SuppressMessage( "StyleCop.CSharp.DocumentationRules", "SA1649:File name should match first type name", Justification = "Simple record used here" )]
    internal readonly ref struct ProjectBuildResults
    {
        public ProjectBuildResults( ProjectCreator creator, BuildOutput output, bool success )
        {
            Creator = creator;
            Output = output;
            Success = success;
        }

        public readonly ProjectCreator Creator { get; }

        public BuildOutput Output { get; }

        public readonly bool Success { get; }

        public readonly void Dispose( )
        {
            Output.Dispose();
        }
    }
}
