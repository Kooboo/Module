using Kooboo.Data;
using Kooboo.Dom;
using Kooboo.IndexedDB;
using Kooboo.Lib.Compatible;
using Kooboo.Lib.Helper;
using Kooboo.Lib.Reflection;
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
        static void Main(string[] args)
        {
            WindowSystem.TryPath.Add("../../../../Kooboo");
            Kooboo.Render.Controller.ModuleFile.ModuleRoots.Add("../../../../");
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

        private static void AddModule()
        {
            var dic = Directory.GetDirectories("../../../../");
            var moduleDic = dic.First(f => f.ToLower().EndsWith(".module"));
            moduleDic = Path.GetFullPath(moduleDic);
            GenerateLang(moduleDic);
            LoadConfig(moduleDic);
            LoadFiles(moduleDic);
            LoadDir(moduleDic);
        }

        private static void GenerateLang(string moduleDic)
        {
            var jsonPath = Path.Combine(moduleDic, "config.json");

            var koobooKeys = XDocument.Load("../../../../Kooboo/Lang/en.xml")
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
                    foreach (var name in alldlls)
                    {

                        var otherAssembly = Assembly.LoadFile(name);
                        AssemblyLoader.AddAssembly(otherAssembly);
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
