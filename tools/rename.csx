var fileExtensions = new[] { ".cs", ".cshtml", ".html", ".js", ".csproj", ".sln", ".xml", ".config", "Dockerfile" };
var excludeDirs = new[] { ".git" };

Console.WriteLine("请输入被替换的字符串，默认‘MyCustom’：");
var oldStr = Console.ReadLine();
if (string.IsNullOrWhiteSpace(oldStr)) oldStr = "MyCustom";

var newStr = "";
while (string.IsNullOrWhiteSpace(newStr))
{
    Console.WriteLine("请输入新替换的字符串：");
    newStr = Console.ReadLine();
}


void Rename(string rootDir)
{
    var path = Path.GetFullPath(rootDir);
    if(path.EndsWith("Kooboo")) return;
    Console.WriteLine($"当前处理目录{path}");

    var dirs = Directory.GetDirectories(path);
    foreach (var dir in dirs)
    {
        if (excludeDirs.Any(a => dir.Contains(a))) continue;
        Rename(dir);
        var dirInfo = new DirectoryInfo(dir);
        var newDirName = dirInfo.Name.Replace(oldStr, newStr);
        newDirName = Path.Combine(path, newDirName);
        if (dir != newDirName)
        {
            Directory.Move(dir, newDirName);
            Console.WriteLine($"成功处理{dir}");
        }
    }

    var files = Directory.GetFiles(rootDir);
    foreach (var file in files)
    {
        if (fileExtensions.All(a =>!file.EndsWith(a))) continue;
        var content = File.ReadAllText(file, Encoding.UTF8);
        content = content.Replace(oldStr, newStr);
        File.WriteAllText(file, content);
        var fileName = Path.GetFileName(file);
        var newFileName = fileName.Replace(oldStr, newStr);
        newFileName = Path.Combine(path, newFileName);
        if (file != newFileName)
        {
            File.Move(file, newFileName);
            Console.WriteLine($"成功处理{file}");
        }

    }
}

Rename("..\\");

Console.WriteLine("处理成功，按任意键退出");
Console.ReadKey();