// -----------------------------------------------------------------------
// <copyright file="AssemblyInfo.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

using Microsoft.VisualStudio.TestTools.UnitTesting;

// In SDK-style projects such as this one, several assembly attributes that were historically
// defined in this file are now automatically added during build and populated with
// values defined in project properties. For details of which attributes are included
// and how to customize this process see: https://aka.ms/assembly-info-properties

// Setting ComVisible to false makes the types in this assembly not visible to COM
// components.  If you need to access a type in this assembly from COM, set the ComVisible
// attribute to true on that type.
[assembly: ComVisible( false )]

// The following GUID is for the ID of the typelib if this project is exposed to COM.
[assembly: Guid( "c2962f0e-5a4d-4dc0-ba02-89562beab4e5" )]

[assembly: CLSCompliant( false )]

// Code coverage should not apply to a unit test assembly
[assembly: ExcludeFromCodeCoverage]

// parallelization causes problems with concurrent builds
// so it must be disabled. Total test time is still ~15s
// for the first (No JIT caching of hot path). There is
// probably a way to resolve this and allow parallel builds
// or tests but it doesn't currently seem worth it to investigate...
[assembly: DoNotParallelize]
