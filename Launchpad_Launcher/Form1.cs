using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Launchpad_Launcher.Buttons;
using System.Reflection;

namespace Launchpad_Launcher
{
    public partial class Form1 : Form
    {
        bool bCanConnectToFTP = true;//assume we can connect until we cannot

        bool bManifestDownloadFailed = false;

        bool bLauncherVersionCheckFailed = false;
        bool bLauncherNeedsUpdate = false;

        bool bGameNeedsUpdate = false;
        bool bGameIsInstalled = false;

        bool bIsInstallingGame = false;
        bool bDidAttemptInstall = false;
        bool bInstallCompleted = false;

        bool bIsUpdatingGame = false;
        bool bUpdateCompleted = false;

        bool bShouldBeginAutoInstall = false;

        //get a reflection to this assembly
        Assembly thisAssembly = Assembly.GetExecutingAssembly();

        //allow borderless window capture
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

        [DllImportAttribute("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImportAttribute("user32.dll")]
        public static extern bool ReleaseCapture();
        /*
        [DllImport("wininet.dll", SetLastError = true)]
        private static extern long DeleteUrlCacheEntry(string lpszUrlName);*/

        //set up Handler references
        MD5Handler md5 = new MD5Handler();
        ConfigHandler Config = new ConfigHandler();
        FTPHandler FTP = new FTPHandler();

        ImageButton mainButton = new ImageButton();
        ImageButton exitButton = new ImageButton();
        ImageButton minimizeButton = new ImageButton();

        public Form1()
        {
            InitializeComponent();

            //Setup main button
            mainButton.Parent = this;
            mainButton.Bounds = new Rectangle(738, 460, 105, 40);
            mainButton.ForeColor = Color.White;
            mainButton.Click += new EventHandler(mainButton_Click);

            //Setup exit button
            exitButton.Parent = this;
            exitButton.Bounds = new Rectangle(815, 8, 24, 24);
            exitButton.ForeColor = Color.White;
            exitButton.Text = "X";

            Stream exitBackground = thisAssembly.GetManifestResourceStream("Launchpad_Launcher.resource.Button_Mini_Default.png");
            exitButton.BackgroundImage = new Bitmap(Image.FromStream(exitBackground), new Size(24, 24));

            Stream exitHover = thisAssembly.GetManifestResourceStream("Launchpad_Launcher.resource.Button_Mini_Hover.png");
            exitButton.HoverImage = new Bitmap(Image.FromStream(exitHover), new Size(24, 24));

            Stream exitPressed = thisAssembly.GetManifestResourceStream("Launchpad_Launcher.resource.Button_Mini_Pressed.png");
            exitButton.PressedImage = new Bitmap(Image.FromStream(exitPressed), new Size(24, 24));

            exitButton.Click += new EventHandler(exitbutton_Click);

            //Setup minimize button
            minimizeButton.Parent = this;
            minimizeButton.Bounds = new Rectangle(788, 8, 24, 24);
            minimizeButton.ForeColor = Color.White;
            minimizeButton.Text = "_";

            Stream miniBackground = thisAssembly.GetManifestResourceStream("Launchpad_Launcher.resource.Button_Mini_Default.png");
            minimizeButton.BackgroundImage = new Bitmap(Image.FromStream(miniBackground), new Size(24, 24));

            Stream miniHover = thisAssembly.GetManifestResourceStream("Launchpad_Launcher.resource.Button_Mini_Hover.png");
            minimizeButton.HoverImage = new Bitmap(Image.FromStream(miniHover), new Size(24, 24));

            Stream miniPressed = thisAssembly.GetManifestResourceStream("Launchpad_Launcher.resource.Button_Mini_Pressed.png");
            minimizeButton.PressedImage = new Bitmap(Image.FromStream(miniPressed), new Size(24, 24));

            minimizeButton.Click += new EventHandler(minimizeButton_Click);

            //Update main window based on our initial state
            UpdateMainWindow();
        }

        private void webBrowser1_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            LoadChangelog();
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            Console.WriteLine("\nForm1_Shown()");

            PerformLauncherChecks();
        }

        private void LoadChangelog()
        {
            string changelogURL = Config.GetChangelogURL();

            WebClient request = new WebClient();
            request.Credentials = new NetworkCredential(Config.GetFTPUsername(), Config.GetFTPPassword());

            try
            {
                byte[] newFileData = request.DownloadData(changelogURL);
                string fileString = System.Text.Encoding.UTF8.GetString(newFileData);
                webBrowser1.DocumentText = fileString;
                webBrowser1.ScrollBarsEnabled = true;
            }
            catch (WebException ex)
            {
                webBrowser1.DocumentText = "Error: Could not load change log from server.";
            }

            request.Dispose();
            /*
            DeleteUrlCacheEntry(changelogURL); //if we do not clear cache old changelogs will still show
            webBrowser1.Navigate(changelogURL);
            */
        }

        private void mainButton_Click(object sender, EventArgs e)
        {
            Console.WriteLine("mainButton_Click()");

            if (bCanConnectToFTP)
            {
                if (bLauncherNeedsUpdate)
                {
                    Console.WriteLine("bLauncherNeedsUpdate");

                    DoLauncherUpdate();
                }
                else if (bGameIsInstalled == false)
                {
                    Console.WriteLine("bGameIsInstalled == false");

                    DoGameInstall();
                }
                else if (bGameNeedsUpdate)
                {
                    Console.WriteLine("bGameNeedsUpdate");

                    DoGameUpdate();
                }
                else
                {
                    Console.WriteLine("Running game process.");

                    ProcessStartInfo gameProcess = new ProcessStartInfo();

                    gameProcess.FileName = Config.GetGameExecutable();
                    gameProcess.UseShellExecute = true;
                    Process.Start(gameProcess);
                }
            }
            else
            {
                MessageBox.Show("Unable to connect to server.");

                PerformLauncherChecks();
            }
        }

        private void verifyInstallation_button_Click(object sender, EventArgs e)
        {
            if (!bCanConnectToFTP)
            {
                MessageBox.Show("Unable to connect to server.");
                PerformLauncherChecks();
                return;
            }

            //verifying is basically the same as updating. Check all files, download replacements, etc
            if (bGameIsInstalled == true)
            {
                DoGameUpdate();
            }
            else
            {
                MessageBox.Show("Please install before verifying.");
            }
        }

        private void PerformLauncherChecks()
        {
            Console.WriteLine("PerformLauncherChecks()");

            //check if we can connect to the FTP where gameVersion/manifest/launcherVersion are held
            DoCanConnectToFTPCheck();

            //check if this is the first time we are launching and prompt user to verify installation path
            DoInitialSetupCheck();

            //check that our launcher is up to date
            DoLauncherUpdateCheck();

            //check that the manifest is up to date
            DoManifestUpdate();

            //check that the game is installed
            DoGameIsInstalledCheck();

            //check that the game is up to date
            DoGameUpdateCheck();

            //check if game should begin auto installing
            DoShouldBeginAutoInstallCheck();

            //update UI
            UpdateMainWindow();
        }

        private bool DoCanConnectToFTPCheck()
        {
            Console.WriteLine("\nDoCanConnectToFTPCheck()");

            string FTPURL = Config.GetFTPUrl();
            string FTPUserName = Config.GetFTPUsername();
            string FTPPassword = Config.GetFTPPassword();

            try
            {
                FtpWebRequest requestDir = (FtpWebRequest)FtpWebRequest.Create(FTPURL);
                requestDir.Credentials = new NetworkCredential(FTPUserName, FTPPassword);
                requestDir.Method = WebRequestMethods.Ftp.ListDirectory;

                try
                {
                    WebResponse response = requestDir.GetResponse();
                    Console.WriteLine("Can connect to FTP at: {0} username: {1} password: {2}", FTPURL, FTPUserName, FTPPassword);
                    requestDir.Abort();//important otherwise FTP remains open and further attemps to access it hang
                    bCanConnectToFTP = true;

                }
                catch
                {
                    requestDir.Abort();
                    bCanConnectToFTP = false;
                }
            }
            catch
            {
                //case where ftp url in config is not valid
                bCanConnectToFTP = false;
            }

            if (!bCanConnectToFTP)
            {
                Console.WriteLine("Failed to connect to FTP at: {0} username: {1} password: {2}", FTPURL, FTPUserName, FTPPassword);
                bCanConnectToFTP = false;
            }

            return bCanConnectToFTP;
        }

        private void DoInitialSetupCheck()
        {
            Console.WriteLine("\nDoInitialSetupCheck()");

            //we use an empty file to determine if this is the first launch or not
            if (!File.Exists(Config.GetUpdateCookie()))
            {
                //this is the first time we're launching
                progress_label.Text = "Performing initial setup...";
                progress_label.Refresh();

                DialogResult firstTimeSetup = MessageBox.Show(String.Format("This appears to be the first time you're starting this launcher.{0}{1}Your current directory is {2}{3}{4}Is this where you would like to install the game?", Environment.NewLine, Environment.NewLine, Directory.GetCurrentDirectory(), Environment.NewLine, Environment.NewLine), "", MessageBoxButtons.YesNo);

                if (firstTimeSetup == DialogResult.Yes)
                {
                    Console.WriteLine("Performing initial setup");

                    bShouldBeginAutoInstall = true;

                    try
                    {
                        Console.WriteLine("Writing launcher version to update cookie");
                        //write the launcher version to the update cookie
                        TextWriter tw = new StreamWriter(Config.GetUpdateCookie());

                        tw.WriteLine(Config.GetLauncherVersion());
                        tw.Close();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Failed to write launcher version to update cookie");
                        Console.WriteLine(ex.StackTrace);
                    }
                }
                else
                {
                    Environment.Exit(0);
                }
            }
            else
            {
                Console.WriteLine("Initial setup already complete.");
            }
        }

        private void DoLauncherUpdateCheck()
        {
            Console.WriteLine("\nDoLauncherUpdateCheck()");

            if (!bCanConnectToFTP)
            {
                Console.WriteLine("Launcher update failed: Unable to connect to FTP");
                return;
            }

            try
            {
                //get the latest launcher version from the FTP
                string remoteLauncherVersionTXTURL = String.Format("{0}/launcher/launcherVersion.txt", Config.GetFTPUrl());
                string FTPUsername = Config.GetFTPUsername();
                string FTPPassword = Config.GetFTPPassword();
                //Console.WriteLine("Attempting to ReadFTPFile: userName: {0}, pass:{1}, remoteLauncherVersionTXTURL: {2}", FTPUsername, FTPPassword, remoteLauncherVersionTXTURL);
                string remoteLauncherVersion = FTP.ReadFTPFile(FTPUsername, FTPPassword, remoteLauncherVersionTXTURL).Replace("\0", string.Empty);

                //get the current launcher version from file
                string launcherVersion = Config.GetLauncherVersion();

                //we create version objects to format the versions correctly to remove unnecessary spaces or new line characters that may exist
                System.Version RemoteVersion = new System.Version(remoteLauncherVersion);
                System.Version LauncherVersion = new System.Version(launcherVersion);

                //update the progress label to let the user know what we are doing
                progress_label.Text = "Checking launcher version...";
                progress_label.Refresh();

                if (RemoteVersion.Equals(LauncherVersion))
                {
                    Console.WriteLine("Launcher version is update to date.");

                    //launcher does not need to be updated
                    bLauncherNeedsUpdate = false;
                    if (File.Exists(String.Format(@"{0}\update.bat", Config.GetLocalDir())))
                    {
                        File.Delete(String.Format(@"{0}\update.bat", Config.GetLocalDir()));
                    }
                }
                else
                {
                    Console.WriteLine("Launcher version {0} is NOT up to date: {1}", LauncherVersion, RemoteVersion);

                    bLauncherNeedsUpdate = true;
                }

                bLauncherVersionCheckFailed = false;
            }
            catch (WebException ex)
            {
                Console.WriteLine(ex.Status);
                bLauncherVersionCheckFailed = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                bLauncherVersionCheckFailed = true;
            }
        }

        private void DoManifestUpdate()
        {
            Console.WriteLine("\nDoManifestUpdate()");

            if (!bCanConnectToFTP)
            {
                Console.WriteLine("Manifest update failed: Unable to connect to FTP");
                return;
            }

            //if we have no manifest, probably first time setup
            if (!File.Exists(Config.GetManifestPath()))
            {
                //download manifest
                try
                {
                    Console.WriteLine("DoManifestUpdate(): Warning - No Manifest attempting to download");

                    FTP.DownloadFTPFile(Config.GetFTPUsername(), Config.GetFTPPassword(), Config.GetManifestURL(), Config.GetManifestPath());

                    if (File.Exists(Config.GetManifestPath()))
                    {
                        Console.WriteLine("Manifest download succeeded");

                        bManifestDownloadFailed = false;
                    }
                    else
                    {
                        Console.WriteLine("Manifest download failed");

                        bManifestDownloadFailed = true;
                    }
                }
                catch (WebException ex)
                {
                    Console.WriteLine("DoManifestUpdate() download failed {0}", ex.StackTrace);

                    bManifestDownloadFailed = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("DoManifestUpdate() download failed: {0}", ex.StackTrace);
                }
            }
            else
            {
                Console.WriteLine("Manifest already exists locally");
            }

            try
            {
                //we should now have a manifest, let's check if it's the latest one
                FileStream localManifestStream = File.OpenRead(Config.GetManifestPath());
                string localManifestChecksum = md5.GetFileHash(localManifestStream);
                localManifestStream.Close();
                string remoteManifestChecksum = FTP.ReadFTPFile(Config.GetFTPUsername(), Config.GetFTPPassword(), Config.GetManifestChecksumURL()).Replace("\0", string.Empty);

                Console.WriteLine("Remote: {0}", remoteManifestChecksum);
                Console.WriteLine("Local:  {0}", localManifestChecksum);

                if (localManifestChecksum == remoteManifestChecksum)
                {
                    Console.WriteLine("Manifest is OK");
                }
                else
                {
                    //our local manifest version is not up to date, download the new one
                    Console.WriteLine("Manifest not up to date: Downloading new manifest");

                    //delete old manifest
                    File.Delete(Config.GetManifestPath());
                    FTP.DownloadFTPFile(Config.GetFTPUsername(), Config.GetFTPPassword(), Config.GetManifestURL(), Config.GetManifestPath());

                    //create .gameNeedsUpdate file to signal that game needs update between launches of the application
                    File.Create(String.Format(@"{0}\.gameNeedsUpdate", Config.GetLocalDir()));

                    bGameNeedsUpdate = true;
                }
            }
            catch (System.IO.FileNotFoundException)
            {
                Console.WriteLine("Manifest does not exist.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Manifest download failed: {0}", ex.StackTrace);
            }
        }

        private void DoGameIsInstalledCheck()
        {
            Console.WriteLine("\nDoGameIsInstalledCheck()");

            if (File.Exists(String.Format(@"{0}\.installComplete", Config.GetGamePath())))
            {
                bGameIsInstalled = true;
                bInstallCompleted = true;
                Console.WriteLine("Game is installed.");
            }
            else
            {
                bGameIsInstalled = false;
                Console.WriteLine("Game is not installed.");
            }
        }

        private void DoGameUpdateCheck()
        {
            Console.WriteLine("\nDoGameUpdateCheck()");

            if (!bCanConnectToFTP)
            {
                Console.WriteLine("Game update failed: Unable to connect to FTP");
                return;
            }

            try
            {
                //set a default version of 0.0.0
                string localVersion = "0.0.0";

                //try to get the local version from file
                try
                {
                    localVersion = File.ReadAllText(String.Format(@"{0}\gameVersion.txt", Config.GetGamePath()));
                }
                catch (IOException ioEx)
                {
                    //if we fail then continue with our version of 0.0.0 to compare against remoteVersion
                }

                //get the remote version from the ftp
                string remoteVersionURL = String.Format("{0}/game/gameVersion.txt", Config.GetFTPUrl());
                string remoteVersion = FTP.ReadFTPFile(Config.GetFTPUsername(), Config.GetFTPPassword(), remoteVersionURL);
                //note remote version may have a trailing '\0' which will cause the console to hang if written

                //we create version objects to format the versions correctly to remove unnecessary spaces, new line characters or '\0' that may exist
                System.Version LocalVersionObject = new System.Version(localVersion);
                System.Version RemoteVersionObject = new System.Version(remoteVersion);

                Console.WriteLine("localGameVersion: {0}", LocalVersionObject);
                Console.WriteLine("remoteGameVersion: {0}", RemoteVersionObject);

                if (LocalVersionObject.Equals(RemoteVersionObject))
                {
                    bGameNeedsUpdate = false;

                    Console.WriteLine("Local game version {0} does not need to be updated.", LocalVersionObject);
                }
                else
                {
                    bGameNeedsUpdate = true;

                    //if the game update is aborted, we'll still have a local update ping. This needs to be improved.
                    File.Create(String.Format(@"{0}\.gameNeedsUpdate", Config.GetLocalDir()));

                    Console.WriteLine("Local game version needs to be updated from {0} to {1}", LocalVersionObject, RemoteVersionObject);
                }
            }
            catch (IOException ioEx)
            {
                Console.WriteLine("DoGameUpdateCheck IOException: {0}", ioEx.StackTrace);
            }
            catch (Exception ex)
            {
                Console.WriteLine("DoGameUpdateCheck exception: {0}", ex.StackTrace);
            }
        }

        private void DoShouldBeginAutoInstallCheck()
        {
            Console.WriteLine("\nDoShouldBeginAutoInstallCheck()");

            if (bShouldBeginAutoInstall)
            {
                Console.WriteLine("Beginning auto install.");

                DoGameInstall();
            }
            else
            {
                Console.WriteLine("Auto install should not begin.");
            }
        }

        private string GetCurrentGameVersion()
        {
            string currentGameVersion = "no version found";
            string gameVersionTxtPath = String.Format(@"{0}\gameVersion.txt", Config.GetGamePath());

            if (File.Exists(gameVersionTxtPath))
            {
                try
                {
                    currentGameVersion = File.ReadAllText(gameVersionTxtPath);
                }
                catch (IOException ex)
                {
                    Console.WriteLine("Could not read gameVersion.txt: {0}", ex);
                }
            }
            else
            {
                Console.WriteLine("gameVersion.txt does not exist at path: {0}", gameVersionTxtPath);
            }

            return new System.Version(currentGameVersion).ToString();
        }

        private void UpdateMainWindow()
        {
            Console.WriteLine("\nUpdateMainWindow()");

            string currentGameVersion = GetCurrentGameVersion();

            if (bCanConnectToFTP == false)
            {
                Console.WriteLine("(bCanConnectToFTP == false)");

                warning_label.ForeColor = Color.Red;
                warning_label.Text = "Could not connect to server. Please try again later.";

                progress_label.Text = "Idle";

                progress_label.Refresh();
                warning_label.Refresh();

            }
            else if (bLauncherVersionCheckFailed)
            {
                Console.WriteLine("(bLauncherVersionCheckFailed == true)");

                warning_label.ForeColor = Color.Red;
                warning_label.Text = "Could not get launcher version from server";

                progress_label.Text = "Idle";

                progress_label.Refresh();
                warning_label.Refresh();
            }
            else if (bLauncherNeedsUpdate)
            {
                Console.WriteLine("(bLauncherNeedsUpdate == true)");

                warning_label.ForeColor = Color.Red;
                warning_label.Text = "Launcher update required";
                warning_label.Refresh();
            }
            else if (bManifestDownloadFailed)
            {
                Console.WriteLine("(bManifestDownloadFailed == true)");

                progress_label.Text = "Launcher version is OK";
                progress_label.Refresh();

                warning_label.ForeColor = Color.Red;
                warning_label.Text = "Manifest download failed!";
                warning_label.Refresh();
            }
            else if (bGameIsInstalled == false)
            {
                Console.WriteLine("(bGameIsInstalled == false)");

                progress_label.Text = "Launcher version is OK";
                progress_label.Refresh();
            }
            else if (bUpdateCompleted)
            {
                Console.WriteLine("(bUpdateCompleted == true)");

                progress_label.Text = "Launcher version is OK";
                progress_label.Refresh();

                progress_label.ForeColor = Color.ForestGreen;
                progress_label.Text = String.Format("Game updated to {0}!", currentGameVersion);
            }
            else if (bInstallCompleted && bDidAttemptInstall)
            {
                Console.WriteLine("(bInstallCompleted == true)");

                progress_label.Text = "Launcher version is OK";
                progress_label.Refresh();

                progress_label.ForeColor = Color.ForestGreen;
                progress_label.Text = "Game install finished!";
            }
            else if (bGameNeedsUpdate)
            {
                Console.WriteLine("bGameNeedsUpdate");

                progress_label.Text = "Launcher version is OK";
                progress_label.Refresh();
            }
            else
            {
                Console.WriteLine("ELSE");

                progress_label.Text = String.Format("Launcher is up to date. Game version is up to date {0}", currentGameVersion);
                progress_label.Refresh();
            }

            if (bGameIsInstalled)
            {
                //now that we are installed verify button should be Enabled so long as we are not currently updating or installing
                verifyInstallation_button.Enabled = (!bIsInstallingGame && !bIsUpdatingGame);

                if (bInstallCompleted)
                {
                    ShowPlayButton();
                }
                else
                {
                    ShowUpdateButton();
                }
            }
            else
            {
                //verify button should be disabled until the game is installed
                verifyInstallation_button.Enabled = false;

                ShowInstallButton();
            }
        }

        private void ShowInstallButton()
        {
            //install button
            //background image
            Stream background = thisAssembly.GetManifestResourceStream("Launchpad_Launcher.resource.Button_Install_Default.png");
            mainButton.BackgroundImage = new Bitmap(Image.FromStream(background), new Size(105, 40));

            //hover image
            Stream hover = thisAssembly.GetManifestResourceStream("Launchpad_Launcher.resource.Button_Install_Hover.png");
            mainButton.HoverImage = new Bitmap(Image.FromStream(hover), new Size(105, 40));

            //pressed image
            Stream pressed = thisAssembly.GetManifestResourceStream("Launchpad_Launcher.resource.Button_Install_Pressed.png");
            mainButton.PressedImage = new Bitmap(Image.FromStream(pressed), new Size(105, 40));
        }

        private void ShowUpdateButton()
        {
            //update button
            //background image
            Stream background = thisAssembly.GetManifestResourceStream("Launchpad_Launcher.resource.Button_Update_Default.png");
            mainButton.BackgroundImage = new Bitmap(Image.FromStream(background), new Size(105, 40));

            //hover image
            Stream hover = thisAssembly.GetManifestResourceStream("Launchpad_Launcher.resource.Button_Update_Hover.png");
            mainButton.HoverImage = new Bitmap(Image.FromStream(hover), new Size(105, 40));

            //pressed image
            Stream pressed = thisAssembly.GetManifestResourceStream("Launchpad_Launcher.resource.Button_Update_Pressed.png");
            mainButton.PressedImage = new Bitmap(Image.FromStream(pressed), new Size(105, 40));
        }

        private void ShowPlayButton()
        {
            //play button
            //background image
            Stream background = thisAssembly.GetManifestResourceStream("Launchpad_Launcher.resource.Button_Play_Default.png");
            mainButton.BackgroundImage = new Bitmap(Image.FromStream(background), new Size(105, 40));

            //hover image
            Stream hover = thisAssembly.GetManifestResourceStream("Launchpad_Launcher.resource.Button_Play_Hover.png");
            mainButton.HoverImage = new Bitmap(Image.FromStream(hover), new Size(105, 40));

            //pressed image
            Stream pressed = thisAssembly.GetManifestResourceStream("Launchpad_Launcher.resource.Button_Play_Pressed.png");
            mainButton.PressedImage = new Bitmap(Image.FromStream(pressed), new Size(105, 40));
        }

        private void exitbutton_Click(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }

        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private void DoLauncherUpdate()
        {
            //maintain the executable name if it was renamed to something other than 'Launchpad' 
            string fullName = Assembly.GetEntryAssembly().Location;
            string executableName = Path.GetFileNameWithoutExtension(fullName); // "Launchpad"

            //if the client wants official updates of Launchpad
            if (Config.GetDoOfficialUpdates() == true)
            {
                FTP.DownloadFTPFile("anonymous", "anonymous", "ftp://directorate.asuscomm.com/launcher/bin/Launchpad.exe", String.Format(@"{0}\{1}.exe", Config.GetTempDir(), executableName));
            }
            else
            {
                //you've forked the launcher and want to update it yourself
                FTP.DownloadFTPFile(Config.GetFTPUsername(), Config.GetFTPPassword(), Config.GetLauncherURL(), String.Format(@"{0}\{1}.exe", Config.GetTempDir(), executableName));
            }

            //create a .bat file that will replace the old Launchpad.exe
            FileStream updateScript = File.Create(String.Format(@"{0}\update.bat", Config.GetLocalDir()));

            TextWriter tw = new StreamWriter(updateScript);
            tw.WriteLine(String.Format(@"timeout 3 & xcopy /s /y ""{0}\{2}.exe"" ""{1}\{2}.exe"" && del ""{0}\{2}.exe""", Config.GetTempDir(), Config.GetLocalDir(), executableName));
            tw.WriteLine(String.Format(@"start {0}.exe", executableName));
            tw.Close();

            ProcessStartInfo updateBatchProcess = new ProcessStartInfo();

            updateBatchProcess.FileName = String.Format(@"{0}\update.bat", Config.GetLocalDir());
            updateBatchProcess.UseShellExecute = true;
            updateBatchProcess.RedirectStandardOutput = false;
            updateBatchProcess.WindowStyle = ProcessWindowStyle.Hidden;

            Process.Start(updateBatchProcess);

            Environment.Exit(0);
        }

        private void DoGameInstall()
        {
            if (!bIsInstallingGame)
            {
                bIsInstallingGame = true;

                //disable buttons while we install
                SetButtonsEnabled(false);

                //run the game installation in the background
                backgroundWorker_GameInstall.RunWorkerAsync();
            }
        }

        private void DoGameUpdate()
        {
            if (!bIsUpdatingGame)
            {
                bIsUpdatingGame = true;

                //disable buttons while we update
                SetButtonsEnabled(false);

                //run game update in the background
                backgroundWorker_GameUpdate.RunWorkerAsync();
            }
        }

        private void SetButtonsEnabled(bool enabled)
        {
            mainButton.Enabled = enabled;
            verifyInstallation_button.Enabled = enabled && bGameIsInstalled;
        }

        private void backgroundWorker_GameInstall_DoWork(object sender, DoWorkEventArgs e)
        {
            bDidAttemptInstall = true; //signal to the UI that we did attempt an install for more accurate messages

            bInstallCompleted = true; //we set InstallCompleted to true and then if we fail at any point set to false

            try
            {
                if (!Directory.Exists(Config.GetGamePath()))
                {
                    Directory.CreateDirectory(Config.GetGamePath());
                }

                IEnumerable<string> manifestFiles = File.ReadLines(Config.GetManifestPath());
                int fileAmount = File.ReadLines(Config.GetManifestPath()).Count();
                int currentProgress = 0;

                foreach (string value in manifestFiles)
                {
                    string[] array = value.Split(':');

                    string path = array[0];
                    string size = array[2];

                    try
                    {
                        int fileBeginningPosition = String.Format(@"{0}{1}", Config.GetGamePath(), path).LastIndexOf(@"\");
                        string truncatedPath = String.Format(@"{0}{1}", Config.GetGamePath(), path).Remove(fileBeginningPosition);
                        Directory.CreateDirectory(truncatedPath);
                    }
                    catch (Exception ex)
                    {
                        bInstallCompleted = false;

                        Console.WriteLine("I failed :( Sorry.");
                        Console.WriteLine(ex.StackTrace);
                    }

                    //skip existing files to allow a failed or stopped download to resume
                    if (!File.Exists(String.Format(@"{0}{1}", Config.GetGamePath(), path)))
                    {
                        FTP.DownloadFTPFile(Config.GetFTPUsername(), Config.GetFTPPassword(), String.Format(@"{0}{1}", Config.GetGameURL(), path.Replace(@"\", "/")), String.Format(@"{0}{1}", Config.GetGamePath(), path));
                    }


                    int fileBeginningPosition_2 = String.Format(@"{0}{1}", Config.GetGamePath(), path).LastIndexOf(@"\");
                    string fileName = String.Format(@"{0}{1}", Config.GetGamePath(), path).Substring(fileBeginningPosition_2);
                    currentProgress++;

                    backgroundWorker_GameInstall.ReportProgress(currentProgress, new Tuple<string, int, int>(fileName, FTP.FTPbytesDownloaded, Convert.ToInt32(size)));
                }

                TextWriter tw = new StreamWriter(Config.GetUpdateCookie());
                tw.WriteLine("installComplete");
                tw.Close();
            }
            catch (IOException ex)
            {
                bInstallCompleted = false;

                DialogResult result = MessageBox.Show("Could not connect to server! Unable to install.", "IOException", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Console.WriteLine("Could not read launcher manifest from server.");
                Console.WriteLine("GameInstallIOException: ");
                Console.WriteLine(ex.StackTrace);
            }
            catch (Exception ex)
            {
                bInstallCompleted = false;

                Console.WriteLine("GameInstallException: ");
                Console.WriteLine(ex.StackTrace);
            }

            if (bInstallCompleted)
            {
                //if the install completed succesfully we update the local gameVersion 
                UpdateVersionNumber();
            }
        }

        private void UpdateVersionNumber()
        {
            string localPath = String.Format(@"{0}\gameVersion.txt", Config.GetGamePath());
            string FTPPath = String.Format("{0}/game/gameVersion.txt", Config.GetFTPUrl());

            try
            {
                FTP.DownloadFTPFile(Config.GetFTPUsername(), Config.GetFTPPassword(), FTPPath, localPath);
                string newVersion = File.ReadAllText(String.Format(@"{0}\gameVersion.txt", Config.GetGamePath()));
                Console.WriteLine("Write new gameVersion: {0} succeeded", newVersion);
            }
            catch (IOException ioEx)
            {
                Console.WriteLine("download gameVersion.txt failed: {0}", ioEx);
            }
        }

        private void backgroundWorker_GameInstall_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            mainPanel_progressBar.Maximum = File.ReadLines(Config.GetManifestPath()).Count();
            //not actually the percentage, but rather the file count
            mainPanel_progressBar.Value = e.ProgressPercentage;

            //fileName, FTPBytesDownloaded.ToString(), size.ToString()
            Tuple<string, int, int> state = (Tuple<string, int, int>)e.UserState;

            double downloadedInMb = state.Item2 / 1000000;
            double sizeInMB = state.Item3 / 1000000;

            progress_label.Text = String.Format(@"Downloading: {0}", state.Item1);

            if (downloadedInMb <= 0 || sizeInMB <= 0)
            {
                fileSizeProgress_label.Text = String.Format("less than 1/1 MB");
            }
            else
            {
                fileSizeProgress_label.Text = String.Format("{0}/{1} MB", downloadedInMb, sizeInMB);
            }


            progress_label.Refresh();
            fileSizeProgress_label.Refresh();
        }

        private void backgroundWorker_GameInstall_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (bInstallCompleted)
            {
                string installCompleteFilePath = String.Format(@"{0}\.installComplete", Config.GetGamePath());

                try
                {
                    File.Create(installCompleteFilePath);
                    bGameIsInstalled = true;
                }
                catch (System.IO.IOException ex)
                {
                    Console.WriteLine("Cannot create file at {0}: {1}", installCompleteFilePath, ex.ToString());
                }
            }

            bIsInstallingGame = false;

            //re-enable the buttons that we disabled during install
            SetButtonsEnabled(true);

            UpdateMainWindow();
        }

        private void backgroundWorker_GameUpdate_DoWork(object sender, DoWorkEventArgs e)
        {
            Console.WriteLine("backgroundWorker_GameUpdate_DoWork()");

            string[] manifestFilesArray = File.ReadAllLines(Config.GetManifestPath());
            if (manifestFilesArray.Length == 0)
            {
                DialogResult result = MessageBox.Show("Could not connect to server! Unable to verify.", "IOException", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            bool GameUpdateCompletedSuccessfully = true;
            int i = 0;
            int manifestFilesArrayLength = manifestFilesArray.Length;

            foreach (string value in manifestFilesArray)
            {
                int progress = i;

                //Console.WriteLine("Progress: {0}", progress);
                //read the value from the manifest as an array with split values (path, MD5 and size)
                string[] manifestFile = value.Split(':');

                string filePath = String.Format(@"{0}{1}", Config.GetGamePath(), manifestFile[0]);
                string fileManifestMD5 = manifestFile[1];

                string FTPPath = String.Format("{0}/game{1}", Config.GetFTPUrl(), manifestFile[0].Replace(@"\", "/"));
                string localPath = String.Format(@"{0}{1}", Config.GetGamePath(), manifestFile[0]);

                long fileSize = Convert.ToInt64(manifestFile[2]);

                //check if the file exists locally
                if (!File.Exists(filePath))
                {
                    //if it does not, download it
                    try
                    {
                        FTP.DownloadFTPFile(Config.GetFTPUsername(), Config.GetFTPPassword(), FTPPath, localPath);
                        //Console.WriteLine("GameUpdateWorker - FileUpdate: ");
                        //Console.WriteLine(localPath);
                        i++;
                        backgroundWorker_GameUpdate.ReportProgress(progress, new Tuple<string, int>(manifestFile[0], manifestFilesArray.Length));
                    }
                    catch (Exception ex)
                    {
                        GameUpdateCompletedSuccessfully = false;
                        Console.WriteLine("GameUpdateWorker - NoFile: ");
                        Console.WriteLine(ex.StackTrace);
                    }
                }
                else
                {
                    //check if the MD5s match
                    try
                    {
                        FileStream localFile = File.OpenRead(localPath);
                        string localMD5 = md5.GetFileHash(localFile);

                        if (!(localMD5 == fileManifestMD5))
                        {
                            //if they do not match, download and replace the file
                            try
                            {
                                FTP.DownloadFTPFile(Config.GetFTPUsername(), Config.GetFTPPassword(), FTPPath, localPath);
                                Console.WriteLine("GameUpdateWorker - MD5Update: {0}", localPath);
                                localFile.Close();
                                i++;
                                backgroundWorker_GameUpdate.ReportProgress(progress, new Tuple<string, int>(manifestFile[0], manifestFilesArray.Length));
                            }
                            catch (IOException ioEx)
                            {
                                GameUpdateCompletedSuccessfully = false;

                                Console.WriteLine("download and replace failed: {0}", ioEx);
                            }
                        }
                        else
                        {
                            //if they match, move on
                            //Console.WriteLine("GameUpdateWorker - AllOK: {0}", filePath);
                            localFile.Close();
                            i++;
                            backgroundWorker_GameUpdate.ReportProgress(progress, new Tuple<string, int>(manifestFile[0], manifestFilesArray.Length));
                        }
                    }
                    catch (Exception ex)
                    {
                        GameUpdateCompletedSuccessfully = false;

                        Console.WriteLine("GameUpdateWorker - MD5: ");
                        Console.WriteLine(ex.StackTrace);
                    }
                }
            }

            if (GameUpdateCompletedSuccessfully)
            {
                //if the update completed succesfully we update the local gameVersion 
                UpdateVersionNumber();
            }
        }

        private void backgroundWorker_GameUpdate_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            Tuple<string, int> state = (Tuple<string, int>)e.UserState;

            mainPanel_progressBar.Maximum = state.Item2;
            mainPanel_progressBar.Value = e.ProgressPercentage;

            progress_label.Text = state.Item1;

            progress_label.Refresh();
        }

        private void backgroundWorker_GameUpdate_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            progress_label.ForeColor = Color.ForestGreen;
            progress_label.Text = "Game update finished!";

            if (File.Exists(String.Format(@"{0}\.gameNeedsUpdate", Config.GetLocalDir())))
            {
                File.Delete(String.Format(@"{0}\.gameNeedsUpdate", Config.GetLocalDir()));
            }

            bInstallCompleted = true;
            bGameNeedsUpdate = false;

            bIsUpdatingGame = false;

            //re-enable the buttons that we disabled during install
            SetButtonsEnabled(true);

            UpdateMainWindow();
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            AboutBox1 a = new AboutBox1();
            a.Show();
        }

        private void minimizeButton_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }
    }
}
