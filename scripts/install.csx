#r "nuget:System.IO.Compression.ZipFile, 4.3.0"
#r "nuget:Newtonsoft.Json, 11.0.2"

using System.IO.Compression;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

var client = new HttpClient();
client.DefaultRequestHeaders.Add("User-Agent", "Kooboo");
var version = await GetVersion();
Console.WriteLine($"install version is {version}");
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
Console.WriteLine($"uninstall old version");
Console.WriteLine($"downloading...");
var result = await client.GetStreamAsync($"https://github.com/Kooboo/Module/archive/{version}.zip");
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
async Task<string> GetVersion()
{
    var tag = Args.FirstOrDefault();
    if (!string.IsNullOrWhiteSpace(tag)) return tag;
    var content = await client.GetStringAsync(new Uri("https://api.github.com/repos/Kooboo/Module/releases/latest"));
    var json = JsonConvert.DeserializeObject<JObject>(content);
    return json.GetValue("tag_name").Value<string>();
}

string GetTemplateDir()
{
    if (!Directory.Exists(extractDir)) return null;
    var moduleName = Directory.GetDirectories(extractDir).FirstOrDefault();
    if (moduleName == null) return null;
    return Path.Combine(extractDir, moduleName, "template");
}