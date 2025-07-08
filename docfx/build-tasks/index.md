# About
The Ubiquity.NET.Versioning.Build.Tasks package provides automated support for build versioning
using a Constrained Semantic Version ([CSemVer](https://csemver.org/)).

>[!WARNING]
> As a [Breaking change in .NET SDK 8](https://learn.microsoft.com/en-us/dotnet/core/compatibility/sdk/8.0/source-link)
> is now setting the build meta data for the `InformationalVersion` property automatically and
> without user consent. (A Highly controversial choice that was more easily handled via an
> OPT-IN pattern) Unfortunately, this was set ON by default and made into an 'OPT-OUT' scenario.
> This library will honor such a setting and does not alter/interfere with it in any way.
> (Though the results can, unfortunately, produce surprising behavior as it is not well
> documented).
>
> If you wish to disable this behavior you can set an MSBUILD property to OPT-OUT as follows:  
> `<IncludeSourceRevisionInInformationalVersion>false</IncludeSourceRevisionInInformationalVersion>`  
>  
> This choice of ignoring the additional data is considered to have the least impact on those
> who are aware of the change and those who use this library to set an explicit build meta data
> string. (Principle of least surprise for what this library can control).
>
> The default behavior added in this breaking change is to use the Repository ID (usually a GIT
> commit hash [FULL SHA form!]) as the build metadata. This is appended with a leading `+` if
> one isn't already in the `InformationalVersion` property. If build metadata is already
> included (Like from use of this task) the id is appended using a `.` instead. So it is ALWAYS
> appended unless the project has opted out of this behavior by setting the property as
> previously described.
>
> Thus, it is ***strongly recommended*** that projects using this package OPT-OUT
> of the new behavior.


## Overview
Officially, SemVer 2.0 doesn't consider or account for publicly available CI builds.
SemVer is only concerned with official releases. This makes CI builds producing 
versioned packages challenging. Fortunately, someone has already defined a solution
to using SemVer in a specially constrained way to ensure compatibility, while also 
allowing for automated CI builds. These new versions are called a [Constrained Semantic
Version](http://csemver.org) (CSemVer and CSemVer-CI).

A CSemVer is unique for each build and always increments while still supporting official
releases. However, in the real world, there are often cases where there are additional builds
that are distinct from official releases and formal CI builds. These include local developer
builds and builds generated from a Pull Request (a.k.a Automated buddy build). Neither SemVer,
nor CSemVer explicitly define any format for these cases. So this library defines a pattern of
versioning that is fully compatible with CSemVer[-CI] and allows for the additional build
types in a way that retains precedence having the least surprising consequences. In particular,
local build versions have a higher precedence than automated or release versions if all other
components of the version match<sup>[1](#footnote_1)</sup>. This ensures that what you are building includes
the dependent components you just built instead of the last one released publicly.

### Understanding CI builds
Understanding CI builds is important and an complete [article](xref:understanding-ci-builds)
is available on the subject. An important point to understand here is that a CI build
version number is a POST-RELEASE version that is ordered AFTER the release it is based
on AND BEFORE the next release. That is a CI build is ordered BETWEEN releases. Though
it carries no guarantees with it that about what the final release version will be.

### supported formats
This library supports a constrained use of CSemVer (essentially a constrained version
of a constrained version!). This prescribes a particular way to use CSemVer to
indicate the many forms of a CI build without violating the ordering rules of CSemVer
or the SemVer it is based on.

The following is a list of the version formats in descending order of precedence:

| Build Type | Format |
|------------|--------|
| Local build  | `{BuildMajor}.{BuildMinor}.{BuildPatch}.ci.{CiBuildIndex}.ZZZ+{BuildMeta}` |
| Pull Request | `{BuildMajor}.{BuildMinor}.{BuildPatch}.ci.{CiBuildIndex}.PRQ+{BuildMeta}` |
| Official CI builds | `{BuildMajor}.{BuildMinor}.{BuildPatch}.ci-{CiBuildIndex}.BLD+{BuildMeta}` |
| Official PreRelease | `{BuildMajor}.{BuildMinor}.{BuildPatch}-{PreReleaseName}[.PreReleaseNumber][.PreReleaseFix]+{BuildMeta}` |
| Official Release | `{BuildMajor}.{BuildMinor}.{BuildPatch}+{BuildMeta}` |

That is the, CSemVer-CI `BuildName` (`ZZZ`, `PRQ`, `BLD`) is specifically chosen to
ensure the ordering matches the expected behavior for a build while still making sense
for most uses.

## What's included in this project?
This project provides an MSBUILD task to automate the generation of these versions in
an easy to use NuGet Package.

The package creates File and Assembly Versions and defines the appropriate MsBuild
properties so the build will automatically incorporate them.
> **NOTE:**  
> The automatic use of MsBuild properties requires using the new SDK attribute support
> for .NET projects. Where the build auto generates the assembly info. If you are
> using some other means to auto generate the assembly level versioning attributes.
> You can use the properties generated by this package to generate the attributes.

File and AssemblyVersions are computed based on the CSemVer "Ordered version", which
is a 64 bit value that maps to a standard windows FILE VERSION Quad with each part
consuming 16 bits. This ensures a strong relationship between the assembly/file
versions and the packages as well as ensures that CI builds can function properly.
Furthermore, this guarantees that each build has a different file and assembly version
so that strong name signing functions properly to enable loading different versions
in the same process.

>[!IMPORTANT]
> A file version quad representation of a CSemVer does NOT carry with it the CI
> information nor any build metadata. It only contains a single bit to indicate a
> Release vs. a CI build. In fact, the official CSemVer specs are silent on the use
> of this bit though the "playground" does indicate an ODD numbered revision is
> reserved as a CI build.

The Major, Minor and Patch versions are only updated in the primary branch at the
time of a release. This ensures the concept that SemVer versions define released
products. The version numbers used are stored in the repository in the
BuildVersion.xml

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
| GeneratedVersionInfoHeader | `<undefined>` | Full path of the header file to generate [No header generated if not set] [Only applies to a VCXPROJ file ] |
| GenerateAssemblyInfo | `<undefined>` | If set, creates `$(IntermediateOutputPath)AssemblyVersionInfo.g.cs` and includes it for compilation. Generally, only used for legacy CSPROJ files as new SDK projects handle this automatically now |

### CiBuildIndex
Unless specified or a release build [$(IsReleaseBuild) is true] this indicates the
CSemVer CI Build index for a build. For a CI build this will default to the ISO-8601
formatted time stamp of the build. Consumers can specify any value desired as an
override but should ensure the value is ALWAYS increasing according to the rules of a
CSemVer-CI. Generating duplicates for the same build is an error condition and can
lead to consumer confusion. Usually, if set externally, this is set to the time stamp
of the head commit of a repository so that any automated builds are consistent with
the build number. (For PullRequest buddy builds this is usually left as a build time
stamp)

### CiBuildName
Unless explicitly provided, the CiBuildName is determined by a set of properties that
indicate the nature of the build. The properties used (in evaluation order) are:

|Name               |Default Value  |CiBuildName    | Description|
|:-----------------:|:-------------:|:-------------:|:-----------|
|IsPullRequestBuild | `<Undefined>` |`PRQ` if true  | Used to indicate if the build is from a pull request |
|IsAutomatedBuild   | `<Undefined>` |`BLD` if true  | Used to indicate if the build is an automated build |
|IsReleaseBuild     | `<Undefined>` |`ZZZ` if !true | Used to indicate if the build is an official release build |

These three values are determined by the automated build in some form. These are
either explicit variables set for the build definition or determined on the fly based
on values set by the build. Commonly a `directory.build.props` for a repository will
specify these. The following is an example for setting them based on an AppVeyor build
in the `Directory.Build.props` file:

```XML
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

Commonly a build is scripted in a build service such as GitHub Actions or AppVeyor.
Such scripting should set these values based on conditions from the back end system.
An example of this for common systems like GitHub and AppVeyor could look like this:

``` PowerShell
enum BuildKind
{
    LocalBuild
    PullRequestBuild
    CiBuild
    ReleaseBuild
}

function Get-CurrentBuildKind
{
<#
.SYNOPSIS
    Determines the kind of build for the current environment

.DESCRIPTION
    This function retrieves environment values set by various automated builds
    to determine the kind of build the environment is for. The return is one of
    the [BuildKind] enumeration values:

    | Name             | Description |
    |------------------|-------------|
    | LocalBuild       | This is a local developer build (e.g. not an automated build)
    | PullRequestBuild | This is a build from a PullRequest with untrusted changes, so build should limit the steps appropriately |
    | CiBuild          | This build is from a Continuous Integration (CI) process, usually after a PR is accepted and merged to the branch |
    | ReleaseBuild     | This is an official release build, the output is ready for publication (Automated builds may use this to automatically publish) |
#>
    [OutputType([BuildKind])]
    param()

    $currentBuildKind = [BuildKind]::LocalBuild

    # IsAutomatedBuild is the top level gate (e.g. if it is false, all the others must be false)
    # This supports identification of APPVEYOR or GitHub explicitly but also supports the common
    # `CI` environment variable. Additional build back-ends that don't set the env var therefore,
    # would need special handling here.
    $isAutomatedBuild = [System.Convert]::ToBoolean($env:CI) `
                        -or [System.Convert]::ToBoolean($env:APPVEYOR) `
                        -or [System.Convert]::ToBoolean($env:GITHUB_ACTIONS)

    if( $isAutomatedBuild )
    {
        # PR and release builds have externally detected indicators that are tested
        # below, so default to a CiBuild (e.g. not a PR, And not a RELEASE)
        $currentBuildKind = [BuildKind]::CiBuild

        # Based on back-end type - determine if this is a release or CI build
        # The assumption here is that a TAG is pushed to the repo for releases
        # and therefore that is what distinguishes a release build. Other conditions
        # would need to use other criteria to determine a PR buddy build, CI build
        # and release build.

        # IsPullRequestBuild indicates an automated buddy build and should not be trusted
        $isPullRequestBuild = $env:GITHUB_BASE_REF -or $env:APPVEYOR_PULL_REQUEST_NUMBER

        if($isPullRequestBuild)
        {
            $currentBuildKind = [BuildKind]::PullRequestBuild
        }
        else
        {
            if([System.Convert]::ToBoolean($env:APPVEYOR))
            {
                $isReleaseBuild = $env:APPVEYOR_REPO_TAG
            }
            elseif([System.Convert]::ToBoolean($env:GITHUB_ACTIONS))
            {
                $isReleaseBuild = $env:GITHUB_REF -like 'refs/tags/*'
            }

            if($isReleaseBuild)
            {
                $currentBuildKind = [BuildKind]::ReleaseBuild
            }
        }
    }

    return $currentBuildKind
}

$currentBuildKind = Get-CurrentBuildKind

# set/reset legacy environment vars for non-script tools
$env:IsAutomatedBuild = $currentBuildKind -ne [BuildKind]::LocalBuild
$env:IsPullRequestBuild = $currentBuildKind -eq [BuildKind]::PullRequestBuild
$env:IsReleaseBuild = $currentBuildKind -eq [BuildKind]::ReleaseBuild
```

## BuildVersion.xml
If the MSBuild property `BuildMajor` is not set, then the base build version is read
from the repository file specified in the BuildVersion.xml, typically this is located
at the root of a repository so that any child projects share the same versioning
information. The location of the file is specified by an MSBuild property
`BuildVersionXml`. The contents of the file are fairly simple and only requires a
single `BuildVersionData` element with a set of attributes. The available attributes
are:

|Name              |Description|
|------------------|-----------|
| BuildMajor       | Major portion of the build number |
| BuildMinor       | Minor portion of the build number |
| BuildPatch       | Patch portion of the build number |
| PreReleaseName   | PreRelease Name of the CSemVer |
| PreReleaseNumber | PreRelease Number of the CSemVer |
| PreReleaseFix    | PreRelease Fix of the CSemVer |

Only the Major, minor and Patch numbers are required.
Example:  
```XML
<BuildVersionData
    BuildMajor = "5"
    BuildMinor = "0"
    BuildPatch = "0"
    PreReleaseName = "alpha"
/>
```

## Generated Properties
|Name                |Description|
|--------------------|-----------|
| BuildTime          | Set to the current time (UTC ISO-8601 format) if not already set by build tooling) |
| IsAutomatedBuild   | Automated build system value to indicate this is an automated build |
| IsPullRequestBuild | Automated build system value to indicate this is a build for an untrusted PR |
| IsReleaseBuild     | Automated build system value to indicate this is an official release build (No CI information) |
| CiBuildName        | If not set externally, this is set based on the kind of build |
| CiBuildIndex       | If not set externally, this is set based on the $(BuildTime) property by parsing the ISO-8601 string and computing an index from that |
| BuildMajor         | Major portion of the build; If not set externally, this is set based on the information in the $(BuildVersionXml) file |
| BuildMinor         | Minor portion of the build; If not set externally, this is set based on the information in the $(BuildVersionXml) file |
| BuildPatch         | Patch portion of the build; If not set externally, this is set based on the information in the $(BuildVersionXml) file |
| PreReleaseName     | PreRelease Name for the build [Optional]; If not set externally, this is set based on the information in the $(BuildVersionXml) file, which may not include a value for this |
| PreReleaseNumber   | PreRelease Number for the build [Optional]; If not set externally, this is set based on the information in the $(BuildVersionXml) file, which may not include a value for this |
| PreReleaseFix      | PreRelease Fix for the build [Optional]; If not set externally, this is set based on the information in the $(BuildVersionXml) file, which may not include a value for this |
| FullBuildNumber    | String form of the full CSemVer value for a build |
| ShortBuildNumber   | Short form of the CSemVer for use with legacy NuGet clients (Modern clients support the full name) |
| FileVersionMajor   | Major portion of the FileVersion number (Used for vcxproj files to generate the version header) |
| FileVersionMinor   | Minor portion of the FileVersion number (Used for vcxproj files to generate the version header) |
| FileVersionBuild   | Build portion of the FileVersion number (Used for vcxproj files to generate the version header) |
| FileVersionRevision | Revision portion of the FileVersion number (Used for vcxproj files to generate the version header) |

### BuildTime
Ordinarily this is set for an entire solution by build scripting to ensure that all
components using this build task report the same version number. If it is not set the
current time at the moment of property evaluation for a project is used. This will
result in a distinct CI version for each project in a solution. Whether, that is
desired or not is left for the consumer. If it is not desired, then a centralized
setting as a build property or environment variable is warranted.

## Automated build flags
`IsAutomatedBuild`, `IsPullRequestBuild`, and `IsReleaseBuild` are normally set by an
automated build script/action based on the build environment used and aid in
determining the CI build name as previously described in [CiBuildName](#cibuildname).

## CiBuildName
If not explicitly set this is determined by the automated build flags as described in
the [CiBuildName](#cibuildname) section of this document.

## Detected Error Conditions

### Targets file
|Code    |Description|
|--------|-----------|
| CSM001 | BuildMajor is a required property, either set it as a global or in the build version XML |
| CSM002 | BuildMinor is a required property, either set it as a global or in the build version XML |
| CSM003 | BuildPatch is a required property, either set it as a global or in the build version XML |
| CSM004 | FileVersion property not provided AND FileVersionMajor property not found to create it from |
| CSM005 | FileVersion property not provided AND FileVersionMinor property not found to create it from |
| CSM006 | FileVersion property not provided AND FileVersionBuild property not found to create it from |
| CSM007 | FileVersion property not provided AND FileVersionRevision property not found to create it from |

### CreateVersionInfo Task
|Code    |Description|
|--------|-----------|
| CSM100 | BuildMajor value must be in range [0-99999] |
| CSM101 | BuildMinor value must be in range [0-49999] |
| CSM102 | BuildPatch value must be in range [0-9999] |
| CSM103 | PreReleaseName is unknown |
| CSM104 | PreReleaseNumber value must be in range [0-99] |
| CSM105 | PreReleaseFix value must be in range [0-99] |
| CSM106<sup>[2](#footnote_2)</sup> | If CiBuildIndex is set then CiBuildName must also be set; If CiBuildIndex is NOT set then CiBuildName must not be set. |
| CSM107 | CiBuildIndex does not match syntax defined by CSemVer |
| CSM108 | CiBuildName does not match syntax defined by CSemVer |

### ParseBuildVersionXml Task
|Code    |Description|
|--------|-----------|
| CSM200 | BuildVersionXml is required and must not be all whitespace |
| CSM201 | Specified BuildVersionXml does not exist `$(BuildVersionXml)`|
| CSM202 | BuildVersionData element does not exist in `$(BuildVersionXml)`|
| CSM203 | [Warning] Unexpected attribute on BuildVersionData Element |
| CSM204 | XML format of file specified by `$(BuildVersionXml)' is invalid |

----
<sup><a id="footnote_1">1</a></sup> CSemVer-CI uses a latest build format and
therefore all version numbers for a CI build, including local builds use a `Patch+1`
pattern as defined by CSemVer-CI. This ensures that all forms of CI builds have a
higher precedence than any release they are based on. To clarify the understanding
of that, a CI build contains everything in a given release and then some more. How
much more, or what release that will eventually become is intentionally undefined.
This allows selection of the final release version based on the contents of the actual
changes. CI builds are understood unstable and subject to complete changes or even
abandoned lines of thought.


<sup><a id="footnote_2">2</a></sup> CSM106 is essentially an internal sanity test. The
props/targets files ensure that `$(CiBuildIndex)` and `$(CiBuildName)` have a value
unless $(IsReleaseBuild) is set. In that case the targets file will force them to
empty. So, there's no way to test for or hit this condition without completely
replacing/bypassing the props/targets files for the task. Which is obviously, an
unsupported scenario :grin:.
