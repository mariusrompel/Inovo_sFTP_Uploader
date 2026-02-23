using log4net;
using Quartz;
using Quartz.Impl;
using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WinSCP;

namespace SFTP_Upload_Client
{
    public partial class SFTPUploader : ServiceBase, IJob
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        Thread thMain;
        public static Boolean isRunning = true;
        public static Boolean bRunning = false;

        public static String presenceDBConnString = "";
        public static String sourceFolder = "";
        public static String destBaseFolder = "";
        public static String sftpHost = "";
        public static Int32 sftpPort = 22;
        public static String sftpUser = "";
        public static String sftpPassword = "";
        public static Boolean bSimpleFileUpload = false;
        public static Boolean bDeleteLocalFile = false;
        public static Boolean bMoveLocalFile = false;
        public static Boolean bCreateDailyFolder = false;
        public static Boolean bRunPeriodic = false;
        public static Int32 waitTime = 30;
        public static Int32 triggerHouor = 5;
        public static Int32 triggerMinute = 0;

        IScheduler scheduler;
        JobKey jobKey;

        public SFTPUploader()
        {
            InitializeComponent();
        }

        public void ConsoleStart(String startParam)
        {
            Log.Debug("Starting console mode");
            OnStart(new string[1]);
        }

        protected override void OnStart(string[] args)
        {
            thMain = new Thread(new ThreadStart(RunService));

            thMain.Start();
        }

        protected override void OnStop()
        {
            isRunning = false;
            Thread.Sleep(1000);
            thMain.Abort();
        }

        TimeSpan ToTime(string value)
        {
            int hours;
            if (Int32.TryParse(value, out hours))
                return new TimeSpan(hours, 0, 0);

            //If it is neither of these then you have an exception
            return TimeSpan.Parse(value);
        }

        private Boolean ReadConfig()
        {
            try
            {
                Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal);
                AppSettingsSection appConfig = (AppSettingsSection)config.GetSection("appSettings");

                presenceDBConnString = appConfig.Settings["Presence DB Conn"].Value;
                sourceFolder = appConfig.Settings["Source Folder"].Value;
                destBaseFolder = appConfig.Settings["Destination Based Folder"].Value;
                sftpHost = appConfig.Settings["SFTP Host"].Value;
                sftpUser = appConfig.Settings["SFTP User"].Value;
                sftpPassword = appConfig.Settings["SFTP Password"].Value;
                try { bSimpleFileUpload = Boolean.Parse(appConfig.Settings["Simple File Upload Only"].Value); } catch (Exception e) { }
                try { bDeleteLocalFile = Boolean.Parse(appConfig.Settings["Delete local file"].Value); } catch (Exception e) { }
                try { bMoveLocalFile = Boolean.Parse(appConfig.Settings["Move local file"].Value); } catch (Exception e) { }
                try { bCreateDailyFolder = Boolean.Parse(appConfig.Settings["Create Daily Folder"].Value); } catch (Exception e) { }
                try { sftpPort = Int32.Parse(appConfig.Settings["SFTP Port"].Value); } catch (Exception e) { }
                try { bRunPeriodic = Boolean.Parse(appConfig.Settings["Run Periodic"].Value); } catch (Exception e) { bRunPeriodic = false; }
                try { waitTime = Int32.Parse(appConfig.Settings["Wait Time Sec"].Value); } catch (Exception e) { }
                try
                {
                    string trigTime = appConfig.Settings["Trigger Time"].Value;
                    TimeSpan ts = ToTime(trigTime);
                    triggerHouor = ts.Hours;
                    triggerMinute = ts.Minutes;
                } catch (Exception e)
                {
                    Log.Debug("Trigger Exception : " + e.Message);
                }


                Log.Debug("Configuration read");
                Log.Debug("presenceDBConnString => [" + presenceDBConnString + "]");
                Log.Debug("sourceFolder => [" + sourceFolder + "]");
                Log.Debug("destBaseFolder => [" + destBaseFolder + "]");
                Log.Debug("SFTP Host => [" + sftpHost + "]");
                Log.Debug("SFTP port => [" + sftpPort + "]");
                Log.Debug("SFTP User => [" + sftpUser + "]");
                Log.Debug("SFTP Password => [" + sftpPassword + "]");
                Log.Debug("Simple File Uploader => [" + bSimpleFileUpload + "]");
                Log.Debug("Trigger time => [" + triggerHouor + ":" + triggerMinute + "]");
                Log.Debug("Delete File after upload => [" + bDeleteLocalFile + "]");
                Log.Debug("Move File after upload => [" + bMoveLocalFile + "]");
                Log.Debug("Create Daily Folder in Destination => [" + bCreateDailyFolder + "]");
                Log.Debug("Run Periodic => [" + bRunPeriodic + "]");
                if(bRunPeriodic)
                    Log.Debug("Run Periodic (Wait time) => [" + waitTime + "]");
                return true;
            }
            catch (Exception e)
            {
                Log.Error("Failed to get configuration settings");
                return false;
            }
        }
        private void CreateScheduler()
        {
            //Lets start the scheduler
            scheduler = StdSchedulerFactory.GetDefaultScheduler();
            scheduler.Start();

            jobKey = new JobKey("Export Recordings");
            IJobDetail job = JobBuilder.Create<SFTPUploader>()
                            .WithIdentity(jobKey) // name "myJob", group "group1"
                            .StoreDurably(true)
                            .Build();

            job.JobDataMap["ScheduleName"] = "Upload Schedule";
            job.JobDataMap["ScheduleId"] = 1;
            job.JobDataMap["JobIds"] = 1;

            scheduler.AddJob(job, true);

            if (bRunPeriodic)
            {
                Log.Debug("Creating periodic schedule");
                //ITrigger periodicSched = TriggerBuilder.Create().WithIdentity("Periodic").StartNow().WithSimpleSchedule(x => x.WithIntervalInSeconds(30).RepeatForever()).ForJob(job).Build();
                ITrigger periodicSched = TriggerBuilder.Create().WithIdentity("Periodic").StartNow().WithSimpleSchedule(x => x.WithIntervalInSeconds(waitTime).RepeatForever()).ForJob(job).Build();
                scheduler.ScheduleJob(periodicSched);
            }
            else
            {
                Log.Debug("Running on schedule");
                ITrigger trigOnce = TriggerBuilder.Create().WithIdentity("Once a day").StartNow().WithSchedule(CronScheduleBuilder.DailyAtHourAndMinute(triggerHouor, triggerMinute)).ForJob(job).Build();
                scheduler.ScheduleJob(trigOnce);
            }


            Log.Debug("Scheduler created !");
        }
        public void CheckJobs()
        {
            if (scheduler == null) return;
            if (jobKey == null) return;

            IJobDetail detail = scheduler.GetJobDetail(jobKey);
            IList<ITrigger> triggers = scheduler.GetTriggersOfJob(jobKey);
            foreach (ITrigger trig in triggers)
            {
                String jobDetail = "Job Name [" + detail.JobDataMap["ScheduleName"] + "] JobIDs [" + detail.JobDataMap["JobIds"] + "] ScheduleId [" + detail.JobDataMap["ScheduleId"] + "] ] NextRun [" + TimeZone.CurrentTimeZone.ToLocalTime(trig.GetNextFireTimeUtc().Value.DateTime) + "]";

                Log.Info(jobDetail);
            }
        }

        public void RunService()
        {
            String s = Guid.NewGuid().ToString();
            Boolean bDone = false;
            if (!ReadConfig())
                return;

            CreateScheduler();
            CheckJobs();

            /*SFTPUpload sftpClient = new SFTPUpload(sftpHost, sftpPort, sftpUser, sftpPassword);
            sftpClient.Connect();
            Log.Debug("Connected? " + sftpClient.IsConnected());
            sftpClient.Disconnect();*/

            //Execute(null);

            while (isRunning)
            {
                Thread.Sleep(1000);
            }
            scheduler.Shutdown();
            //scheduler2.Shutdown();
            Log.Debug("Exit main thread");
        }

        private String GetDestFolder(SFTPUpload client)
        {
            String folder;
            //Use the destBaseFolder
            if (bCreateDailyFolder)
                folder = destBaseFolder + "/" + DateTime.Now.ToString("yyyyMMdd");
            else
                folder = destBaseFolder;

            client.CreateFolder(folder);
            return folder + "/";
        }

        public void Execute(IJobExecutionContext context)
        {
            if (bRunning)
            {
                Log.Debug("Skip schedule for now");
                return;
            }
            bRunning = true;
            try
            {
                SFTPUpload sftpClient = new SFTPUpload(sftpHost, sftpPort, sftpUser, sftpPassword);
                //Get the list from the DB
                Boolean bDoLoop = true;
                if (bSimpleFileUpload)
                {
                    //We will just do a simple directory upload of everything in the folder
                    Log.Debug("Configured for simple file upload from folder [" + sourceFolder + "]");
                    DirectoryInfo di = new DirectoryInfo(sourceFolder);
                    FileInfo[] fileInfo = di.GetFiles("*.*");
                    if (fileInfo != null && fileInfo.Length > 0)
                    {
                        String targetFolder = "";
                        foreach (var fi in fileInfo)
                        {
                            //Lets do the copy
                            if (!sftpClient.IsConnected())
                                sftpClient.Connect();

                            Log.Debug("Uploading file [" + fi.FullName + "]...");

                            if (targetFolder.Equals(""))
                                targetFolder = GetDestFolder(sftpClient);
                            String destFile = targetFolder + fi.Name;
                            if (sftpClient.UploadFile(fi.FullName, destFile/*, bDeleteLocalFile*/))
                            {
                                Log.Debug("Upload [" + fi.Name + "] - Success ");
                                if(bMoveLocalFile)
                                {
                                    sftpClient.MoveLocalFile(fi.FullName,"Uploaded");
                                }
                                else if (bDeleteLocalFile)
                                {
                                    sftpClient.DeleteLocalFile(fi.FullName);
                                }
                            }
                            else
                            {
                                Log.Debug("Failed to upload file [" + fi.Name + "] ! [" + sftpClient.IsConnected().ToString() + "]");
                            }
                        }
                        if (sftpClient.IsConnected())
                            sftpClient.Disconnect();
                        Log.Debug("Upload completed");
                    }
                }
                else
                {
                    cDatabase cdb = new cDatabase();
                    do
                    {
                        Queue<cDatabase.tsCopyRequest> reqQueue = cdb.getCopyRequests(100);
                        if (reqQueue.Count > 0)
                        {
                            String targetFolder = "";
                            foreach (var item in reqQueue)
                            {
                                //Lets do the copy
                                if (!sftpClient.IsConnected())
                                    sftpClient.Connect();

                                DirectoryInfo di = new DirectoryInfo(sourceFolder);
                                //Find the file mathing the search
                                FileInfo[] fileInfo = di.GetFiles(item.source + "*");
                                if (fileInfo != null && fileInfo.Length > 0)
                                {
                                    foreach (var fi in fileInfo)
                                    {
                                        Log.Debug("Uploading file [" + fi.Name + "]...");

                                        if (targetFolder.Equals(""))
                                            targetFolder = GetDestFolder(sftpClient);
                                        String destFile = targetFolder + fi.Name;
                                        if (sftpClient.UploadFile(fi.FullName, destFile, bDeleteLocalFile))
                                            cdb.updateCopyRequests(item, 4);
                                        else
                                        {
                                            Log.Debug("Failed to upload file [" + fi.Name + "] ! [" + sftpClient.IsConnected().ToString() + "]");
                                            cdb.updateCopyRequests(item, 8);
                                        }
                                    }
                                }
                                else
                                {
                                    Log.Debug("File [" + item.source + "] not found in local folder");
                                    cdb.updateCopyRequests(item, 9);
                                }
                            }
                        }
                        else
                        {
                            //Lets see if there are any CSV files to copy
                            DirectoryInfo di = new DirectoryInfo(sourceFolder);
                            FileInfo[] fileInfo = di.GetFiles("*.csv");
                            if (fileInfo.Length > 0)
                            {
                                Log.Debug("There are [" + fileInfo.Length + "] CSV files in the folder !"); ;
                                String targetFolder = GetDestFolder(sftpClient);
                                foreach (FileInfo fi in fileInfo)
                                {
                                    Log.Debug("Uploading file [" + fi.FullName + "]");
                                    String destFile = targetFolder + fi.Name;
                                    sftpClient.UploadFile(fi.FullName, destFile, bDeleteLocalFile);
                                }
                            }
                            else
                                Log.Debug("No CSV files found to upload !");

                            if (sftpClient.IsConnected())
                                sftpClient.Disconnect();
                            bDoLoop = false;
                        }
                    } while (bDoLoop);
                }
            }
            catch (Exception e)
            {
                Log.Debug("Exception in scheduler Execute method : " + e.Message);
            }

            bRunning = false;
        }
    }
}
