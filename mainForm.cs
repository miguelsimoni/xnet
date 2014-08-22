using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Net;
using System.Security.Cryptography;

namespace xnet
{
    public partial class mainForm : Form
    {
        private string autoCompleteFile = Path.GetDirectoryName(Application.ExecutablePath) + @"\autocmpl.txt";

        private string WindowText
        {
            get { return Application.ProductName + " v" + Application.ProductVersion; }
        }

        private string currentFileName = string.Empty;
        private string CurrentFileName
        {
            get { return currentFileName; }
            set { currentFileName = value; this.Text = WindowText + (value == string.Empty ? string.Empty : "  [" + Path.GetFileName(value) + "]"); statusLabel.Text = value; }
        }

        public mainForm()
        {
            InitializeComponent();
            this.Text = WindowText;
            this.Height = Screen.PrimaryScreen.WorkingArea.Height - (int)(this.Height * 0.25);
            this.Width = Screen.PrimaryScreen.WorkingArea.Width - (int)(this.Width * 0.25);
            this.splitContainer2.SplitterDistance = this.splitContainer2.Width / 2;
            this.splitContainer1.SplitterDistance = (int)(this.splitContainer1.Height * 0.75);
            loadAutoComplete();
        }

        private void loadAutoComplete()
        {
            cmbURL.Items.AddRange(File.ReadAllLines(autoCompleteFile));
            cmbURL.Items.Remove(string.Empty);
        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CurrentFileName = string.Empty;
            txbData.Text = string.Empty;
            lstLog.Items.Clear();
            txbResult.Text = string.Empty;
            btnSend.Enabled = true;
            statusLabel.Text = "Preparado";
            progressBar.Value = 0;
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if(openFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    txbData.Text = File.ReadAllText(openFileDialog.FileName);
                    CurrentFileName = openFileDialog.FileName;
                }
                catch(Exception ex)
                {
                    MessageBox.Show(ex.Message, ex.Source, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if(CurrentFileName != string.Empty)
            {
                File.WriteAllText(CurrentFileName, txbData.Text);
            }
            else
            {
                this.saveAsToolStripMenuItem_Click(sender, e);
            }
        }


        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if(saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    File.WriteAllText(saveFileDialog.FileName, txbData.Text);
                    CurrentFileName = openFileDialog.FileName;
                }
                catch(Exception ex)
                {
                    MessageBox.Show(ex.Message, ex.Source, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void undoToolStripMenuItem_Click(object sender, EventArgs e)
        {
     
            txbData.Undo();
        }

        private void redoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //txbData.Redo();
        }

        private void cutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if(cmbURL.Focused)
            {
                try
                {
                    Clipboard.SetText(cmbURL.Text);
                }
                catch(Exception ex)
                {
                    MessageBox.Show(ex.Message, ex.Source, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                cmbURL.Text = string.Empty;
            }
            if(txbData.Focused)
            {
                txbData.Cut();
            }
            if(txbResult.Focused)
            {
                txbResult.Cut();
            }
        }

        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if(cmbURL.Focused)
            {
                try
                {
                    Clipboard.SetText(cmbURL.Text);
                }
                catch(Exception ex)
                {
                    MessageBox.Show(ex.Message, ex.Source, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            if(txbData.Focused)
            {
                txbData.Copy();
            }
            if(txbResult.Focused)
            {
                txbResult.Copy();
            }
        }

        private void pasteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if(cmbURL.Focused)
            {
                try
                {
                    cmbURL.Text = Clipboard.GetText();
                }
                catch(Exception ex)
                {
                    MessageBox.Show(ex.Message, ex.Source, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            if(txbData.Focused)
            {
                txbData.Paste();
            }
            if(txbResult.Focused)
            {
                txbResult.Paste();
            }
        }

        private void selectAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if(cmbURL.Focused)
            {
                cmbURL.SelectAll();
            }
            if(txbData.Focused)
            {
                txbData.SelectAll();
            }
            if(txbResult.Focused)
            {
                txbResult.SelectAll();
            }
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            try
            {
                saveAutoComplete();
                if(rdbPost.Checked)
                {
                    btnSend.Enabled = false;
                    lstLog.Items.Clear();
                    txbResult.Text = string.Empty;
                    statusLabel.Text = "Procesando...";
                    progressBar.Style = ProgressBarStyle.Marquee;
                    if(chkEncrypt.Checked)
                    {
                        backgroundWorker.RunWorkerAsync(new string[] { cmbURL.Text, encrypt(txbData.Text, txbEncryptKey.Text) });
                    }
                    else
                    {
                        backgroundWorker.RunWorkerAsync(new string[] { cmbURL.Text, txbData.Text });
                    }
                }
                else if(rdbGet.Checked)
                {
                    httpGet();
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                backgroundWorker.CancelAsync();
                btnSend.Enabled = true;
                statusLabel.Text = "Detenido";
                progressBar.Value = 0;
                progressBar.Style = ProgressBarStyle.Blocks;
                //tabControl1.SelectTab(1);
            }
        }

        private void saveAutoComplete()
        {
            if(cmbURL.Text.Length > 0 && !cmbURL.Items.Contains(cmbURL.Text))
            {
                cmbURL.Items.Add(cmbURL.Text);
                File.AppendAllText(autoCompleteFile, Environment.NewLine + cmbURL.Text);
            }
        }

        private string httpPost(string url, string info)
        {
            WebRequest request = WebRequest.Create(url);
            backgroundWorker.ReportProgress(10, "URL: " + url);
            request.ContentType = "application/x-www-form-urlencoded";
            backgroundWorker.ReportProgress(20, "Content-Type: " + request.ContentType);
            request.Method = "POST";
            backgroundWorker.ReportProgress(30, "Method: " + request.Method);
            byte[] data = Encoding.UTF8.GetBytes(info);
            request.ContentLength = data.Length;
            backgroundWorker.ReportProgress(40, "Content-Length: " + request.ContentLength);
            int timeout = 0;
            int.TryParse(txbTimeout.Text, out timeout);
            if(timeout > 0)
                request.Timeout = timeout;
            backgroundWorker.ReportProgress(50, "Timeout: " + request.Timeout);
            Stream os = request.GetRequestStream();
            backgroundWorker.ReportProgress(60, "Enviando datos...");
            os.Write(data, 0, data.Length);
            backgroundWorker.ReportProgress(70, "Datos enviados.");
            os.Close();
            backgroundWorker.ReportProgress(80, "Obteniendo respuesta...");
            WebResponse resp = request.GetResponse();
            if(resp == null)
            {
                backgroundWorker.ReportProgress(100, "Response: null");
                return "[null response]";
            }
            StreamReader sr = new StreamReader(resp.GetResponseStream());
            backgroundWorker.ReportProgress(100, "Response: (ver pestaña de Resultado)");
            return sr.ReadToEnd();
        }

        private void httpGet()
        {
            WebRequest request = WebRequest.Create(cmbURL.Text);
            request.Method = "GET";
        }

        private void backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            string[] arg = (string[])e.Argument;
            try
            {
                e.Result = httpPost(arg[0], arg[1]);
                backgroundWorker.ReportProgress(100, "Response: (ver pestaña Resultado)");
            }
            catch(System.Net.WebException ex)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("Message: ");
                sb.Append(ex.Message);
                sb.Append(Environment.NewLine);
                sb.Append("Status: ");
                sb.Append(ex.Status);
                sb.Append(Environment.NewLine);
                sb.Append("Exception: ");
                sb.Append(ex.ToString());
                e.Result = sb.ToString();
            }
        }

        private void backgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            btnSend.Enabled = true;
            statusLabel.Text = "Procesado";
            progressBar.Value = 0;
            progressBar.Style = ProgressBarStyle.Blocks;
            txbResult.Text = e.Result.ToString();
            //tabControl1.SelectTab(1);
        }

        private void backgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar.Value = e.ProgressPercentage;
            lstLog.Items.Add(e.UserState.ToString());
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string info = Application.ProductName + Environment.NewLine + "v" + Application.ProductVersion + Environment.NewLine + "Por: " + Application.CompanyName;
            MessageBox.Show(info, "Acerca de...", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void chkEncrypt_CheckedChanged(object sender, EventArgs e)
        {
            txbEncryptKey.Enabled = chkEncrypt.Checked;
        }

        private string encrypt(string data, string key)
        {
            byte[] text = Encoding.UTF8.GetBytes(data);
            TripleDESCryptoServiceProvider tdes = new TripleDESCryptoServiceProvider();
            MD5CryptoServiceProvider hashMD5 = new MD5CryptoServiceProvider();
            tdes.Mode = System.Security.Cryptography.CipherMode.CBC;
            tdes.Key = hashMD5.ComputeHash(System.Text.ASCIIEncoding.ASCII.GetBytes(key));
            tdes.Padding = PaddingMode.None;
            return Convert.ToBase64String(tdes.CreateEncryptor().TransformFinalBlock(text, 0, text.Length));
        }

        private void chkTimeout_CheckedChanged(object sender, EventArgs e)
        {
            txbTimeout.Enabled = chkTimeout.Checked;
        }

        private void editAutocompleteListToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(autoCompleteFile);
        }

    }
}