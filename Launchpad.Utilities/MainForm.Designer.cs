namespace Launchpad.Utilities
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
            this.generateManifest_button = new System.Windows.Forms.Button();
            this.utilTools_progressBar = new System.Windows.Forms.ProgressBar();
            this.label1 = new System.Windows.Forms.Label();
            this.fileProgress_label = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.currentFile_label = new System.Windows.Forms.Label();
            this.folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
            this.generateSingle_button = new System.Windows.Forms.Button();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.label3 = new System.Windows.Forms.Label();
            this.md5Result_Textbox = new System.Windows.Forms.TextBox();
            this.backgroundWorker_manifestGenerator = new System.ComponentModel.BackgroundWorker();
            this.SuspendLayout();
            // 
            // generateManifest_button
            // 
            this.generateManifest_button.Location = new System.Drawing.Point(13, 13);
            this.generateManifest_button.Name = "generateManifest_button";
            this.generateManifest_button.Size = new System.Drawing.Size(249, 35);
            this.generateManifest_button.TabIndex = 0;
            this.generateManifest_button.Text = "Generate Launcher Manifest";
            this.generateManifest_button.UseVisualStyleBackColor = true;
            this.generateManifest_button.Click += new System.EventHandler(this.generateManifest_button_Click);
            // 
            // utilTools_progressBar
            // 
            this.utilTools_progressBar.Location = new System.Drawing.Point(13, 352);
            this.utilTools_progressBar.Name = "utilTools_progressBar";
            this.utilTools_progressBar.Size = new System.Drawing.Size(249, 23);
            this.utilTools_progressBar.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 101);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(84, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Completed files: ";
            // 
            // fileProgress_label
            // 
            this.fileProgress_label.AutoSize = true;
            this.fileProgress_label.Location = new System.Drawing.Point(102, 101);
            this.fileProgress_label.Name = "fileProgress_label";
            this.fileProgress_label.Size = new System.Drawing.Size(24, 13);
            this.fileProgress_label.TabIndex = 4;
            this.fileProgress_label.Text = "0/0";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 126);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(60, 13);
            this.label2.TabIndex = 5;
            this.label2.Text = "Current file:";
            // 
            // currentFile_label
            // 
            this.currentFile_label.AutoSize = true;
            this.currentFile_label.Location = new System.Drawing.Point(12, 139);
            this.currentFile_label.Name = "currentFile_label";
            this.currentFile_label.Size = new System.Drawing.Size(0, 13);
            this.currentFile_label.TabIndex = 6;
            // 
            // generateSingle_button
            // 
            this.generateSingle_button.Location = new System.Drawing.Point(12, 54);
            this.generateSingle_button.Name = "generateSingle_button";
            this.generateSingle_button.Size = new System.Drawing.Size(250, 35);
            this.generateSingle_button.TabIndex = 7;
            this.generateSingle_button.Text = "Generate Single MD5 Hash";
            this.generateSingle_button.UseVisualStyleBackColor = true;
            this.generateSingle_button.Click += new System.EventHandler(this.generateSingle_button_Click);
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(11, 169);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(33, 13);
            this.label3.TabIndex = 8;
            this.label3.Text = "MD5:";
            // 
            // md5Result_Textbox
            // 
            this.md5Result_Textbox.Location = new System.Drawing.Point(50, 169);
            this.md5Result_Textbox.Name = "md5Result_Textbox";
            this.md5Result_Textbox.ReadOnly = true;
            this.md5Result_Textbox.Size = new System.Drawing.Size(212, 20);
            this.md5Result_Textbox.TabIndex = 9;
            // 
            // backgroundWorker_manifestGenerator
            // 
            this.backgroundWorker_manifestGenerator.WorkerReportsProgress = true;
            this.backgroundWorker_manifestGenerator.DoWork += new System.ComponentModel.DoWorkEventHandler(this.backgroundWorker_manifestGenerator_DoWork);
            this.backgroundWorker_manifestGenerator.ProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(this.backgroundWorker_manifestGenerator_ProgressChanged);
            this.backgroundWorker_manifestGenerator.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.backgroundWorker_manifestGenerator_RunWorkerCompleted);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(274, 387);
            this.Controls.Add(this.md5Result_Textbox);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.generateSingle_button);
            this.Controls.Add(this.currentFile_label);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.fileProgress_label);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.utilTools_progressBar);
            this.Controls.Add(this.generateManifest_button);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "MainForm";
            this.Text = "Launchpad - Utilites";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button generateManifest_button;
        private System.Windows.Forms.ProgressBar utilTools_progressBar;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label fileProgress_label;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label currentFile_label;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog1;
        private System.Windows.Forms.Button generateSingle_button;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox md5Result_Textbox;
        private System.ComponentModel.BackgroundWorker backgroundWorker_manifestGenerator;
    }
}

