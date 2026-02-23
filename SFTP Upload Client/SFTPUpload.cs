using log4net;
using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SFTP_Upload_Client
{
    class SFTPUpload
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private String host;
        private Int32 port;
        private String username;
        private String password;

        private SftpClient client = null;

        void HandleKeyEvent(Object sender, Renci.SshNet.Common.AuthenticationPromptEventArgs e)
        {
            foreach (Renci.SshNet.Common.AuthenticationPrompt prompt in e.Prompts)
            {
                if (prompt.Request.IndexOf("Password:", StringComparison.InvariantCultureIgnoreCase) != -1)
                {
                    prompt.Response = password;
                }
            }
        }

        public SFTPUpload()
        {

        }
        public SFTPUpload (String host, Int32 port, String username, String password)
        {
            this.host = host;
            this.port = port;
            this.username = username;
            this.password = password;
        }

        public Boolean IsConnected() { try { return client.IsConnected;  } catch (Exception e) { return false; } }

        public Boolean Connect(String host, Int32 port, String username, String password)
        {
            this.host = host;
            this.port = port;
            this.username = username;
            this.password = password;

            try
            {
                client = new SftpClient(host, port, username, password);
                client.Connect();
                return client.IsConnected;
            }
            catch (Exception e)
            {
                Log.Debug("Failed to connect to SFTP Server at [" + host + "] : " + e.Message);
                return false;
            }
        }
        public Boolean Connect()
        {
            try
            {
                KeyboardInteractiveAuthenticationMethod kauth = new KeyboardInteractiveAuthenticationMethod(username);
                PasswordAuthenticationMethod pauth = new PasswordAuthenticationMethod(username, password);
                kauth.AuthenticationPrompt += new EventHandler<Renci.SshNet.Common.AuthenticationPromptEventArgs>(HandleKeyEvent);

                ConnectionInfo connectionInfo = new ConnectionInfo(host, port, username, pauth, kauth);

                //client = new SftpClient(host, port, username, password);
                client = new SftpClient(connectionInfo);
                client.Connect();
                return client.IsConnected;
            } 
            catch (Exception e)
            {
                Log.Debug("Failed to connect to SFTP Server at [" + host + "] : " + e.Message);
                return false;
            }
        }
        public void Disconnect()
        {
            try
            {
                if (client != null && client.IsConnected)
                {
                    client.Disconnect();
                    client.Dispose();
                    client = null;
                }
            }
            catch (Exception e)
            {
                Log.Debug("Failed to disconnect from SFTP server : " + e.Message);
            }
        }

        public Boolean CreateFolder(String folder)
        {
            if (IsConnected())
            {
                try
                {
                    client.CreateDirectory(folder);
                    return true;
                }
                catch(Exception e)
                {
                    Log.Debug("Failed to create folder [" + folder + "] : " + e.Message); 
                }
            }
            return false;
        }

        public Boolean UploadFile(String uploadfile, String remoteFile, Boolean bDeleteLocal = false)
        {
            if(IsConnected())
            {
                try
                {
                    var fileStream = new FileStream(uploadfile, FileMode.Open);
                    if (fileStream != null)
                    {
                        Log.Debug("Local file opened [" + uploadfile + "]");
                        client.BufferSize = 4 * 1024;
                        client.UploadFile(fileStream, remoteFile, null);
                        fileStream.Close();
                        if (bDeleteLocal)
                        {

                            File.Delete(uploadfile);
                        }
                        return true;
                    }
                } 
                catch (Exception e)
                {
                    Log.Debug("Failed to upload file [" + uploadfile + "]->[" + remoteFile + "] : " + e.Message);
                    //Lets disconnect and try again
                    Disconnect();
                }
                return false;
            }
            else
            {
                return false;
            }
        }

        public void DeleteLocalFile(String uploadfile)
        {
            File.Delete(uploadfile);
        }

        public void MoveLocalFile(String uploadfile, String doneFolder)
        {
            string currentDirectory = Path.GetDirectoryName(uploadfile);
            string subFolderPath = Path.Combine(currentDirectory, doneFolder);
            string destFilePath = Path.Combine(subFolderPath, Path.GetFileName(uploadfile));

            try { 
                Log.Debug("Moving file [" + uploadfile + "]->[" + destFilePath + "]");
                if (!Directory.Exists(subFolderPath))
                {
                    Directory.CreateDirectory(subFolderPath);
                }   
                File.Move(uploadfile, destFilePath);
            } 
            catch (Exception e)
            {
                Log.Debug("Failed to move file [" + uploadfile + "]->[" + destFilePath + "] : " + e.Message); 
            }
        }
        //public Boolean CreateDirectory(String folder)
        //{
        //    try
        //    {
        //        if (IsConnected())
        //            client.CreateDirectory(folder);
        //        return true;
        //    }
        //    catch (Exception e)
        //    {
        //        Log.Debug("Failed to create folder [" + folder + "] : " + e.Message);
        //        return false;
        //    }
        //}
    }
}
