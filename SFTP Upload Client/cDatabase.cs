using log4net;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SFTP_Upload_Client
{
    class cDatabase
    {
        private static ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public class tsCopyRequest
        {
            public String id;
            public String status;
            public String source;
            public String destination;
        }


        static Boolean bDBConfigOK = false;
        static String presenceDbString = "";
        private void GetDBConnectString()
        {
            //dbConnString = "Server=172.24.8.120;Database=SQLPR1;User ID=PTOOLS;Pwd=PTOOLS;";
            try
            {
                Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal);
                AppSettingsSection appConfig = (AppSettingsSection)config.GetSection("appSettings");
                presenceDbString = appConfig.Settings["Presence DB Conn"].Value;
                bDBConfigOK = true;
            }
            catch (Exception e)
            {
                Log.Debug("Exception getting DB conn Strings : " + e.Message);
            }
        }

        public Queue<tsCopyRequest> getCopyRequests(Int32 entryCount)
        {
            SqlDataReader myResult = null;
            SqlConnection myConnection = null;
            SqlCommand myCommand = null;

            Queue<tsCopyRequest> lookupQueue = new Queue<tsCopyRequest>();
            //Try Connection
            try
            {

                string sSQL = "";

                if (!bDBConfigOK)
                {
                    GetDBConnectString();
                    Log.Debug("Using conn String [" + presenceDbString + "]");
                }

                myConnection = new SqlConnection(presenceDbString);

                myCommand = new SqlCommand(sSQL, myConnection);
                myCommand.CommandTimeout = 30;

                //DB Connection
                myConnection.Open();

                //DB Command
                myCommand.Connection = myConnection;

                //sSQL = "SELECT ID,SESSIONID,IDNUMBER,PHONE FROM [dbo].[CIM_REQUESTLOOKUPS] WHERE [STATUS] NOT IN ('Complete','In Progress') AND TIMESTAMP >= DATEADD(HOUR, -8, GETDATE()) ORDER BY ID DESC ";
                sSQL = "SELECT top " + entryCount + " id,status,unique_id,unique_id FROM [PTOOLS].[RECORDINGEXPORT] where status = 3 order by id";

                //SELECT * FROM " + DatabaseSchema + "." + queueTableName + " WHERE INBOUNDMAILID <= " + lastInboundMailId + " AND INBOUNDMAILID > " + lastInboundArchivedMailId

                Log.Debug("QUERY : [" + sSQL + "]");

                //Set and Execute
                myCommand.CommandText = sSQL;
                myResult = myCommand.ExecuteReader();
                while (myResult.Read() == true)
                {
                    tsCopyRequest copyRequest = new tsCopyRequest();

                    copyRequest.id = myResult.GetInt32(0).ToString();
                    copyRequest.status = myResult.GetInt32(1).ToString();
                    try { copyRequest.source = myResult.GetString(2); } catch (Exception e) { copyRequest.source = ""; }
                    try { copyRequest.destination = myResult.GetString(3); } catch (Exception e) { copyRequest.destination = ""; }

                    Log.Debug(copyRequest);
                    lookupQueue.Enqueue(copyRequest);
                }
                return lookupQueue;
            }
            catch (Exception ex)
            {
                Log.Debug("Exception caught in classDatabase::getCopyRequests : " + ex.Message);
                return lookupQueue;
            }
            finally
            {
                if (myCommand != null)
                    myCommand.Cancel();
                if (myResult != null)
                    myResult.Close();
                //Destroy
                if (myConnection != null)
                {
                    myConnection.Close();
                    myConnection.Dispose();
                }
            }
        }

        public Boolean updateCopyRequests(tsCopyRequest req, Int32 status)
        {
            SqlDataReader myResult = null;
            SqlConnection myConnection = null;
            SqlCommand myCommand = null;

            //Try Connection
            try
            {

                string sSQL = "";

                if (!bDBConfigOK)
                {
                    GetDBConnectString();
                    Log.Debug("Using conn String [" + presenceDbString + "]");
                }

                myConnection = new SqlConnection(presenceDbString);

                myCommand = new SqlCommand(sSQL, myConnection);
                myCommand.CommandTimeout = 30;

                //DB Connection
                myConnection.Open();

                //DB Command
                myCommand.Connection = myConnection;

                sSQL = "UPDATE [PTOOLS].[RECORDINGEXPORT] SET status = " + status + ", [UPDATED] = GETDATE() WHERE ID = " + req.id;

                //SELECT * FROM " + DatabaseSchema + "." + queueTableName + " WHERE INBOUNDMAILID <= " + lastInboundMailId + " AND INBOUNDMAILID > " + lastInboundArchivedMailId

                Log.Debug("QUERY : [" + sSQL + "]");

                //Set and Execute
                myCommand.CommandText = sSQL;
                myCommand.ExecuteNonQuery();

                return true;
            }
            catch (Exception ex)
            {
                Log.Debug("Exception caught in classDatabase::updateCopyRequests : " + ex.Message);
                return false;
            }
            finally
            {
                if (myCommand != null)
                    myCommand.Cancel();
                if (myResult != null)
                    myResult.Close();
                //Destroy
                if (myConnection != null)
                {
                    myConnection.Close();
                    myConnection.Dispose();
                }
            }
        }
    }
}
