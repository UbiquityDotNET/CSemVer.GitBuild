---
uid: understanding-ci-builds
---
# Understanding Continuous Integration (CI) Builds
The use of a version specific to CI builds is unique to the Constrained Semantic
Version spec, in particular the CSemVer-CI support. These versions are NOT compatible
with a CSemVer but are compatible with a SemVer. If that's not confusing enough the
way in which they are constrained makes things a LOT more complicated. This task and
the [Ubiquity.NET.Versioning](https://www.nuget.org/packages/Ubiquity.NET.Versioning)
library BOTH got the behavior wrong initially! This article will hopefully make sense
out of things...

## Examples
Hopefully examples will make things more clear:

1) `v5.0.4--ci.123456.ZZZ`
    * This is a CI build based on a release `v5.0.3`
        * That is, it is ordered AFTER a release of `v5.0.3`
    * It is also a SemVer 'pre-release' and therefore ordered BEFORE
      `v5.0.4`.
        * Thus this CI build is ordered BETWEEN the previously released `v5.0.3` and
          the as of yet unreleased `v5.0.4`.
    * Using this task the `ZZZ` indicates this is a developer local build. No
      guarantees of uniqueness are provided for such versions across machines.
        * It is possible for two developers to achieve the same build version for two
          completely distinct builds.
            * Though in practical terms this is a very unlikely statistical
              probability.
2) 'v5.0.4-beta.0.1.ci.123456.ZZZ`
    * This is a CI Build based on a previously released "pre-release" version
      `v5.0.3-alpha.0.1`
    * As with the previous example this is ordered AFTER the release it is based on
      and BEFORE the Patch+1 version (`v5.0.4-beta.0.1`).

## lifetime scope of a CI Build
The lifetime of a CI build is generally very short and once a version is released
all CIs that led up to that release are essentially moot.

## CI versions are POST-RELEASE based
CI versioning falls into one of two categories:
1) Never had a release to base anything on
    1) Intermediate builds while developing the very first release of a product.
2) A build that occurs ***AFTER*** something was released, but ***BEFORE*** the
   next formal release. Such builds usually include:
    1) Local Developer Builds
    2) Pull Request builds (Automated "buddy" builds)
    3) "CI" builds
        1) Formal builds of the code base either from any PR or on a schedule (usually
           nightly builds)

For CSemVer-CI, which are based on SemVer, the pre-release syntax is all that is
available to indicate the status of a CI build. Additionally, a CSemVer[-CI] supports
representation as an "ordered" version number OR as a File Version Quad. The file
version quad keeps things 'interesting'...

### String forms of versions
As a string a CI build is represented with pre-release component of 'ci' or '-ci'. The
latter is used if the base version already includes pre-release data (The 'double dash'
trick)

`5.0.5--ci.BuildIndex.BuildName'

#### As a SemVer

| Value | Description
|-------|------------|
| 5 | Major portion of the version |
| 0 | Minor portion of the version |
| 5 | Patch portion of the version |
| 'ci.BuildIndex.BuildName' | pre-release data indicating a CI build where the version is patch+1 |

Since this is a pre-release it is ordered BEFORE an actual release of (5.0.5) and
AFTER any release of (5.0.4).

#### As a CSemVer-CI
As a CSemVer-CI that is interpreted a bit differently than you might expect, in
particular the 'Major.Minor.Patch' are what is known as the "Patch+1" form. That is
the `base build` version for this CSemVer-CI version is v5.0.4! That is what is used
as the defining value of a build as it isn't known what the actual release version
will be (it hasn't released yet!).

SemVer has rules regarding compatibilities with respect to changes in elements of a
version. Thus, any versioning strategy that follows those rules does NOT have a fixed
(next version). So CSemVer-CI uses a POST release strategy where the
`<major>.<minor>.<patch>` is actually `<major>.<minor>.<patch+1>` to guarantee it is
ordered AFTER the release it is based on but BEFORE the next possible release. The
actual release value may use an even greater value, but CSemVer-CI doesn't care.

The CSemVer-CI spec is silent on the point of what happens if the base build
version patch is already `9999` (The max allowed value). Does it roll over to
0 and +1 to minor? While that does maintain ordering it pushes the boundaries
of the meaning of the version numbers. Though it is a rather common scenario for
a build to require a major version update when only some small portion of things
is actually incompatible and the rest is largely backwards compatible. It just doesn't
guarantee it anymore.

This task assumes it is proper to roll over into the next form. (In fact it relies
on the ordered integral form of the version and increments that, until it reaches the
maximum)

### Ordered Version
Ordered versions are a concept unique to Constrained Semantic versions. The constraints
applied to a SemVer allow creation of an integral value for all versions, except CI
builds. Ignoring CI builds for the moment, the ordered number is computed from the
values of the various parts of a version as they are constrained by the CSemVer spec.
The math involved is not important for this discussion. Just that each Constrained
Version is representable as a distinct integral value (63 bits actually). A CSemVer-CI
build has two elements the base build and the additional 'BuildIndex' and 'BuildName'
components. This means the string, File version and ordered version numbers are
confusingly different for a CI build. The ordered version number does NOT account for
CI in any way. It is ONLY able to convert to/from a CSemVer. Thus, a CSemVer-CI has
an ambiguous conversion. Should it convert the Patch+1 form in a string or the
base build number?.

### File Version Quad and UINT64
A file Version quad is a data structure that is blittable as an unsigned 64 bit value.
Each such value is broken down into four fields (thus the 'quad' in the common naming)
that are 16 bits each (unsigned). These versions are common in Windows platforms as
that is the form used in resource file versioning. (Though this form is used in other
cases on other platforms as well.) Such a value is really the ordered version number
shifted left by one bit and the LSB used to indicate a CI build with odd numbered
values to represent a CI build. Thus, a File Version of a CI build is derived from
the base version of the CSemVer-CI. The ordering of such values is the same as an
integral conversion. (or, most likely, a simple re-interpret style cast to an unsigned
64 bit value). The LSB reserved to indicate a CI ensures that a CI file Version is
ordered AFTER a non-CI File version for the same base build. This is the most
confusing and subtle aspect of the versioning and where this task an related library
went wrong in early releases.

>[!IMPORTANT]
> To be crystal clear, the FILEVERSION for a CI build comes from the ***base build***
> version as it includes a bit that, when set, indicates a CI build, but also a
> greater integral value. Thus, ordering for a CI build as a POST release version is
> built into this representation already.
>
> The ***string*** form of a version uses the `Patch+1` version to maintain proper
> ordering following the rules defined by SemVer. That is the FileVersion and string
> forms will use different versions at first glance. The string form requires some
> interpretation to fully understand the meaning, while still retaining the desired
> ordering based on SemVer defined rules.

File Version QUAD (Bit 63 is the MSB and bit 0 is the LSB)<sup>[1](#footnote_1)</sup>

|Field | Name | Description |
|------|------|-------------|
|Bits 48-63 | Major | Major portion of a build |
|Bits 32-47 | Minor | Minor portion of a build |
|Bits 16-31| Build |  Build number for a build |
|Bits 1-15 | Revision | Revision of the build |
|Bit 0 | CI Build | normally incorporated into the "revision" (1 == CI Build, 0 == release build)|

Bits 1-63 are the same as the ordered version of the base build for a CI build and
the same as the ordered version of a release build.

------
<sup><a id="footnote_1">1</a></sup> Endianess of the platform does not matter as the bits are numbered as MSB->LSB
and the actual byte layout is dependent on the target platform even though the bits
are not. It is NOT safe to transfer a FileVersion (or Ordered version) as in integral
value without considering the endianess of the source, target and transport mechanism,
all of which are out of scope for this library and the CSemVer spec in general.


