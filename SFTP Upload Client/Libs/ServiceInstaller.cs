using System;
using System.Configuration.Install;
using System.ServiceProcess;
using System.Reflection;
using System.IO;
using System.Collections;

//Remember to include the following References
//  System.Management
//  System.ServiceProcess
//  System.Configuration.Install
//  System.Windows.Forms        -   This implies that "using System.Windows.Forms" should also be in the Program.cs file

// The program must have the following structure
//static void Main(string[] args)
//  {
//  if (args.Length > 0)
//            {
//                if (args[0] == "-i")
//                {
//                    ServiceUtil Svc = new ServiceUtil("BCX Application Name Service", "Service Description");
//                    Svc.UninstallService();
//                    Svc.InstallService();
//                }
//                else if (args[0] == "-u")
//                {
//                    ServiceUtil Svc = new ServiceUtil("BCX Application Name Service", "Service Description");
//                    Svc.UninstallService();
//                }
//                else if (args[0].ToLower() == "-debug")
//                {
//                    //Then run it as a console app
//                    Service1 consoleService = new Service1();
//                    consoleService.ConsoleStart();
//                    Application.Run();
//                }
//            }
//            else
//            {
//                System.ServiceProcess.ServiceBase[] ServicesToRun;
//                //More than one user Service may run within the same process. To add
//                //another service to this process, change the following line to
//                //create a second service object. For example,
//
//                //ServicesToRun = new ServiceBase[] {new Service1(), new MySecondUserService()};
//
//                ServicesToRun = new System.ServiceProcess.ServiceBase[] { new Service1() };
//                System.ServiceProcess.ServiceBase.Run(ServicesToRun);
//            }
//  }

// !!!!!!!!!!!!!!!!!!!!!! NOTE !!!!!!!!!!!!!!!!!!!!!!!!
//The Service.cs file MUST have the following method included as well
//  public void ConsoleStart()
//        {
//            OnStart(new string[1]);
//        }
// !!!!!!!!!!!!!!!!!!!!!! NOTE !!!!!!!!!!!!!!!!!!!!!!!!


public class SvcInstaller : Installer
{

    public SvcInstaller(string szServiceName, string szDescription)
    {
        ServiceProcessInstaller spi = new ServiceProcessInstaller();
        spi.Account = ServiceAccount.LocalSystem;

        ServiceInstaller si = new ServiceInstaller();
        si.ServiceName = szServiceName;
        si.Description = szDescription;
        si.StartType = ServiceStartMode.Automatic;


        this.Installers.Add(spi);
        this.Installers.Add(si);
    }

    public SvcInstaller(string szServiceName, string szDescription, string szUserName, string szPassword)
    {
        ServiceProcessInstaller spi = new ServiceProcessInstaller();
        if (szUserName == "")
            spi.Username = null;
        else
            spi.Username = szUserName;
        if (szPassword == "")
            spi.Password = null;
        else
            spi.Password = szPassword;


        ServiceInstaller si = new ServiceInstaller();
        si.ServiceName = szServiceName;
        si.Description = szDescription;
        si.StartType = ServiceStartMode.Automatic;


        this.Installers.Add(spi);
        this.Installers.Add(si);
    }
}

public class ServiceUtil
{
    private string ServiceName;
    private string ServiceDescription;

    public ServiceUtil(string sServiceName, string sDescription)
    {
        ServiceName = sServiceName;
        ServiceDescription = sDescription;
    }

    public bool InstallService()
    {
        try
        {
            TransactedInstaller ti = new TransactedInstaller();
            SvcInstaller si = new SvcInstaller(ServiceName, ServiceDescription);
            ti.Installers.Add(si);

            string basePath = Environment.ProcessPath;
            string path = string.Format("/assemblypath={0}", basePath);
            string[] cmdline = { path };
            InstallContext ctx = new InstallContext(Path.ChangeExtension(basePath, ".InstallLog"), cmdline);
            ti.Context = ctx;
            ti.Install(new Hashtable());
        }
        catch (Exception e)
        {
            Console.WriteLine("Failed to install service: " + e.Message);
            return false;
        }
        return true;
    }

    public bool UninstallService()
    {
        try
        {
            TransactedInstaller ti = new TransactedInstaller();
            SvcInstaller si = new SvcInstaller(ServiceName, ServiceDescription);
            ti.Installers.Add(si);
            string basePath = Environment.ProcessPath;
            string path = string.Format("/assemblypath=\"{0}\"", basePath);
            string[] cmdline = { path };
            InstallContext ctx = new InstallContext(Path.ChangeExtension(basePath, ".UninstallLog"), cmdline);
            ti.Context = ctx;
            ti.Uninstall(null);
        }
        catch (Exception e)
        {
            Console.WriteLine("Failed to uninstall service: " + e.Message);
            return false;
        }
        return true;
    }
}

