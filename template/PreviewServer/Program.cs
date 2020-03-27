using Kooboo.Data;
using Kooboo.IndexedDB;
using Kooboo.Lib.Compatible;
using Kooboo.Lib.Helper;
using Kooboo.Lib.VirtualFile.Zip;
using Kooboo.Mail;
using Kooboo.Web;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using VirtualFile;

namespace PreviewServer
{
    class Program
    {
        static string[] _langs = new[] { "zh" };
        static string _koobooPath = "../../../../Kooboo";
        static string _slnPath = "../../../../";
        static string _modulePath = "../../../../MyCustom.Module";

        static void Main(string[] args)
        {
            WindowSystem.TryPath.Add(_koobooPath);
            Kooboo.Render.Controller.ModuleFile.ModuleRoots.Add(_slnPath);
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            AddModule();

            CompatibleManager.Instance.Framework.RegisterEncoding();
            GlobalSettings.RootPath = AppSettings.DatabasePath;

            var initport = AppSettings.InitPort();
            if (!initport.Ok)
            {
                Console.WriteLine(initport.ErrorMessage);
                return;
            }

            Kooboo.Data.Hosts.WindowsHost.change = new Kooboo.Data.Hosts.HostChange() { NoChange = true };
            AppSettings.DefaultLocalHost = "localkooboo.com";
            AppSettings.StartHost = "127.0.0.1";

            SystemStart.Start(initport.HttpPort);
            Console.WriteLine("Web Server Started at port:" + initport.HttpPort.ToString());

            EmailWorkers.Start();
            CompatibleManager.Instance.Framework.ConsoleWait();
        }

        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            var assemblyName = new AssemblyName(args.Name);
            var name = assemblyName.Name;
            var path = Directory.GetFiles(Path.GetFullPath(_koobooPath), $"{name}.dll", SearchOption.AllDirectories).FirstOrDefault();
            if (path == null) return null;
            return Assembly.Load(File.ReadAllBytes(path));
        }

        private static void AddModule()
        {
            var moduleDic = Path.GetFullPath(_modulePath);
            GenerateLang(moduleDic);
            LoadConfig(moduleDic);
            LoadFiles(moduleDic);
            LoadDir(moduleDic);
        }

        private static void GenerateLang(string moduleDic)
        {
            var jsonPath = Path.Combine(moduleDic, "config.json");

            var koobooKeys = XDocument.Load(Path.Combine(_koobooPath, "Lang", "en.xml"))
                                      .Root
                                      .Elements()
                                      .Select(s => s.Attribute("id").Value)
                                      .ToArray();

            var moduleKeys = GetKeys(moduleDic);
            var exceptKeys = moduleKeys.Except(koobooKeys);

            var json = JsonHelper.DeserializeJObject(File.ReadAllText(jsonPath));
            if (!json.ContainsKey("langs")) json.Add("langs", new JObject());
            var langs = json.Property("langs").Value as JObject;

            foreach (var item in _langs)
            {
                if (!langs.ContainsKey(item)) langs.Add(item, new JObject());
                var lang = langs.Property(item).Value as JObject;

                var removeKeys = new List<string>();

                foreach (var key in lang.Properties())
                {
                    if (!exceptKeys.Contains(key.Name) && string.IsNullOrEmpty(key.Value.ToString()))
                    {
                        removeKeys.Add(key.Name);
                    }
                }

                foreach (var key in removeKeys)
                {
                    lang.Remove(key);
                }

                foreach (var key in exceptKeys)
                {
                    if (!lang.ContainsKey(key))
                    {
                        lang.Add(key, "");
                    }
                }
            }

            File.WriteAllText(jsonPath, json.ToString(), Encoding.UTF8);
        }

        private static HashSet<string> GetKeys(string moduleDic)
        {
            var htmls = Directory.GetFiles(moduleDic, "*.html", SearchOption.AllDirectories);
            var allKeys = new HashSet<string>();

            foreach (var item in htmls)
            {
                var text = File.ReadAllText(item);
                var keys = Kooboo.Data.Language.MultiLingualHelper.GetDomKeys(text);
                foreach (var key in keys)
                {
                    if (string.IsNullOrEmpty(key)) continue;
                    if (key.Contains("{{") && key.Contains("}}")) continue;
                    allKeys.Add(key);
                }
            }

            return allKeys;
        }

        private static void LoadConfig(string moduleDic)
        {
            var jsonPath = Path.Combine(moduleDic, "config.json");
            var moduleName = Path.GetFileName(moduleDic);
            var path = Path.Combine(AppSettings.ModulePath, moduleName, "config.json");
            path = Helper.NormalizePath(path);
            VirtualResources.Entries.TryAdd(path, new ModuleFile(jsonPath, "module"));

            var jObject = JsonHelper.DeserializeJObject(File.ReadAllText(jsonPath));
            if (jObject.TryGetValue("mappings", out var obj))
            {
                var fileMaps = obj.ToObject<FileMapping[]>();
                foreach (var fileMap in fileMaps)
                {
                    fileMap.From = fileMap.From.Trim();
                    if (fileMap.From.StartsWith("/")) fileMap.From = fileMap.From.Substring(1);
                    var fromPath = Path.Combine(AppSettings.RootPath, fileMap.From);
                    var fileMapFrom = Helper.NormalizePath(fromPath);
                    var virtualFile = new ModuleFile(Path.Combine(moduleDic, fileMap.To), "module");
                    VirtualResources.Mappings[fileMapFrom] = virtualFile;
                }
            }
        }

        private static void LoadDir(string moduleDic)
        {
            foreach (var item in Directory.GetDirectories(moduleDic))
            {
                var dirName = Path.GetFileName(item);
                if (dirName == "obj") continue;
                if (dirName == "bin")
                {
                    var dllPath = Path.Combine(item, "Debug", "netstandard2.0");
                    var alldlls = Directory.GetFiles(dllPath, "*.dll", SearchOption.TopDirectoryOnly);
                    var koobooDlls = Directory.GetFiles(_koobooPath, "*.dll", SearchOption.AllDirectories)
                        .Select(s => Path.GetFileName(s));
                    foreach (var name in alldlls)
                    {
                        if (koobooDlls.Any(a => a == Path.GetFileName(name))) continue;
                        var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "modules", Path.GetFileName(name));
                        VirtualResources.Entries.TryAdd(Helper.NormalizePath(path), new ModuleFile(name, "module"));
                    }
                }
                else
                {
                    var files = Directory.GetFiles(item, "*", SearchOption.AllDirectories);
                    foreach (var file in files)
                    {
                        var normalPath = Helper.NormalizePath(file);
                        VirtualResources.Entries.TryAdd(normalPath, new ModuleFile(normalPath, "module"));
                    }
                }
            }
        }

        private static void LoadFiles(string parent)
        {
            var files = Directory.GetFiles(parent);
            foreach (var file in files)
            {
                var normalPath = Helper.NormalizePath(file);
                VirtualResources.Entries.TryAdd(normalPath, new ModuleFile(normalPath, "module"));
            }
        }
    }
}
