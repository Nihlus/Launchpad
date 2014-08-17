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

namespace Launchpad_Launcher
{
    public partial class Form1 : Form
    {
        bool bManifestDownloadFailed = false;
        bool bLauncherVersionCheckFailed = false;
        bool bLauncherNeedsUpdate = false;
        bool bGameNeedsUpdate = false;
        bool bGameIsProbablyInstalled = false;
        bool bInstallCompleted = false;

        //allow borderless window capture
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

        [DllImportAttribute("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImportAttribute("user32.dll")]
        public static extern bool ReleaseCapture();        

        //set up Handler references
        MD5Handler md5 = new MD5Handler();
        ConfigHandler Config = new ConfigHandler();
        FTPHandler FTP = new FTPHandler();

        public Form1()
        {
            InitializeComponent();                        
        }

        private void webBrowser1_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            webBrowser1.Navigate(Config.GetChangelogURL());

            
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            DoInitialSetupCheck();
            DoLauncherUpdateCheck();

            //manifest is both checked and updated
            DoManifestUpdate();

            DoGameIsInstalledCheck();
            DoGameUpdateCheck();
            UpdateMainWindow();
        }

        private void DoInitialSetupCheck()
        {
            if (!File.Exists(Config.GetUpdateCookie()))
            {
                //this is the first time we're launching
                progress_label.Text = "Performing initial setup...";
                progress_label.Refresh();

                DialogResult firstTimeSetup = MessageBox.Show(String.Format("This appears to be the first time you're starting this launcher.{0}{1}Your current directory is {2}{3}{4}Is this where you would like to install the game?", Environment.NewLine, Environment.NewLine, Directory.GetCurrentDirectory(), Environment.NewLine, Environment.NewLine), "", MessageBoxButtons.YesNo);

                if (firstTimeSetup == DialogResult.Yes)
                {
                    try
                    {
                        //write the launcher version to the update cookie
                        TextWriter tw = new StreamWriter(Config.GetUpdateCookie());

                        tw.WriteLine(Config.GetLauncherVersion());
                        tw.Close();

                        //relaunch update function
                        UpdateMainWindow();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.StackTrace);
                    }

                }
                else
                {
                    Environment.Exit(0);
                }

            }
        }

        private void DoLauncherUpdateCheck()
        {
            try
            {
                string remoteLauncherVersion = FTP.ReadFTPFile(Config.GetFTPUsername(), Config.GetFTPPassword(), String.Format("{0}/launcher/launcherVersion.txt", Config.GetFTPUrl())).Replace("\0", string.Empty);

                progress_label.Text = "Checking launcher version...";
                progress_label.Refresh();

                if (Config.GetLauncherVersion() == "")
                {
                    Console.WriteLine("LauncherUpdateCheck: Local version is NULL!");
                    warning_label.ForeColor = Color.Red;
                    warning_label.Text = "Could not retrieve local launcher version!";
                    warning_label.Refresh();

                    UpdateMainWindow();
                }
                else if (remoteLauncherVersion == Config.GetLauncherVersion())
                {
                    //launcher does not need to be updated
                    File.Delete(String.Format(@"{0}\update.bat", Config.GetLocalDir()));
                    Console.WriteLine("SYSMSG: Launcher version is OK");                    

                    progress_label.Text = "Launcher version is OK";
                    progress_label.Refresh();

                    UpdateMainWindow();
                }
                else
                {
                    bLauncherNeedsUpdate = true;
                    warning_label.ForeColor = Color.Red;
                    warning_label.Text = "Launcher update required";

                    mainPanel_mainButton.Text = "Update Launcher";

                    mainPanel_mainButton.Refresh();
                    warning_label.Refresh();

                    UpdateMainWindow();
                }
                bLauncherVersionCheckFailed = false;
            }
            catch (WebException ex)
            {
                Console.WriteLine(ex.Status);

                warning_label.ForeColor = Color.Red;
                warning_label.Text = "Could not get launcher version from server";

                progress_label.Text = "Idle";

                progress_label.Refresh();
                warning_label.Refresh();

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
            if (!File.Exists(Config.GetManifestPath()))
            {
                //we have no manifest, probably first time setup

                //download manifest
                try
                {
                    Console.WriteLine("MANIFESTUPDATECHECK: Warning - No Manifest");
                    FTP.DownloadFTPFile(Config.GetFTPUsername(), Config.GetFTPPassword(), Config.GetManifestURL(), Config.GetManifestPath());
                    
                    if (File.Exists(Config.GetManifestPath()))
                    {
                        bManifestDownloadFailed = false;
                        UpdateMainWindow();
                    }
                    else
                    {
                        bManifestDownloadFailed = true;
                        UpdateMainWindow();
                    }
                }
                catch (WebException ex)
                {
                    Console.WriteLine(ex.Status);

                    warning_label.ForeColor = Color.Red;
                    warning_label.Text = "Manifest download failed!";
                    warning_label.Refresh();

                    bManifestDownloadFailed = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.StackTrace);
                }
                              
            }

            try
            {
                //we should now have a manifest, let's check if it's the latest one
                FileStream localManifestStream = File.OpenRead(Config.GetManifestPath());
                string localManifestChecksum = md5.GetFileHash(localManifestStream);

                localManifestStream.Close();

                Console.Write("Remote: ");
                Console.WriteLine(FTP.ReadFTPFile(Config.GetFTPUsername(), Config.GetFTPPassword(), Config.GetManifestChecksumURL()).Replace("\0", string.Empty));
                Console.Write("Local:  ");
                Console.WriteLine(localManifestChecksum);

                if (localManifestChecksum == FTP.ReadFTPFile(Config.GetFTPUsername(), Config.GetFTPPassword(), Config.GetManifestChecksumURL()).Replace("\0", string.Empty))
                {
                    Console.WriteLine("Manifest is OK");
                }
                else
                {
                    //our local version is not OK, download a new one
                    Console.WriteLine("Manifest not OK: Downloading new manifest");
                    File.Delete(Config.GetManifestPath());
                    FTP.DownloadFTPFile(Config.GetFTPUsername(), Config.GetFTPPassword(), Config.GetManifestURL(), Config.GetManifestPath());
                    File.Create(String.Format(@"{0}\.gameNeedsUpdate", Config.GetLocalDir()));
                    bGameNeedsUpdate = true;
                }
                UpdateMainWindow();
            }
            catch (Exception ex)
            {
                if (bManifestDownloadFailed == true)
                {
                    warning_label.ForeColor = Color.Red;
                    warning_label.Text = "Manifest download failed!";
                    warning_label.Refresh();
                }
                Console.WriteLine(ex.StackTrace);
            }
            
        }

        private void DoGameUpdateCheck()
        {
            try
            {
                string localVersion = File.ReadAllText(String.Format(@"{0}\gameVersion.txt", Config.GetGamePath()));
                string versionURL = String.Format("{0}/game/gameVersion.txt", Config.GetFTPUrl());
                string remoteVersion = FTP.ReadFTPFile(Config.GetFTPUsername(), Config.GetFTPPassword(), versionURL).Replace("\0", string.Empty);

                if (!(localVersion == remoteVersion))
                {
                    bGameNeedsUpdate = true;
                    Console.Write("localGameVersion: ");
                    Console.Write(localVersion);

                    Console.Write("remoteGameVersion: ");
                    Console.Write(remoteVersion);
                    File.Create(String.Format(@"{0}\.gameNeedsUpdate", Config.GetLocalDir()));
                }

                if (File.Exists(String.Format(@"{0}\.gameNeedsUpdate", Config.GetLocalDir())))
                {
                    bGameNeedsUpdate = true;
                }
                Console.Write("localVersion: ");
                Console.WriteLine(localVersion);

                Console.Write("remoteVersion: ");
                Console.WriteLine(remoteVersion);
                UpdateMainWindow();
            }
            catch (Exception ex)
            {
                Console.Write("DoGameUpdateCheck: ");
                Console.WriteLine(ex.StackTrace);
            }
        }

        private void DoGameIsInstalledCheck()
        {
            if (File.Exists(String.Format(@"{0}\game\{1}\Saved\Config\WindowsNoEditor\Scalability.ini", Config.GetLocalDir(), Config.GetGameName())))
            {
                bGameIsProbablyInstalled = true;
                bInstallCompleted = true;

                UpdateMainWindow();
            }
            else
            {
                bGameIsProbablyInstalled = false;
                bInstallCompleted = true;

                UpdateMainWindow();
            }
        }

        private void UpdateMainWindow()
        {                     
            if (bLauncherNeedsUpdate)
            {
                mainPanel_mainButton.Text = "Update Launcher";
                mainPanel_mainButton.Refresh();
            }
            else if (bGameIsProbablyInstalled == false || bInstallCompleted == false)
            {
                mainPanel_mainButton.Text = "Install";
                mainPanel_mainButton.Refresh();
            }
            else if (bGameNeedsUpdate)
            {
                mainPanel_mainButton.Text = "Update Game";
                mainPanel_mainButton.Refresh();
            }
            else
            {
                mainPanel_mainButton.Text = "Play";
                mainPanel_mainButton.Refresh();
            }
        }                      

        private void exit_button_Click(object sender, EventArgs e)
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
            FTP.DownloadFTPFile(Config.GetFTPUsername(), Config.GetFTPPassword(), Config.GetLauncherURL(), String.Format(@"{0}\Launchpad.exe", Config.GetTempDir()));

            FileStream updateScript = File.Create(String.Format(@"{0}\update.bat", Config.GetLocalDir()));

            TextWriter tw = new StreamWriter(updateScript);
            tw.WriteLine(String.Format(@"timeout 3 & xcopy /s /y ""{0}Launchpad.exe"" ""{1}\Launchpad.exe"" && del ""{0}Launchpad.exe""", Config.GetTempDir(), Config.GetLocalDir()));
            tw.WriteLine(String.Format(@"start Launchpad.exe"));
            tw.Close();

            ProcessStartInfo updateBatchProcess = new ProcessStartInfo();

            updateBatchProcess.FileName = String.Format(@"{0}\update.bat", Config.GetLocalDir());
            updateBatchProcess.UseShellExecute = true;
            updateBatchProcess.RedirectStandardOutput = false;
            updateBatchProcess.WindowStyle = ProcessWindowStyle.Hidden;

            Process.Start(updateBatchProcess);

            Environment.Exit(0); 
        }

        private void mainPanel_mainButton_Click(object sender, EventArgs e)
        {
            if (bLauncherNeedsUpdate)
            {
                DoLauncherUpdate();
            }
            else if (bGameIsProbablyInstalled == false)
            {
                //run the game installation in the background
                backgroundWorker_GameInstall.RunWorkerAsync();
            }
            else if (bGameNeedsUpdate)
            {
                //run game update in the background
                backgroundWorker_GameUpdate.RunWorkerAsync();
            }
            else
            {
                ProcessStartInfo gameProcess = new ProcessStartInfo();

                gameProcess.FileName = Config.GetGameExecutable();
                gameProcess.UseShellExecute = true;
                Process.Start(gameProcess);
            }
        }

        private void backgroundWorker_GameInstall_DoWork(object sender, DoWorkEventArgs e)
        {
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
                DialogResult result = MessageBox.Show("Could not read launcher manifest! Unable to install.", "IOException", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Console.Write("GameInstallIOException: ");
                Console.WriteLine(ex.StackTrace);
            }
            catch (Exception ex)
            {                
                Console.Write("GameInstallException: ");
                Console.WriteLine(ex.StackTrace);
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
            bInstallCompleted = true;

            progress_label.ForeColor = Color.ForestGreen;
            progress_label.Text = "Game install finished!";

            UpdateMainWindow();
        }

        private void backgroundWorker_GameUpdate_DoWork(object sender, DoWorkEventArgs e)
        {
            string[] manifestFilesArray = File.ReadAllLines(Config.GetManifestPath());

            int i = 0;

            foreach (string value in manifestFilesArray)
            {
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
                        Console.Write("GameUpdateWorker - FileUpdate: ");
                        Console.WriteLine(localPath);
                        i++;
                        backgroundWorker_GameUpdate.ReportProgress(i, new Tuple<string, int>(manifestFile[0], manifestFilesArray.Length));
                    }
                    catch (Exception ex)
                    {
                        Console.Write("GameUpdateWorker - NoFile: ");
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
                            FTP.DownloadFTPFile(Config.GetFTPUsername(), Config.GetFTPPassword(), FTPPath, localPath);
                            Console.Write("GameUpdateWorker - MD5Update: ");
                            Console.WriteLine(localPath);
                            localFile.Close();
                            i++;
                            backgroundWorker_GameUpdate.ReportProgress(i, new Tuple<string, int>(manifestFile[0], manifestFilesArray.Length));
                        }
                        //if they match, move on
                        Console.Write("GameUpdateWorker - AllOK: ");
                        Console.WriteLine(filePath);
                        localFile.Close();
                        i++;
                        backgroundWorker_GameUpdate.ReportProgress(i, new Tuple<string, int>(manifestFile[0], manifestFilesArray.Length));
                    }
                    catch (Exception ex)
                    {
                        Console.Write("GameUpdateWorker - MD5: ");
                        Console.WriteLine(ex.StackTrace);
                    }                                        
                }                                                
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
            UpdateMainWindow();
        }

        private void verifyInstallation_button_Click(object sender, EventArgs e)
        {
            //verifying is basically the same as updating. Check all files, download replacements, etc
            backgroundWorker_GameUpdate.RunWorkerAsync();
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            AboutBox1 a = new AboutBox1();
            a.Show();
        }
    }
}
