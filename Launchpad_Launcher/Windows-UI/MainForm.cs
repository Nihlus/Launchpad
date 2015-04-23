using System;
using System.IO;
using System.Windows.Forms;

namespace Launchpad
{
    internal partial class MainForm : Form
    {
        /// <summary>
        /// Does the launcher need an update?
        /// </summary>
        bool bLauncherNeedsUpdate = false;

        /// <summary>
        /// The checks handler reference.
        /// </summary>
        ChecksHandler Checks = new ChecksHandler();

        /// <summary>
        /// The config handler reference.
        /// </summary>
        ConfigHandler Config = ConfigHandler._instance;

        /// <summary>
        /// The launcher handler. Allows updating the launcher and loading the changelog
        /// </summary>
        LauncherHandler Launcher = new LauncherHandler();

        /// <summary>
        /// The game handler. Allows updating, installing and repairing the game.
        /// </summary>
        GameHandler Game = new GameHandler();

        public MainForm()
        {
            InitializeComponent();

            Config.Initialize();
            MessageLabel.Text = "Idle";
            downloadProgressLabel.Text = String.Empty;

            //set the window text to match the game name
            this.Text = "Launchpad - " + Config.GetGameName();

            //first of all, check if we can connect to the FTP server.
            if (!Checks.CanConnectToFTP())
            {
                MessageBox.Show(
                    this,
                    "Failed to connect to the FTP server. Please check your FTP settings.",
                    "Connection Failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error,
                    MessageBoxDefaultButton.Button1);

                MessageLabel.Text = "No FTP connection.";
                PrimaryButton.Text = ":(";
                PrimaryButton.Enabled = false;
            }
            else
            {
                //if we can connect, proceed with the rest of our checks.                
                if (ChecksHandler.IsInitialStartup())
                {
                    DialogResult shouldInstallHere = MessageBox.Show(
                        this,
                        String.Format(
                        "This appears to be the first time you're starting the launcher.\n" +
                        "Is this the location where you would like to install the game?" +
                        "\n\n{0}", ConfigHandler.GetLocalDir()),
                        "Initial Startup",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question,
                        MessageBoxDefaultButton.Button1);

                    if (shouldInstallHere == DialogResult.Yes)
                    {
                        //yes, install here
                        Console.WriteLine("Installing in current directory.");
                        ConfigHandler.CreateUpdateCookie();
                    }
                    else
                    {
                        //no, don't install here
                        Console.WriteLine("Exiting...");
                        Environment.Exit(2);
                    }
                }

                //this section sends some anonymous usage stats back home. If you don't want to do this for your game, simply change this boolean to false.
                bool bSendAnonStats = false;
                if (bSendAnonStats)
                {
                    StatsHandler.SendUsageStats();
                }
                else
                {
                    Stream iconStream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("Launchpad.Resources.RocketIcon.ico");
                    if (iconStream != null)
                    {
                        NotifyIcon noUsageStatsNotification = new NotifyIcon();
                        noUsageStatsNotification.Icon = new System.Drawing.Icon(iconStream);
                        noUsageStatsNotification.Visible = true;

                        noUsageStatsNotification.BalloonTipTitle = "Usage Stats";
                        noUsageStatsNotification.BalloonTipText = "Not sending anonymous usage stats. TURN THIS ON BEFORE RELEASING.";

                        noUsageStatsNotification.ShowBalloonTip(10000);
                    }   
                }

                if(Checks.IsLauncherOutdated())
                {
                    Console.WriteLine("Launcher is outdated.");
                    PrimaryButton.Enabled = true;
                    PrimaryButton.Text = "Update";
                    bLauncherNeedsUpdate = true;
                }

                Launcher.ChangelogDownloadFinished += OnChangelogDownloadFinished;
                Launcher.LoadChangelog();

                if (!bLauncherNeedsUpdate)
                {
                    if (Checks.IsManifestOutdated())
                    {
                        Launcher.DownloadManifest();
                    }

                    if(!Checks.IsGameInstalled())
                    {
                        Console.WriteLine("Game is not installed.");
                        PrimaryButton.Enabled = true;
                        PrimaryButton.Text = "Install";
                    }
                    else
                    {
                        if (Checks.IsGameOutdated())
                        {
                            Console.WriteLine("Game is outdated or not installed.");
                            PrimaryButton.Enabled = true;
                            PrimaryButton.Text = "Update";
                        }
                        else
                        {
                            PrimaryButton.Enabled = true;
                            PrimaryButton.Text = "Launch";
                        }
                    }
                }
            }
            //this is after the CanConnect check. Nothing should be done here.
        }		      

        /// <summary>
        /// Handles switching between different functionalities depending on what is visible on the button to the user, such as
        /// * Installing
        /// * Updating
        /// * Repairing
        /// * Launching
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">Empty arguments.</param>
        private void mainButton_Click(object sender, EventArgs e)
        {
            string Mode = PrimaryButton.Text;
            switch (Mode)
            {
                case "Repair":
                    {
                        Console.WriteLine("Repairing installation...");
                        //bind events for UI updating					
                        Game.ProgressChanged += OnGameDownloadProgressChanged;
                        Game.GameRepairFinished += OnRepairFinished;
                        Game.GameDownloadFailed += OnGameDownloadFailed;

                        if (Checks.DoesServerProvidePlatform(Config.GetSystemTarget()))
                        {
                            //install the game asynchronously
                            Game.RepairGame();
                        }
                        else
                        {
                            Stream iconStream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("Launchpad.Resources.RocketIcon.ico");
                            if (iconStream != null)
                            {
                                NotifyIcon launchFailedNotification = new NotifyIcon();
                                launchFailedNotification.Icon = new System.Drawing.Icon(iconStream);
                                launchFailedNotification.Visible = true;

                                launchFailedNotification.BalloonTipTitle = "Platform not provided";
                                launchFailedNotification.BalloonTipText = "The server does not provide the game for the selected platform.";

                                launchFailedNotification.ShowBalloonTip(10000);
                            } 

                            PrimaryButton.Text = "Install";
                            PrimaryButton.Enabled = true;
                        }

                        break;
                    }
                case "Install":
                    {
                        Console.WriteLine("Installing game...");

                        MessageLabel.Text = "Installing...";
                        PrimaryButton.Text = "Installing...";
                        PrimaryButton.Enabled = false;

                        //bind events for UI updating
                        Game.GameDownloadFinished += OnGameDownloadFinished;
                        Game.ProgressChanged += OnGameDownloadProgressChanged;
                        Game.GameDownloadFailed += OnGameDownloadFailed;

                        //check for a .provides file in the platform directory on the server
                        //if there is none, the server does not provide a game for that platform
                        if (Checks.DoesServerProvidePlatform(Config.GetSystemTarget()))
                        {
                            //install the game asynchronously
                            Game.InstallGame();
                        }
                        else
                        {
                            Stream iconStream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("Launchpad.Resources.RocketIcon.ico");
                            if (iconStream != null)
                            {
                                NotifyIcon launchFailedNotification = new NotifyIcon();
                                launchFailedNotification.Icon = new System.Drawing.Icon(iconStream);
                                launchFailedNotification.Visible = true;

                                launchFailedNotification.BalloonTipTitle = "Platform not provided";
                                launchFailedNotification.BalloonTipText = "The server does not provide the game for the selected platform.";

                                launchFailedNotification.ShowBalloonTip(10000);
                            }

                            MessageLabel.Text = "Server does not provide the game for the selected platform.";

                            PrimaryButton.Text = "Install";
                            PrimaryButton.Enabled = true;
                        }   

                        break;
                    }
                case "Update":
                    {
                        Console.WriteLine("Updating game...");
                        PrimaryButton.Text = "Updating...";
                        PrimaryButton.Enabled = false;

                        //bind events for UI updating
                        Game.GameDownloadFinished += OnGameDownloadFinished;
                        Game.ProgressChanged += OnGameDownloadProgressChanged;
                        Game.GameDownloadFailed += OnGameDownloadFailed;

                        //update the game asynchronously
                        if (Checks.DoesServerProvidePlatform(Config.GetSystemTarget()))
                        {
                            //install the game asynchronously
                            Game.UpdateGame();
                        }
                        else
                        {
                            Stream iconStream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("Launchpad.Resources.RocketIcon.ico");
                            if (iconStream != null)
                            {
                                NotifyIcon launchFailedNotification = new NotifyIcon();
                                launchFailedNotification.Icon = new System.Drawing.Icon(iconStream);
                                launchFailedNotification.Visible = true;

                                launchFailedNotification.BalloonTipTitle = "Platform not provided";
                                launchFailedNotification.BalloonTipText = "The server does not provide the game for the selected platform.";

                                launchFailedNotification.ShowBalloonTip(10000);
                            } 

                            PrimaryButton.Text = "Install";
                            PrimaryButton.Enabled = true;
                        }

                        break;
                    }
                case "Launch":
                    {
                        Console.WriteLine("Launching game...");
                        Game.GameLaunchFailed += OnGameLaunchFailed;
                        Game.LaunchGame();

                        break;
                    }
                default:
                    {
                        Console.WriteLine("No functionality for this mode.");

                        break;
                    }
            }
        }

        /// <summary>
        /// Updates the web browser with the asynchronously loaded changelog from the server.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The arguments containing the HTML from the server.</param>
        private void OnChangelogDownloadFinished(object sender, GameDownloadFinishedEventArgs e)
        {
            changelogBrowser.DocumentText = e.Result;
            changelogBrowser.Refresh();         
        }

        /// <summary>
        /// Warns the user when the game fails to launch, and offers to attempt a repair.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">Empty event args.</param>
        private void OnGameLaunchFailed(object sender, EventArgs e)
        {
            this.Invoke((MethodInvoker)delegate
            {
                Stream iconStream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("Launchpad.Resources.RocketIcon.ico");
                if (iconStream != null)
                {
                    NotifyIcon launchFailedNotification = new NotifyIcon();
                    launchFailedNotification.Icon = new System.Drawing.Icon(iconStream);
                    launchFailedNotification.Visible = true;

                    launchFailedNotification.BalloonTipTitle = "Launch Failed";
                    launchFailedNotification.BalloonTipText = "The game failed to launch. Try repairing the installation.";

                    launchFailedNotification.ShowBalloonTip(10000);
                }

                PrimaryButton.Text = "Repair";
                PrimaryButton.Enabled = true;
            });
        }

        /// <summary>
        /// Provides alternatives when the game fails to download, either through an update or through an installation.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">Contains the type of failure that occurred.</param>
        private void OnGameDownloadFailed(object sender, GameDownloadFailedEventArgs e)
        {
            this.Invoke((MethodInvoker)delegate
            {
                switch (e.ResultType)
                {
                    case "Install":
                        {
                            Console.WriteLine(e.Metadata);
                            MessageLabel.Text = e.Metadata;
                            break;
                        }
                    case "Update":
                        {
                            Console.WriteLine(e.Metadata);
                            MessageLabel.Text = e.Metadata;
                            break;
                        }
                    case "Repair":
                        {
                            Console.WriteLine(e.Metadata);
                            MessageLabel.Text = e.Metadata;
                            break;
                        }
                }

                PrimaryButton.Text = e.ResultType;
                PrimaryButton.Enabled = true; 
            });                                   
        }

        /// <summary>
        /// Updates the progress bar and progress label during installations, repairs and updates.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">Contains the progress values and current filename.</param>
        private void OnGameDownloadProgressChanged(object sender, FileDownloadProgressChangedEventArgs e)
        {
            this.Invoke((MethodInvoker)delegate
            {
                if (!String.IsNullOrEmpty(e.FileName))
                {
                    string progressbarText = String.Format("Downloading file {0}: {1} of {2} bytes.",
                                                        System.IO.Path.GetFileNameWithoutExtension(e.FileName),
                                                        e.DownloadedBytes.ToString(),
                                                        e.TotalBytes.ToString());

                    downloadProgressLabel.Text = progressbarText;

                    mainProgressBar.Minimum = 0;
                    mainProgressBar.Maximum = 10000;
                    
                    if (e.DownloadedBytes > 0 && e.TotalBytes > 0)
                    {
                        double fraction = ((double)e.DownloadedBytes / (double)e.TotalBytes) * 10000;

                        mainProgressBar.Value = (int)fraction;
                        mainProgressBar.Update();
                    }                    
                }                
            });                      
        }

        /// <summary>
        /// Allows the user to launch or repair the game once installation finishes.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">Contains the result of the download.</param>
        protected void OnGameDownloadFinished(object sender, GameDownloadFinishedEventArgs e)
        {
            this.Invoke((MethodInvoker)delegate
            {
                if (e.Result == "1") //there was an error
                {
                    MessageLabel.Text = "Game download failed. Are you missing the manifest?";

                    Stream iconStream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("Launchpad.Resources.RocketIcon.ico");
                    if (iconStream != null)
                    {
                        NotifyIcon launchFailedNotification = new NotifyIcon();
                        launchFailedNotification.Icon = new System.Drawing.Icon(iconStream);
                        launchFailedNotification.Visible = true;

                        launchFailedNotification.BalloonTipTitle = "Download Failed";
                        launchFailedNotification.BalloonTipText = "The game failed to install. Are you missing the manifest?";

                        launchFailedNotification.ShowBalloonTip(10000);
                    }

                    PrimaryButton.Text = e.ResultType; //URL is used here to set the desired retry action
                    PrimaryButton.Enabled = true;
                }
                else //the game has finished downloading, and we should be OK to launch
                {
                    MessageLabel.Text = "Idle";
                    downloadProgressLabel.Text = "";

                    Stream iconStream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("Launchpad.Resources.RocketIcon.ico");
                    if (iconStream != null)
                    {
                        NotifyIcon launchFailedNotification = new NotifyIcon();
                        launchFailedNotification.Icon = new System.Drawing.Icon(iconStream);
                        launchFailedNotification.Visible = true;

                        launchFailedNotification.BalloonTipTitle = "Installation complete";
                        launchFailedNotification.BalloonTipText = "Game download finished. Play away!";

                        launchFailedNotification.ShowBalloonTip(10000);
                    }

                    PrimaryButton.Text = "Launch";
                    PrimaryButton.Enabled = true;
                }             
            });            
        }

        /// <summary>
        /// Alerts the user that a repair action has finished.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">Empty arguments.</param>
        private void OnRepairFinished(object sender, EventArgs e)
        {
            this.Invoke((MethodInvoker)delegate
            {
                Stream iconStream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("Launchpad.Resources.RocketIcon.ico");
                if (iconStream != null)
                {
                    NotifyIcon launchFailedNotification = new NotifyIcon();
                    launchFailedNotification.Icon = new System.Drawing.Icon(iconStream);
                    launchFailedNotification.Visible = true;

                    launchFailedNotification.BalloonTipTitle = "IGame repair finished";
                    launchFailedNotification.BalloonTipText = "Launchpad has finished repairing the game installation. Play away!";

                    launchFailedNotification.ShowBalloonTip(10000);
                }

                downloadProgressLabel.Text = "";

                PrimaryButton.Text = "Launch";
                PrimaryButton.Enabled = true; 
            });                       
        }

        private void aboutLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            LaunchpadAboutBox about = new LaunchpadAboutBox();
            about.ShowDialog();
        }
    }
}
