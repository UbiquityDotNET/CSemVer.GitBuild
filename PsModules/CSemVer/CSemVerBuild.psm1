class PrereleaseVersion
{
    [ValidateSet("", "alpha", "beta", "delta", "epsilon", "gamma", "kappa", "prerelease", "rc")]
    [string] $Name;
    
    [ValidateSet("", "a", "b", "d", "e", "g", "k", "p", "r")]
    [string] $ShortName;
    
    [ValidateRange(-1,8)]
    [int] $Index;
    
    [ValidateRange(0,99)]
    [int] $Number;
    
    [ValidateRange(0,99)]
    [int] $Fix;

    [string] $CiBuildName;

    [string] $CiBuildIndex;

    PrereleaseVersion([hashtable]$buildInfo)
    {
        $preRelName = $buildInfo['PreReleaseName']
        $preRelNumber = $buildInfo['PreReleaseNumber']
        $preRelFix = $buildInfo['PreReleaseFix']
        $this.CiBuildName = $buildInfo["CiBuildName"];
        $this.CiBuildIndex = $buildInfo["CiBuildIndex"];

        if( (![string]::IsNullOrEmpty($this.CiBuildName)) -and !($this.CiBuildName -match '\A[a-z0-9-]+\Z') )
        {
            throw "CiBuildName is invalid"
        }

        if( (![string]::IsNullOrEmpty($this.CiBuildIndex)) -and !($this.CiBuildIndex -match '\A[a-z0-9-]+\Z') )
        {
            throw "CiBuildIndex is invalid"
        }

        if( ![string]::IsNullOrWhiteSpace( $preRelName ) )
        {
            $this.Index = [PrereleaseVersion]::GetPrerelIndex($preRelName)
            $this.Name = ($this.Index -ge 0) ? [PrereleaseVersion]::PreReleaseNames[$this.Index] : ""
            $this.ShortName = ($this.Index -ge 0) ? [PrereleaseVersion]::PreReleaseShortNames[$this.Index] : ""
            $this.Number = $preRelNumber;
            $this.Fix = $preRelFix;
        }
        else
        {
            $this.Index = -1;
        }

        if( (![string]::IsNullOrEmpty( $this.CiBuildName )) -and [string]::IsNullOrEmpty( $this.CiBuildIndex ) )
        {
            throw "CiBuildIndex is required if CiBuildName is provided";
        }
    }

    [string] ToString([bool] $useShortForm = $false)
    {
        $hasCIBuild = ![string]::IsNullOrEmpty($this.CiBuildName)
        $hasPreRel = $this.Index -ge 0

        $bldr = [System.Text.StringBuilder]::new()
        if($hasPreRel)
        {
            $bldr.Append('-').Append($useShortForm ? $this.ShortName : $this.Name)
            $delimFormat = $useShortForm ? '-{0:D02}' : '.{0}'
            if(($this.Number -gt 0))
            {
                $bldr.AppendFormat($delimFormat, $this.Number)
                if(($this.Fix -gt 0))
                {
                    $bldr.AppendFormat($delimFormat, $this.Fix)
                }
            }
        }

        if($hasCIBuild)
        {
            $bldr.Append($hasPreRel ? '.' : '--')
            $bldr.AppendFormat('ci.{0}.{1}', $this.CiBuildIndex, $this.CiBuildName)
        }

        return $bldr.ToString()
    }

    hidden static [int] GetPrerelIndex([string] $preRelName)
    {
        $preRelIndex = -1
        if(![string]::IsNullOrWhiteSpace($preRelName))
        {
            $preRelIndex = [PrereleaseVersion]::PreRleaseNames |
                         ForEach-Object {$index=0} {@{Name = $_; Index = $index++}} |
                         Where-Object {$_["Name"] -ieq $preRelName} |
                         ForEach-Object {$_["Index"]} |
                         Select-Object -First 1

            # if not found in long names, test against the short names
            if($preRelIndex -lt 0)
            {
                $preRelIndex = [PrereleaseVersion]::PreReleaseShortNames |
                             ForEach-Object {$index=0} {@{Name = $_; Index = $index++}} |
                             Where-Object {$_["Name"] -ieq $preRelName} |
                             ForEach-Object {$_["Index"]} |
                             Select-Object -First 1
            }
        }
        return $preRelIndex
    }

    hidden static [string[]] $PreReleaseNames = @("alpha", "beta", "delta", "epsilon", "gamma", "kappa", "prerelease", "rc" );
    hidden static [string[]] $PreReleaseShortNames = @("a", "b", "d", "e", "g", "k", "p", "r");
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

    [ulong] $OrderedVersion;

    [Version] $FileVersion;

    [PrereleaseVersion] $PrereleaseVersion;

    CSemVer([hashtable]$buildInfo)
    {
        $this.Major = $buildInfo["BuildMajor"]
        $this.Minor = $buildInfo["BuildMinor"]
        $this.Patch = $buildInfo["BuildPatch"]
        $this.PrereleaseVersion = [PrereleaseVersion]::new($buildInfo)
        $this.BuildMetadata = $buildInfo["BuildMetadata"]
        $this.OrderedVersion = [CSemVer]::GetOrderedVersion($this.Major, $this.Minor, $this.Patch, $this.PreleaseVersion)
        $this.FileVersion = [CSemVer]::ConvertToVersion($this.OrderedVersion -shl 1)
    }

    [string] ToString([bool] $includeMetadata, [bool]$useShortForm)
    {
        $bldr = [System.Text.StringBuilder]::new()
        $bldr.AppendFormat('{0}.{1}.{2}', $this.Major, $this.Minor, $this.Patch)
        if($this.PrereleaseVersion)
        {
            $bldr.Append($this.PrereleaseVersion.ToString($useShortForm))
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

    hidden static [ulong] GetOrderedVersion($Major, $Minor, $Patch, [PrereleaseVersion] $PreReleaseVersion)
    {
        [ulong] $MulNum = 100;
        [ulong] $MulName = $MulNum * 100;
        [ulong] $MulPatch = ($MulName * 8) + 1;
        [ulong] $MulMinor = $MulPatch * 10000;
        [ulong] $MulMajor = $MulMinor * 50000;

        [ulong] $retVal = (([ulong]$Major) * $MulMajor) + (([ulong]$Minor) * $MulMinor) + ((([ulong]$Patch) + 1) * $MulPatch);
        if( $PrereleaseVersion -and $PrereleaseVersion.Index -gt 0 )
        {
            $retVal -= $MulPatch - 1;
            $retVal += [ulong]($PrereleaseVersion.Index) * $MulName;
            $retVal += [ulong]($PrereleaseVersion.Number) * $MulNum;
            $retVal += [ulong]($PrereleaseVersion.Fix);
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
