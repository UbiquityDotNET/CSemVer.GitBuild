# Documentation
>NOTE:
> This file itself does NOT participate in the generated docs output. It documents the input
> to the doc generation and the process for maintainers of this library (Who clearly don't
> have great memories or are otherwise easily confused. :nerd_face:)

DocFX is used to generate the documentation for this library. There is confusion on
what the statictoc template means and requires. It is ***LITERALLY*** that the Table
of Contents (TOC) is statically generated. So that the entire site is servable from a
file path. This ***DOES NOT*** mean that the default+modern template is unusable for
hosted static site scenarios like 'gh-pages' in GitHub. It only means that the TOC
support will ***require*** a hosted site to provide the contents needed by the generated
TOC client side scripting. That's it. Don't fear the built-in templates (Despite the lack 
of decent docs explaining the details [Yeah, this project previously fell into those gaps
and even constructed a custom template to deal with it... Sigh, what a waste of time...
:facepalm: ])

DocFX has obsoleted the `docfxconsole` NuGet package that was used to run docfx for
a project via MSBUILD. Instead it focused on a .NET tool to do it all via the
command line. Ultimately the docfx.json serves as the "project" file for the
different site builds. The PowerShell script `Build-Docs.ps1` was updated to use the
new tool directly. Using that script should have little or no impact on the overall
flow.

## Files used by the docs generation
There are a lot of files used to generate the docs and the concept of a Table of Contents
(TOC) gets confusing fast when using docfx. So this tries to explain them all.

### .gitignore
This marks the files that are generated as ignored for GIT operations (Don't include
generated sources in the repo - the automated build will generate them) Some specific
files are excluded from this but most of the api-* folders are ignored.

### docfx.json
This file serves as the "project" file for docs generation. Sadly, docfx has deprecated
old docfxconsole that supported creation of a csproj project file to generate the docs
from. So this file serves as the only equivalent of a project file. Unfortunately it is
in JSON format and unlike any other project is unusable directly in an IDE as they don't
understand the format of such a thing.

### favicon.ico
This provides the standard web browser icon for the site as a whole.

### index.md
Markdown for the index (Home) of this site.

### toc.yml
YAML file containing the links for the Table of contents for the SITE as a whole (across
all child pages/folders). This is for the TOP row navigation on the site. (It has NOTHING
to do with the left navigation for any given sub folder)

### Folders
There are a few folders containing input for the site generation.

#### api-*
These folders contain the Generated contents for each project as (YAML) metadata files
parsed and generated from the source. Since most of these files are generated they are
excluded from git in the [.gitignore](#gitignore) file.

##### api-*/Index.md
This contains the main landing page for a given library it has the top bar navigation
from [toc.yml](#tocyml) AND the left navigation from the generated
[toc.yml](#generated-left-nav-tocyml) for this API library. 

#### Generated left nav TOC.YML
This version of the overloaded toc.yml file name is used to produce the left navigation
for an API surface. This is generated from the source files and normally contains the
set of namespaces, types, enums etc... Each library project generates it's own version
of this file. Since this is generated it is listed in the [.gitignore](#gitignore) file.

#### Library Content
These folders (named after the `*` portion of the [api-*](#api) folder names contains
manually written additional files, articles, samples etc... related to a given library.
>NOTE
> The TOC.YML file format used in these topics is DIFFERENT from what is auto generated.
>

#### namespaces
The `namespaces` folder contains overrides markdown files to allow documenting whole
namespaces. .NET doc comments do not allow namespace documentation comments. However,
docfx, when leveraged properly can. These files, when they include the correct YAML
header are merged to the contents for the docs before generation of the final static
site. This is enabled by adding the namespace content to the `overwrite` section of the
docfx.json file:
``` JSON
{
  "build":{
    "content":[
      "files":[
        "<project dir>.**.{md,yml}"
      ],
      // Exclude the namespace overwrites as they are listed explicitly elsewhere
      "exclude": [
          "**/namespaces/**.md"
      ]
    ],
    "overwrite": [
      {
        "files": [
          "**/apidocs/**.md",
          "**/namespaces/**.md"
        ]
      }
    ],
  }
}
```

The YAML header should include the following:
``` YAML
---
uid: <namespace UID>
remarks: *content
---
```

The `<namespace UID>` is usually just the full name of the namespace but is verifiable
by examining the generated YML files in the [api-*](#api) folder. The `remarks` entry
causes the contents of the file to merge to the contents of the remarks section. This
will order the content ahead of the normally generated list of members. It is generally
recommended to finish all such documents with a horizontal rule (`---` in markdown) to
allow separation of the namespace remarks from the generated content. 
