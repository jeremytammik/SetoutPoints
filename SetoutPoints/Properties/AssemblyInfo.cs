using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle( "Revit Setout Points Add-In" )]
[assembly: AssemblyDescription( "Automatically generate and manage setout points on geometry vertices of structural elements" )]
[assembly: AssemblyConfiguration( "" )]
[assembly: AssemblyCompany( "Autodesk Inc." )]
[assembly: AssemblyProduct( "Revit Setout Points Add-In" )]
[assembly: AssemblyCopyright( "Copyright © 2012-2015 Jeremy Tammik Autodesk Inc., The Building Coder" )]
[assembly: AssemblyTrademark( "" )]
[assembly: AssemblyCulture( "" )]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible( false )]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid( "118f7279-630d-4661-afe5-c23c23acf46f" )]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers 
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]
// 2014-11-03 2015.0.0.1 added instance transformation suggested by sanjaymann
// 2015-09-09 2016.0.0.0 flat migration to Revit 2016
// 2017-02-15 2017.0.0.0 flat migration to revit 2017 added test file upgraded setoutpoint symbol family
//
[assembly: AssemblyVersion( "2017.0.0.0" )]
[assembly: AssemblyFileVersion( "2017.0.0.0" )]
