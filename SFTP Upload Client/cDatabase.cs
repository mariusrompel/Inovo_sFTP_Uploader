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


        private String connectionString;

        public cDatabase(String connectionString)
        {
            this.connectionString = connectionString;
        }

        public Queue<tsCopyRequest> getCopyRequests(Int32 entryCount)
        {
            Queue<tsCopyRequest> lookupQueue = new Queue<tsCopyRequest>();
            //Try Connection
            try
            {
                using (SqlConnection myConnection = new SqlConnection(connectionString))
                {
                    myConnection.Open();

                    string sSQL = "SELECT top (@entryCount) id,status,unique_id,unique_id FROM [PTOOLS].[RECORDINGEXPORT] where status = 3 order by id";
                    Log.Debug("QUERY : [" + sSQL + "]");

                    using (SqlCommand myCommand = new SqlCommand(sSQL, myConnection))
                    {
                        myCommand.CommandTimeout = 30;
                        myCommand.Parameters.AddWithValue("@entryCount", entryCount);

                        using (SqlDataReader myResult = myCommand.ExecuteReader())
                        {
                            while (myResult.Read())
                            {
                                tsCopyRequest copyRequest = new tsCopyRequest();

                                copyRequest.id = myResult.GetInt32(0).ToString();
                                copyRequest.status = myResult.GetInt32(1).ToString();
                                copyRequest.source = !myResult.IsDBNull(2) ? myResult.GetString(2) : "";
                                copyRequest.destination = !myResult.IsDBNull(3) ? myResult.GetString(3) : "";

                                Log.Debug(copyRequest);
                                lookupQueue.Enqueue(copyRequest);
                            }
                        }
                    }
                }
                return lookupQueue;
            }
            catch (Exception ex)
            {
                Log.Error("Exception caught in classDatabase::getCopyRequests : " + ex.Message, ex);
                return lookupQueue;
            }
        }

        public Boolean updateCopyRequests(tsCopyRequest req, Int32 status)
        {
            //Try Connection
            try
            {
                using (SqlConnection myConnection = new SqlConnection(connectionString))
                {
                    myConnection.Open();

                    string sSQL = "UPDATE [PTOOLS].[RECORDINGEXPORT] SET status = @status, [UPDATED] = GETDATE() WHERE ID = @id";
                    Log.Debug("QUERY : [" + sSQL + "]");

                    using (SqlCommand myCommand = new SqlCommand(sSQL, myConnection))
                    {
                        myCommand.CommandTimeout = 30;
                        myCommand.Parameters.AddWithValue("@status", status);
                        myCommand.Parameters.AddWithValue("@id", req.id);

                        myCommand.ExecuteNonQuery();
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.Error("Exception caught in classDatabase::updateCopyRequests : " + ex.Message, ex);
                return false;
            }
        }
    }
}
