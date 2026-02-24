#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX
using System;
using System.Threading.Tasks;

#if UNITY_STANDALONE_WIN
using Ookii.Dialogs;
using System.Windows.Forms;
using System.Runtime.InteropServices;
#endif

#if UNITY_STANDALONE_OSX
using System.Diagnostics;
using System.IO;
using System.Text;
#endif

namespace AnotherFileBrowser
{
    public class BrowserProperties
    {
        public string title; //Title of the Dialog
        public string initialDir; //Where dialog will be opened initially
        public string filter; //aka File Extension for filtering files
        public int filterIndex; //Index of filter, if there is multiple filter. Default is 0. 
        public bool restoreDirectory = true; //Restore to last return directory

        public BrowserProperties() { }
        public BrowserProperties(string title) { this.title = title; }
    }
}

#if UNITY_STANDALONE_WIN
namespace AnotherFileBrowser.Windows
{
    public class FileBrowser
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetActiveWindow();

        public FileBrowser() { }

        /// <summary>
        /// FileDialog for picking a single file
        /// </summary>
        /// <param name="browserProperties">Special Properties of File Dialog</param>
        /// <param name="filepath">User picked path (Callback)</param>
        public void OpenFileBrowser(AnotherFileBrowser.BrowserProperties browserProperties, Action<string> filepath)
        {
            var ofd = new VistaOpenFileDialog
            {
                Multiselect = false,
                Title = browserProperties.title ?? "Select a File",
                InitialDirectory = browserProperties.initialDir ?? @"C:\",
                Filter = browserProperties.filter ?? "All files (*.*)|*.*",
                FilterIndex = browserProperties.filterIndex + 1,
                RestoreDirectory = browserProperties.restoreDirectory
            };

            if (ofd.ShowDialog(new WindowWrapper(GetActiveWindow())) == DialogResult.OK)
            {
                filepath(ofd.FileName);
            }
        }
    }

    public class WindowWrapper : System.Windows.Forms.IWin32Window
    {
        public WindowWrapper(IntPtr handle)
        {
            _hwnd = handle;
        }

        public IntPtr Handle
        {
            get { return _hwnd; }
        }

        private IntPtr _hwnd;
    }
}
#endif

#if UNITY_STANDALONE_OSX
namespace AnotherFileBrowser.Mac
{
    public class FileBrowser
    {
        public FileBrowser() { }

        /// <summary>
        /// FileDialog for picking a single file
        /// </summary>
        /// <param name="browserProperties">Special Properties of File Dialog</param>
        /// <param name="filepath">User picked path (Callback)</param>
        public void OpenFileBrowser(AnotherFileBrowser.BrowserProperties browserProperties, Action<string> filepath)
        {
            // Build AppleScript to choose a file. Use a temp file for the script to avoid quoting issues.
            string title = browserProperties.title ?? "Select a File";
            string initialDir = browserProperties.initialDir;

            // Ensure paths are POSIX style for AppleScript default location
            string posixInitial = string.IsNullOrEmpty(initialDir) ? null : initialDir;

            string tempFile = null;
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("try");
                if (!string.IsNullOrEmpty(posixInitial))
                {
                    // AppleScript: choose file default location (POSIX file "/path/") with prompt "Title"
                    sb.AppendLine($"set chosenFile to (choose file default location (POSIX file \"{EscapeAppleScriptString(posixInitial)}\") with prompt \"{EscapeAppleScriptString(title)}\")");
                }
                else
                {
                    sb.AppendLine($"set chosenFile to (choose file with prompt \"{EscapeAppleScriptString(title)}\")");
                }
                sb.AppendLine("POSIX path of chosenFile");
                sb.AppendLine("on error errMsg");
                sb.AppendLine("return \"\"");
                sb.AppendLine("end try");

                tempFile = Path.Combine(Path.GetTempPath(), $"choose_file_{Guid.NewGuid():N}.applescript");
                File.WriteAllText(tempFile, sb.ToString(), Encoding.UTF8);

                var psi = new ProcessStartInfo("/usr/bin/osascript", $"\"{tempFile}\"")
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var proc = Process.Start(psi);
                if (proc == null)
                {
                    return;
                }

                string output = proc.StandardOutput.ReadToEnd();
                string error = proc.StandardError.ReadToEnd();
                proc.WaitForExit();

                if (proc.ExitCode == 0)
                {
                    var result = output?.Trim();
                    if (!string.IsNullOrEmpty(result))
                    {
                        filepath(result);
                    }
                }
                else
                {
                    // Non-zero exit code, treat as cancel/no selection. Do nothing.
                }
            }
            catch
            {
                // Ignore exceptions, do not throw from native dialog helper.
            }
            finally
            {
                try
                {
                    if (!string.IsNullOrEmpty(tempFile) && File.Exists(tempFile))
                    {
                        File.Delete(tempFile);
                    }
                }
                catch { }
            }
        }

        private static string EscapeAppleScriptString(string s)
        {
            if (s == null) return string.Empty;
            // Escape double quotes and backslashes for inclusion in AppleScript string literals
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}
#endif

#endif

