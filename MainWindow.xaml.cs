using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace ClearOffice
{
    public partial class MainWindow : Window
    {
        private static readonly (string Name, string Resource)[] _apps =
        {
            ("ClearWord.exe", "ClearOffice.ClearWord.exe"),
            ("ClearPaint.exe", "ClearOffice.ClearPaint.exe"),
            ("ClearDoc.exe", "ClearOffice.ClearDoc.exe")
        };

        public MainWindow()
        {
            InitializeComponent();
            PathBox.Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "ClearOffice");
            CancelBtn.Click += (_, _) => Close();
        }

        private void BrowseBtn_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Select installation folder",
                ShowNewFolderButton = true
            };

            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                PathBox.Text = dlg.SelectedPath;
        }

        private async void InstallBtn_Click(object sender, RoutedEventArgs e)
        {
            InstallBtn.IsEnabled = false;
            BrowseBtn.IsEnabled = false;
            CancelBtn.IsEnabled = false;
            ProgressBar.Visibility = Visibility.Visible;
            ProgressBar.Value = 0;
            StatusText.Text = "Installing...";

            var folder = PathBox.Text;

            try
            {
                Directory.CreateDirectory(folder);

                bool installWord = ClearWordCheck.IsChecked == true;
                bool installPaint = ClearPaintCheck.IsChecked == true;
                bool installDoc = ClearDocCheck.IsChecked == true;

                int step = 0;
                int total = (installWord ? 1 : 0) + (installPaint ? 1 : 0) + (installDoc ? 1 : 0);

                if (installWord)
                {
                    StatusText.Text = "Extracting ClearWord...";
                    await ExtractApp("ClearOffice.ClearWord.exe", Path.Combine(folder, "ClearWord.exe"));
                    ProgressBar.Value = (++step * 100) / total;
                }

                if (installPaint)
                {
                    StatusText.Text = "Extracting ClearPaint...";
                    await ExtractApp("ClearOffice.ClearPaint.exe", Path.Combine(folder, "ClearPaint.exe"));
                    ProgressBar.Value = (++step * 100) / total;
                }

                if (installDoc)
                {
                    StatusText.Text = "Extracting ClearDoc...";
                    await ExtractApp("ClearOffice.ClearDoc.exe", Path.Combine(folder, "ClearDoc.exe"));
                    ProgressBar.Value = (++step * 100) / total;
                }

                CreateDesktopShortcut(folder);

                ProgressBar.Value = 100;
                StatusText.Text = "Complete!";

                MessageBox.Show($"Installed to:\n{folder}\n\nShortcut on Desktop.",
                    "ClearOffice", MessageBoxButton.OK, MessageBoxImage.Information);

                Process.Start(new ProcessStartInfo(folder) { UseShellExecute = true });
                Close();
            }
            catch (Exception ex)
            {
                StatusText.Text = "Error!";
                MessageBox.Show($"Installation failed:\n{ex.Message}", "Error");
            }

            InstallBtn.IsEnabled = true;
            BrowseBtn.IsEnabled = true;
            CancelBtn.IsEnabled = true;
        }

        private static async System.Threading.Tasks.Task ExtractApp(string resourceName, string destPath)
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
            if (stream == null) throw new Exception($"Resource not found: {resourceName}");
            using var fs = new FileStream(destPath, FileMode.Create, FileAccess.Write);
            await stream.CopyToAsync(fs);
        }

        private void CreateDesktopShortcut(string folder)
        {
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string shortcutPath = Path.Combine(desktop, "ClearOffice.lnk");
            string scriptPath = Path.Combine(Path.GetTempPath(), "shortcut.vbs");

            string vbs = $@"
Set WshShell = WScript.CreateObject(""WScript.Shell"")
Set Shortcut = WshShell.CreateShortcut(""{shortcutPath}"")
Shortcut.TargetPath = ""{Path.Combine(folder, "ClearWord.exe")}""
Shortcut.WorkingDirectory = ""{folder}""
Shortcut.Description = ""ClearOffice""
Shortcut.Save
";
            File.WriteAllText(scriptPath, vbs);
            var p = Process.Start(new ProcessStartInfo("wscript.exe", scriptPath) { UseShellExecute = true });
            p?.WaitForExit(3000);
            try { File.Delete(scriptPath); } catch { }
        }
    }
}