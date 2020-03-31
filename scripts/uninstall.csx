var extractDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".koobooTemplate");
var templateDir = GetTemplateDir();

if (templateDir != null)
{
    Process.Start(new ProcessStartInfo
    {
        FileName = "dotnet",
        Arguments = $"new -u {templateDir}"
    }).WaitForExit();
}

if (Directory.Exists(extractDir)) Directory.Delete(extractDir, true);
Console.WriteLine($"uninstall success");


string GetTemplateDir()
{
    if (!Directory.Exists(extractDir)) return null;
    var moduleName = Directory.GetDirectories(extractDir).FirstOrDefault();
    if (moduleName == null) return null;
    return Path.Combine(extractDir, moduleName, "template");
}