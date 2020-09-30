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
        static void Main(string[] args)
        {
            Module.Load();

            #region Kooboo Code
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
            #endregion
        }
    }
}
