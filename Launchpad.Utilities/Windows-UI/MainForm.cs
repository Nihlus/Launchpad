using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Security.Cryptography;
using System.Windows.Forms;

namespace Launchpad.Utilities
{
    public partial class MainForm : Form
    {
        bool bIsGeneratingManifest = false;

        public MainForm()
        {
            InitializeComponent();
        }
        
        /// <summary>
        /// Computes the MD5 hash for the specified file stream.
        /// </summary>
        /// <param name="fileStream"></param>
        /// <returns>The MD5 hash.</returns>
        private static string GetFileHash(Stream fileStream)
        {
            try
            {
                using (MD5 md5 = MD5.Create())
                {
                    //we got a valid file, calculate the MD5 and return it
                    string hash = BitConverter.ToString(md5.ComputeHash(fileStream)).Replace("-", "");
                    return hash;
                }
            }
            catch (IOException iex)
            {
                Console.WriteLine("IOException in GetFileHash(): " + iex.Message);                
                return null;
            }
        }

        private void generateManifest_button_Click(object sender, EventArgs e)
        {
            if ( bIsGeneratingManifest == false)
            {
                DialogResult folderSelectionResult = folderBrowserDialog1.ShowDialog();
                backgroundWorker_manifestGenerator.RunWorkerAsync(folderSelectionResult);
            }            
        }

        private static string GenerateSingleHash(string filePath)
        {
            if (File.Exists(filePath))
            {
                Stream selectedFile = null;
                try
                {
                    selectedFile = File.OpenRead(filePath);

                    string hash = GetFileHash(selectedFile);

                    return hash;
                }
                catch (IOException iex)
                {
                    Console.WriteLine("IOException in GenerateSingle(): " + iex.Message);
                    return null;
                }
                catch (UnauthorizedAccessException uex)
                {
                    Console.WriteLine("UnauthorizedAccessException in GenerateSingle(): " + uex.Message);
                    return null;
                }
                finally
                {
                    if (selectedFile != null)
                    {
                        selectedFile.Close();
                    }
                }
            }
            else
            {
                return null;
            }
        }

        private void generateSingle_button_Click(object sender, EventArgs e)
        {
            string filePath = openFileDialog1.FileName;
            string hash = GenerateSingleHash(filePath);

            if (!String.IsNullOrEmpty(hash))
            {
                md5Result_Textbox.Text = hash;
            }                       
        }

        private void backgroundWorker_manifestGenerator_DoWork(object sender, DoWorkEventArgs e)
        {
            bIsGeneratingManifest = true;
            DialogResult result = (DialogResult)e.Argument;

            if (result == DialogResult.OK)
            {
                string parentDirectory = Directory.GetParent(folderBrowserDialog1.SelectedPath).ToString();
                string manifestPath = String.Format(@"{0}{1}LauncherManifest.txt", parentDirectory, Path.DirectorySeparatorChar);
                string manifestChecksumPath = String.Format(@"{0}{1}LauncherManifest.checksum", parentDirectory, Path.DirectorySeparatorChar);


                if (File.Exists(manifestPath))
                {
                    //create a new empty file and close it (effectively deleting the old manifest)
                    File.Create(manifestPath).Close();
                }

                TextWriter tw = new StreamWriter(manifestPath);

                string[] files = Directory.GetFiles(folderBrowserDialog1.SelectedPath, "*", SearchOption.AllDirectories);                
                int completedFiles = 0;
                
                IEnumerable<string> enumeratedFiles = Directory
                                                    .EnumerateFiles(folderBrowserDialog1.SelectedPath, "*", SearchOption.AllDirectories);

                foreach (string file in enumeratedFiles)
                {
                    if (file != null)
                    {
                        FileStream fileStream = File.OpenRead(file);
                        var skipDirectory = folderBrowserDialog1.SelectedPath;

                        int fileAmount = files.Length; 
                        string currentFile = file.Substring(skipDirectory.Length);

                        //get file size on disk
                        FileInfo Info = new FileInfo(file);
                        string fileSize = Info.Length.ToString();
                        
                        string manifestLine = String.Format(@"{0}:{1}:{2}", file.Substring(skipDirectory.Length), GetFileHash(fileStream), fileSize);
                        fileStream.Close();

                        completedFiles++;
                        

                        tw.WriteLine(manifestLine);
                        backgroundWorker_manifestGenerator.ReportProgress(completedFiles, new Tuple<int, int, string>(fileAmount, completedFiles, currentFile));
                    }
                }
                tw.Close();


                //create a manifest checksum file.
                string manifestHash = GetFileHash(File.OpenRead(manifestPath));
                
                FileStream checksumStream = File.Create(manifestChecksumPath);

                TextWriter tw2 = new StreamWriter(checksumStream);
                tw2.WriteLine(manifestHash);
                tw2.Close();
            }
        }

        private void backgroundWorker_manifestGenerator_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            Tuple<int, int, string> state = (Tuple<int, int, string>)e.UserState;

            utilTools_progressBar.Maximum = state.Item1;

            fileProgress_label.Text = String.Format(@"{0}/{1}", state.Item2, state.Item1);
            fileProgress_label.Refresh();

            currentFile_label.Text = state.Item3;
            currentFile_label.Refresh();

            utilTools_progressBar.Increment(1);
        }

        private void backgroundWorker_manifestGenerator_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            bIsGeneratingManifest = false;            
        }
    }
}
