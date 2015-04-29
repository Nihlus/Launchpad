namespace Launchpad
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.changelogBrowser = new System.Windows.Forms.WebBrowser();
            this.mainProgressBar = new System.Windows.Forms.ProgressBar();
            this.MessageLabel = new System.Windows.Forms.Label();
            this.downloadProgressLabel = new System.Windows.Forms.Label();
            this.aboutLink = new System.Windows.Forms.LinkLabel();
            this.PrimaryButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // changelogBrowser
            // 
            this.changelogBrowser.Location = new System.Drawing.Point(18, 18);
            this.changelogBrowser.MinimumSize = new System.Drawing.Size(20, 20);
            this.changelogBrowser.Name = "changelogBrowser";
            this.changelogBrowser.ScrollBarsEnabled = false;
            this.changelogBrowser.Size = new System.Drawing.Size(390, 400);
            this.changelogBrowser.TabIndex = 0;
            // 
            // mainProgressBar
            // 
            this.mainProgressBar.Location = new System.Drawing.Point(18, 460);
            this.mainProgressBar.Name = "mainProgressBar";
            this.mainProgressBar.Size = new System.Drawing.Size(390, 23);
            this.mainProgressBar.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            this.mainProgressBar.TabIndex = 2;
            // 
            // MessageLabel
            // 
            this.MessageLabel.AutoSize = true;
            this.MessageLabel.BackColor = System.Drawing.Color.Transparent;
            this.MessageLabel.Location = new System.Drawing.Point(15, 444);
            this.MessageLabel.Name = "MessageLabel";
            this.MessageLabel.Size = new System.Drawing.Size(24, 13);
            this.MessageLabel.TabIndex = 5;
            this.MessageLabel.Text = Catalog.GetString("idleString");
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
            this.aboutLink.Location = new System.Drawing.Point(414, 18);
            this.aboutLink.Name = "aboutLink";
            this.aboutLink.Size = new System.Drawing.Size(35, 13);
            this.aboutLink.TabIndex = 10;
            this.aboutLink.TabStop = true;
            this.aboutLink.Text = "About";
            this.aboutLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.aboutLink_LinkClicked);
            // 
            // PrimaryButton
            // 
            this.PrimaryButton.Location = new System.Drawing.Point(739, 460);
            this.PrimaryButton.Name = "PrimaryButton";
            this.PrimaryButton.Size = new System.Drawing.Size(104, 39);
            this.PrimaryButton.TabIndex = 11;
            this.PrimaryButton.Text = "PrimaryButton";
            this.PrimaryButton.UseVisualStyleBackColor = true;
            this.PrimaryButton.Click += new System.EventHandler(this.mainButton_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.BackgroundImage = global::Launchpad.Properties.Resources.Background_hexagon_textbox;
            this.ClientSize = new System.Drawing.Size(855, 512);
            this.Controls.Add(this.PrimaryButton);
            this.Controls.Add(this.aboutLink);
            this.Controls.Add(this.downloadProgressLabel);
            this.Controls.Add(this.MessageLabel);
            this.Controls.Add(this.mainProgressBar);
            this.Controls.Add(this.changelogBrowser);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "MainForm";
            this.Text = "Launchpad - <GameName>";
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
    }
}

