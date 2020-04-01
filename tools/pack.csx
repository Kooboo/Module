#r "nuget:System.IO.Compression.ZipFile, 4.3.0"

using System.IO.Compression;
using System.Xml.Linq;

readonly string[] exclude = new[] { ".dll", ".cs", ".csproj", "csproj.user" };
var sourcePath = Path.GetFullPath("../");
var koobooPath = Path.Combine(sourcePath, "Kooboo");
var modulePath = Path.Combine(sourcePath, "SqlEx.Module");
var publishPath = GetPublishPath();
var zipDirPath = Path.Combine(publishPath, "..", "module");
var zipFilePath = Path.Combine(publishPath, "..", "SqlEx.Module.zip");
var publishFiles = Directory.GetFiles(publishPath, "*.*", SearchOption.AllDirectories);

var KoobooFiles = Directory.GetFiles(koobooPath, "*.*", SearchOption.AllDirectories)
                           .Select(s => s.Substring(koobooPath.Length).Trim(' ', '/', '\\'));

var moduleFiles = Directory.GetFiles(modulePath, "*.*", SearchOption.AllDirectories);

if (Directory.Exists(zipDirPath)) Directory.Delete(zipDirPath, true);
if (File.Exists(zipFilePath)) File.Delete(zipFilePath);

foreach (var item in publishFiles)
{
    var relativePath = item.Substring(publishPath.Length).Trim(' ', '/', '\\');
    if (KoobooFiles.Contains(relativePath)) continue;
    if (relativePath.StartsWith("PreviewServer")) continue;
    var targetPath = Path.Combine(zipDirPath, relativePath);
    var targetDir = Path.GetDirectoryName(targetPath);
    if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);
    File.Copy(item, targetPath, true);
}

foreach (var item in moduleFiles)
{
    var relativePath = item.Substring(modulePath.Length).Trim(' ', '/', '\\');
    if (relativePath.StartsWith("bin/") || relativePath.StartsWith("bin\\")) continue;
    if (relativePath.StartsWith("obj/") || relativePath.StartsWith("obj\\")) continue;
    if (exclude.Any(s => item.EndsWith(s))) continue;
    var targetPath = Path.Combine(zipDirPath, relativePath);
    var targetDir = Path.GetDirectoryName(targetPath);
    if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);
    File.Copy(item, targetPath, true);
}

ZipFile.CreateFromDirectory(zipDirPath, zipFilePath);

string GetPublishPath()
{
    var xml = Directory.GetFiles(sourcePath, "*generateZip.pubxml", SearchOption.AllDirectories).FirstOrDefault();
    if (xml == null) throw new Exception("cant find generateZip.pubxml");

    var publishPath = XDocument.Load(xml)?
                            .Root?.Elements()?.FirstOrDefault(f => f.Name.LocalName == "PropertyGroup")
                            ?.Elements()?.FirstOrDefault(f => f.Name.LocalName == "PublishDir")?.Value;

    return Path.Combine(sourcePath, "PreviewServer", publishPath);
}
