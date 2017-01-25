//
//  AssemblyInfo.cs
//
//  Author:
//       Jarl Gullberg <jarl.gullberg@gmail.com>
//
//  Copyright (c) 2016 Jarl Gullberg
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System.Reflection;
using System.Runtime.InteropServices;
using System.Resources;

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Launchpad")]
[assembly: AssemblyDescription("A free, open-source UE4-compatible game launcher\n\nIcons used were taken from www.icons8.com\n\n\n")]
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
[assembly: AssemblyVersion("0.3.1.*")]
[assembly: AssemblyFileVersion("0.3.0.0")]
[assembly: NeutralResourcesLanguageAttribute("en-GB")]

// Log4Net XML activation
[assembly: log4net.Config.XmlConfigurator(Watch=true)]