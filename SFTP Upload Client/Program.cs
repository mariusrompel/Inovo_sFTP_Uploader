using log4net;
using log4net.Config;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SFTP_Upload_Client
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        static void Main(string[] args)
        {
            XmlConfigurator.Configure();
            Log.Info("Application : Audio Converter Service");
            Assembly assembly = Assembly.GetExecutingAssembly();
            FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
            Log.Info("Version     : " + fileVersionInfo.ProductVersion);
            //InovoLogger.Log4Net.getInstance().Trace(3, "Test Log");
            if (args.Length > 0)
            {
                if (args[0] == "-i")
                {
                    ServiceUtil Svc = new ServiceUtil("Inovo SFTP Upload Service", "Service to upload files to SFTP server.");
                    Svc.InstallService();
                }
                else if (args[0] == "-u")
                {
                    ServiceUtil Svc = new ServiceUtil("Inovo SFTP Upload Service", "Service to upload files to SFTP server.");
                    Svc.UninstallService();
                }
                else if (args[0].ToLower() == "-debug")
                {
                    //Then run it as a console app
                    Log.Debug("Starting app in debug mode");
                    SFTPUploader consoleService = new SFTPUploader();
                    if (args.Length > 1)
                        consoleService.ConsoleStart(args[1]);
                    else
                        consoleService.ConsoleStart("");
                    Application.Run();
                }
            }
            else
            {

                ServiceBase[] ServicesToRun;

                ServicesToRun = new ServiceBase[] { new SFTPUploader() };

                ServiceBase.Run(ServicesToRun);
            }
        }
    }
}
