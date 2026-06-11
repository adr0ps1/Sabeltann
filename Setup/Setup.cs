using WixSharp;
using WixSharp.UI.Forms;

class Script
{
    static public void Main()
    {
        var project = new Project("Sabeltann",
            new Dir(@"%ProgramFiles%\Sabeltann",
                new Files(@"..\bin\Release\net10.0\publish\*.*")),
            new Dir(@"%ProgramMenu%\Sabeltann",
                new ExeFileShortcut("Sabeltann IPTV", @"[ProgramFiles64Folder]\Sabeltann\Sabeltann.exe", ""),
                new ExeFileShortcut("Uninstall Sabeltann", "[System64Folder]msiexec.exe", "/x [ProductCode]")),
            new Dir(@"%Desktop%",
                new ExeFileShortcut("Sabeltann IPTV", @"[ProgramFiles64Folder]\Sabeltann\Sabeltann.exe", ""))
        );

        project.GUID = new Guid("A1B2C3D4-E5F6-7890-ABCD-EF1234567890");
        project.Version = Environment.GetEnvironmentVariable("PRODUCT_VERSION") is string v && !string.IsNullOrEmpty(v)
            ? Version.Parse(v.TrimStart('v'))
            : new Version(1, 0, 0);
        project.Manufacturer = "Sabeltann";
        project.UI = WUI.WixUI_InstallDir;
        project.Description = "IPTV Player";
        project.MajorUpgrade = MajorUpgrade.Default;

        Compiler.BuildMsi(project);
    }
}
