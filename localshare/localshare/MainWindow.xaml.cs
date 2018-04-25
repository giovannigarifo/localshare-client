using System;
using System.ComponentModel;
using System.Windows;
using System.IO.Compression;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.IO;
using localshare.model;

namespace localshare
{
    /// <summary>
    /// Logica di interazione per MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Properties of the MainWindow
        /// </summary>
        DataModel dm;
        UserList uList;
        UserProgress uProgress;

        BackgroundWorker[] Workers; //workers array
        int numWorkers = 0; //number of workers
        int numFinishedJobs = 0; //number of finished jobs, e.g. the number of workers that correctly returned

        /// <summary>
        /// MainWindow constructor
        /// </summary>
        /// <param name="dataModel">object that encapsultate the whole application model</param>
        public MainWindow(DataModel dataModel)
        {
            InitializeComponent();
            this.dm = dataModel;
            this.DataContext = dm; //defines the binding scope

            /*
             * Create the UserList custom control and attach it to ContentControl defined in the MainWindow
             */
            this.uList = new UserList();
            this.contentControl.Content = this.uList;
        }



        /**************************************
         * Event handlers for the two buttons
         */

        /*
         * action to take when the "Share" button has been clicked (event fired)
         */
        private void ShareBtnClicked(object sender, RoutedEventArgs e)
        {
            int i = 0;
            WorkerResource wr = null;

            // check if user didn't selected anything
            if (uList.UsersListView.SelectedItems.Count == 0)
                return;

            // 1) obtain a single archive for the resources to be sent
            try
            {
                dm.compressedPath = Path.GetTempPath() + "localshare_tmp_" + Stopwatch.GetTimestamp().ToString() + ".zip";

                if (dm.resourcePath_isDirectory)
                {
                    ZipFile.CreateFromDirectory(dm.resourcePath, dm.compressedPath, CompressionLevel.NoCompression, true);
                }
                else
                {
                    using (FileStream zipToCreate = new FileStream(dm.compressedPath, FileMode.Create))
                    {
                        using (ZipArchive archive = new ZipArchive(zipToCreate, ZipArchiveMode.Create))
                        {
                            archive.CreateEntryFromFile(dm.resourcePath, dm.resourceName, CompressionLevel.NoCompression);
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                if (exc is System.Security.SecurityException)
                {
                    Console.WriteLine("[ERROR] unable to retrieve the tmp folder path");
                    Application.Current.Shutdown(1);
                }
                else if (exc is IOException)
                {
                    //a file in the directory cannot be added to the archive, the archive is left incomplete and invalid.
                    Console.WriteLine("[ERROR] unable to compress the directory");
                    Application.Current.Shutdown(1);
                }
                else
                {
                    Console.WriteLine("[ERROR] unable to perform the creation of the compressed version of the file to be sent");
                    Application.Current.Shutdown(1);
                }
            }

            // 2) for each selected user, fire a worker that send the resources to him and add him to the SelectedUsers collection
            this.numWorkers = uList.UsersListView.SelectedItems.Count;
            this.Workers = new BackgroundWorker[numWorkers];
            i = 0;

            foreach (User u in uList.UsersListView.SelectedItems)
            {
                this.Workers[i] = new BackgroundWorker();

                this.Workers[i].WorkerReportsProgress = true;
                this.Workers[i].WorkerSupportsCancellation = true;
                this.Workers[i].DoWork += WorkerJob; //registring WorkerJob as the last event handler for the RunWorkerAsync event
                this.Workers[i].ProgressChanged += WorkerJobProgressChanged; //event handler for the progresschanged event
                this.Workers[i].RunWorkerCompleted += WorkerJobCompleted; //fired when the worker has finished is job or it has been cancelled

                //aggregate all the needed resources for the worker
                try
                {
                    wr = new WorkerResource(dm.compressedPath, dm.resourceName, u, i);
                }
                catch (Exception FileInfoExc)
                {
                    Console.WriteLine("[ERROR] unable to retrieve FileInfo, gracefully exiting application.");
                    Application.Current.Shutdown(1);
                }

                this.Workers[i].RunWorkerAsync(wr); //fires the event with the WorkerResource as argument

                //add the user to the SelectedUsers collection
                this.dm.AddSelectedUser(u);

                i++;
            }

            // 3) update the ContentControl with the new view and hide the button
            this.uProgress = new UserProgress();
            this.contentControl.Content = this.uProgress;
            this.ShareBtn.Visibility = Visibility.Collapsed;
        }


        /*
         * action to take when the "Cancel" button has been clicked (event fired): Controlled shutdown of the program
         */
        private void CancelBtnClicked(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < this.numWorkers; i++)
            {
                this.Workers[i].CancelAsync(); //set the CancellationPending property of each worker to true
            }

            Application.Current.Shutdown(0);
        }
       

        private void ObtainResourceToBeSent()
        {

        }



        /*************************************
         *  Event handlers for the Workers
         */

        /// <summary>
        /// This event handler performs the task of a single worker: send the resource to the remote user
        /// 
        /// Protocol, send file message header (use TLV format):
        /// 
        /// SENDF
        /// UserNameLength[4->Int32]
        /// UserName[length]
        /// isFolder[1->bool]
        /// FileNameLength[4->Int32]
        /// FileName[length]
        /// DataLength[8->Int64]
        /// 
        /// after the header, the whole file is sended.
        /// 
        /// </summary>
        /// <param name="sender">the object that sent the event</param>
        /// <param name="e">arguments of the event, a user object</param>
        private void WorkerJob(object sender, DoWorkEventArgs e)
        {
            //retrieving all the needed resources from the arguments
            WorkerResource wr = (WorkerResource)e.Argument;
            int WorkerID = (int)wr.WorkerID;
            User recipient = (User)wr.Recipient;
            string filePath = (string)wr.CompressedPath;
            FileInfo fileInfo = (FileInfo)wr.CompressedFileInfo;
            long fileSize = fileInfo.Length; //actual size of the file to send

            //sending percentage and time remaining
            Stopwatch sw = new Stopwatch();
            int percComplete = 0; //value used to calculate the actual progress of the file sending
            double remainingTimeInSeconds = -1;

            //file
            int numRead = 0; //actually read bytes from filestream
            long numSent = 0; //number of bytes that have been actually sent

            //data buffer
            const int datachunkLen = 1500; //number of bytes to read from file stream (maximum MTU for ethernet frame)
            byte[] datachunk = new byte[datachunkLen]; //buffer that contains each chunk of the value [V]
            byte[] recvBuf = new byte[5]; //OK---, NOMEM

            //remote connection
            Socket s = null;
            IPAddress ipAddr = IPAddress.Parse(recipient.Address);
            Int32 tcpPort = 30200;
            IPEndPoint ipEndPoint = new IPEndPoint(ipAddr, tcpPort);

            //create socket and connect to the remote host
            try
            {
                s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                s.SendTimeout = 5000; //blocking time in milliseconds
                s.ReceiveTimeout = 5000;
                s.SendBufferSize = 65536; //default value 8192B

                s.Connect(ipEndPoint);
            }
            catch (Exception exc)
            {
                if (exc is InvalidOperationException) //the socket is listening
                {
                    s.Shutdown(SocketShutdown.Both);
                    s.Close();
                }

                Console.WriteLine("[ERROR] unable to connect to the remote endpoint.");

                e.Cancel = true;
                return;
            }

            try
            {
                // 1) send header
                int un_len = recipient.UserName.Length * 2; //coded in Unicode (UTF-16)
                int rn_len = wr.ResourceName.Length * 2;

                byte[] sendf = System.Text.Encoding.ASCII.GetBytes("SENDF"); //SENDF[5 bytes]
                byte[] userNameLen = BitConverter.GetBytes(Convert.ToInt32(un_len)); //UserNameLength[4 bytes]
                byte[] userName = System.Text.Encoding.Unicode.GetBytes(recipient.UserName); //UserName[UserNameLength]
                byte[] isFolder = BitConverter.GetBytes(dm.resourcePath_isDirectory); //isFolder[1 byte]
                byte[] fileNameLen = BitConverter.GetBytes(Convert.ToInt32(rn_len)); //FileNameLength[4 bytes] 
                byte[] fileName = System.Text.Encoding.Unicode.GetBytes(wr.ResourceName); //FileName[FileNameLength]
                byte[] fileLen = BitConverter.GetBytes(Convert.ToInt64(fileSize)); //DataLength[8 bytes]

                int msgHeaderLen = 5 + 4 + un_len + 1 + 4 + rn_len + 8;
                byte[] msgHeader = new byte[msgHeaderLen];

                Buffer.BlockCopy(sendf, 0, msgHeader, 0, 5);
                Buffer.BlockCopy(userNameLen, 0, msgHeader, 5, 4);
                Buffer.BlockCopy(userName, 0, msgHeader, 9, un_len);
                Buffer.BlockCopy(isFolder, 0, msgHeader, 9 + un_len, 1);
                Buffer.BlockCopy(fileNameLen, 0, msgHeader, 9 + un_len + 1, 4);
                Buffer.BlockCopy(fileName, 0, msgHeader, 9 + un_len + 1 + 4, rn_len);
                Buffer.BlockCopy(fileLen, 0, msgHeader, 9 + un_len + 1 + 4 + rn_len, 8);

                //s.Send(msgHeader, 0, msgHeaderLen, SocketFlags.None);
                Utils.SendExactly(s, msgHeader);

                //send photo of the user who send the file
                if(dm.onlineUsers.MyPhotoVersion > 0)
                {
                    //send photo length and photo if photo is not the default one
                    String PhotoPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\QuickShare\\Photos\\myphoto-" + dm.onlineUsers.MyPhotoVersion + ".jpg";
                    FileInfo fi_userPhoto = new FileInfo(PhotoPath);
                    byte[] userPhotoLen = BitConverter.GetBytes(Convert.ToInt32(fi_userPhoto.Length)); //UserPhotoLength[4 bytes]
                    Utils.SendExactly(s, userPhotoLen);
                    s.SendFile(PhotoPath);

                } else
                {
                    //if the user photo is the default one just send the length = 0
                    byte[] userPhotoLen = BitConverter.GetBytes(Convert.ToInt32(0)); //UserPhotoLength[4 bytes]
                    Utils.SendExactly(s, userPhotoLen);
                }


                // waiting for response from server
                recvBuf = Utils.ReceiveExactly(s, 5);
                //s.Receive(recvBuf, SocketFlags.None);

                if (System.Text.Encoding.ASCII.GetString(recvBuf, 0, 5) != "OK---")
                {
                    throw new Exception(); //TODO                    
                }
                Console.WriteLine("[DEBUG] BackgroundWorker" + wr.WorkerID + " received response from remote endpoint: " + System.Text.Encoding.ASCII.GetString(recvBuf, 0, 5));

                // 2) send file blocks 
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    sw.Start();
                    int n_kBps = 0;

                    while (numSent != fileSize && this.Workers[WorkerID].CancellationPending != true)
                    {
                        numRead = fs.Read(datachunk, 0, datachunkLen); //read a chunk from file

                        if (numRead < datachunkLen)
                        {
                            Console.WriteLine("[DEBUG] BackgroundWorker" + wr.WorkerID + " sending last chunk");
                            Utils.SendExactly(s, datachunk, numRead); //last chunk, or file smaller than a MTU
                        }
                        else
                        {
                            Utils.SendExactly(s, datachunk); //whole chunk
                        }

                        numSent += numRead;

                        // 3) Update the progress bar and expected remainingtime

                        if (sw.ElapsedMilliseconds / 1000 > n_kBps && sw.IsRunning) //every second
                        {
                            remainingTimeInSeconds = (double)(fileSize / numSent);
                            n_kBps++;
                        }
                        wr.RemainingTimeInSeconds = (int)remainingTimeInSeconds;

                        percComplete = (int)((numSent * 100) / fileSize);

                        (sender as BackgroundWorker).ReportProgress(percComplete, wr);
                    }
                }
            }
            catch (Exception SendExc)
            {
                if (SendExc is ArgumentNullException || SendExc is ArgumentOutOfRangeException)
                {
                    Console.WriteLine("[ERROR] BackgroundWorker" + wr.WorkerID + " catched ArgumentNullException or ArgumentOutOfRangeException.");
                }
                else
                {
                    Console.WriteLine("[ERROR] BackgroundWorker" + wr.WorkerID + " catched generic Exception during socket send process.");
                }
            }

            sw.Stop();

            //test for pending shutdown request and Release the socket and all the resources.
            if (this.Workers[WorkerID].CancellationPending == true)
                e.Cancel = true;

            //update progress bar to inform user that the worker completed the process
            wr.RemainingTimeInSeconds = 0;
            (sender as BackgroundWorker).ReportProgress(percComplete, wr);

            s.Shutdown(SocketShutdown.Both);
            s.Close();
        }


        /// <summary>
        /// callback for the event raised when the job has been completed, if all job have been completed, inform the user and exit the program gracefully
        /// </summary>
        /// <param name="sender">the sender is the related worker</param>
        /// <param name="e">event data</param>
        private void WorkerJobCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            this.numFinishedJobs++;

            if (e.Cancelled)
            {
                //the worker job has been cancelled for some reason
                Console.WriteLine("[DEBUG] A WorkerJob has been cancelled for some reason. error value: " + e.Error);
            }
            else
            {
                //the worker correctly finished his job without errors
                if (this.numFinishedJobs == numWorkers)
                {
                    //all jobs completed, exit the program
                    Console.WriteLine("[DEBUG] All the jobs have been completed! shutting down the program gracefully.");
                    Application.Current.Shutdown(0);
                }
                else
                {
                    Console.WriteLine("[DEBUG] A worker correctly finished his job.");
                }

            }
        }


        /// <summary>
        /// this event handler is used to update the progress bar and the expected time remaining in the view every time a progresschanged event is fired
        /// </summary>
        /// <param name="sender">the sender is the related worker</param>
        /// <param name="e">the progress percentage sended by the worker</param>
        private void WorkerJobProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            WorkerResource wr = (WorkerResource)e.UserState;
            User u = wr.Recipient;

            u.updatePercComplete(e.ProgressPercentage);
            u.updateMsgTimeRemaining(wr.RemainingTimeInSeconds);
        }
        

    }
}
