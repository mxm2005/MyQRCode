using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.Drawing.Printing;
using System.Threading;
using System.Globalization;
using WinSearchFile;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using EncodeUtil;


namespace QRCodeSample
{
    public partial class MainForm : Form
    {
        CspParameters cspPas = new CspParameters() { KeyContainerName = "MyRSA" };
        private string[] fixedKey = { "dddddddd", "dddddddddddddddd", "dddddddddddddddddddddddd" };
        private static string RSAKey = "";
        private static string OpenedFileName = "";
        private static bool useHybridEncryption;
        private bool _running = false;
        private object lockObj = new object();

        public MainForm()
        {
            InitializeComponent();
            skinEngine.SkinFile = @"Skins\MSN.ssk";   // 选择皮肤
        }

        public bool Running
        {
            get
            {
                lock (lockObj)
                {
                    return _running;
                }
            }
            set
            {
                lock (lockObj)
                {
                    _running = value;
                }
            }
        }

        private void frmSample_Load(object sender, EventArgs e)
        {
            cboEncoding.SelectedIndex = 1;
            cboVersion.SelectedIndex = 0;
            cboCorrectionLevel.SelectedIndex = 1;
            cboEncryptAlgo.SelectedIndex = cboEncryptAlgo.Items.Count - 1;
            cboDecryptAlgo.SelectedIndex = cboEncryptAlgo.Items.Count - 1;
            cspPas.KeyContainerName = "MyRSA";
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void btnEncode_Click(object sender, EventArgs e)
        {
            if (txtEncodeData.Text.Trim() == String.Empty)
            {
                MessageBox.Show("Data must not be empty.");
                return;
            }
            try
            {
                useHybridEncryption = false;
                RSAKey = "";
                ShowStatusLabel(sender, e);

                string data = txtEncodeData.Text;
                string key = txtEncodeKey.Text;

                data = data.TrimEnd();

                #region 加密
                //此处可以对data进行加密
                int encryption = cboEncryptAlgo.SelectedIndex;
                switch (encryption)
                {
                    case 0:
                    //混合
                    string tempKey = new Random().Next(10000000, 99999999).ToString();
                    RSAKey = (Encryption.RSA.Operate.Encrypt(tempKey, cspPas))[0];
                    useHybridEncryption = true;
                    data = Encryption.DES.Operate.Encrypt(data, tempKey);
                    break;

                    case 1:
                    //des 
                    //密钥为8位
                    if (String.IsNullOrEmpty(key))
                    {
                        key = fixedKey[0];
                    }
                    data = Encryption.DES.Operate.Encrypt(data, key);
                    break;

                    case 2:
                    //RSA
                    data = Encryption.RSA.Operate.Encrypt(data, cspPas)[0];
                    break;

                    default:
                    break;
                }
                #endregion

                txtEncodeData.Text = data;
                picEncode.Image = QRcodeHelper.Encode(new QRcodeHelper.QRCodeInput()
                {
                    Source = data,
                    Width = picEncode.Width,
                    Height = picEncode.Height,
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (picEncode.Image == null)
            {
                return;
            }
            if (picDecode.Image != null)//已修正
            {
                picDecode.Image.Dispose();
                picDecode.Image = null;
            }

            SaveFileDialog SVD = new SaveFileDialog();
            SVD.Filter = "JPeg Image|*.jpg|Bitmap Image|*.bmp|Gif Image|*.gif|PNG Image|*.png";
            SVD.Title = "Save";
            SVD.FileName = string.Empty;
            SVD.ShowDialog();

            // If the file name is not an empty string open it for saving.
            if (SVD.FileName != "")
            {
                // Saves the Image via a FileStream created by the OpenFile method.
                FileStream fs = (FileStream)SVD.OpenFile();
                // Saves the Image in the appropriate ImageFormat based upon the
                // File type selected in the dialog box.
                // NOTE that the FilterIndex property is one-based.
                switch (SVD.FilterIndex)
                {
                    case 1:
                    picEncode.Image.Save(fs, ImageFormat.Jpeg);
                    break;

                    case 2:
                    picEncode.Image.Save(fs, ImageFormat.Bmp);
                    break;

                    case 3:
                    picEncode.Image.Save(fs, ImageFormat.Gif);
                    break;
                    case 4:
                    picEncode.Image.Save(fs, ImageFormat.Png);
                    break;
                }
                fs.Close();

                if (useHybridEncryption && !String.IsNullOrEmpty(RSAKey))
                {
                    Operate.ReadWriteFile.String2File(RSAKey, SVD.FileName + ".key");
                }
            }
        }

        private void btnPrint_Click(object sender, EventArgs e)
        {
            if (picEncode.Image == null)
            {
                return;
            }

            PrintDialog PD = new PrintDialog();
            PD.UseEXDialog = true;
            PD.Document = new PrintDocument();
            PD.Document.PrintPage += new PrintPageEventHandler((object sender1, PrintPageEventArgs e1) =>
            {
                e1.Graphics.DrawImage(picEncode.Image, 0, 0);
            });

            if (PD.ShowDialog() == DialogResult.OK)
            {
                PD.Document.Print();
            }
        }

        private void btnOpen_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            //openFileDialog1.InitialDirectory = "c:\\";
            openFileDialog.Filter = "JPeg Image|*.jpg|Bitmap Image|*.bmp|Gif Image|*.gif|PNG Image|*.png|All files (*.*)|*.*";
            openFileDialog.FilterIndex = 1;
            openFileDialog.RestoreDirectory = true;
            openFileDialog.FileName = string.Empty;

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                if (picEncode.Image != null)//已修正
                {
                    picEncode.Image.Dispose();
                    picEncode.Image = null;
                }

                if (picDecode.Image != null)//已修正
                {
                    picDecode.Image.Dispose();
                    picDecode.Image = null;
                }

                string fileName = openFileDialog.FileName;
                OpenedFileName = fileName;
                picDecode.Image = new Bitmap(fileName);//这里会占用文件，需要修正

            }
        }

        private void btnDecode_Click(object sender, EventArgs e)
        {
            try
            {
                string data = QRcodeHelper.Decode(picDecode.Image);

                //此处可以对decodedString进行解密
                #region 解密
                int encryption = cboDecryptAlgo.SelectedIndex;
                string key = txtDecodeKey.Text;

                switch (encryption)
                {
                    case 0:
                    //混合
                    string tempRSAKey = Operate.ReadWriteFile.File2String(OpenedFileName + ".key");
                    string tempKey = Encryption.RSA.Operate.Decrypt(tempRSAKey, cspPas);
                    data = Encryption.DES.Operate.Decrypt(data, tempKey);
                    break;

                    case 1:
                    //des
                    //密钥为8位                        
                    if (String.IsNullOrEmpty(key))
                    {
                        key = fixedKey[0];
                    }
                    data = Encryption.DES.Operate.Decrypt(data, key);
                    data = data.TrimEnd();
                    break;

                    case 2:
                    //RSA
                    data = Encryption.RSA.Operate.Decrypt(data, cspPas);
                    break;

                    default:
                    break;
                }
                #endregion

                txtDecodedData.Text = data;
                ShowStatusLabel(sender, e);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void ShowStatusLabel(object sender, EventArgs e)
        {
            if (tabMain.SelectedIndex == 0)
            {
                toolStripStatusLabel1.Text = "内容长度：";
                toolStripStatusLabel2.Text = txtEncodeData.Text.Length.ToString();
            }
            else if (tabMain.SelectedIndex == 1)
            {
                toolStripStatusLabel1.Text = "内容长度：";
                toolStripStatusLabel2.Text = txtDecodedData.Text.Length.ToString();
            }
            else if (tabMain.SelectedIndex == 2)
            {
                toolStripStatusLabel1.Text = "当前状态：";
                toolStripStatusLabel2.Text = "";
            }
            else if (tabMain.SelectedIndex == 3)
            {
                toolStripStatusLabel1.Text = "当前状态：";
                toolStripStatusLabel2.Text = "";
            }
        }

        private void btnFormat_Click(object sender, EventArgs e)
        {
            try
            {
                //txtEncodeData.Text = txtEncodeData.Text.Replace("##", "");
                // txtEncodeData.Text = txtEncodeData.Text.Replace("|", Environment.NewLine);
                txtEncodeData.Text = txtEncodeData.Text.Replace(Environment.NewLine, "|");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnIdentify_Click(object sender, EventArgs e)
        {
            string source = txtDecodedData.Text;
            if (String.IsNullOrEmpty(source))
            {
                return;
            }

            Regex IdentifyDelim = new Regex("|");
            string[] subStr = IdentifyDelim.Split(source);

            try
            {
                txtComCode.Text = subStr[0].Trim();
                txtComName.Text = subStr[1].Trim();
                txtComManager.Text = subStr[2].Trim();
                txtBusinessScope.Text = subStr[3].Trim();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        private void btnOpen2_Click(object sender, EventArgs e)
        {
            OpenFileDialog OFD = new OpenFileDialog();
            OFD.Filter = "JPeg Image|*.jpg|Bitmap Image|*.bmp|Gif Image|*.gif|PNG Image|*.png";
            OFD.FilterIndex = 1;
            OFD.RestoreDirectory = true;
            OFD.FileName = string.Empty;

            if (OFD.ShowDialog() != DialogResult.OK)
            {
                return;
            }
            txtQRCodePath.Text = OFD.FileName;

            try
            {

                string txt = QRcodeHelper.Decode(txtQRCodePath.Text);
                txt = txt.Replace(Environment.NewLine, " ");
                if (txt.Length > 50)
                {
                    txt = txt.Substring(0, 50);
                    txt += "...";
                }
                txtSearch.Text = txt;

            }
            catch (Exception ex)
            {
                txtSearch.Text = "";
                MessageBox.Show(ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnSelectPath_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog FBD = new FolderBrowserDialog();
            FBD.RootFolder = Environment.SpecialFolder.Desktop;

            if (FBD.ShowDialog() != DialogResult.OK)
            {
                return;
            }
            txtSelectPath.Text = FBD.SelectedPath;
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(txtSearch.Text) && String.IsNullOrEmpty(txtQRCodePath.Text))
            {
                return;
            }

            Running = true;

            try
            {

                tabMain.AllowSelect = false;
                btnSearch.Enabled = false;
                btnRemove.Enabled = false;
                AutoResetEvent areFinish = new AutoResetEvent(false);
                Thread waitTime = new Thread(() =>
                {
                    DateTime dtStart = DateTime.Now;
                    Invoke(new MethodInvoker(() =>
                    {
                        toolStripStatusLabel2.Text = "正在搜索文件和解码二维码";
                    }));
                    areFinish.WaitOne();
                    DateTime dtFinish = DateTime.Now;
                    Invoke(new MethodInvoker(() =>
                    {
                        toolStripStatusLabel2.Text = "搜索完成，所用时间："
                            + ((int)(dtFinish - dtStart).TotalSeconds).ToString("d") + "秒";

                        btnSearch.Enabled = true;
                        btnRemove.Enabled = true;
                        tabMain.AllowSelect = true;
                        Running = false;
                    }));
                });
                waitTime.IsBackground = true;
                waitTime.Name = "计时线程";
                waitTime.Start();

                string source = String.Empty;
                if (String.IsNullOrEmpty(txtQRCodePath.Text))
                {
                    source = txtSearch.Text;
                }
                else
                {
                    source = QRcodeHelper.Decode(txtQRCodePath.Text);
                }
                lvFileSearch.Items.Clear();
                string Dir = txtSelectPath.Text;
                string SearchPattern = "*.jpg,*.png,*gif,*.bmp";
                FileSearch fs = new FileSearch(Dir, SearchPattern, source, this);
                Thread searchFileThread = new Thread(() =>
                {
                    fs.Start();
                    areFinish.Set();

                });
                searchFileThread.IsBackground = true;
                searchFileThread.Name = "搜索主线程";
                searchFileThread.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void listFileFounded_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lvFileSearch.SelectedItems.Count <= 0)
            {
                return;
            }

            try
            {
                Thread showThread = new Thread(() =>
                {
                    try
                    {
                        Invoke(new MethodInvoker(() =>
                        {
                            label17.Text = "解码中";
                        }));

                        string path = "";
                        Invoke(new MethodInvoker(() =>
                        {
                            path = lvFileSearch.SelectedItems[0].SubItems[1].Text;
                        }));
                        string txt = QRcodeHelper.Decode(path);
                        txt = txt.Replace("\r\n", " ");
                        if (txt.Length > 50)
                        {
                            txt = txt.Substring(0, 50);
                            txt += "...";
                        }
                        Invoke(new MethodInvoker(() =>
                        {
                            label17.Text = txt;
                        }));
                    }
                    catch
                    {
                        Invoke(new MethodInvoker(() =>
                        {
                            label17.Text = "解码失败";
                        }));
                    }
                    finally
                    {
                    }
                });
                showThread.Start();
            }
            catch
            {
                label17.Text = "解码失败";
            }
        }

        private void listFileFounded_DoubleClick(object sender, EventArgs e)
        {
            if (lvFileSearch.SelectedItems.Count <= 0)
            {
                return;
            }
            try
            {
                string path = lvFileSearch.SelectedItems[0].SubItems[1].Text;
                Process.Start("Explorer", "/select," + path);
            }
            catch { }
        }

        private void btnOpen3_Click(object sender, EventArgs e)
        {
            OpenFileDialog OFD = new OpenFileDialog();
            OFD.Filter = "文本文档|*.txt";
            OFD.FilterIndex = 1;
            OFD.RestoreDirectory = false;
            OFD.FileName = string.Empty;

            if (OFD.ShowDialog() != DialogResult.OK)
            {
                return;
            }
            txtFilePath.Text = OFD.FileName;
        }

        private void btnCreate_Click(object sender, EventArgs e)
        {
            if (String.IsNullOrWhiteSpace(txtFilePath.Text))
            {
                return;
            }
            lvBatchEncode.Items.Clear();
            Running = true;

            /*
                        try
                        {
                            tabMain.AllowSelect = false;
                            this.btnCreate.Enabled = false;
                            AutoResetEvent finish = new AutoResetEvent(false);
                            Thread waitTime = new Thread(() =>
                            {
                                DateTime dtStart = DateTime.Now;
                                this.Invoke(new MethodInvoker(() =>
                                {
                                    this.toolStripStatusLabel2.Text = "正在读取文件和生成二维码";
                                }));
                                finish.WaitOne();
                                DateTime dtFinish = DateTime.Now;
                                this.Invoke(new MethodInvoker(() =>
                                {
                                    this.toolStripStatusLabel2.Text = "生成完成，所用时间："
                                        + ((int)(dtFinish - dtStart).TotalSeconds).ToString("d") + "秒";
                                    this.btnCreate.Enabled = true;
                                    tabMain.AllowSelect = true;
                                    Running = false;
                                }));
                            });
                            waitTime.IsBackground = true;
                            waitTime.Name = "计时线程";
                            waitTime.Start();

                            string filePath = txtFilePath.Text;
                            listFileReaded.Items.Clear();
                            BatchEncode be = new BatchEncode(filePath, this);
                            Thread createThread = new Thread(() =>
                            {
                                be.Start();
                                finish.Set();
                            });
                            createThread.IsBackground = true;
                            createThread.Name = "生成主线程";
                            createThread.Start();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
             */

            /*
                        string filePath = txtFilePath.Text;
                        string outDir = Path.GetDirectoryName(filePath) + "\\CreatedQRcode";
                        toolStripStatusLabel2.Text = "正在读取文件和生成二维码";
                        tabMain.AllowSelect = false;
                        btnCreate.Enabled = false;

                        Task.Factory.StartNew(() =>
                        {
                            TimeSpan timeSpan = BatchEncode(filePath, outDir);
                            this.Invoke(new MethodInvoker(() =>
                            {
                                this.toolStripStatusLabel2.Text = string.Format("生成完成，所用时间：{0}秒", 
                                    timeSpan.TotalSeconds.ToString("2f"));
                                this.btnCreate.Enabled = true;
                                tabMain.AllowSelect = true;
                                Running = false;
                            }));

                        });
             */
            string filePath = txtFilePath.Text;
            string outDir = Path.Combine(Path.GetDirectoryName(filePath), "CreatedQRcode");
            BatchEncoder encoder = new BatchEncoder(filePath, outDir,
            (EventInfo eventInfo) =>
            {
                BatchEncoder.ReadLineEventTarget target =
                    eventInfo.Target as BatchEncoder.ReadLineEventTarget;
                
                ListViewItem item = new ListViewItem(new[] { target.LineIndex.ToString(), target.Text, "" });
                AddItemForListView(lvBatchEncode, item);
            },
            (EventInfo eventInfo) =>
            {
                if (eventInfo.Type == EventInfo.EventType.OK)
                {
                    BatchEncoder.EncodeLineToFileEventTarget target =
                        eventInfo.Target as BatchEncoder.EncodeLineToFileEventTarget;
                    SetCellForListView(lvBatchEncode, target.LineIndex, 2, "成功");
                }
                else if (eventInfo.Type == EventInfo.EventType.ERROR)
                {
                }
            });

            Task.Factory.StartNew(() =>
            {
                Stopwatch watch = new Stopwatch();
                watch.Start();
                encoder.BatchEncode(false);
                watch.Stop();
                toolStripStatusLabel2.Text =
                    string.Format("生成完成，所用时间：{0}秒", watch.Elapsed.TotalSeconds.ToString("f3"));
                Running = false;
            });

        }

        public void AddItemForListView(ListView lv, ListViewItem item)
        {
            if (lv.InvokeRequired)
            {
                lv.Invoke(new MethodInvoker(() =>
                {
                    lvBatchEncode.Items.Add(item);
                }));
            }
            else
            {
                lvBatchEncode.Items.Add(item);
            }
        }

        public void SetCellForListView(ListView lv, int itemIndex, int subItemIndex, string text)
        {
            if (lv.InvokeRequired)
            {
                lv.Invoke(new MethodInvoker(() =>
                {
                    lv.Items[itemIndex].SubItems[subItemIndex].Text = text;
                }));
            }
            else
            {
                lv.Items[itemIndex].SubItems[subItemIndex].Text = text;
            }
        }

        private void btnRemove_Click(object sender, EventArgs e)
        {
            if (lvFileSearch.Items.Count <= 0)
            {
                return;
            }

            lvFileSearch.BeginUpdate();
            for (int i = 0; i < lvFileSearch.Items.Count; i++)
            {
                if (lvFileSearch.Items[i].SubItems[2].Text != "符合")
                {
                    lvFileSearch.Items.RemoveAt(i);
                    i--;
                }
            }
            for (int i = 0; i < lvFileSearch.Items.Count; i++)
            {
                lvFileSearch.Items[i].SubItems[0].Text = (i + 1).ToString();
            }
            lvFileSearch.EndUpdate();
        }

        private void SetControlText(Control ctr, string text)
        {
            if (ctr.InvokeRequired)
            {
                ctr.Invoke(new MethodInvoker(() =>
                {
                    ctr.Text = text;
                }));
            }
            else
            {
                ctr.Text = text;
            }
        }
    }
}