# Ubiquity.NET.Versioning.Tasks
`Ubiquity.NET.Versioning.Tasks` library provides support for automated build scenarios
with the primary focus is automated build versioning that embraces the principal of
least surprise while conforming to the syntax of CSemVer and CSemVer-CI for versioning.

## The Libraries in this repository

| Library | Description |
|---------|-------------|
| [Ubiquity.NET.Versioning.Build.Tasks](build-tasks/index.md) | This library contains support for automated versioning at BUILD time |

>[!IMPORTANT]
> There is confusion on the ordering of a CI build with relation to a release build with
> CSemVer-CI. A CI Build is either an initial build of an unreleased version with
> [Major.Minor.Patch] == [0.0.0]. Or, it is based on the previously released version and
> is [Major.Minor.Patch+1]. That is, a CI build is ordered ***BEFORE*** all release builds,
> or it is ordered ***AFTER*** the ***specific*** release it is based on!
>
> In particular a CI build version does ***NOT*** indicate what it will become when it is
> finally released/ It ONLY indicates what release it was based on (If any). To simplify
> that, for clarity, a CI build contains everything in the release it was based on and
> additional changes (that might remove things). CI builds are, by definition NOT stable
> and consumers cannot rely on them for predictions of future stability. A given CI build
> may even represent an abandoned approach that never becomes a release!

---
[Attributions](Attributions.md)
