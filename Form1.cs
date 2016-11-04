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
using System.Text.RegularExpressions;

namespace Watcher2
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        static string finalPath="";
        static string exePath = "";
        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                String p = Environment.GetEnvironmentVariable("watchpath", EnvironmentVariableTarget.Machine);
                if (string.IsNullOrEmpty(p))         // ADD SCHEDULED TASK IF NOT PRESENT
                {
                    string exePath = System.Windows.Forms.Application.ExecutablePath;
                    System.Diagnostics.Process process = new System.Diagnostics.Process();
                    System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                    startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                    startInfo.FileName = "cmd.exe";
                    startInfo.Arguments = "/C schtasks /create /sc onlogon /tn watcher /rl highest /tr \"" + exePath + "\"";
                    process.StartInfo = startInfo;
                    process.Start();
                    process.WaitForExit();
                }
            }
            catch ( Exception ex)
            {
                MessageBox.Show("Errore nell'accesso al registro", "Errore", MessageBoxButtons.OK);
            }
        }

        private void fileSystemWatcher1_Changed(object sender, FileSystemEventArgs e)
        {
            richTextBox1.Text += "\nChanged: " + e.FullPath;
        }
        private void move(string inp, string outp) // RENAME FILE
        {
            bool copied = false;
            int i = 0;
            do {
                try
                {
                    File.Move(inp, outp);
                    copied = true;
                }
                catch (Exception ex)
                {
                    /*if ((i % 1000 == 0) && (i != 0))  VERBOSE MODE
                    {
                        richTextBox1.Text += "\nTentativi correnti: "+i;
                        DialogResult opts = MessageBox.Show("Windows impiega troppo a rispondere...Riprovare o interrompere?\n" + ex.ToString(), "Errore", MessageBoxButtons.RetryCancel);
                        if (opts == DialogResult.Cancel)
                        {
                            richTextBox1.Text += "\nProcesso interrotto.";
                            break;
                        }
                        else {
                            i++;
                            continue;
                        }
                    }*/
                    copied = false;
                    i++;
                }
             } while (!copied);
        }
        private void fileSystemWatcher1_Created(object sender, FileSystemEventArgs e)
        {
            int count = 0;
            richTextBox1.Text += "\nCreated: " + e.FullPath;
            Regex rgx = new Regex(@"(([012]\d)|3[01])-((0\d)|(1[012]))-\d{4} \d{2,2}-\d{2,2}-\d{2,2}$");  // check for date at the end of file
            Match a = rgx.Match(e.FullPath.Split('.')[0]);
            if (!a.Success)
            {
                DateTime now = new DateTime();
                now = DateTime.Now;
                int index = e.FullPath.IndexOf(".");
                finalPath = e.FullPath.Substring(0, index) + " " + now.ToString().Replace('/', '-').Replace(':', '-') + e.FullPath.Substring(e.FullPath.IndexOf('.'));
                exePath = e.FullPath;
                richTextBox1.Text += "\nTentativi di copia in corso...";
                count++;
                BackgroundWorker worker = new BackgroundWorker();
                worker.DoWork += Worker_DoWork;
                worker.RunWorkerAsync(count);
            }
        }

        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            move(exePath, finalPath);
        }

        private void fileSystemWatcher1_Deleted(object sender, FileSystemEventArgs e)
        {
            richTextBox1.Text += "\nDeleted: " + e.FullPath;
        }

        private void fileSystemWatcher1_Renamed(object sender, RenamedEventArgs e)
        {
            richTextBox1.Text += "\nRenamed: " + e.OldFullPath + " -> " + e.FullPath;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox cb = sender as CheckBox;
            if (cb.Checked)
            {
                richTextBox1.Hide();
            } else
            {
                richTextBox1.Show();
                cb.Text = "Nascondi dettagli";
            }
        }


        private void button1_Click(object sender, EventArgs e)
        {
            folderBrowserDialog1.ShowDialog();
            if (button1.Text == "Sfoglia")
            {
                button1.Text = "Modifica";
            }
            label1.Text = "Premere OK per iniziare";
            if (!string.IsNullOrWhiteSpace(folderBrowserDialog1.SelectedPath)) {
                textBox1.Text = folderBrowserDialog1.SelectedPath;
            } else {
                MessageBox.Show("Selezionare un vero percorso");
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (System.IO.Directory.Exists(textBox1.Text))
            {
                richTextBox1.Text += "\nImpostazione variabile d'ambiente...";
                fileSystemWatcher1.Path = textBox1.Text;
                Environment.SetEnvironmentVariable("watchpath", textBox1.Text,EnvironmentVariableTarget.Machine); // installed
                richTextBox1.Text += "\nIn esecuzione su cartella: "+textBox1.Text+"\n";
                label1.Text = "Inserire Directory manualmente o premere Modifica:";
            }
            else
            {
                MessageBox.Show("Directory non esistente.", "Errore", MessageBoxButtons.OK);

            }
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                Hide();     
                notifyIcon1.Visible = true;
            }
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            String p = Environment.GetEnvironmentVariable("watchpath", EnvironmentVariableTarget.Machine);
            if (!string.IsNullOrEmpty(p))
            {
                fileSystemWatcher1.Path = p; // start listening
                textBox1.Text = p;
                richTextBox1.Text += "\nIn esecuzione su cartella: "+p+"\n";
                Hide();
                notifyIcon1.Visible = true;
            }
        }

        private void notifyIcon1_Click(object sender, EventArgs e)
        {
            Show();
            this.WindowState = FormWindowState.Normal;
            this.ShowInTaskbar = true;
            notifyIcon1.Visible = false;
        }
    }
}
