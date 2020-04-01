#r "nuget:System.IO.Compression.ZipFile, 4.3.0"

using System.IO.Compression;
using System.Net;
using System.Net.Http;

var client = new HttpClient();
client.DefaultRequestHeaders.Add("User-Agent", "Kooboo");
var extractDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".koobooTemplate");
var templateDir = GetTemplateDir();

Console.WriteLine($"uninstall old version");
if (templateDir != null)
{
    Process.Start(new ProcessStartInfo
    {
        FileName = "dotnet",
        Arguments = $"new -u {templateDir}"
    }).WaitForExit();
}

if (Directory.Exists(extractDir)) Directory.Delete(extractDir, true);

Console.WriteLine($"downloading...");
var result = await client.GetStreamAsync($"https://github.com/Kooboo/Module/archive/master.zip");
var zipFile = new ZipArchive(result);
zipFile.ExtractToDirectory(extractDir);
zipFile.Dispose();

Process.Start(new ProcessStartInfo
{
    WorkingDirectory = GetTemplateDir(),
    FileName = "dotnet",
    Arguments = $"new -i ./"
}).WaitForExit();

Console.WriteLine($"install success !");

string GetTemplateDir()
{
    if (!Directory.Exists(extractDir)) return null;
    var moduleName = Directory.GetDirectories(extractDir).FirstOrDefault();
    if (moduleName == null) return null;
    return Path.Combine(extractDir, moduleName, "template");
}