using Cocona;
using Microsoft.Extensions.Configuration;
using Microsoft.Win32;
using System.Diagnostics;

namespace MsixStoreTool.Commands;

internal class MsixCommand
{
    private readonly IConfiguration _configuration;
    private readonly string _appDirectory;
    private readonly string _tempDirectory;

    public MsixCommand(IConfiguration configuration)
    {
        this._configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

        this._appDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MsixStoreTool");
        this._tempDirectory = Path.Combine(this._appDirectory, "temp");
    }


    [Command("bundle")]
    public async Task Bundle([Argument(Description = "path to the directory with all msix-files")] string msixDirectory,
        [Argument(Description = "the output msixbundle filename")] string outputMsixFile,
        [Argument(Description = "path to the pfx file")] string pfxFile,
        [Argument(Description = "the hash-algorithm like SHA256")] string hashAlgorithm)
    {
        this.CleanUp();

        #region looking for windows sdk

        Console.WriteLine("looking for windows sdk...");
        string? sdkDirectory = GetSdkDirectory();
        if (sdkDirectory == null)
        {
            Console.WriteLine("no sdk found :(");
            return;
        }
        Console.WriteLine($"sdk found at {sdkDirectory}");

        #endregion

        #region create temp directory

        Directory.CreateDirectory(this._tempDirectory);

        #endregion

        try
        {
            #region search for msix files

            Console.WriteLine("");
            Console.WriteLine("search for msix-files");

            string[] files = Directory.GetFiles(msixDirectory, "*.msix");

            if (!files.Any())
            {
                Console.WriteLine("no msix-files were found");

                this.CleanUp();

                return;
            }

            Console.WriteLine($"{files.Count()} files found:");
            files.ToList().ForEach(x => Console.WriteLine(x));

            #endregion

            #region copy msix files to temp

            files.ToList().ForEach(x => File.Copy(x, Path.Combine(this._tempDirectory, Path.GetFileName(x)), true));

            #endregion

            #region generate msixbundle file

            Console.WriteLine("");
            Console.Write("generate msixbundle...");

            // generate msixbundle file in temp
            // cli: .\makeappx.exe bundle /d {msix-source-dir} /p {msixbundle-target-file}
            string makeappxPath = Path.Combine(sdkDirectory, "makeappx.exe");
            string msixbundleFile = Path.Combine(this._appDirectory, outputMsixFile);
            this.ExecuteCliCommand(makeappxPath, $"bundle /d {this._tempDirectory} /p {msixbundleFile}");

            Console.WriteLine("    finished");

            #endregion

            #region sign msixbundle

            Console.WriteLine("");
            Console.Write("sign msixbundle...");

            // sign msixbundle file
            // cli: .\signtool.exe sign /fd {hashAlgorithm} /a /f {pfx-file} {msixbundle-target-file}
            string signtoolPath = Path.Combine(sdkDirectory, "signtool.exe");
            this.ExecuteCliCommand(signtoolPath, $"sign /fd {hashAlgorithm} /a /f {pfxFile} {msixbundleFile}");

            Console.WriteLine("    finished");

            #endregion

            #region copy msixbundle to source directory

            // copy msixbundle file from temp to msix-directory
            string msixTarget = Path.Combine(msixDirectory, Path.GetFileName(msixbundleFile));
            File.Copy(msixbundleFile, msixTarget, true);

            #endregion

            Console.WriteLine("");
            Console.Write("msixbundle successfully generated :D");
            Console.WriteLine("");
            Console.Write($"your find the msixbundle at '{msixTarget}'");
        }
        catch (Exception exception)
        {
            Console.WriteLine("ERROR");
            Console.WriteLine(exception.Message);
        }
        finally
        {
            this.CleanUp();
        }
    }

    private void CleanUp()
    {
        try
        {
            // delete temp directory
            Directory.Delete(this._appDirectory, true);
        }
        catch (Exception exception)
        {
            Console.WriteLine("ERROR");
            Console.WriteLine(exception.Message);
        }
    }

    private void ExecuteCliCommand(string executable, string arguments)
    {
        Process process = new Process();

        ProcessStartInfo startInfo = new ProcessStartInfo();
        startInfo.WindowStyle = ProcessWindowStyle.Hidden;
        startInfo.FileName = executable;
        startInfo.Arguments = arguments;
        startInfo.UseShellExecute = true;

        process.StartInfo = startInfo;
        process.Start();

        process.WaitForExit();
    }

    private static List<string> GetMappedDrives()
    {
        List<string> mappedDrives = DriveInfo.GetDrives()
                    .Where(x => x.DriveType == DriveType.Fixed)
                    .Select(x => x.Name[0].ToString())
                    .ToList();

        return mappedDrives;
    }

    private string? GetSdkDirectory()
    {
        // get platform (x86 or x64)
        string arch = ((Environment.Is64BitOperatingSystem) ? "x64" : "x86");

        // get all drives
        List<string> mappedDrives = GetMappedDrives();

        // load sdk directories from config
        List<string> sdkDirectoriesFromConfig = this._configuration.GetSection("sdk-tools-directories")
                    .AsEnumerable()
                    .Where(x => x.Value != null)
                    .Select(x => x.Value)
                    .ToList();

        // get installed windows sdks for current platform
        IEnumerable<string> sdkVersions = GetInstalledSdkVersions();

        if (!sdkVersions.Any())
        {
            Console.WriteLine("no sdk found");

            return null;
        }

        foreach (string sdkDirectoryEntry in sdkDirectoriesFromConfig)
        {
            foreach (string sdkVersion in sdkVersions)
            {
                foreach (string mappedDrive in mappedDrives)
                {
                    Dictionary<string, string> parameters = new Dictionary<string, string>
                    {
                        { "drive", mappedDrive },
                        { "sdk-version", sdkVersion },
                        { "platform", arch }
                    };

                    string sdkPath = parameters.Aggregate(sdkDirectoryEntry, (s, kv) => s.Replace("{" + kv.Key + "}", kv.Value));

                    if (!Directory.Exists(sdkPath))
                    {
                        continue;
                    }

                    if (!File.Exists($"{sdkPath}makeappx.exe")
                        || !File.Exists($"{sdkPath}signtool.exe"))
                    {
                        continue;
                    }

                    return sdkPath;
                }
            }
        }

        return null;
    }

    private IEnumerable<string> GetInstalledSdkVersions()
    {
        // regedit Computer\HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows Kits\Installed Roots\10.0.20348.0
        RegistryKey? sdkVersionsRegistry = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows Kits\\Installed Roots");
        if (sdkVersionsRegistry == null)
        {
            return Enumerable.Empty<string>();
        }

        var versions = sdkVersionsRegistry.GetSubKeyNames();
        if (versions == null)
        {
            return Enumerable.Empty<string>();
        }

        List<string> sdkVersions = versions
            .OrderByDescending(x => x)
            .ToList();

        return sdkVersions;
    }
}
