# CSemVer.GitBuild
Automated Constrained Semantic Versioning for Git repositories

## Build Status
[![Build status](https://ci.appveyor.com/api/projects/status/nfixkakus282t06u?svg=true)](https://ci.appveyor.com/project/UbiquityDotNet/csemver-gitbuild)


## Overview
NUGET Packages use a SemVer 2.0 (see http://semver.org)

However, SemVer 2.0 doesn't consider or account for publicly available CI builds.
SemVer is only concerned with official releases. This makes CI builds producing 
versioned packages challenging. Fortunately, some one has already defined a solution
to using SemVer in a specially constrained way to ensure compatibility, while also 
allowing for automated CI builds. These new versions are called a [Constrained Semantic
Version](http://csemver.org) (CSemVer).

A CSemVer is unique for each CI build and always increments while supporting official releases.
In the real world there are often cases where there are additional builds that are distinct from
official releases and CI builds. Including Local developer builds, builds generated from a Pull 
Request (a.k.a Automated buddy build). CSemVer doesn't explicitly define any format for these cases.
So this library defines a pattern of versioning that is fully compatible with CSemVer and allows for
the additional build types in a way that retains precedence having the least surprising consequences.
In particular, local build packages have a higher precedence that CI or release versions if all other
components of the version match. This ensure that what you are building includes the dependent packages
you just built instead of the last one released publicly.

The following is a list of the version formats in descending order of precedence:

| Build Type | Format |
|------------|--------|
| Local build  | {BuildMajor}.{BuildMinor}.{BuildPatch}--ci-DEV-{UTCTIME of build in hex} |
| Pull Request | {BuildMajor}.{BuildMinor}.{BuildPatch}--ci-PRQ-{UTCTIME of PR Commit}+{COMMIT ID} |
| Official CI builds | {BuildMajor}.{BuildMinor}.{BuildPatch}--ci-REL-{UTCTIME of HEAD Commit}+{COMMIT ID} |
| Official PreRelease | {BuildMajor}.{BuildMinor}.{BuildPatch}-{PreReleaseName}[.PreReleaseNumber][.PreReleaseFix]+{COMMIT ID} |
| Official Release | {BuildMajor}.{BuildMinor}.{BuildPatch}+{COMMIT ID} |

This library provides a single package to automate the generation of these versions in an easy to use
NuGet Package.

The package creates File and Assembly Versions and defines the appropriate MsBuild properties
so the build will automatically incorporate them.
> **NOTE:**  
The automatic use of MsBuild properties requires using the new SDK attribute support for .NET projects.
Where the build auto generates the assembly info. If you are using some other means to auto generate the
assembly level versioning attributes. You can use the properties generated by this package to generate the
attributes.

File and AssemblyVersions are computed based on the CSemVer "Ordered version", which
is a 64 bit value that maps to a standard windows FILEVERSION Quad with each part
consuming 16 bits. This ensures a strong relationship between the  assembly/file versions
and the packages as well as ensures that CI builds can function properly. Furthermore, this
guarantees that each build has a different file and assembly version so that strong name
signing functions properly to enable loading different versions in the same process. 

The Major, Minor and Patch versions are only updated in the master branch at the time
of a release. This ensures the concept that SemVer versions define released products. The
version numbers used are stored in the repository in the BuildVersion.xml
