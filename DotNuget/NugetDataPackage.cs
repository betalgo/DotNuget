using System.Diagnostics;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace DotNuget;

public class NugetDataPackage
{
    public NugetDataPackage()
    {
        
    }
    public NugetDataPackage(string projectFile, string packageName, string versionFile, string version, string nugetKey, string nugetSource, bool includeSymbols, bool noBuild, bool tagCommit, string tagFormat, string? certificatePath, string? certificatePassword,string versionRegexPattern)
    {
        ProjectFile = projectFile;
        PackageName = packageName;
        VersionFile = versionFile;
        Version = version;
        NugetKey = nugetKey;
        NugetSource = nugetSource;
        IncludeSymbols = includeSymbols;
        NoBuild = noBuild;
        TagCommit = tagCommit;
        TagFormat = tagFormat;
        CertificatePath = certificatePath;
        CertificatePassword = certificatePassword;
    }

    public string ProjectFile { get; set; }
    public string PackageName { get; set; }
    public string? VersionFile { get; set; }
    public string? Version { get; set; }
    public string? NugetKey { get; set; }
    public string? NugetSource { get; set; }
    public bool IncludeSymbols { get; set; }
    public bool NoBuild { get; set; }
    public bool TagCommit { get; set; }
    public string TagFormat { get; set; }
    public string? CertificatePath { get; set; }
    public string? CertificatePassword { get; set; }
    public string? VersionRegexPattern { get; set; }

    public void Validate()
    {
        //print all fields as json
        Console.WriteLine(JsonSerializer.Serialize(this));
        if (string.IsNullOrEmpty(ProjectFile) || !File.Exists(ProjectFile))
        {
            PrintErrorAndExit("Project file not found");
        }

        if (string.IsNullOrEmpty(PackageName) || !File.Exists(PackageName))
        {
            PrintErrorAndExit("PackageName not found");
        }

        if (string.IsNullOrEmpty(Version))
        {
            if (VersionFile != ProjectFile && !File.Exists(VersionFile))
            {
                PrintErrorAndExit("Version file not found");
            }
        
            var versionFileContent = File.ReadAllText(VersionFile);
            var versionMatch = Regex.Match(versionFileContent, VersionRegexPattern, RegexOptions.Multiline);

            if (!versionMatch.Success)
            {
                PrintErrorAndExit("Unable to extract version info!");
            }

            Version = versionMatch.Groups[1].Value;
        }

    }
    public async Task CheckForUpdate()
    {
        Validate();
        using var httpClient = new HttpClient();
        var url = $"{NugetSource}/v3-flatcontainer/{PackageName.ToLower()}/index.json";
        var response = await httpClient.GetAsync(url);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            Console.WriteLine("404 response, assuming new package");
            PushPackage();
            return;
        }

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var body = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            if (root.TryGetProperty("versions", out var versionsElement) && versionsElement.ValueKind == JsonValueKind.Array)
            {
                var versionExists = false;
                foreach (var versionElement in versionsElement.EnumerateArray())
                {
                    if (versionElement.GetString() == Version)
                    {
                        versionExists = true;
                        break;
                    }
                }

                if (!versionExists)
                {
                    PushPackage();
                }
            }
        }
    }

    private void PushPackage()
    {
        Console.WriteLine($"✨ Found new version ({Version}) of {PackageName}");

        if (string.IsNullOrEmpty(NugetKey))
        {
            Console.WriteLine("##[warning]😢 NUGET_KEY not given");
            return;
        }

        Console.WriteLine($"NuGet Source: {NugetSource}");

        // Delete existing .nupkg and .snupkg files
        foreach (var file in Directory.GetFiles(".", "*.nupkg"))
        {
            File.Delete(file);
        }

        foreach (var file in Directory.GetFiles(".", "*.snupkg"))
        {
            File.Delete(file);
        }

        // Build the project if noBuild is false
        if (!NoBuild)
        {
            ExecuteCommand($"dotnet build -c Release {ProjectFile}");
        }

        // Pack the project
        var symbolsFlag = IncludeSymbols ? "--include-symbols -p:SymbolPackageFormat=snupkg" : "";
        ExecuteCommand($"dotnet pack {symbolsFlag} -c Release {ProjectFile} -o .");


        // Sign the package if signing is enabled
        if (!string.IsNullOrEmpty(CertificatePath) && !string.IsNullOrEmpty(CertificatePassword))
        {
            SignPackage();
        }


        // Push the package
        const string skipDuplicate = "--skip-duplicate";
        var pushCommand = $"dotnet nuget push *.nupkg -s {NugetSource}/v3/index.json -k {NugetKey} {skipDuplicate}";
        var pushOutput = ExecuteCommand(pushCommand);

        Console.WriteLine(pushOutput);

        if (pushOutput.Contains("error"))
        {
            PrintErrorAndExit($"Error: {pushOutput}");
        }

        // Tag the commit if tagCommit is true
        if (TagCommit)
        {
            TagTheCommit();
        }
    }

    private void TagTheCommit()
    {
        var tag = TagFormat.Replace("*", Version);
        Console.WriteLine($"✨ Creating new tag {tag}");
        ExecuteCommand($"git tag {tag}");
        ExecuteCommand($"git push origin {tag}");
    }

    private string ExecuteCommand(string command)
    {
        var processInfo = new ProcessStartInfo("cmd.exe", "/c " + command)
        {
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        var process = new Process
        {
            StartInfo = processInfo
        };

        process.Start();

        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        return output;
    }

    private void SignPackage()
    {
        Console.WriteLine("✨ Signing the NuGet package");

        // Assuming only one .nupkg file is present in the directory
        string packageFile = null;
        foreach (var file in Directory.GetFiles(".", "*.nupkg"))
        {
            packageFile = file;
            break;
        }

        if (packageFile == null)
        {
            PrintErrorAndExit("No .nupkg file found for signing");
        }

        var signCommand = $"nuget sign {packageFile} -CertificatePath {CertificatePath} -CertificatePassword {CertificatePassword}";
        var signOutput = ExecuteCommand(signCommand);

        Console.WriteLine(signOutput);

        if (signOutput.Contains("error"))
        {
            PrintErrorAndExit($"Error while signing: {signOutput}");
        }
    }

    private static void PrintErrorAndExit(string message)
    {
        Console.WriteLine($"##[error]😭 {message}");
        Environment.Exit(1);
    }
}