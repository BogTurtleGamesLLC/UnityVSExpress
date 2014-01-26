using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;

namespace UnityVSExpress {
    /// <summary>
    /// This is a simple application that acts as a middleman between Unity and Visual Studio Express for C# editing.
    /// It allows the user to double-click on files in Unity (or errors) and have the file be opened in Visual Studio Express at the correct line number.
    /// To install this application as your Unity editor:
    /// 1. Place the compiled UnityVSExpress.exe somewhere.
    /// 2. In Unity, select Edit-->Preferences-->External Tools-->External Script Editor.
    /// 3. Browse for and select UnityVSExpress.exe.
    /// 4. In Unity, fill out the External Script Editor Args field with:
    ///     "$(File)" $(Line)
    /// The default Visual Studio Express year to run is 2010, i.e., Visual C# Express 2010.
    /// If you would like to run a different year, you can add that as a third parameter:
    ///     "$(File)" $(Line) 2013
    /// In this case, Unity would run Visual Studio Express 2013, i.e., Windows Desktop Express 2013.
    /// 5. Make sure that .cs files are opened with the Visual Studio Express version you want by default.
    /// To make sure of this, go to a .cs file, right click on it, select Open with-->Choose default program...
    /// Then select the version of Visual Studio Express that you are using for Unity scripts.
    /// </summary>
    class Program {
        /// <summary>
        /// This constant is used if you double click on an error line in Unity and your Unity solution is not already open in Visual Studio Express.
        /// In this case, this program must wait before issuing the goto line keyboard macro command, otherwise Visual Studio Express will ignore it.
        /// I could not figure out a way to determine when Visual Studio Express is done loading (i.e., is ready to accept the keyboard macro), so this delay is a hack that works reasonably well.
        /// In the worst case, the goto line keyboard macro command will be ignored and you will have to go there manually (or you can double click on the error in Unity a second time and it will take you to the correct line in Visual Studio Express).
        /// </summary>
        const int WaitSecondsBeforeGotoLine = 2;

        /// <summary>
        /// We would like to open the C# specific Unity project to avoid errors with Visual Studio Express.
        /// Thanks to Giometric on the Unity forum.
        /// Code derived from: http://forum.unity3d.com/threads/222633-Using-Visual-Studio-Express-with-Unity-instead-of-Mono-Develop?p=1487721&viewfull=1#post1487721
        /// </summary>
        const string SolutionEnding = "-csharp";

        static void Main( string[] args ) {
            // Default Visual Studio Express version to run is Visual C# Express 2010.
            const int DefaultVSExpressYear = 2010;

            /// <summary>
            /// The Visual Studio Express version year to run.
            /// Possible values:
            ///     2008 (Visual C# Express 2008) - not tested
            ///     2010 (Visual C# Express 2010)
            ///     2012 (Windows Desktop Express 2012) - not tested
            ///     2013 (Windows Desktop Express 2013)
            /// </summary>
            int vsExpressYear = DefaultVSExpressYear;

            if (args.Length >= 3) {
                // Handle Visual Studio Express version year arg.
                if (!int.TryParse( args[ 2 ], out vsExpressYear )) {
                    vsExpressYear = DefaultVSExpressYear;
                }
            }

            // Locate where the Visual Studio Express executable is.
            string expressExePath = GetExpressExePath( GetVSVersion( vsExpressYear ), GetExpressRegistryKeyPath( vsExpressYear) );
            string expressExe = GetExpressExe( vsExpressYear );
            string expressTitleEnding = GetExpressTitleEnding( vsExpressYear );

            DirectoryInfo unityRootDir = null;
            bool openScript = true;
            if (args.Length >= 1) {
                // Handle filename arg.
                string arg = args[ 0 ];
                if (expressExePath != null) {
                    unityRootDir = GetUnityRootDir( arg );
                    if (unityRootDir != null) {
                        if (!IsSolutionOpen( expressTitleEnding, unityRootDir )) {
                            OpenSolutionAndScript( expressExePath, expressExe, unityRootDir, arg );
                            // We have already opened the script so no need to do it again.
                            openScript = false;
                        }
                    }
                }
                if (openScript) {
                    OpenScript( arg );
                }
            }

            if (args.Length >= 2 && unityRootDir != null) {
                // Handle line number arg.
                int line;
                if (int.TryParse( args[ 1 ], out line )) {
                    if (line > 0) {
                        // Unity is giving us a line number to go to.
                        if (!openScript) {
                            // We opened the solution and the script so we don't know when this action will finish.
                            // As a hack, we just delay here a user-defined amount of time before sending the goto line command.
                            System.Threading.Thread.Sleep( WaitSecondsBeforeGotoLine * 1000 );
                        }
                        GotoLineInSolution( expressTitleEnding, unityRootDir, line );
                    }
                }
            }
        }

        /// <summary>
        /// Given a Visual Studio Express year, returns a Visual Studio version.
        /// Possible return values:
        ///     9.0 (Visual Studio 9.0 or Visual C# Express 2008) - not tested
        ///     10.0 (Visual Studio 10.0 or Visual C# Express 2010)
        ///     11.0 (Visual Studio 11.0 or Windows Desktop Express 2012) - not tested
        ///     12.0 (Visual Studio 12.0 or Windows Desktop Express 2013) - not tested
        /// </summary>
        static string GetVSVersion( int vsExpressYear ) {
            switch (vsExpressYear) {
                case 2008:
                    return "9.0";
                case 2010:
                    return "10.0";
                case 2012:
                    return "11.0";
                case 2013:
                    return "12.0";
                default:
                    return "10.0";
            }
        }

        /// <summary>
        /// Given a Visual Studio Express year, return whether or not it is a Windows Desktop Express version.
        /// </summary>
        static bool IsWDExpressYear( int vsExpressYear ) {
            switch (vsExpressYear) {
                case 2008:
                case 2010:
                    return false;
                case 2012:
                case 2013:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Given a Visual Studio Express year, return a registry key path for that version.
        /// </summary>
        static string GetExpressRegistryKeyPath( int visualStudioExpressVersionYear ) {
            const string VCSExpressRegistryKeyPath = @"SOFTWARE\Microsoft\VCSExpress";
            const string WDExpressRegistryKeyPath = @"SOFTWARE\Microsoft\WDExpress";

            if (IsWDExpressYear( visualStudioExpressVersionYear )) {
                return WDExpressRegistryKeyPath;
            }
            else {
                return VCSExpressRegistryKeyPath;
            }
        }

        /// <summary>
        /// Given a Visual Studio version and a registry key path, returns a Visual Studio Express executable path.
        /// Code derived from: http://stackoverflow.com/questions/30504/programmatically-retrieve-visual-studio-install-directory 
        /// </summary>
        static string GetExpressExePath( string vsVersion, string expressRegistryKeyPath ) {
            Version version = new Version( vsVersion );
            RegistryKey registryBase32 = RegistryKey.OpenBaseKey( RegistryHive.LocalMachine, RegistryView.Registry32 );
            RegistryKey visualStudioVersionRegistryKey = registryBase32.OpenSubKey( string.Format( @"{0}\{1}.{2}", expressRegistryKeyPath, version.Major, version.Minor ) );
            if (visualStudioVersionRegistryKey == null) { return null; }
            return visualStudioVersionRegistryKey.GetValue( "InstallDir", string.Empty ).ToString();
        }

        /// <summary>
        /// Given a Visual Studio version, returns a Visual Studio Express executable name.
        /// </summary>
        static string GetExpressExe( int vsExpressYear ) {
            const string VCSExpressExe = @"VCSExpress.exe";
            const string WDExpressExe = @"WDExpress.exe";

            if (IsWDExpressYear( vsExpressYear )) {
                return WDExpressExe;
            }
            else {
                return VCSExpressExe;
            }
        }

        /// <summary>
        /// Given a Visual Studio version, returns a Visual Studio Express title ending.
        /// This name is used to detect open Visual Studio windows.
        /// </summary>
        static string GetExpressTitleEnding( int vsExpressYear ) {
            const string VCSExpressTitleStart = @" - Microsoft Visual C# ";
            const string VCSExpressTitleEnd = @" Express";
            const string WDExpressTitleStart = @" - Microsoft Visual Studio Express ";
            const string WDExpressTitleEnd = @" for Windows Desktop";

            if (IsWDExpressYear( vsExpressYear )) {
                return WDExpressTitleStart + vsExpressYear.ToString() + WDExpressTitleEnd;
            }
            else {
                return VCSExpressTitleStart + vsExpressYear.ToString() + VCSExpressTitleEnd;
            }
        }

        /// <summary>
        /// Determines the Unity project root directory from a script path.
        /// </summary>
        static DirectoryInfo GetUnityRootDir( string arg ) {
            DirectoryInfo di = new DirectoryInfo( arg );
            bool gotRoot = false;
            while (!gotRoot && di.Parent != null) {
                if (di.Name == "Assets") {
                    gotRoot = true;
                }
                di = di.Parent;
            }
            return gotRoot ? di : null;
        }

        /// <summary>
        /// Locates a window that matches the Visual Studio Express window solution title.
        /// </summary>
        static IntPtr GetExpressSolutionHandle( string expressTitleEnding, DirectoryInfo unityRootDir ) {
            string expressTitle = unityRootDir.Name + SolutionEnding + expressTitleEnding;
            IntPtr handle = IntPtr.Zero;
            Process[] processes = Process.GetProcesses();
            foreach (Process p in processes) {
                if (expressTitle == p.MainWindowTitle) {
                    handle = p.MainWindowHandle;
                }
            }
            return handle;
        }

        /// <summary>
        /// Determines if a particular solution is already open in Visual Studio Express.
        /// </summary>
        static bool IsSolutionOpen( string expressTitleEnding, DirectoryInfo unityRootDir ) {
            return GetExpressSolutionHandle( expressTitleEnding, unityRootDir ) != IntPtr.Zero;
        }

        /// <summary>
        /// Opens a solution and script in Visual Studio Express with one command.
        /// </summary>
        static void OpenSolutionAndScript( string expressExePath, string expressExe, DirectoryInfo unityRootDir, string scriptName ) {
            // Open the solution and script in Visual Studio Express.
            ProcessStartInfo psInfo = new ProcessStartInfo();
            psInfo.FileName = expressExePath + expressExe;
            psInfo.Arguments = "\"" + unityRootDir.FullName + Path.DirectorySeparatorChar + unityRootDir.Name + SolutionEnding + ".sln\" \"" + scriptName + "\"";
            Process.Start( psInfo );
        }

        /// <summary>
        /// Opens a script in Visual Studio Express.
        /// </summary>
        static void OpenScript( string scriptName ) {
            // This assumes C# files are automatically opened by Visual Studio Express.
            // If we attempt to open them directly with Visual Studio Express, a new instance of the Express executable is created.
            try {
                Process.Start( "\"" + scriptName + "\"" );
            }
            catch (System.ComponentModel.Win32Exception e) {
                // When you select Assets-->Sync MonoDevelop Project from Unity, it will call UnityVSExpress with invalid parameters.
                // Make sure UnityVSExpress doesn't crash here when it does.
            }
        }

        [DllImport( "user32.dll" )]
        [return: MarshalAs( UnmanagedType.Bool )]
        static extern bool SetForegroundWindow( IntPtr hWnd );

        /// <summary>
        /// Makes the active Visual Studio Express window goto a specific line by sending macro keys to it.
        /// </summary>
        static void GotoLineInSolution( string expressTitleEnding, DirectoryInfo unityRootDir, int line ) {
            IntPtr handle = GetExpressSolutionHandle( expressTitleEnding, unityRootDir );

            if (handle == IntPtr.Zero) { return; }

            // Goto the line number using the Visual Studio Express shortcut key.
            SetForegroundWindow( handle );
            SendKeys.SendWait( "^{g}" + line.ToString() + "{ENTER}" );
        }
    }
}
