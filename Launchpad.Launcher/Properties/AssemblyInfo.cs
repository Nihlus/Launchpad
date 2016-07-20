using System.Reflection;
using System.Runtime.InteropServices;
using System.Resources;

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Launchpad")]
[assembly: AssemblyDescription("A free, open-source UE4-compatible game launcher\n\nIcons used were taken from www.icons8.com\n\n\nUI graphics were made by Isaac Nichols")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Jarl Gullberg")]
[assembly: AssemblyProduct("Launchpad.Launcher")]
[assembly: AssemblyCopyright("Copyright ©  2015 Jarl Gullberg")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("54f48b80-fbe4-46b6-b6c1-5243c178cb2d")]

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
[assembly: AssemblyVersion("0.3.0.*")]
[assembly: AssemblyFileVersion("0.3.0.0")]
[assembly: NeutralResourcesLanguageAttribute("en-GB")]

// Log4Net XML activation
[assembly: log4net.Config.XmlConfigurator(Watch=true)]