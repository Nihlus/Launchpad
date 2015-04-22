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
    public partial class MainForm : Form
    {
		//set up Handler references
		MD5Handler md5 = new MD5Handler();
		ConfigHandler Config = ConfigHandler._instance;
		FTPHandler FTP = new FTPHandler();

        public MainForm()
        {
            InitializeComponent();

            //set the window text to match the game name
            this.Text = "Launchpad - " + Config.GetGameName();

            //this section sends some anonymous usage stats back home. If you don't want to do this for your game, simply change this boolean to false.
            bool bSendAnonStats = true;
            if (bSendAnonStats)
            {
				StatsHandler.SendUseageStats ();
            }
        }		      

        private void Form1_Load(object sender, EventArgs e)
        {
            
        }

        private void Form1_Shown(object sender, EventArgs e)
        {

        }

        private void mainButton_Click(object sender, EventArgs e)
        {

        }
    }
}
