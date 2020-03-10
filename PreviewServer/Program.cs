using Kooboo.Data;
using Kooboo.IndexedDB;
using Kooboo.Lib.Reflection;
using Kooboo.Mail;
using Kooboo.Render.Controller;
using Kooboo.Web;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace PreviewServer
{
    class Program
    {
        static void Main(string[] args)
        {
            AddModule();

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

        private static void AddModule()
        {
            var slnDir= Path.GetFullPath("../../../../");
            ModuleFile.ModuleRoots.Add(slnDir);
            var dirs = Directory.GetDirectories(slnDir);
            foreach (var item in dirs.Where(s=>s.ToLower().EndsWith("module")))
            {
                var dllPath = Path.Combine(item, "bin", "Debug", "netstandard2.0");

                if (Directory.Exists(dllPath))
                {
                    var alldlls = Directory.GetFiles(dllPath, "*.dll", SearchOption.TopDirectoryOnly);
                    foreach (var name in alldlls)
                    {

                        var otherAssembly = Assembly.LoadFile(name);
                        AssemblyLoader.AddAssembly(otherAssembly);
                    }
                }
            }
        }
    }
}
