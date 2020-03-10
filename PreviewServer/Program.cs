using Kooboo.Data;
using Kooboo.IndexedDB;
using Kooboo.Lib.Reflection;
using Kooboo.Mail;
using Kooboo.Render.Controller;
using Kooboo.Web;
using System;
using System.IO;
using System.Reflection;

namespace PreviewServer
{
    class Program
    {
        static void Main(string[] args)
        {
            LoadModuleDll();
            ModuleFile.ModuleRoots.Add(Path.Combine(AppContext.BaseDirectory,"view"));

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

        private static void LoadModuleDll()
        {
            var path = AppDomain.CurrentDomain.BaseDirectory;
            var alldlls = Directory.GetFiles(path, "*.dll", SearchOption.TopDirectoryOnly);
            foreach (var name in alldlls)
            {
                string dllname = name.Substring(path.Length);

                if (string.IsNullOrWhiteSpace(dllname))
                {
                    continue;
                }

                dllname = dllname.Trim('\\').Trim('/');

                if (dllname.EndsWith(".dll"))
                {
                    var otherAssembly = Assembly.LoadFile(name);
                    AssemblyLoader.AddAssembly(otherAssembly);
                }
            }
        }
    }
}
