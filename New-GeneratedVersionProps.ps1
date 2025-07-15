using module "PSModules/CommonBuild/CommonBuild.psd1"
using module "PSModules/RepoBuild/RepoBuild.psd1"

<#
.SYNOPSIS
    Script to Create the generateVersion.props file used in this repo build

.PARAMETER Configuration
    This sets the build configuration to use, default is "Release" though for inner loop development this
    may be set to "Debug".

.PARAMETER ForceClean
    Forces a complete clean (Recursive delete of the build output)

.DESCRIPTION
    This script is used by the local and automated builds to create the versioning information for the assemblies
    in this repository. The assembly build CANNOT reference itself to get the versioning (only consumers of the
    OUTPUT of this repo can). Thus, this will generate the information using the same algorithms, at least in theory.
    Unit tests will VERIFY that the actual task assembly matches the versioning it would produce itself. So, the
    "duality" of code paths is still verified.
#>
[cmdletbinding()]
Param(
    [hashtable]$buildInfo,
    [string]$Configuration='Release',
    [switch]$ForceClean
)

function ConvertTo-BuildIndex
{
<#
.SYNOPSIS
    Converts a TimeStamp into a build index

.DESCRIPTION
    The algorithm used is the same as the package published. The resulting index is a 32bit value that
    is a combination of the number of days since a fixed point (Upper 16 bits) and the number of seconds since
    midnight (on the day of the input time stamp) divided by 2 (Lower 16 bits)
#>
    param(
        [Parameter(Mandatory=$true, ValueFromPipeLine)]
        [DateTime]$timeStamp
    )

    # This is VERY EXPLICIT on type conversions and truncation as there's a difference in the implicit conversion
    # between the C# for the tasks and library vs. PowerShell.
    # PowerShell:
    #> [UInt16]1.5
    # 2 <== Rounded UP!
    # But...
    # C#
    #> (ushort)1.5
    # 1 <== Truncated!
    # This needs to match the C# exactly so explicit behavior is used to unify the variances

    $commonBaseDate = [DateTime]::new(2000, 1, 1, 0, 0, 0, [DateTimeKind]::Utc)

    $timeStamp = $timeStamp.ToUniversalTime()
    $midnightTodayUtc = [DateTime]::new($timeStamp.Year, $timeStamp.Month, $timeStamp.Day, 0, 0, 0, [DateTimeKind]::Utc)

    $buildNumber = ([Uint32]($timeStamp - $commonBaseDate).Days) -shl 16
    $buildNumber += [UInt16]([Math]::Truncate(($timeStamp - $midnightTodayUtc).TotalSeconds / 2))

    return $buildNumber
}

class PreReleaseVersion
{
    [ValidateSet('alpha', 'beta', 'delta', 'epsilon', 'gamma', 'kappa', 'prerelease', 'rc')]
    [string] $Name;

    [ValidateRange(-1,7)]
    [int] $Index;

    [ValidateRange(0,99)]
    [int] $Number;

    [ValidateRange(0,99)]
    [int] $Fix;

    PreReleaseVersion([hashtable]$buildVersionXmlData)
    {
        $preRelName = $buildVersionXmlData['PreReleaseName']

        if( ![string]::IsNullOrWhiteSpace( $preRelName ) )
        {
            $this.Index = [PreReleaseVersion]::GetPrerelIndex($preRelName)
            if($this.Index -ge 0)
            {
                $this.Name = [PreReleaseVersion]::PreReleaseNames[$this.Index]
            }

            $this.Number = $buildVersionXmlData['PreReleaseNumber'];
            $this.Fix = $buildVersionXmlData['PreReleaseFix'];
        }
        else
        {
            $this.Index = -1;
        }
    }

    [string] ToString($alwaysIncludeZero = $false)
    {
        $hasPreRel = $this.Index -ge 0

        $bldr = [System.Text.StringBuilder]::new()
        if($hasPreRel)
        {
            $bldr.Append('-').Append( $this.Name)
            $delimFormat = '.{0}'
            if(($this.Number -gt 0 -or $alwaysIncludeZero))
            {
                $bldr.AppendFormat($delimFormat, $this.Number)
                if(($this.Fix -gt 0 -or $alwaysIncludeZero))
                {
                    $bldr.AppendFormat($delimFormat, $this.Fix)
                }
            }
        }

        return $bldr.ToString()
    }

    hidden static [string[]] $PreReleaseNames = @('alpha', 'beta', 'delta', 'epsilon', 'gamma', 'kappa', 'prerelease', 'rc' );

    hidden static [int] GetPrerelIndex([string] $preRelName)
    {
        $preRelIndex = -1
        if(![string]::IsNullOrWhiteSpace($preRelName))
        {
            $preRelIndex = [PreReleaseVersion]::PreReleaseNames |
                         ForEach-Object {$index=0} {@{Name = $_; Index = $index++}} |
                         Where-Object {$_['Name'] -ieq $preRelName} |
                         ForEach-Object {$_['Index']} |
                         Select-Object -First 1
        }

        return $preRelIndex
    }

}

class CSemVer
{
    [ValidateRange(0,99999)]
    [int] $Major;

    [ValidateRange(0,49999)]
    [int] $Minor;

    [ValidateRange(0,9999)]
    [int] $Patch;

    [ValidateLength(0,20)]
    [string] $BuildMetadata;

    [ValidatePattern('\A[a-z0-9-]+\Z')]
    [string] $CiBuildIndex;

    [ValidatePattern('\A[a-z0-9-]+\Z')]
    [string] $CiBuildName;

    [ulong] $OrderedVersion;

    [Version] $FileVersion;

    [PreReleaseVersion] $PreReleaseVersion;

    CSemVer([hashtable]$buildVersionData)
    {
        $this.Major = $buildVersionData['BuildMajor']
        $this.Minor = $buildVersionData['BuildMinor']
        $this.Patch = $buildVersionData['BuildPatch']
        if($buildVersionData['PreReleaseName'])
        {
            $this.PreReleaseVersion = [PreReleaseVersion]::new($buildVersionData)
            if(!$this.PreReleaseVersion)
            {
                throw 'Internal ERROR: PreReleaseVersion version is NULL!'
            }
        }

        $this.BuildMetadata = $buildVersionData['BuildMetadata']

        if( ![string]::IsNullOrEmpty( $buildVersionData["CiBuildIndex"] ) -and ![string]::IsNullOrEmpty( $buildVersionData["CiBuildIndex"] ) )
        {
            $this.CiBuildName = $buildVersionData['CiBuildName'];
            $this.CiBuildIndex = $buildVersionData['CiBuildIndex'];
        }


        if( (![string]::IsNullOrEmpty( $this.CiBuildName )) -and [string]::IsNullOrEmpty( $this.CiBuildIndex ) )
        {
            throw 'CiBuildIndex is required if CiBuildName is provided';
        }

        if( (![string]::IsNullOrEmpty( $this.CiBuildIndex )) -and [string]::IsNullOrEmpty( $this.CiBuildName ) )
        {
            throw 'CiBuildName is required if CiBuildIndex is provided';
        }

        # For a CI build the OrderedVersion value is that of the BASE build
        $this.OrderedVersion = [CSemVer]::GetOrderedVersion($this.Major, $this.Minor, $this.Patch, $this.PreReleaseVersion)
        $fileVer64 = $this.OrderedVersion -shl 1

        # If this is a CI build include that in the file version
        # AND increment the ordered version so that it is patch+1
        if($this.CiBuildIndex -and $this.CiBuildName)
        {
            $this.OrderedVersion = [CSemVer]::MakePatchPlus1($this.OrderedVersion)
            $this.UpdateFromOrderedVersion($this.OrderedVersion)
            $fileVer64 += 1;
        }

        $this.FileVersion = [CSemVer]::ConvertToVersion($fileVer64)
    }

    hidden UpdateFromOrderedVersion([long]$orderedVersion)
    {
        [ulong] $MulNum = 100;
        [ulong] $MulName = $MulNum * 100;
        [ulong] $MulPatch = ($MulName * 8) + 1;
        [ulong] $MulMinor = $MulPatch * 10000;
        [ulong] $MulMajor = $MulMinor * 50000;

        # This effectively reverses the math used in computing the ordered version.
        $accumulator = [UInt64]$orderedVersion;
        $preRelPart = $accumulator % $MulPatch;

        # skipping pre-release info as it is used AS-IS
        if($preRelPart -eq 0)
        {
            $accumulator -= $MulPatch;
        }

        $this.Major = [Int32]($accumulator / $MulMajor);
        $accumulator %= $MulMajor;

        $this.Minor = [Int32]($accumulator / $MulMinor);
        $accumulator %= $MulMinor;

        $this.Patch = [Int32]($accumulator / $MulPatch);
    }

    [string] ToString([bool] $includeMetadata)
    {
        $isCiBuild = $this.CiBuildIndex -and $this.CiBuildName
        $bldr = [System.Text.StringBuilder]::new()
        $bldr.AppendFormat('{0}.{1}.{2}', $this.Major, $this.Minor, $this.Patch)
        if($this.PreReleaseVersion)
        {
            $bldr.Append($this.PreReleaseVersion.ToString($isCiBuild))
        }

        $hasPreRel = $this.PreReleaseVersion -and $this.PreReleaseVersion.Index -ge 0
        if($this.CiBuildIndex -and $this.CiBuildName)
        {
            $bldr.Append($hasPreRel ? '.' : '--')
            $bldr.AppendFormat('ci.{0}.{1}', $this.CiBuildIndex, $this.CiBuildName)
        }

        if(![string]::IsNullOrWhitespace($this.BuildMetadata) -and $includeMetadata)
        {
            $bldr.AppendFormat( '+{0}', $this.BuildMetadata )
        }

        return $bldr.ToString();
    }

    [string] ToString()
    {
        return $this.ToString($true, $false);
    }

    hidden static [ulong] MakePatchPlus1($orderedVersion)
    {
        [ulong] $MulNum = 100;
        [ulong] $MulName = $MulNum * 100;
        [ulong] $MulPatch = ($MulName * 8) + 1;
        return $orderedVersion + $MulPatch;
    }

    hidden static [ulong] GetOrderedVersion($Major, $Minor, $Patch, [PreReleaseVersion] $PreReleaseVersion)
    {
        [ulong] $MulNum = 100;
        [ulong] $MulName = $MulNum * 100;
        [ulong] $MulPatch = ($MulName * 8) + 1;
        [ulong] $MulMinor = $MulPatch * 10000;
        [ulong] $MulMajor = $MulMinor * 50000;

        [ulong] $retVal = (([ulong]$Major) * $MulMajor) + (([ulong]$Minor) * $MulMinor) + ((([ulong]$Patch) + 1) * $MulPatch);
        if( $PreReleaseVersion -and $PreReleaseVersion.Index -ge 0 )
        {
            $retVal -= $MulPatch - 1;
            $retVal += [ulong]($PreReleaseVersion.Index) * $MulName;
            $retVal += [ulong]($PreReleaseVersion.Number) * $MulNum;
            $retVal += [ulong]($PreReleaseVersion.Fix);
        }
        return $retVal;
    }

    hidden static [Version] ConvertToVersion([ulong]$value)
    {
        $revision = [ushort]($value % 65536);
        $rem = [ulong](($value - $revision) / 65536);

        $build = [ushort]($rem % 65536);
        $rem = ($rem - $build) / 65536;

        $minorNum = [ushort]($rem % 65536);
        $rem = ($rem - $minorNum) / 65536;

        $majorNum = [ushort]($rem % 65536);

        return [Version]::new( $majorNum, $minorNum, $build, $revision );
    }
}

Set-StrictMode -Version 3.0

Push-Location $PSScriptRoot
$oldPath = $env:Path
try
{
    # Pull in the repo specific support and force a full initialization of all the environment
    # if current build information is not provided.
    if(!$buildInfo)
    {
        $buildInfo = Initialize-BuildEnvironment -FullInit
        if(!$buildInfo -or $buildInfo -isnot [hashtable])
        {
            throw 'build scripts BUSTED; Got null buildinfo hashtable...'
        }
    }

    # PowerShell doesn't export enums from a script module, so the type of this return is
    # "unpronounceable" [In C++ terminology]. So, convert it to a string so it is usable in this
    # script.
    [string] $buildKind = Get-CurrentBuildKind

    $verInfo = Get-ParsedBuildVersionXML -BuildInfo $buildInfo
    if($buildKind -ne 'ReleaseBuild')
    {
        $verInfo['CiBuildIndex'] = ConvertTo-BuildIndex $env:BuildTime
    }

    switch($buildKind)
    {
        'LocalBuild' { $verInfo['CiBuildName'] = 'ZZZ' }
        'PullRequestBuild' { $verInfo['CiBuildName'] = 'PRQ' }
        'CiBuild' { $verInfo['CiBuildName'] = 'BLD' }
        'ReleaseBuild' { }
        default {throw 'unknown build kind' }
    }

    # Generate props file with the version information for this build.
    # While it is plausible to use ENV vars to overload or set properties
    # that leads to dangling values during development, which makes for
    # a LOT of wasted time chasing down why a change didn't work...
    # [Been there, done that, worn out the bloody T-Shirt...]
    $csemVer = [CSemVer]::New($verInfo)
    $xmlDoc = [System.Xml.XmlDocument]::new()
    $projectElement = $xmlDoc.CreateElement('Project')
    $xmlDoc.AppendChild($projectElement) | Out-Null

    $propGroupElement = $xmlDoc.CreateElement('PropertyGroup')
    $projectElement.AppendChild($propGroupElement) | Out-Null

    $fileVersionElement = $xmlDoc.CreateElement('FileVersion')
    $fileVersionElement.InnerText = $csemVer.FileVersion.ToString()
    $propGroupElement.AppendChild($fileVersionElement) | Out-Null

    $packageVersionElement = $xmlDoc.CreateElement('PackageVersion')
    $packageVersionElement.InnerText = $csemVer.ToString($false) # (No metadata)
    $propGroupElement.AppendChild($packageVersionElement) | Out-Null

    $productVersionElement = $xmlDoc.CreateElement('ProductVersion')
    $productVersionElement.InnerText = $csemVer.ToString($true) # (With metadata)
    $propGroupElement.AppendChild($productVersionElement) | Out-Null

    $assemblyVersionElement = $xmlDoc.CreateElement('AssemblyVersion')
    $assemblyVersionElement.InnerText = $csemVer.FileVersion.ToString()
    $propGroupElement.AppendChild($assemblyVersionElement) | Out-Null

    $informationalVersionElement = $xmlDoc.CreateElement('InformationalVersion')
    $informationalVersionElement.InnerText = $csemVer.ToString($true) # (With metadata)
    $propGroupElement.AppendChild($informationalVersionElement) | Out-Null

    # inform unit testing of the environment as the env vars are NOT accessible to the tests
    # Sadly, the `dotnet test` command does not spawn the tests with an inherited environment.
    # So they cannot know what the scenario is unless that information is conveyed by some other
    # means.
    $buildKindElement = $xmlDoc.CreateElement('BuildKind')
    $buildKindElement.InnerText = $buildKind
    $propGroupElement.AppendChild($buildKindElement) | Out-Null

    # Unit tests need to see the CI build info as it isn't something they can determine on their own.
    # The Build index is based on a time stamp and the build name depends on the runtime environment
    # to set some env vars etc...
    if($buildKind -ne 'ReleaseBuild')
    {
        $buildTimeElement = $xmlDoc.CreateElement('BuildTime')
        $buildTimeElement.InnerText = $env:BuildTime
        $propGroupElement.AppendChild($buildTimeElement) | Out-Null

        $ciBuildIndexElement = $xmlDoc.CreateElement('CiBuildIndex')
        $ciBuildIndexElement.InnerText = $verInfo['CiBuildIndex']
        $propGroupElement.AppendChild($ciBuildIndexElement) | Out-Null

        $ciBuildNameElement = $xmlDoc.CreateElement('CiBuildName')
        $ciBuildNameElement.InnerText = $verInfo['CiBuildName']
        $propGroupElement.AppendChild($ciBuildNameElement) | Out-Null
    }

    $buildGeneratedPropsPath = Join-Path $buildInfo['RepoRootPath'] 'GeneratedVersion.props'
    $xmlDoc.Save($buildGeneratedPropsPath)
}
catch
{
    # everything from the official docs to the various articles in the blog-sphere says this isn't needed
    # and in fact it is redundant - They're all WRONG! By re-throwing the exception the original location
    # information is retained and the error reported will include the correct source file and line number
    # data for the error. Without this, only the error message is retained and the location information is
    # Line 1, Column 1, of the outer most script file, which is, of course, completely useless.
    throw
}
finally
{
    Pop-Location
    $env:Path = $oldPath
}
