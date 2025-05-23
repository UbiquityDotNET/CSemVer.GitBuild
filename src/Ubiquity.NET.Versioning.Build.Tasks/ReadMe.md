# About
The Ubiquity.NET.Versioning.Build.Tasks package provides automated support for build versioning
using a Constrained Semantic Version ([CSemVer](https://csemver.org/)).

## Overview
Officially, NUGET Packages use a SemVer 2.0 (see http://semver.org).
However, SemVer 2.0 doesn't consider or account for publicly available CI builds.
SemVer is only concerned with official releases. This makes CI builds producing 
versioned packages challenging. Fortunately, someone has already defined a solution
to using SemVer in a specially constrained way to ensure compatibility, while also 
allowing for automated CI builds. These new versions are called a [Constrained Semantic
Version](http://csemver.org) (CSemVer).

A CSemVer is unique for each CI build and always increments while supporting official releases.
In the real world there are often cases where there are additional builds that are distinct from
official releases and CI builds. Including Local developer builds, builds generated from a Pull 
Request (a.k.a Automated buddy build). CSemVer doesn't explicitly define any format for these
cases. So this library defines a pattern of versioning that is fully compatible with CSemVer and
allows for the additional build types in a way that retains precedence having the least
surprising consequences. In particular, local build packages have a higher precedence than CI or
release versions if all other components of the version match. This ensures that what you are
building includes the dependent packages you just built instead of the last one released
publicly.

The following is a list of the version formats in descending order of precedence:

| Build Type | Format |
|------------|--------|
| Local build  | `{BuildMajor}.{BuildMinor}.{BuildPatch}--ci-{UTCTIME of build }-ZZZ` |
| Pull Request | `{BuildMajor}.{BuildMinor}.{BuildPatch}--ci-{UTCTIME of PR Commit}-PRQ+{COMMIT ID}` |
| Official CI builds | `{BuildMajor}.{BuildMinor}.{BuildPatch}--ci-{UTCTIME of HEAD Commit}-BLD+{COMMIT ID}` |
| Official PreRelease | `{BuildMajor}.{BuildMinor}.{BuildPatch}-{PreReleaseName}[.PreReleaseNumber][.PreReleaseFix]+{COMMIT ID}` |
| Official Release | `{BuildMajor}.{BuildMinor}.{BuildPatch}+{COMMIT ID}` |

This package provides a single package to automate the generation of these versions in an easy
to use NuGet Package.

The package creates File and Assembly Versions and defines the appropriate MsBuild properties
so the build will automatically incorporate them.
> **NOTE:**  
The automatic use of MsBuild properties requires using the new SDK attribute support for .NET
projects. Where the build auto generates the assembly info. If you are using some other means to
auto generate the assembly level versioning attributes. You can use the properties generated by
this package to generate the attributes.

File and AssemblyVersions are computed based on the CSemVer "Ordered version", which
is a 64 bit value that maps to a standard windows FILEVERSION Quad with each part
consuming 16 bits. This ensures a strong relationship between the  assembly/file versions
and the packages as well as ensures that CI builds can function properly. Furthermore, this
guarantees that each build has a different file and assembly version so that strong name
signing functions properly to enable loading different versions in the same process.

The Major, Minor and Patch versions are only updated in the primary branch at the time
of a release. This ensures the concept that SemVer versions define released products. The
version numbers used are stored in the repository in the BuildVersion.xml

## Properties used to determine the version
CSemVer.Build uses MSBuild properties to determine the final version number.

|Name               |Default Value                                                 | Description|
|-------------------|--------------------------------------------------------------|------------|
| BuildMajor        | Read from BuildVersion.xml                                   | Major portion of the build number |
| BuildMinor        | Read from BuildVersion.xml                                   | Minor portion of the build number |
| BuildPatch        | Read from BuildVersion.xml                                   | Patch portion of the build number |
| PreReleaseName    | `<Undefined>` or value read from BuildVersion.xml if present | PreRelease Name of the CSemVer |
| PreReleaseNumber  | `<Undefined>` or value read from BuildVersion.xml if present | PreRelease Number of the CSemVer |
| PreReleaseFix     | `<Undefined>` or value read from BuildVersion.xml if present | PreRelease Fix of the CSemVer |
| BuildMeta         | `<undefined>`                                                | Build meta for the version
| CiBuildIndex      | ISO 8601 formated UTC time-stamp for the build               | Provides a unique build to build value guaranteed to increase with each build
| CiBuildName       | `<see notes>`                                                | CSemVer CI name

### CiBuildName
Unless explicitly provided, the CiBuildName is determined by a set of properties that indicate
the nature of the build. The properties used (in evaluation order) are:

|Name               |Default Value  |CiBuildName    | Description|
|-------------------|---------------|---------------|------------|
|IsPullRequestBuild | `<Undefined>` |`PRQ` if true  | Used to indicate if the build is from a pull request |
|IsAutomatedBuild   | `<Undefined>` |`BLD` if true  | Used to indicate if the build is an automated build |
|IsReleaseBuild     | `<Undefined>` |`ZZZ` if !true | Used to indicate if the build is an official release build |

These three values are determined by the automated build in some form. These are either explicit
variables set for the build definition or determined on the fly based on values set by the build.
Commonly a `directory.build.props` for a repository will specify these. The following is an
example for setting them based on an AppVeyor build in the `Directory.Build.props` file:

```xml
<PropertyGroup>
    <!-- If running in APPVEYOR it is an automated build -->
    <IsAutomatedBuild Condition="'$(IsAutomatedBuild)'=='' AND '$(APPVEYOR)'!=''">true</IsAutomatedBuild>
    <IsAutomatedBuild Condition="'$(IsAutomatedBuild)'==''">false</IsAutomatedBuild>

    <!-- If it has a PR number associated it is a PR build -->
    <IsPullRequestBuild Condition="'$(IsPullRequestBuild)'=='' AND '$(APPVEYOR_PULL_REQUEST_NUMBER)'!=''">true</IsPullRequestBuild>
    <IsPullRequestBuild Condition="'$(IsPullRequestBuild)'==''">false</IsPullRequestBuild>

    <!-- Tags applied without a PR are release builds -->
    <IsReleaseBuild Condition="'$(IsReleaseBuild)'=='' AND '$(APPVEYOR_REPO_TAG)'=='true' AND '$(APPVEYOR_PULL_REQUEST_NUMBER)'==''">true</IsReleaseBuild>
    <IsReleaseBuild Condition="'$(IsReleaseBuild)'==''">false</IsReleaseBuild>
</PropertyGroup>
```

## BuildVersion.xml
If the MSBuild property `BuildMajor` is not set, then the base build version is read from the
repository file specified in the BuildVersion.xml, typically this is located at the root of a
repository so that any child projects share the same versioning information. The location of
the file is specified by an MSBuild property `BuildVersionXml`. The contents of the file are
fairly simple and only requires a single `BuildVersionData` element with a set of attributes.
The available attributes are:

|Name               |Description|
|-------------------|-----------|
| BuildMajor        | Major portion of the build number |
| BuildMinor        | Minor portion of the build number |
| BuildPatch        | Patch portion of the build number |
| PreReleaseName    | PreRelease Name of the CSemVer |
| PreReleaseNumber  | PreRelease Number of the CSemVer |
| PreReleaseFix     | PreRelease Fix of the CSemVer |

Only the Major, minor and Patch numbers are required.
