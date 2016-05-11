namespace Launchpad.Launcher
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.changelogBrowser = new System.Windows.Forms.WebBrowser();
            this.mainProgressBar = new System.Windows.Forms.ProgressBar();
            this.MessageLabel = new System.Windows.Forms.Label();
            this.downloadProgressLabel = new System.Windows.Forms.Label();
            this.aboutLink = new System.Windows.Forms.LinkLabel();
            this.PrimaryButton = new System.Windows.Forms.Button();
            this.imageList1 = new System.Windows.Forms.ImageList(this.components);
            this.SuspendLayout();
            // 
            // changelogBrowser
            // 
            this.changelogBrowser.Location = new System.Drawing.Point(500, 236);
            this.changelogBrowser.MinimumSize = new System.Drawing.Size(20, 20);
            this.changelogBrowser.Name = "changelogBrowser";
            this.changelogBrowser.ScrollBarsEnabled = false;
            this.changelogBrowser.Size = new System.Drawing.Size(389, 208);
            this.changelogBrowser.TabIndex = 0;
            // 
            // mainProgressBar
            // 
            this.mainProgressBar.Location = new System.Drawing.Point(12, 457);
            this.mainProgressBar.Name = "mainProgressBar";
            this.mainProgressBar.Size = new System.Drawing.Size(390, 23);
            this.mainProgressBar.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            this.mainProgressBar.TabIndex = 2;
            // 
            // MessageLabel
            // 
            this.MessageLabel.AutoSize = true;
            this.MessageLabel.BackColor = System.Drawing.Color.Transparent;
            this.MessageLabel.Location = new System.Drawing.Point(9, 431);
            this.MessageLabel.Name = "MessageLabel";
            this.MessageLabel.Size = new System.Drawing.Size(24, 13);
            this.MessageLabel.TabIndex = 5;
            this.MessageLabel.Text = "Idle";
            // 
            // downloadProgressLabel
            // 
            this.downloadProgressLabel.AutoSize = true;
            this.downloadProgressLabel.BackColor = System.Drawing.Color.Transparent;
            this.downloadProgressLabel.Location = new System.Drawing.Point(15, 431);
            this.downloadProgressLabel.Name = "downloadProgressLabel";
            this.downloadProgressLabel.Size = new System.Drawing.Size(0, 13);
            this.downloadProgressLabel.TabIndex = 7;
            // 
            // aboutLink
            // 
            this.aboutLink.AutoSize = true;
            this.aboutLink.BackColor = System.Drawing.Color.Transparent;
            this.aboutLink.Location = new System.Drawing.Point(9, 487);
            this.aboutLink.Name = "aboutLink";
            this.aboutLink.Size = new System.Drawing.Size(35, 13);
            this.aboutLink.TabIndex = 10;
            this.aboutLink.TabStop = true;
            this.aboutLink.Text = "About";
            this.aboutLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.aboutLink_LinkClicked);
            // 
            // PrimaryButton
            // 
            this.PrimaryButton.Location = new System.Drawing.Point(785, 457);
            this.PrimaryButton.Name = "PrimaryButton";
            this.PrimaryButton.Size = new System.Drawing.Size(104, 39);
            this.PrimaryButton.TabIndex = 11;
            this.PrimaryButton.Text = "PrimaryButton";
            this.PrimaryButton.UseVisualStyleBackColor = true;
            this.PrimaryButton.Click += new System.EventHandler(this.mainButton_Click);
            // 
            // imageList1
            // 
            this.imageList1.ColorDepth = System.Windows.Forms.ColorDepth.Depth8Bit;
            this.imageList1.ImageSize = new System.Drawing.Size(16, 16);
            this.imageList1.TransparentColor = System.Drawing.Color.Transparent;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ControlDark;
            this.BackgroundImage = global::Launchpad.Launcher.Properties.Resources.launcher_background1;
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.ClientSize = new System.Drawing.Size(901, 508);
            this.Controls.Add(this.PrimaryButton);
            this.Controls.Add(this.aboutLink);
            this.Controls.Add(this.downloadProgressLabel);
            this.Controls.Add(this.MessageLabel);
            this.Controls.Add(this.mainProgressBar);
            this.Controls.Add(this.changelogBrowser);
            this.DoubleBuffered = true;
            this.ForeColor = System.Drawing.Color.FloralWhite;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "MainForm";
            this.Text = "Soverance - <GameName>";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.WebBrowser changelogBrowser;
        private System.Windows.Forms.ProgressBar mainProgressBar;
        private System.Windows.Forms.Label MessageLabel;
        private System.Windows.Forms.Label downloadProgressLabel;
        private System.Windows.Forms.LinkLabel aboutLink;
        private System.Windows.Forms.Button PrimaryButton;
        private System.Windows.Forms.ImageList imageList1;
    }
}

