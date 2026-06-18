using WixSharp;

if (args.Length < 3)
{
    Console.Error.WriteLine("Usage: SabeltannSetup <publishDir> <version> <outDir>");
    return 1;
}

var publishDir = Path.GetFullPath(args[0]);
var version = args[1].TrimStart('v');
var outDir = Path.GetFullPath(args[2]);

if (!Directory.Exists(publishDir))
{
    Console.Error.WriteLine($"Publish directory not found: {publishDir}");
    return 1;
}

Directory.CreateDirectory(outDir);

var mainExe = Path.Combine(publishDir, "Sabeltann.exe");

var project = new Project("Sabeltann",
    new Dir(@"%ProgramFiles%\Sabeltann",
        BuildEntries(publishDir, mainExe)
    ))
{
    // UpgradeCode — must stay constant across all releases so upgrades work.
    // ProductCode is auto-generated per build by default.
    GUID = new Guid("A1B2C3D4-E5F6-7890-ABCD-EF1234567890"),
    Version = new Version(version),
    Manufacturer = "Sabeltann",
    Platform = Platform.x64,
    InstallScope = InstallScope.perMachine,
    MajorUpgrade = new MajorUpgrade
    {
        DowngradeErrorMessage = "A newer version of Sabeltann is already installed.",
    },
    ControlPanelInfo = new ProductInfo
    {
        Manufacturer = "Sabeltann",
        Comments = "IPTV Player for Windows",
    },
    OutFileName = $"Sabeltann-{version}",
    OutDir = outDir,
};

project.BuildMsi();
Console.WriteLine($"MSI: {Path.Combine(outDir, $"Sabeltann-{version}.msi")}");
return 0;

// Recursively collect all files and subdirectories from the publish folder.
// The main exe gets Start Menu and Desktop shortcuts; everything else is a plain file.
// This handles the VLC plugins/ subdirectory tree automatically.
static WixEntity[] BuildEntries(string dir, string mainExePath)
{
    var entities = new List<WixEntity>();

    foreach (var file in Directory.GetFiles(dir))
    {
        if (file.Equals(mainExePath, StringComparison.OrdinalIgnoreCase))
        {
            entities.Add(new File(file,
                new FileShortcut("Sabeltann IPTV", "%Desktop%")
                {
                    IconFile = mainExePath,
                    IconIndex = 0,
                    WorkingDirectory = "[INSTALLFOLDER]",
                },
                new FileShortcut("Sabeltann IPTV", @"%ProgramMenu%\Sabeltann")
                {
                    IconFile = mainExePath,
                    IconIndex = 0,
                    WorkingDirectory = "[INSTALLFOLDER]",
                }));
        }
        else
        {
            entities.Add(new File(file));
        }
    }

    foreach (var subDir in Directory.GetDirectories(dir))
    {
        var children = BuildEntries(subDir, mainExePath);
        if (children.Length > 0)
            entities.Add(new Dir(Path.GetFileName(subDir), children));
    }

    return [.. entities];
}
