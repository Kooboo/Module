#r "nuget:System.IO.Compression.ZipFile, 4.3.0"

using System.IO.Compression;
using System.Net.Http;

var client = new HttpClient();
var version = Args.FirstOrDefault() ?? "0.1";
var extractDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".koobooTemplate");
if (Directory.Exists(extractDir)) Directory.Delete(extractDir, true);

var result = await client.GetStreamAsync($"https://github.com/Kooboo/Module/archive/{version}.zip");
var zipFile = new ZipArchive(result);
zipFile.ExtractToDirectory(extractDir);
zipFile.Dispose();
var moduleName = Directory.GetDirectories(extractDir).First();
var templatePath = Path.Combine(extractDir, moduleName, "template");

Process.Start(new ProcessStartInfo
{
    WorkingDirectory = templatePath,
    FileName = "dotnet",
    Arguments = $"new -i ./"
}).WaitForExit();
