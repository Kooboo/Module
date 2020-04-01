#r "nuget:System.IO.Compression.ZipFile, 4.3.0"

using System.IO.Compression;
using System.Net.Http;

var branch = "master";
try
{
    branch = File.ReadAllText("./branch.txt");
}
catch (System.Exception)
{
}

Console.WriteLine($"use branch {branch} to update");
var client = new HttpClient();
client.DefaultRequestHeaders.Add("User-Agent", "Kooboo");
var extractDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".koobooSource");

if (Directory.Exists(extractDir))
{
    Console.WriteLine("clean old kooboo source");
    Directory.Delete(extractDir, true);
}

Console.WriteLine("downloading...");
var result = await client.GetStreamAsync($"https://github.com/Kooboo/Kooboo/archive/{branch}.zip");
var zipFile = new ZipArchive(result);
zipFile.ExtractToDirectory(extractDir);
zipFile.Dispose();
var entryDir = Path.Combine(extractDir, $"Kooboo-{branch.Replace('/', '-')}", "Kooboo.App.Standard");

Console.WriteLine("publish win-x64");
Process.Start(new ProcessStartInfo
{
    WorkingDirectory = entryDir,
    FileName = "dotnet",
    Arguments = $"restore -r win-x64 -v q"
}).WaitForExit();

Process.Start(new ProcessStartInfo
{
    WorkingDirectory = entryDir,
    FileName = "dotnet",
    Arguments = $"publish -o bin\\Release\\PublishOutput -c Debug -f netcoreapp2.1 -r win-x64 --no-restore -v q --self-contained false"
}).WaitForExit();

var zipPath = Path.Combine(entryDir, "bin", "Release", "KoobooLinux.zip");
var winZipDir = Path.Combine(entryDir, "bin", "Release", "win");
ZipFile.ExtractToDirectory(zipPath, winZipDir);
var winKoobooDir=Path.Combine(winZipDir, "Kooboo");

foreach (var item in Directory.GetFiles(winKoobooDir, "*.*", SearchOption.AllDirectories))
{
    var relativePath = item.Substring(winKoobooDir.Length).Trim(' ', '/', '\\');
    var targetPath = Path.Combine(Path.GetFullPath("../Kooboo"), relativePath);
    var targetDir = Path.GetDirectoryName(targetPath);
    if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);
    File.Copy(item, targetPath, true);
}
File.Delete(zipPath);
Directory.Delete(Path.Combine(entryDir, "bin", "Release", "PublishOutput"), true);

Console.WriteLine("publish linux-x64");
Process.Start(new ProcessStartInfo
{
    WorkingDirectory = entryDir,
    FileName = "dotnet",
    Arguments = $"restore -r linux-x64 -v q"
}).WaitForExit();

Process.Start(new ProcessStartInfo
{
    WorkingDirectory = entryDir,
    FileName = "dotnet",
    Arguments = $"publish -o bin\\Release\\PublishOutput -c Release -f netcoreapp2.1 -r linux-x64 --no-restore -v q --self-contained false"
}).WaitForExit();

File.Move(zipPath, "../KoobooLinux.zip", true);

Console.WriteLine("success");

