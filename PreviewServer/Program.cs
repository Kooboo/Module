using Kooboo.Data;
using Kooboo.IndexedDB;
using Kooboo.Lib.Compatible;
using Kooboo.Lib.Reflection;
using Kooboo.Mail;
using Kooboo.Web;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using VirtualFile;

namespace PreviewServer
{
    class Program
    {
        static void Main(string[] args)
        {
            WindowSystem.TryPath.Add("../../../../Kooboo");
            AddModule("../");

            Kooboo.Lib.Compatible.CompatibleManager.Instance.Framework.RegisterEncoding();
            GlobalSettings.RootPath = Kooboo.Data.AppSettings.DatabasePath;

            var initport = AppSettings.InitPort();
            if (!initport.Ok)
            {
                Console.WriteLine(initport.ErrorMessage);
                return;
            }

            Kooboo.Data.Hosts.WindowsHost.change = new Kooboo.Data.Hosts.HostChange() { NoChange = true };
            Kooboo.Data.AppSettings.DefaultLocalHost = "localkooboo.com";
            Kooboo.Data.AppSettings.StartHost = "127.0.0.1";

            SystemStart.Start(initport.HttpPort);
            Console.WriteLine("Web Server Started at port:" + initport.HttpPort.ToString());

            EmailWorkers.Start();
            Kooboo.Lib.Compatible.CompatibleManager.Instance.Framework.ConsoleWait();
        }

        private static void AddModule(string path)
        {
            path = Path.GetFullPath(path);
            var parent = Directory.GetParent(path).FullName;
            var configJsons = Directory.GetFiles(parent, "*config.json", SearchOption.AllDirectories);
            Kooboo.Render.Controller.ModuleFile.ModuleRoots.Add(parent);

            if (configJsons.Count() > 0)
            {
                foreach (var item in configJsons)
                {
                    var moduleDic = Path.GetDirectoryName(item);
                    LoadConfig(item);
                    LoadFiles(moduleDic);
                    LoadDir(moduleDic);
                }
            }

            if (Directory.GetFiles(parent, "*.sln").Count() > 0)
            {
                return;
            }
            else
            {
                AddModule(parent);
            }
        }

        private static void LoadConfig(string item)
        {
            var moduleName = Path.GetFileName(Path.GetDirectoryName(item));
            var path = Path.Combine(AppSettings.ModulePath, moduleName, "config.json");
            path = Helper.NormalizePath(path);
            VirtualResources.Entries.TryAdd(path, new ModuleFile(item, "module"));
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
