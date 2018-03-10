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
        /// Constructor for the MainWindow
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            this.dm = new DataModel();
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

            // 1) obtain a single archive for the resources to be sent
            if (dm.resourcePath_isDirectory == true)
            {
                //la risorsa da inviare è una directory, necessaria compressione prima di invio

                try
                {
                    dm.compressedPath = System.IO.Path.GetTempPath() + "localshare_tmp_" + Stopwatch.GetTimestamp().ToString() + ".zip";
                    ZipFile.CreateFromDirectory(dm.resourcePath, dm.compressedPath, CompressionLevel.NoCompression, true);

                }
                catch (Exception exc)
                {
                    if (exc is System.Security.SecurityException)
                    {
                        MessageBox.Show("ERROR: unable to retrieve the tmp folder path");
                        System.Windows.Application.Current.Shutdown();
                    }
                    else if (exc is IOException)
                    {
                        //a file in the directory cannot be added to the archive, the archive is left incomplete and invalid.
                        MessageBox.Show("ERROR: unable to compress the directory");

                        DeleteTempFile(dm.compressedPath);
                        System.Windows.Application.Current.Shutdown();
                    }
                    else
                    {
                        MessageBox.Show("ERROR: unable to perform the creation of the compressed version of the file to be sent");
                        System.Windows.Application.Current.Shutdown();
                    }
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

                //aggregate all the needed resources for the worker
                try
                {
                    wr = new WorkerResource(dm.compressedPath, dm.resourceName, u, i);
                }
                catch (Exception FileInfoExc)
                {
                    System.Windows.MessageBox.Show("ERROR: unable to retrieve FileInfo, gracefully exiting application.");
                    DeleteTempFile(dm.compressedPath);
                    System.Windows.Application.Current.Shutdown();
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

            DeleteTempFile(dm.compressedPath);
            System.Windows.Application.Current.Shutdown();
        }

        private void DeleteTempFile(string compressedPath)
        {
            if (File.Exists(compressedPath))
                File.Delete(compressedPath);
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
        /// Messages are in TLV format: SENDF[T][L][V]
        /// 
        /// SENDF = hello of the protocol
        /// T = 1 byte: 1=filename, 2=filedata
        /// L = 8 byte: length of V
        /// V = [L] byte: filename if T=1, filedata if T=2
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
            string fileName = (string)wr.ResourceName;
            FileInfo fileInfo = (FileInfo)wr.CompressedFileInfo;
            long fileSize = fileInfo.Length; //actual size of the file to send

            int percComplete = 0; //value used to calculate the actual progress of the file sending

            FileStream fs;
            int numRead = 0; //actually read bytes from filestream
            long numSent = 0; //number of bytes that have been actually sent

            //buffers
            int datachunkLen = 1500; //number of bytes to read from file stream (maximum MTU for ethernet frame)
            int headerLen = 14;
            byte[] header = new byte[headerLen]; //buffer that contains the header: SENDF[T][L]
            byte[] datachunk = new byte[datachunkLen]; //buffer that contains each chunk of the value [V]

            byte[] sendf = System.Text.Encoding.ASCII.GetBytes("SENDF"); //5 bytes
            byte[] type = new byte[1]; //1 byte
            ulong len;

            //remote connection
            Socket s;
            IPAddress ipAddr = IPAddress.Parse(recipient.IpAddress);
            IPEndPoint ipEndPoint = new IPEndPoint(ipAddr, 5000);


            s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            //create socket and connect to the remote host
            try
            {
                s.Connect(ipEndPoint);
            }
            catch (Exception SocketExc)
            {
                MessageBox.Show("ERROR: unable to connect to the remote endpoint", "Prompt", MessageBoxButton.OK, MessageBoxImage.Warning, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);

                e.Cancel = true;
                return;
            }

            //send file
            try
            {
                // 1) Sending filename: SENDF1[filename_length][filename]
                headerLen = 14;
                type[0] = 1;
                len = Convert.ToUInt64(fileName.Length);

                Array.Copy(sendf, 0, header, 0, 5);
                Array.Copy(type, 0, header, 5, 1);
                Array.Copy(BitConverter.GetBytes(len), 0, header, 6, 8);
                s.Send(header, 0, headerLen, SocketFlags.None); // SENDF1[filename_length]
                s.Send(System.Text.Encoding.ASCII.GetBytes(fileName), 0, fileName.Length, SocketFlags.None); // [filename]

                // 2) Sending file: 2[file_size][file]. The file is actually sended in chunks of 1500B 
                headerLen = 9;
                type[0] = 2;
                len = Convert.ToUInt64(fileSize);

                Array.Copy(type, 0, header, 0, 1);
                Array.Copy(BitConverter.GetBytes(len), 0, header, 1, 8);
                s.Send(header, 0, headerLen, SocketFlags.None); // 2[file_size]

                fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);

                while (numSent != fileSize && this.Workers[WorkerID].CancellationPending != true)
                {
                    numRead = fs.Read(datachunk, 0, datachunkLen); //read a chunk

                    if (numRead < datachunkLen)
                    {
                        s.Send(datachunk, 0, numRead, SocketFlags.None); //last chunk, or file smaller than a MTU
                    }
                    else
                    {
                        s.Send(datachunk, 0, datachunkLen, SocketFlags.None); //whole chunk
                    }

                    numSent += numRead;

                    // Update the progress bar 
                    percComplete = (int) (fileSize / numSent) * 100;
                    (sender as BackgroundWorker).ReportProgress(percComplete, wr.Recipient);
                }

            }
            catch (Exception SendExc)
            {
                if (SendExc is ArgumentNullException || SendExc is ArgumentOutOfRangeException)
                {
                    System.Windows.MessageBox.Show("ERROR: worker error in send due to argument exception.");
                }
                else
                {
                    System.Windows.MessageBox.Show("ERROR: worker error in send due to socket exception");
                }
            }

            //test for pending shutdown request and Release the socket and all the resources.
            if (this.Workers[WorkerID].CancellationPending == true)
                e.Cancel = true;

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
                MessageBox.Show("A WorkerJob has been cancelled for some reason", "Prompt", MessageBoxButton.OK, MessageBoxImage.Warning, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);

            }
            else
            {
                //the worker correctly finished his job without errors
                if (this.numFinishedJobs == numWorkers)
                {
                    //all jobs completed, exit the program
                    MessageBox.Show("All the jobs have been completed! shutting down the program gracefully", "Prompt", MessageBoxButton.OK, MessageBoxImage.Warning, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                    System.Windows.Application.Current.Shutdown();
                }
                else
                {
                    MessageBox.Show("A worker correctly finished his job", "Prompt", MessageBoxButton.OK, MessageBoxImage.Warning, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                }

            }
        }


        /// <summary>
        /// this event handler is used to update the progress bar in the view every time a progresschanged event is fired
        /// </summary>
        /// <param name="sender">the sender is the related worker</param>
        /// <param name="e">the progress percentage sended by the worker</param>
        private void WorkerJobProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            User u = (User)e.UserState;
            u.IncrementPercComplete(e.ProgressPercentage);
        }


    }
}
