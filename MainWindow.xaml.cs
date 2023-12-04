using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

using WPFFolderBrowser;
using System.IO;
using System.Security.Cryptography;
using System.Security.AccessControl;
using System.Security.Principal;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace Folder_Compare
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly BackgroundWorker worker = new BackgroundWorker { WorkerReportsProgress = true };
        private const int GWL_STYLE = -16;
        private const int WS_SYSMENU = 0x80000;
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        public MainWindow()
        {
            InitializeComponent();
            worker.DoWork += worker_DoWork;
            worker.ProgressChanged += new ProgressChangedEventHandler(backgroundWorker1_ProgressChanged);
            worker.RunWorkerCompleted += worker_RunWorkerCompleted;
        }

        private void buttonBrowseFolder1_Click(object sender, RoutedEventArgs e)
        {
            WPFFolderBrowserDialog fd = new WPFFolderBrowserDialog();
            if (fd.ShowDialog().Equals(true))
            {
                textBoxFolder1.Text = fd.FileName;
            }
        }

        private void buttonBrowseFolder2_Click(object sender, RoutedEventArgs e)
        {
            WPFFolderBrowserDialog fd = new WPFFolderBrowserDialog();
            if (fd.ShowDialog().Equals(true))
            {
                textBoxFolder2.Text = fd.FileName;
            }
        }

        private void buttonBrowseFolderSave_Click(object sender, RoutedEventArgs e)
        {
            WPFFolderBrowserDialog fd = new WPFFolderBrowserDialog();
            if (fd.ShowDialog().Equals(true))
            {
                textBoxFolderSave.Text = fd.FileName;
            }
        }


        private void buttonExit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void buttonDiff_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (textBoxFolder1.Text.Length > 0 && textBoxFolder2.Text.Length > 0 && textBoxFolderSave.Text.Length > 0)
                {
                    gridMain.Visibility = Visibility.Hidden;
                    gridProgress.Visibility = Visibility.Visible;

                    List<object> arguments = new List<object>();
                    arguments.Add(textBoxFolder1.Text);
                    arguments.Add(textBoxFolder2.Text);
                    arguments.Add(textBoxFolderSave.Text);
                    worker.RunWorkerAsync(arguments);
                    //MessageBox.Show(debugmessage);
                }
                else
                {
                    MessageBox.Show("Require field(s) is empty");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            List<object> genericlist = e.Argument as List<object>;

            int progress = 0;
            // run all background tasks here
            string debugmessage = "";
            DirectoryInfo
                dir1 = new DirectoryInfo((string)genericlist[0]),
                dir2 = new DirectoryInfo((string)genericlist[1]),
                dirsave = new DirectoryInfo((string)genericlist[2]);

            // Take a snapshot of the file system.
            IEnumerable<FileInfo>
                listdir1 = dir1.GetFiles("*.*", SearchOption.AllDirectories),
                listdir2 = dir2.GetFiles("*.*", SearchOption.AllDirectories),
                listdirsave = dirsave.GetFiles("*.*", SearchOption.AllDirectories);
            
            if (listdirsave.Count() > 0)
            {
                throw new Exception("Folder Delta should be empty, please change the location");
            }

            FileCompare myFileCompare = new FileCompare((string)genericlist[0], (string)genericlist[1]);
            
            progress = 5;
            worker.ReportProgress(progress);

            debugmessage += Environment.NewLine;
            debugmessage += "The following files are in folder 1:" + Environment.NewLine;
            foreach (var v in listdir1)
            {
                debugmessage += v.FullName + Environment.NewLine + "  ==> (file size : " + v.Length + " / hash : " + v.GetMD5HashCode() + ")" + Environment.NewLine;
            }

            debugmessage += Environment.NewLine;
            debugmessage += "The following files are in folder 2:" + Environment.NewLine;
            foreach (var v in listdir2)
            {
                debugmessage += v.FullName + Environment.NewLine + "  ==> (file size : " + v.Length + " / hash : " + v.GetMD5HashCode() + ")" + Environment.NewLine;
            }

            // Determines whether the two folders contain identical file lists
            // true = same, false = not same
            var identical = listdir1.SequenceEqual(listdir2, myFileCompare);
            debugmessage += Environment.NewLine;
            debugmessage += "The two folders are " + (identical.Equals(true) ? "" : "not") + " the same" + Environment.NewLine; debugmessage += Environment.NewLine;

            progress = 10;
            worker.ReportProgress(progress);

            // Find the common files
            var queryCommonFiles = listdir1.Intersect(listdir2, myFileCompare);
            if (queryCommonFiles.Count() > 0)
            {
                debugmessage += "The following files are in both folders:" + Environment.NewLine;
                foreach (var v in queryCommonFiles)
                {
                    debugmessage += v.FullName + Environment.NewLine + "  ==> (file size : " + v.Length + " / hash : " + v.GetMD5HashCode() + ")" + Environment.NewLine;
                }
            }
            else
            {
                debugmessage += "There are no common files in the two folders." + Environment.NewLine;
            }

            progress = 15;
            worker.ReportProgress(progress);

            // Find the set difference between the two folders.
            debugmessage += Environment.NewLine;
            var queryList1Only = listdir1.Except(listdir2, myFileCompare);
            
            progress = 20;
            worker.ReportProgress(progress);

            var queryList2Only = listdir2.Except(listdir1, myFileCompare);
            
            progress = 25;
            worker.ReportProgress(progress);

            debugmessage += Environment.NewLine;
            debugmessage += "The following files are in folder 1 but not folder 2:" + Environment.NewLine;
            foreach (var v in queryList1Only)
            {
                debugmessage += v.FullName + Environment.NewLine + "  ==> (file size : " + v.Length + " / hash : " + v.GetMD5HashCode() + ")" + Environment.NewLine;
            }

            progress = 30;
            worker.ReportProgress(progress);

            debugmessage += Environment.NewLine;
            debugmessage += "The following files are in folder 2 but not folder 1:" + Environment.NewLine;
            double qcount = (((double)1 / (double)queryList2Only.Count()) * (double)70);
            foreach (var v in queryList2Only)
            {

                debugmessage += v.FullName + Environment.NewLine + "  ==> (file size : " + v.Length + " / hash : " + v.GetMD5HashCode() + ")" + Environment.NewLine;

                if (FileCopy(v, dir1, dirsave))
                {
                    debugmessage += "Copy OK!" + Environment.NewLine;
                }
                else
                {
                    debugmessage += "Copy Fail!" + Environment.NewLine;
                }

                progress = (int)((double)progress + qcount);
                worker.ReportProgress(progress);
            }
            progress = 100;
            worker.ReportProgress(progress);
        }

        void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            loadingBar.Value = e.ProgressPercentage;
        }

        private void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //update ui once worker complete his work
            gridMain.Visibility = Visibility.Visible;
            gridProgress.Visibility = Visibility.Hidden; 
            MessageBox.Show("Done");
        }

        public bool FileCopy(FileInfo file, DirectoryInfo oldpath, DirectoryInfo newpath)
        {
            try
            {
                string
                    FilePathwithoutRoot = file.FullName.Substring(oldpath.FullName.Length),
                    NewFilePathWithFileName = newpath.FullName + "\\" + FilePathwithoutRoot,
                    NewFilePathWithoutFileName = Path.GetDirectoryName(NewFilePathWithFileName);
                if (!Directory.Exists(NewFilePathWithoutFileName))
                {
                    Directory.CreateDirectory(NewFilePathWithoutFileName);
                }
                File.Copy(file.FullName, NewFilePathWithFileName);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            SetWindowLong(hwnd, GWL_STYLE, GetWindowLong(hwnd, GWL_STYLE) & ~WS_SYSMENU);
        }
    }

    public class FileCompare : IEqualityComparer<FileInfo>
    {
        string _dir1, _dir2;
        public FileCompare() { }

        public FileCompare(string dir1, string dir2)
        {
            _dir1 = dir1; 
            _dir2 = dir2; 
        }

        public bool Equals(FileInfo f1, FileInfo f2)
        {
            string f1dir = string.Empty, f2dir = string.Empty;
            if (f1.DirectoryName.Contains(_dir1))
            {
                f1dir = _dir1;
            }
            if (f1.DirectoryName.Contains(_dir2))
            {
                f1dir = _dir2;
            }
            if (f2.DirectoryName.Contains(_dir1))
            {
                f2dir = _dir1;
            }
            if (f2.DirectoryName.Contains(_dir2))
            {
                f2dir = _dir2;
            }
            return f1.Name.Equals(f2.Name) && f1.DirectoryName.Substring(f1dir.Length).Equals(f2.DirectoryName.Substring(f2dir.Length)) && f1.Length.Equals(f2.Length) && f1.GetMD5HashCode().Equals(f2.GetMD5HashCode());
        }

        public int GetHashCode(FileInfo fi)
        {
            return string.Format("{0}{1}", fi.Name, fi.Length).GetHashCode();
        }
    }

    static class FileInfoExtention
    {
        public static bool IsReadable(this DirectoryInfo di)
        {
            AuthorizationRuleCollection rules;
            WindowsIdentity identity;
            try
            {
                rules = di.GetAccessControl().GetAccessRules(true, true, typeof(SecurityIdentifier));
                identity = WindowsIdentity.GetCurrent();
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }

            bool isAllow = false;
            string userSID = identity.User.Value;

            foreach (FileSystemAccessRule rule in rules)
            {
                if (rule.IdentityReference.ToString() == userSID || identity.Groups.Contains(rule.IdentityReference))
                {
                    if ((rule.FileSystemRights.HasFlag(FileSystemRights.Read) ||
                        rule.FileSystemRights.HasFlag(FileSystemRights.ReadAttributes) ||
                        rule.FileSystemRights.HasFlag(FileSystemRights.ReadData)) && rule.AccessControlType == AccessControlType.Deny)
                        return false;
                    else if ((rule.FileSystemRights.HasFlag(FileSystemRights.Read) &&
                        rule.FileSystemRights.HasFlag(FileSystemRights.ReadAttributes) &&
                        rule.FileSystemRights.HasFlag(FileSystemRights.ReadData)) && rule.AccessControlType == AccessControlType.Allow)
                        isAllow = true;

                }
            }
            return isAllow;
        }

        public static bool IsWriteable(this DirectoryInfo me)
        {
            AuthorizationRuleCollection rules;
            WindowsIdentity identity;
            try
            {
                rules = me.GetAccessControl().GetAccessRules(true, true, typeof(System.Security.Principal.SecurityIdentifier));
                identity = WindowsIdentity.GetCurrent();
            }
            catch (UnauthorizedAccessException)
            {
                 return false;
            }

            if (me.Attributes.HasFlag(FileAttributes.ReadOnly))
            {
                return false;
            }

            string userSID = identity.User.Value;

            foreach (FileSystemAccessRule rule in rules)
            {
                if (rule.IdentityReference.ToString() == userSID || identity.Groups.Contains(rule.IdentityReference))
                {
                    if ((rule.FileSystemRights.HasFlag(FileSystemRights.Write) ||
                        rule.FileSystemRights.HasFlag(FileSystemRights.WriteAttributes) ||
                        rule.FileSystemRights.HasFlag(FileSystemRights.WriteData) ||
                        rule.FileSystemRights.HasFlag(FileSystemRights.CreateDirectories) ||
                        rule.FileSystemRights.HasFlag(FileSystemRights.CreateFiles)) && rule.AccessControlType == AccessControlType.Deny)
                        return false;
                    else if ((rule.FileSystemRights.HasFlag(FileSystemRights.Write) &&
                        rule.FileSystemRights.HasFlag(FileSystemRights.WriteAttributes) &&
                        rule.FileSystemRights.HasFlag(FileSystemRights.WriteData) &&
                        rule.FileSystemRights.HasFlag(FileSystemRights.CreateDirectories) &&
                        rule.FileSystemRights.HasFlag(FileSystemRights.CreateFiles)) && rule.AccessControlType == AccessControlType.Allow)
                        return true;

                }
            }
            return false;
        }

        public static string GetMD5HashCode(this FileInfo fi)
        {
            return GetHashFromFile(fi.FullName);
        }

        private static string GetHashFromFile(string fileName)
        {
            try
            {
                byte[] retVal;
                using (FileStream file = new FileStream(fileName, FileMode.Open, FileAccess.Read))
                {
                    MD5 md5 = new MD5CryptoServiceProvider();
                    retVal = md5.ComputeHash(file);
                    file.Close();
                }

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < retVal.Length; i++)
                {
                    sb.Append(retVal[i].ToString("x2"));
                }
                return sb.ToString();
            }
            catch
            {
                throw new Exception("Unable to hash " + fileName + " content");
            }
        }
    }
}
