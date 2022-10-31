using Cocona;
using Microsoft.Extensions.Configuration;
using Microsoft.Win32;

namespace MsixStoreTool.Commands;

internal class MsixCommand
{
    private readonly IConfiguration _configuration;

    public MsixCommand(IConfiguration configuration)
    {
        this._configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }


    [Command("bundle")]
    public async Task Bundle([Argument(Description = "path to the directory with all msix-files")] string msixDirectory,
        [Argument(Description = "the output msixbundle filename")] string outputMsixFile,
        [Argument(Description = "path to the pfx file")] string pfxFile,
        [Argument(Description = "the hash-algorithm like SHA256")] string hashAlgorithm)
    {
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

        // create temp directory
        string tempDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MsixStoreTool", "temp");
        Directory.CreateDirectory(tempDirectory);

        try
        {
            // load all msix files
            string[] files = Directory.GetFiles(msixDirectory, "*.msix");

            // copy msix files to temp directory
            files.ToList().ForEach(x => File.Copy(x, Path.Combine(tempDirectory, Path.GetFileName(x)), true));

            // generate msixbundle file in temp
            // cli: .\makeappx.exe bundle /d "D:\Repos\lk-code\simple-markdown\SimpleMarkdown\AppPackages\SimpleMarkdown_2.1.47.0_Test\" /p "D:\Repos\lk-code\simple-markdown\SimpleMarkdown\AppPackages\SimpleMarkdown_2.1.47.0_Test\SimpleMarkdown_2.1.47.0_x86_x64_arm64.msixbundle"

            // sign msixbundle file
            // cli: .\signtool.exe sign /fd SHA256 /a /f "D:\Repos\lk-code\simple-markdown\SimpleMarkdown\SimpleMarkdown_TemporaryKey.pfx" "D:\Repos\lk-code\simple-markdown\SimpleMarkdown\AppPackages\SimpleMarkdown_2.1.47.0_Test\SimpleMarkdown_2.1.47.0_x86_x64_arm64.msixbundle"

            // copy msixbundle file from temp to msix-directory
        }
        catch (Exception exception)
        {
            Console.WriteLine("ERROR");
            Console.WriteLine(exception.Message);
        }
        finally
        {
            try
            {
                // delete temp directory
                Directory.Delete(tempDirectory, true);
            }
            catch (Exception exception)
            {
                Console.WriteLine("ERROR");
                Console.WriteLine(exception.Message);
            }
        }
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
