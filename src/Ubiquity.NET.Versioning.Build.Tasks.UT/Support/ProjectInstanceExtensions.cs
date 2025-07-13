// -----------------------------------------------------------------------
// <copyright file="ProjectInstanceExtensions.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Globalization;

using Microsoft.Build.Execution;
using Microsoft.Build.Utilities.ProjectCreation;

namespace Ubiquity.NET.Versioning.Build.Tasks.UT.Support
{
    internal static class ProjectInstanceExtensions
    {
        public static T? GetPropertyAs<T>( this ProjectInstance self, string name, T? defaultValue = null )
            where T : struct
        {
            var prop = self.GetProperty(name);
            return prop is null ? defaultValue : (T?)Convert.ChangeType(prop.EvaluatedValue, typeof(T), CultureInfo.InvariantCulture);
        }

        public static string? GetOptionalProperty( this ProjectInstance self, string name, string? defaultValue = null )
        {
            var prop = self.GetProperty(name);
            return prop is null ? defaultValue : prop.EvaluatedValue;
        }

        // Sadly, project creator library doesn't have support for after build state retrieval...
        // Fortunately, it is fairly easy to create an extension to handle that scenario.
        public static BuildResult Build( this ProjectInstance self, BuildOutput buildOutput, params string[] targetsToBuild )
        {
            return BuildManager.DefaultBuildManager.Build(
                                          new BuildParameters() { Loggers = [buildOutput] },
                                          new BuildRequestData(
                                              self,
                                              targetsToBuild,
                                              new HostServices(),
                                              BuildRequestDataFlags.ProvideProjectStateAfterBuild
                                              )
                                          );
        }
    }
}
