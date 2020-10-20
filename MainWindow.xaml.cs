using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using DiffCountByBeyond.Services;
using System.IO;
using Microsoft.Win32;
using System.Diagnostics;
using System.Data;
using System.Collections.Concurrent;

namespace DiffCountByBeyond
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>

    public partial class MainWindow : Window
    {
        public const string MyRegisterKey = "software\\DiffCount";
        public const string beyondPathKey = "Path";
        public const string BCSoftName = "BCompare.exe";
        public const string BCSoftName2 = "BComp.exe";
        public const string Cmdtxt = "command.txt";
        public const string Command = "criteria rules-based\n" +
                                      "load <default>\n" +
                              "text-report layout:summary output-to:%1 output-options:print-mono %2 %3";
        public const string SoftFile = "D:\\DiffCountByBeyond";
        public const string SoftLogFile = SoftFile + "\\log.txt";
        public const string SoftTempFile = SoftFile + "\\temp";
        public const string reportFileName = SoftTempFile + "\\report.txt";
        public const string configFileName = SoftFile + "\\config.txt";
        public const string SoftTempDir = SoftFile + "\\tempDir";
        public const string emptyFile = SoftFile + "\\empty.txt";
        private const string baseDirOrFileKey = "baseFileOrDir";
        private const string threadNumBackKey = "threadNum";
        private  int ThreadNum;



        private const string newDirOrFileKey = "newFileOrDir";

        private string CompareBeyondPath = null;
        private string CompareBeyondPathName;
        DataBanding db = new DataBanding();

        private int CompNum = 0;
        private int AnalyNum = 0;
        public MainWindow()
        {
            InitializeComponent();
            db.ItemAndLines.Add(new ItemAndLine(Items.代码New孤立行, 0, "仅在New中存在的代码行数"));
            db.ItemAndLines.Add(new ItemAndLine(Items.代码差异行, 0, "new和base中有部分差异的代码行数"));
            db.ItemAndLines.Add(new ItemAndLine(Items.代码差异行和New新增行总数, 0, "*New代码孤立行+代码差异行"));
            db.ItemAndLines.Add(new ItemAndLine(Items.代码Base孤立行, 0, "仅在base中存在的代码行数(不包含base中的孤立文件)"));
            db.ItemAndLines.Add(
                new ItemAndLine(Items.非代码Base孤立行, 0, "仅在base中存在的注释/空行等非代码行数"));
            db.ItemAndLines.Add(
                new ItemAndLine(Items.非代码New孤立行, 0, "仅在new中存在的注释/空行等非代码行数"));
            db.ItemAndLines.Add(new ItemAndLine(Items.非代码差异行, 0, "new和base中有部分差异的注释行数"));


            db.ItemAndLines.Add(new ItemAndLine(Items.代码不同行总数, 0, "Base中代码孤立行+代码差异行+New中代码孤立行"));
            db.ItemAndLines.Add(new ItemAndLine(Items.不相同行总数, 0, "代码不同行+注释不同行"));

            this.DataContext = db;
            this.ChcCCpp_Click(chcCCpp, null);


        }







        private void BtnBegin_Click(object sender, RoutedEventArgs e)
        {
            if (InitDirAndConfigFile())
            {
                return;
            }
            BackWindows();
            foreach (var item in this.db.ItemAndLines)
            {
                item.Lines = 0;
            }

            Task t = new Task(ExecuteCom);
            t.Start();

            return;
        }

        private void BackWindows()
        {
            this.btnBegin.IsEnabled = !btnBegin.IsEnabled;
            this.txtBasePath.IsEnabled = !txtBasePath.IsEnabled;
            this.txtNewPath.IsEnabled = !txtNewPath.IsEnabled;
            this.btnBase.IsEnabled = !btnBase.IsEnabled;
            this.btnNew.IsEnabled = !btnNew.IsEnabled;
            this.txtFileMask.IsEnabled = !txtFileMask.IsEnabled;
            this.cmbMask.IsEnabled = !cmbMask.IsEnabled;
            this.txtBackThread.IsEnabled = !txtBackThread.IsEnabled;
        }

        private bool InitDirAndConfigFile()
        {
            if (string.IsNullOrEmpty(db.FileMask))
            {
                MessageBox.Show("FileMask is empty!", "error", MessageBoxButton.OK, MessageBoxImage.Error);
                return true;
            }
            if (!Directory.Exists(SoftTempFile))
            {
                Directory.CreateDirectory(SoftTempFile);
            }

            if (File.Exists(configFileName))
            {
                string tempFileText = File.ReadAllText(configFileName);
                if (!tempFileText.Equals(Command))
                {
                    File.Delete(configFileName);
                }
            }
            if (!File.Exists(configFileName))
            {
                using (StreamWriter sw = new StreamWriter(configFileName))
                {
                    sw.Write(Command);
                }
            }
            if (File.Exists(SoftLogFile))
            {
                File.Delete(SoftLogFile);
            }
            File.Create(emptyFile).Close();
            if (Directory.Exists(SoftTempDir))
            {
                try
                {
                    DirectoryInfo directoryInfo = new DirectoryInfo(SoftTempDir);
                    foreach (var dir in directoryInfo.GetDirectories())
                    {
                        dir.Delete(true);
                    }
                }
                catch
                {
                    Console.WriteLine("error");
                }
            }
            Directory.CreateDirectory(SoftTempDir);

            if (CompareBeyondPathName == null)
            {
                if (!Directory.Exists(CompareBeyondPath))
                {
                    goto ErrorBcPath;
                }
                if (Directory.GetFiles(CompareBeyondPath, "*", SearchOption.TopDirectoryOnly).Where(l => l.Contains(BCSoftName)).FirstOrDefault() != null)
                {
                    CompareBeyondPathName = CompareBeyondPath + '\\' + BCSoftName;
                }
                else if (Directory.GetFiles(CompareBeyondPath, "*", SearchOption.TopDirectoryOnly).Where(l => l.Contains(BCSoftName2)).FirstOrDefault() != null)
                {
                    CompareBeyondPathName = CompareBeyondPath + '\\' + BCSoftName2;
                }
                else
                {
                    goto ErrorBcPath;
                }
            }
            if (string.IsNullOrEmpty(txtBackThread.Text))
            {
                txtBackThread.Text = "5";
            }
            if ((ThreadNum=int.Parse(txtBackThread.Text)) > 15)
            {
                MessageBox.Show("后台线程数不建议大于15", "error", MessageBoxButton.OK, MessageBoxImage.Error);
                txtBackThread.Text = "5";
                return true;
            }

            SetRegistryKey(MyRegisterKey, threadNumBackKey, txtBackThread.Text);

            return false;
        ErrorBcPath:
            MessageBox.Show("BeyondCompared path is not correct!\nplease config the path", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return true;
        }

        private void ExecuteCom()
        {
            if (File.Exists(db.BasePath) && File.Exists(db.NewPath))
            {
                this.Dispatcher.Invoke(() =>
                {
                    if (!(bool)rbtnFile.IsChecked)
                    {
                        rbtnFile.IsChecked = true;
                    }
                });
                CompareFile(db.BasePath, db.NewPath);
                this.Dispatcher.Invoke(() => { BackWindows(); });
            }
            else if (Directory.Exists(db.BasePath) && Directory.Exists(db.NewPath))
            {
                this.Dispatcher.Invoke(() =>
                {
                    if (!(bool)rbtDir.IsChecked)
                    {
                        rbtDir.IsChecked = true;
                    }
                });
                CompareDirectory(db.BasePath, db.NewPath);
            }
            else
            {
                MessageBox.Show("Directory or file path is not correct!", "error", MessageBoxButton.OK, MessageBoxImage.Error);
                this.Dispatcher.Invoke(() => { BackWindows(); });
            }
        }

        FileSystemWatcher fileSystemWatcher;
        private void CompareDirectory(string basePath, string newPath)
        {
            docPro.Dispatcher.Invoke(() =>
            {
                docPro.Visibility = Visibility.Visible;
            });
            string[] newFiles = Directory.GetFiles(newPath, "*.*", SearchOption.AllDirectories).Where(l => l.fileInMask(db.FileMask)).ToArray();
            CreateDirInTem(newPath);
            CompNum = newFiles.Count();
            if (CompNum == 0)
            {
                this.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"new Directory has type of file end with ({db.FileMask}) is zero", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    BackWindows();
                });
                return;
            }

            AnalyNum = 0;
            string[] baseFiles = Directory.GetFiles(basePath, "*.*", SearchOption.AllDirectories).Where(l => l.fileInMask(db.FileMask)).ToArray();

            docPro.Dispatcher.Invoke(() =>
            {
                proBar.Maximum = CompNum;
                proBar.Value = AnalyNum;
            });

            fileSystemWatcher = new FileSystemWatcher(SoftTempDir, "*.*");
            fileSystemWatcher.Created += FileSystemWatcher_Created;
            fileSystemWatcher.NotifyFilter = NotifyFilters.FileName;
            fileSystemWatcher.IncludeSubdirectories = true;
            fileSystemWatcher.EnableRaisingEvents = true;

            ConcurrentQueue<string> Qs = new ConcurrentQueue<string>(newFiles);
            CreatWorkThread(Qs, baseFiles, newPath, basePath);
        }

        private void CreatWorkThread(ConcurrentQueue<string> qs, string[] baseFiles, string newPath, string basePath)
        {
            Task[] tasks = new Task[ThreadNum];
            for (int i = 0; i < ThreadNum; i++)
            {
                tasks[i]=Task.Run(() =>
                {
                    string filenew;

                    while (qs.TryDequeue(out filenew))
                    {
                        var cmdAndReportName = GetCmdstrAndtempReportFile(filenew, newPath, basePath, baseFiles);
                        if (ExecuteError(cmdAndReportName.Item1, 0))
                        {
                            qs.Enqueue(filenew);
                            continue;
                        }
                    }
                });
            }
            Task.WaitAll(tasks);
        }


        private void FileSystemWatcher_Created(object sender, FileSystemEventArgs e)
        {
            AnalyzeFileAndCal(e.FullPath);
        }
        private void AnalyzeFileAndCal(string reportFile)
        {

            var UnimporAndImpor = AnalyzeFile(reportFile);
            UInt64 allAdd = UnimporAndImpor.Item1 + UnimporAndImpor.Item2;
            lock (objLockAddNum)
            {
                db.ItemAndLines.Where(l => l.Items == Items.代码不同行总数).FirstOrDefault().Lines += UnimporAndImpor.Item2;
                db.ItemAndLines.Where(l => l.Items == Items.不相同行总数).FirstOrDefault().Lines += allAdd;
                db.ItemAndLines.Where(l => l.Items == Items.代码差异行和New新增行总数).FirstOrDefault().Lines += UnimporAndImpor.Item3;
            }

            proBar.Dispatcher.Invoke(() => { proBar.Value = Interlocked.Increment(ref AnalyNum); });

            if (AnalyNum >= CompNum)
            {

                fileSystemWatcher.EnableRaisingEvents = false;
                this.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(this, "Compared Over!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                    BackWindows();
                });

            }


        }

        private Tuple<string, string> GetCmdstrAndtempReportFile(string newfile, string newPath, string basePath, string[] baseFiles)
        {
            string newToBaseFile = newfile.Replace(newPath, basePath);
            string tempDir = System.IO.Path.GetDirectoryName(newfile.Replace(newPath, SoftTempDir));

            string tempReport = tempDir + "\\" + System.IO.Path.GetFileName(newfile);
            string findBasePath = baseFiles.Where(l => l.Equals(newToBaseFile)).FirstOrDefault();
            db.NewFileName = "New:" + System.IO.Path.GetFileName(newfile);
            db.BaseFileName = "Base:" + (findBasePath == null ? "" : System.IO.Path.GetFileName(findBasePath));
            string cmdstr;
            if (findBasePath != null)
            {
                cmdstr = " /silent " + $"\"@{configFileName}\" \"{tempReport}\" \"{findBasePath}\" \"{newfile}\"";
            }
            else
            {
                cmdstr = " /silent " + $"\"@{configFileName}\" \"{tempReport}\" \"{"D:\\DiffCountByBeyond\\empty.txt"}\" \"{newfile}\"";
            }
            return new Tuple<string, string>(cmdstr, tempReport);
        }

        private void CreateDirInTem(string newPath)
        {
            Directory.CreateDirectory(SoftTempDir);
            string[] dirs = Directory.GetDirectories(newPath, "*", SearchOption.AllDirectories);
            foreach (var newdir in dirs)
            {
                string tempDir = newdir.Replace(newPath, SoftTempDir);
                Directory.CreateDirectory(tempDir);
            }
        }

        private void CompareFile(string basePath, string newPath)
        {
            db.BaseFileName = "Base:" + System.IO.Path.GetFileName(basePath);
            db.NewFileName = "New:" + System.IO.Path.GetFileName(newPath);
            string cmdstr = $"\"@{configFileName}\" \"{reportFileName}\" \"{basePath}\" \"{newPath}\"";
            ExecuteError(cmdstr, 0);

            var UnimporAndImpor = AnalyzeFile(reportFileName);
            db.ItemAndLines.Where(l => l.Items == Items.代码不同行总数).FirstOrDefault().Lines += UnimporAndImpor.Item2;
            UInt64 allAdd = UnimporAndImpor.Item1 + UnimporAndImpor.Item2;
            db.ItemAndLines.Where(l => l.Items == Items.不相同行总数).FirstOrDefault().Lines += allAdd;
            db.ItemAndLines.Where(l => l.Items == Items.代码差异行和New新增行总数).FirstOrDefault().Lines += UnimporAndImpor.Item3;
        }
        private Object objLockAddNum = new object();
        private Tuple<UInt32, UInt32, UInt32> AnalyzeFile(string reportFileName)
        {
            Thread.Sleep(100);
            while (FileStatusHelper.IsFileOccupied(reportFileName))
            {
                Thread.Sleep(100);
            }
            string[] lines = null;
        Retry:
            try
            {
                lines = File.ReadAllLines(reportFileName, Encoding.Default);
            }
            catch (System.IO.IOException)
            {
                Thread.Sleep(100);
                goto Retry;
            }

            UInt32 importantLines = 0;
            UInt32 unImportantLines = 0;
            UInt32 imChangedAndNewAdd = 0;
            lock (objLockAddNum)
            {
                //Task t= File.wir(SoftLogFile, lines, Encoding.Default);
                Task t = AsyncWrite(SoftLogFile, lines);
                foreach (var line in lines)
                {
                    UInt32 num = 0;

                    if (line.Contains("unimportant left orphan line(s)") || line.Contains("不重要左侧孤立行"))
                    {
                        num = line.GetNumFirst();
                        db.ItemAndLines.Where(l => l.Items == Items.非代码Base孤立行).FirstOrDefault().Lines += (uint)num;
                        unImportantLines += num;
                    }
                    else if (line.Contains("unimportant right orphan line(s)") || line.Contains("不重要右侧孤立行"))
                    {
                        num = line.GetNumFirst();
                        db.ItemAndLines.Where(l => l.Items == Items.非代码New孤立行).FirstOrDefault().Lines += (uint)num;
                        unImportantLines += num;
                    }
                    else if (line.Contains("unimportant difference line(s)") || line.Contains("不重要差异行"))
                    {
                        num = line.GetNumFirst();
                        db.ItemAndLines.Where(l => l.Items == Items.非代码差异行).FirstOrDefault().Lines += (uint)num;
                        unImportantLines += num;
                    }
                    else if (line.Contains("important left orphan line(s)") || line.Contains("重要的左侧孤立行"))
                    {
                        num = line.GetNumFirst();
                        db.ItemAndLines.Where(l => l.Items == Items.代码Base孤立行).FirstOrDefault().Lines += (uint)num;
                        importantLines += num;
                    }
                    else if (line.Contains("important right orphan line(s)") || line.Contains("重要的右侧孤立行"))
                    {
                        num = line.GetNumFirst();
                        db.ItemAndLines.Where(l => l.Items == Items.代码New孤立行).FirstOrDefault().Lines += (uint)num;
                        importantLines += num;
                        imChangedAndNewAdd += num;
                    }
                    else if (line.Contains("important difference line(s)") || line.Contains("重要的差异行"))
                    {
                        num = line.GetNumFirst();
                        db.ItemAndLines.Where(l => l.Items == Items.代码差异行).FirstOrDefault().Lines += (uint)num;
                        importantLines += num;
                        imChangedAndNewAdd += num;
                    }
                }
                t.Wait();
            }
            return new Tuple<uint, uint, uint>(unImportantLines, importantLines, imChangedAndNewAdd);
        }

        private async Task AsyncWrite(string softLogFile, string[] lines)
        {
            using (StreamWriter streamWriter = new StreamWriter(softLogFile, true, Encoding.Default))
            {
                foreach (var str in lines)
                {
                    await streamWriter.WriteLineAsync(str);
                }
            }
        }





        /// <summary>
        /// 执行DOS命令，返回DOS命令的输出
        /// </summary>
        /// <param name="dosCommand">dos命令</param>
        /// <param name="milliseconds">等待命令执行的时间（单位：毫秒），
        /// 如果设定为0，则无限等待</param>
        /// <returns>返回DOS命令的输出</returns>
        public bool ExecuteError(string command, int seconds = 500)
        {

            if (command != null && !command.Equals(""))
            {
                Process process = new Process();//创建进程对象
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = CompareBeyondPathName;//设定需要执行的命令

                startInfo.Arguments = command;//“/C”表示执行完命令后马上退出
                startInfo.UseShellExecute = false;//不使用系统外壳程序启动
                startInfo.RedirectStandardInput = false;//不重定向输入
                startInfo.RedirectStandardOutput = false; //重定向输出
                startInfo.RedirectStandardError = false;
                startInfo.CreateNoWindow = true;//不创建窗口
                process.StartInfo = startInfo;

                bool status;
                status = process.Start();
                
                if (status)//开始进程
                {
                    if (seconds == 0)
                    {
                        process.WaitForExit();//这里无限等待进程结束
                        if (process.ExitCode != 0)
                        {
                            Console.WriteLine($"process.ExitCode error,command is {command}");
                            return true;
                        }
                    }
                    else if (seconds > 0)
                    {
                        process.WaitForExit(seconds); //等待进程结束，等待时间为指定的毫秒
                    }
                    return false;
                    //output = process.StandardOutput.ReadToEnd();//读取进程的输出
                }
                Console.WriteLine("start error");
            }
            return true;
        }


        private void BtnBase_Click(object sender, RoutedEventArgs e)
        {
            if ((bool)rbtDir.IsChecked)
            {
                var openFolderDialog = new System.Windows.Forms.FolderBrowserDialog();
                openFolderDialog.SelectedPath = db.BasePath;
                if (openFolderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    db.BasePath = openFolderDialog.SelectedPath;
                }
            }
            else
            {
                var openFileDialog = new System.Windows.Forms.OpenFileDialog();

                if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    db.BasePath = openFileDialog.FileName;
                }
            }
            SetRegistryKey(MyRegisterKey, baseDirOrFileKey, db.BasePath);
        }


        private void BtnNew_Click(object sender, RoutedEventArgs e)
        {
            if ((bool)rbtDir.IsChecked)
            {
                var openFolderDialog = new System.Windows.Forms.FolderBrowserDialog();
                openFolderDialog.SelectedPath = db.NewPath;
                if (openFolderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    db.NewPath = openFolderDialog.SelectedPath;

                }
            }
            else
            {
                var openFileDialog = new System.Windows.Forms.OpenFileDialog();
                if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    db.NewPath = openFileDialog.FileName;
                }
            }
            SetRegistryKey(MyRegisterKey, newDirOrFileKey, db.NewPath);
        }

        private void RbtDir_Checked(object sender, RoutedEventArgs e)
        {
            this.btnBase.Content = "BaseDirPath";
            this.btnNew.Content = "NewDirPath";

        }

        private void RbtnFile_Checked(object sender, RoutedEventArgs e)
        {
            this.btnBase.Content = "BaseFirePath";
            this.btnNew.Content = "NewFirePath";

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            
            CompareBeyondPath = GetRegistryValue(MyRegisterKey, beyondPathKey)?.ToString();
            if (CompareBeyondPath == null)
            {
                ShowWindowAndSetBeyondComPath();
            }
            string tempBackTStr = GetRegistryValue(MyRegisterKey, threadNumBackKey)?.ToString();
            if (  tempBackTStr!= null)
            {
                int tempBackThread;
                if(int.TryParse(tempBackTStr,out tempBackThread) && tempBackThread > 0 && tempBackThread <= 15){
                    txtBackThread.Text = tempBackTStr;
                }
                    
            }
             

            bool isFile;
            string baseFOD = GetFileOrDirIsFile(baseDirOrFileKey, out isFile);
            if (isFile)
            {
                rbtnFile.IsChecked = true;
            }
            if (!string.IsNullOrEmpty(baseFOD))
            {
                this.db.BasePath = baseFOD;
            }
            string newFOD = GetFileOrDirIsFile(newDirOrFileKey, out isFile);
            if (!string.IsNullOrEmpty(newFOD))
            {
                this.db.NewPath = newFOD;
            }
            SetDataGridRowColour();

        }
        private string GetFileOrDirIsFile(string key, out bool isFile)
        {
            string _file_Dir = GetRegistryValue(MyRegisterKey, key)?.ToString();
            if (!string.IsNullOrEmpty(_file_Dir))
            {
                if (File.Exists(_file_Dir))
                {
                    isFile = true;
                    return _file_Dir;
                }
                else if (Directory.Exists(_file_Dir))
                {
                    isFile = false;
                    return _file_Dir;
                }
            }
            isFile = false;
            return null;

        }

        private void SetDataGridRowColour()
        {
            foreach (var item in dataGridShow.ItemsSource)
            {
                ItemAndLine itemAndLine = item as ItemAndLine;
                DataGridRow dataGridRow = dataGridShow.ItemContainerGenerator.ContainerFromItem(item) as DataGridRow;

                if (itemAndLine.Items == Items.代码差异行和New新增行总数 || itemAndLine.Items == Items.代码New孤立行 || itemAndLine.Items == Items.代码差异行)
                {

                    dataGridRow.FontWeight = FontWeights.Bold;
                    dataGridRow.Background = new SolidColorBrush(Colors.YellowGreen);
                    dataGridRow.Foreground = new SolidColorBrush(Colors.Black);
                }
                else
                {
                    dataGridRow.Foreground = new SolidColorBrush(Colors.Gray);
                }
            }
        }

        private void SetComparePath(string path)
        {
            CompareBeyondPath = path;
        }
        private void ShowWindowAndSetBeyondComPath(string bcPath = null)
        {
            WindowSetBeyondComPath windowSetBeyondComPath = new WindowSetBeyondComPath(bcPath);
            windowSetBeyondComPath.ShowInTaskbar = false;
            windowSetBeyondComPath.SetBeyondComPathEvent += SetComparePath;
            windowSetBeyondComPath.Owner = this;
            windowSetBeyondComPath.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            windowSetBeyondComPath.ShowDialog();

            SetRegistryKey(MyRegisterKey, beyondPathKey, CompareBeyondPath);
        }
        private void SetRegistryKey(string path, string key, string value)
        {
            RegistryKey registryKey = Registry.CurrentUser;
            registryKey = registryKey.CreateSubKey(path);
            registryKey.SetValue(key, value);
            registryKey.Close();
        }
        private object GetRegistryValue(string path, string key)
        {
            return Registry.CurrentUser.OpenSubKey(path)?.GetValue(key);
        }
        private void BtnSetBCPath_Click(object sender, RoutedEventArgs e)
        {
            ShowWindowAndSetBeyondComPath(CompareBeyondPath);
        }



        private void ChcAll_Checked(object sender, RoutedEventArgs e)
        {
            foreach (var item in cmbMask.Items)
            {
                if (item is CheckBox)
                {
                    var chc = item as CheckBox;
                    if (chc.Name != "chcAll")
                    {
                        chc.IsChecked = true;
                    }
                }
            }
            DisplayMask();
        }

        private void ChcCCpp_Click(object sender, RoutedEventArgs e)
        {
            DisplayMask();
        }

        private void DisplayMask()
        {
            StringBuilder sbMask = new StringBuilder();
            StringBuilder sbComb = new StringBuilder();
            foreach (var item in cmbMask.Items)
            {
                if (item is CheckBox)
                {
                    CheckBox cb = item as CheckBox;
                    if ((bool)cb.IsChecked && cb.Name.Equals("chcCCpp"))
                    {
                        if (sbMask.Length > 0)
                        {
                            sbMask.Append("|");
                            sbComb.Append(";");
                        }
                        sbMask.Append(".c|.cpp|.cc|.hpp|.h");
                        sbComb.Append(cb.Content);

                    }
                    if ((bool)cb.IsChecked && cb.Name.Equals("chcPy"))
                    {
                        if (sbMask.Length > 0)
                        {
                            sbMask.Append("|");
                            sbComb.Append(";");
                        }
                        sbMask.Append(".py");
                        sbComb.Append(cb.Content);
                    }
                    if ((bool)cb.IsChecked && cb.Name.Equals("chcJave"))
                    {
                        if (sbMask.Length > 0)
                        {
                            sbMask.Append("|");
                            sbComb.Append(";");
                        }
                        sbMask.Append(".java");
                        sbComb.Append(cb.Content);
                    }
                    if ((bool)cb.IsChecked && cb.Name.Equals("chcGo"))
                    {
                        if (sbMask.Length > 0)
                        {
                            sbMask.Append("|");
                            sbComb.Append(";");
                        }
                        sbMask.Append(".go");
                        sbComb.Append(cb.Content);
                    }
                    if ((bool)cb.IsChecked && cb.Name.Equals("chcSql"))
                    {

                        if (sbMask.Length > 0)
                        {
                            sbMask.Append("|");
                            sbComb.Append(";");
                        }
                        sbMask.Append(".sql|.orasql|.oracle");
                        sbComb.Append(cb.Content);
                    }
                }
            }
            db.FileMask = sbMask.ToString();
            cmbMask.Text = sbComb.ToString();

        }
        private void ChcSql_Click(object sender, RoutedEventArgs e)
        {
            DisplayMask();
        }

        private void ChcPy_Click(object sender, RoutedEventArgs e)
        {
            DisplayMask();
        }

        private void ChcJave_Click(object sender, RoutedEventArgs e)
        {
            DisplayMask();
        }

        private void ChcGo_Click(object sender, RoutedEventArgs e)
        {
            DisplayMask();
        }

        private void TxtBackThread_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            bool shiftKey = (Keyboard.Modifiers & ModifierKeys.Shift) != 0;//判断shifu键是否按下
            if (shiftKey == true)               //当按下shift
            {
                e.Handled = true;//不可输入
            }
            else                      //未按shift
            {
                if (!((e.Key >= Key.D0 && e.Key <= Key.D9) || (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9) || e.Key == Key.Delete || e.Key == Key.Back || e.Key == Key.Tab || e.Key == Key.Enter))
                {
                    e.Handled = true;//不可输入
                }
            }
        }
    }
}
