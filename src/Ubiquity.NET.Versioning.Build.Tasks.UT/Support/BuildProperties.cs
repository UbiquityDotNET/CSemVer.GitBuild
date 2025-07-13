// -----------------------------------------------------------------------
// <copyright file="BuildProperties.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;

using Microsoft.Build.Execution;

namespace Ubiquity.NET.Versioning.Build.Tasks.UT.Support
{
    /// <summary>Captures all properties for the build task</summary>
    /// <remarks>
    /// It is worth noting that the properties captured are ALL considered
    /// "OPTIONAL" here. It is up to tests to VERIFY if a property is supposed
    /// to be present or not.
    /// </remarks>
    internal readonly record struct BuildProperties
    {
        public BuildProperties( ProjectInstance inst )
        {
            ArgumentNullException.ThrowIfNull(inst);

            // Manually or from Ubiquity.NET.Versioning.Build.Tasks.props
            BuildTime = inst.GetPropertyValue(PropertyNames.BuildTime);
            CiBuildName = inst.GetOptionalProperty(PropertyNames.CiBuildName);

            // from Ubiquity.NET.Versioning.Build.Tasks.targets/GetRepositoryInfo/GetBuildIndexFromTime task
            CiBuildIndex = inst.GetOptionalProperty(PropertyNames.CiBuildIndex);

            // Either manually or from Ubiquity.NET.Versioning.Build.Tasks.targets/GetRepositoryInfo/ParseBuildVersionXml task
            BuildMajor = inst.GetPropertyAs<ushort>(PropertyNames.BuildMajor);
            BuildMinor = inst.GetPropertyAs<ushort>(PropertyNames.BuildMinor);
            BuildPatch = inst.GetPropertyAs<ushort>(PropertyNames.BuildPatch);
            PreReleaseName = inst.GetOptionalProperty(PropertyNames.PreReleaseName);
            PreReleaseNumber = inst.GetPropertyAs<ushort>(PropertyNames.PreReleaseNumber);
            PreReleaseFix = inst.GetPropertyAs<ushort>(PropertyNames.PreReleaseFix);

            // from Ubiquity.NET.Versioning.Build.Tasks.targets/GetRepositoryInfo/CreateVersionInfo task
            FullBuildNumber = inst.GetOptionalProperty(PropertyNames.FullBuildNumber);
            FileVersionMajor = inst.GetPropertyAs<ushort>(PropertyNames.FileVersionMajor);
            FileVersionMinor =inst.GetPropertyAs<ushort>(PropertyNames.FileVersionMinor);
            FileVersionBuild = inst.GetPropertyAs<ushort>(PropertyNames.FileVersionBuild);
            FileVersionRevision = inst.GetPropertyAs<ushort>(PropertyNames.FileVersionRevision);
            PackageVersion = inst.GetOptionalProperty(PropertyNames.PackageVersion);

            BuildMeta = inst.GetOptionalProperty(PropertyNames.BuildMeta);

            // from Ubiquity.NET.Versioning.Build.Tasks.targets/SetVersionDependentProperties target
            FileVersion = inst.GetOptionalProperty(PropertyNames.FileVersion);
            AssemblyVersion = inst.GetOptionalProperty(PropertyNames.AssemblyVersion);
            InformationalVersion = inst.GetOptionalProperty(PropertyNames.InformationalVersion);

            IsPullRequestBuild = inst.GetPropertyAs<bool>(EnvVarNames.IsPullRequestBuild);
            IsAutomatedBuild = inst.GetPropertyAs<bool>(EnvVarNames.IsAutomatedBuild);
            IsReleaseBuild = inst.GetPropertyAs<bool>(EnvVarNames.IsReleaseBuild);
        }

        public ushort? BuildMajor { get; }

        public ushort? BuildMinor { get; }

        public ushort? BuildPatch { get; }

        public string? PreReleaseName { get; }

        public ushort? PreReleaseNumber { get; }

        public ushort? PreReleaseFix { get; }

        public string? FullBuildNumber { get; }

        public string? PackageVersion { get; }

        public string? BuildTime { get; }

        public string? CiBuildIndex { get; }

        public string? CiBuildName { get; }

        public ushort? FileVersionMajor { get; }

        public ushort? FileVersionMinor { get; }

        public ushort? FileVersionBuild { get; }

        public ushort? FileVersionRevision { get; }

        public string? BuildMeta { get; }

        public string? FileVersion { get; }

        public string? AssemblyVersion { get; }

        public string? InformationalVersion { get; }

        public bool? IsPullRequestBuild { get; }

        public bool? IsAutomatedBuild { get; }

        public bool? IsReleaseBuild { get; }
    }
}
