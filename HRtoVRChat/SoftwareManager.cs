using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace HRtoVRChat;

public static class SoftwareManager {
    public static Action<string?, string> OnConsoleUpdate = (line, color) => { };
    public static Action<int, int> RequestUpdateProgressBars = (x, y) => { };

    // UI Interaction Delegates
    public static Func<string, string, Task<bool>>? RequestConfirmation;
    public static Action<string, string, bool>? ShowMessage;

    private static Process? CurrentProcess;
    private static StreamWriter? myStreamWriter;

    private static readonly string[] ExcludeFilesOnDelete = {
        "config.cfg"
    };

    private static readonly string[] ExcludeDirectoriesOnDelete = {
        "SDKs",
        "Logs"
    };

    public static string LocalDirectory {
        get {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "HRtoVRChat");
            return string.Empty;
        }
    }

    public static string OutputPath {
        get {
            if (LocalDirectory != string.Empty)
                return Path.Combine(LocalDirectory, "HRtoVRChat_OSC");
            return "HRtoVRChat_OSC";
        }
    }

    public static string ExecutableName {
        get {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return "HRtoVRChat_OSC.exe";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return "HRtoVRChat_OSC-macos";
            return "HRtoVRChat_OSC-linux";
        }
    }

    public static string ExecutableLocation {
        get => Path.Combine(OutputPath, ExecutableName);
    }

    public static bool IsInstalled {
        get => Directory.Exists(OutputPath) && File.Exists(ExecutableLocation);
    }

    public static string gitUrl {
        get => "https://github.com/200Tigersbloxed/HRtoVRChat_OSC/releases/latest";
    }

    public static string latestDownload {
        get => "https://github.com/200Tigersbloxed/HRtoVRChat_OSC/releases/latest/download/HRtoVRChat_OSC.zip";
    }

    public static bool IsUpdating { get; private set; }

    public static bool IsSoftwareRunning {
        get {
            bool nativeRunning;
            try {
                nativeRunning = !(CurrentProcess?.HasExited ?? true);
            }
            catch (Exception) {
                // Last resort, try the Process Way
                nativeRunning = Process.GetProcessesByName("HRtoVRChat_OSC").Length > 0;
            }

            return nativeRunning;
        }
    }

    private static string GetArgs() {
        List<string> Args = new();
        if (ConfigManager.LoadedUIConfig != null) {
            if (ConfigManager.LoadedUIConfig.AutoStart)
                Args.Add("--auto-start");
            if (ConfigManager.LoadedUIConfig.SkipVRCCheck)
                Args.Add("--skip-vrc-check");
            if (ConfigManager.LoadedUIConfig.NeosBridge)
                Args.Add("--neos-bridge");
            if (ConfigManager.LoadedUIConfig.UseLegacyBool)
                Args.Add("--use-01-bool");
            try {
                foreach (var s in ConfigManager.LoadedUIConfig.OtherArgs.Split(' '))
                    Args.Add(s);
            }
            catch (Exception) { }
        }

        var newargs = string.Empty;
        foreach (var arg in Args)
            newargs += arg + " ";
        return newargs;
    }

    private static void ContinueStartSoftware() {
        if (CurrentProcess == null) return;
        CurrentProcess.Start();
        CurrentProcess.StandardInput.AutoFlush = true;
        myStreamWriter = CurrentProcess.StandardInput;
        CurrentProcess.BeginOutputReadLine();
    }

    public static void StartSoftware() {
        if (IsInstalled) {
            var chmodHandle = false;
            CurrentProcess = new Process();
            CurrentProcess.StartInfo = new ProcessStartInfo {
                WorkingDirectory = OutputPath,
                Arguments = GetArgs(),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                CurrentProcess.StartInfo.FileName = Path.Combine(OutputPath, ExecutableName);
            else {
                chmodHandle = true;
                // Make sure the file is executable first
                var chmodProcess = new Process {
                    StartInfo = new ProcessStartInfo("chmod", $"+x {Path.Combine(OutputPath, ExecutableName)}") {
                        CreateNoWindow = true
                    },
                    EnableRaisingEvents = true
                };
                chmodProcess.Exited += (sender, args) => {
                    CurrentProcess.StartInfo.FileName = Path.Combine(OutputPath, ExecutableName);
                    ContinueStartSoftware();
                };
                chmodProcess.Start();
            }

            CurrentProcess.OutputDataReceived += (sender, eventArgs) => {
                OnConsoleUpdate.Invoke(eventArgs.Data, string.Empty);
            };
            if (!chmodHandle)
                ContinueStartSoftware();
        }
        else {
            ShowMessage?.Invoke("HRtoVRChat", "HRtoVRChat_OSC is not installed! Please navigate to the Updates tab and install it.", true);
        }
    }

    public static void SendCommand(string command) {
        if (IsSoftwareRunning && myStreamWriter != null) {
            try {
                myStreamWriter?.WriteLine(command);
                OnConsoleUpdate.Invoke("> " + command, "Purple");
            }
            catch (Exception) {
                ShowMessage?.Invoke("HRtoVRChat", "Failed to send command due to an error!", true);
            }
        }
    }

    public static void StopSoftware() {
        if (IsSoftwareRunning) {
            try {
                SendCommand("exit");
                myStreamWriter?.Close();
            }
            catch (Exception) { }
        }
    }

    public static async Task InstallSoftware(Action? callback = null) {
        if (!IsUpdating) {
            if (!IsSoftwareRunning) {
                UpdateProgressBars(0, 0);
                // Make sure the Directory Exists
                if (!Directory.Exists(OutputPath))
                    Directory.CreateDirectory(OutputPath);
                // Check if there's any files in the Directory
                if (Directory.GetFiles(OutputPath).Length > 0) {
                    var result = true;
                    if (RequestConfirmation != null)
                        result = await RequestConfirmation.Invoke("HRtoVRChat", "All files in " + Path.GetFullPath(OutputPath) +
                                         " are going to be deleted! Are you sure?");

                    if (result) {
                        IsUpdating = true;
                        // Delete all files
                        foreach (var file in Directory.GetFiles(OutputPath)) {
                            if (!ExcludeFilesOnDelete.Contains(Path.GetFileName(file)))
                                DeleteFileAndWait(file);
                        }

                        // Delete all directories
                        foreach (var directory in Directory.GetDirectories(OutputPath)) {
                            var dirName = new DirectoryInfo(directory).Name;
                            if (!ExcludeDirectoriesOnDelete.Contains(dirName))
                                DeleteDirectoryAndWait(directory);
                        }
                    }
                    else
                        return;
                }

                UpdateProgressBars(0, 25);
                // Download the file
                try {
                    var outputFile = Path.Combine(OutputPath, "HRtoVRChat_OSC.zip");
                    await DownloadFileWithProgressAsync(latestDownload, outputFile, (p) => UpdateProgressBars(p, 25));

                    UpdateProgressBars(0, 50);
                    // Extract the Zip
                    ZipFile.ExtractToDirectory(outputFile, OutputPath);
                    UpdateProgressBars(100, 50);
                    // Create version.txt
                    UpdateProgressBars(0, 75);
                    File.WriteAllText(Path.Combine(OutputPath, "version.txt"), GetLatestVersion());
                    UpdateProgressBars(100, 100);
                    IsUpdating = false;
                    callback?.Invoke();
                }
                catch (Exception e) {
                    ShowMessage?.Invoke("HRtoVRChat", "Failed to download/install update: " + e.Message, true);
                    IsUpdating = false;
                }
            }
            else {
                ShowMessage?.Invoke("HRtoVRChat", "There's an instance of HRtoVRChat_OSC running, please close out of it before continuing!", true);
            }
        }
    }

    private static async Task DownloadFileWithProgressAsync(string url, string destinationFilePath, Action<int> progressCallback)
    {
        using (var client = new HttpClient())
        {
            using (var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
            {
                response.EnsureSuccessStatusCode();
                var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                var canReportProgress = totalBytes != -1;

                using (var contentStream = await response.Content.ReadAsStreamAsync())
                using (var fileStream = new FileStream(destinationFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                {
                    var totalRead = 0L;
                    var buffer = new byte[8192];
                    var isMoreToRead = true;

                    do
                    {
                        var read = await contentStream.ReadAsync(buffer, 0, buffer.Length);
                        if (read == 0)
                        {
                            isMoreToRead = false;
                        }
                        else
                        {
                            await fileStream.WriteAsync(buffer, 0, read);

                            totalRead += read;
                            if (canReportProgress)
                            {
                                progressCallback((int)((totalRead * 100) / totalBytes));
                            }
                        }
                    }
                    while (isMoreToRead);
                }
            }
        }
    }

    private static void UpdateProgressBars(int x, int y) {
        RequestUpdateProgressBars.Invoke(x, y);
    }

    /// <summary>
    ///     Method made by JPK from Stackoverflow
    ///     No edits were made
    ///     https://stackoverflow.com/a/39254732/12968919
    /// </summary>
    /// <param name="filepath">Path of the file</param>
    /// <param name="timeout">Optional Timeout</param>
    private static void DeleteFileAndWait(string filepath, int timeout = 30000) {
        var dir = Path.GetDirectoryName(filepath);
        if (dir == null) return;
        using (var fw = new FileSystemWatcher(dir, Path.GetFileName(filepath)))
        using (var mre = new ManualResetEventSlim()) {
            fw.EnableRaisingEvents = true;
            fw.Deleted += (sender, e) => {
                mre.Set();
            };
            File.Delete(filepath);
            mre.Wait(timeout);
        }
    }

    private static void DeleteDirectoryAndWait(string directory, int timeout = 30000) {
        var dir = Path.GetDirectoryName(directory);
        if (dir == null) return;
        using (var fw = new FileSystemWatcher(dir))
        using (var mre = new ManualResetEventSlim()) {
            fw.EnableRaisingEvents = true;
            fw.Deleted += (sender, e) => {
                mre.Set();
            };
            Directory.Delete(directory, true);
            mre.Wait(timeout);
        }
    }

    public static string GetLatestVersion() {
        // Get the URL
        var url = GetFinalRedirect(gitUrl);
        if (!string.IsNullOrEmpty(url)) {
            // Parse the Url
            var slashSplit = url.Split('/');
            var tag = slashSplit[slashSplit.Length - 1];
            return tag;
        }

        return string.Empty;
    }

    public static string GetInstalledVersion() {
        if (IsInstalled) {
            var file = Path.Combine(OutputPath, "version.txt");
            if (File.Exists(file)) {
                using (var sr = new StreamReader(file)) {
                    var text = sr.ReadToEnd();
                    return text;
                }
            }

            return "unknown";
        }

        return "unknown";
    }

    /// <summary>
    ///     Method by Marcelo Calbucci and edited by Uwe Keim.
    ///     No changes to this method were made.
    ///     https://stackoverflow.com/a/28424940/12968919
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
#pragma warning disable SYSLIB0014
    private static string? GetFinalRedirect(string url) {
        if (string.IsNullOrWhiteSpace(url))
            return url;

        var maxRedirCount = 8; // prevent infinite loops
        var newUrl = url;
        do {
            HttpWebRequest? req = null;
            HttpWebResponse? resp = null;
            try {
                req = (HttpWebRequest)WebRequest.Create(url);
                req.Method = "HEAD";
                req.AllowAutoRedirect = false;
                resp = (HttpWebResponse)req.GetResponse();
                switch (resp.StatusCode) {
                    case HttpStatusCode.OK:
                        return newUrl;
                    case HttpStatusCode.Redirect:
                    case HttpStatusCode.MovedPermanently:
                    case HttpStatusCode.RedirectKeepVerb:
                    case HttpStatusCode.RedirectMethod:
                        newUrl = resp.Headers["Location"];
                        if (newUrl == null)
                            return url;

                        if (newUrl.IndexOf("://", StringComparison.Ordinal) == -1) {
                            // Doesn't have a URL Schema, meaning it's a relative or absolute URL
                            var u = new Uri(new Uri(url), newUrl);
                            newUrl = u.ToString();
                        }

                        break;
                    default:
                        return newUrl;
                }

                url = newUrl;
            }
            catch (WebException) {
                // Return the last known good URL
                return newUrl;
            }
            catch (Exception) {
                return null;
            }
            finally {
                if (resp != null)
                    resp.Close();
            }
        } while (maxRedirCount-- > 0);

        return newUrl;
    }
#pragma warning restore SYSLIB0014
}