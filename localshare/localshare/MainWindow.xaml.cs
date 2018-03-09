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
            string compressedPath;

            // 1) obtain a single archive for the resources to be sent
            if (dm.resourcePath_isDirectory == true)
            {
                //la risorsa da inviare è una directory, necessaria compressione prima di invio

                try
                {
                    compressedPath = System.IO.Path.GetTempPath() + "localshare_tmp_" + Stopwatch.GetTimestamp().ToString() + ".zip";
                    ZipFile.CreateFromDirectory(dm.resourcePath, compressedPath, CompressionLevel.NoCompression, true);
                    dm.resourcePath = compressedPath; //update the path of the resource to be sent with the path of the compressed file

                } catch(Exception exc)
                {
                    if(exc is System.Security.SecurityException)
                    {
                        MessageBox.Show("ERROR: unable to retrieve the tmp folder path");
                        System.Windows.Application.Current.Shutdown();
                    }
                    else if(exc is IOException)
                    {
                        //a file in the directory cannot be added to the archive, the archive is left incomplete and invalid.
                        MessageBox.Show("ERROR: unable to compress the directory");

                        DeleteTempFile(dm.resourcePath);
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
                    wr = new WorkerResource(dm.resourcePath, u, i);
                }
                catch (Exception FileInfoExc)
                {
                    System.Windows.MessageBox.Show("ERROR: unable to retrieve FileInfo, gracefully exiting application.");
                    DeleteTempFile(dm.resourcePath);
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
            for(int i = 0; i< this.numWorkers; i++)
            {
                this.Workers[i].CancelAsync(); //set the CancellationPending property of each worker to true
            }

            DeleteTempFile(dm.resourcePath);
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
        /// </summary>
        /// <param name="sender">the object that sent the event</param>
        /// <param name="e">arguments of the event, a user object</param>
        private void WorkerJob(object sender, DoWorkEventArgs e)
        {
            //retrieving all the needed resources from the arguments
            WorkerResource wr = (WorkerResource)e.Argument;

            int WorkerID = (int)wr.WorkerID;
            User recipient = (User)wr.Recipient;
            string filePath = (string)wr.FinalResourcePath;
            FileInfo fileInfo = (FileInfo)wr.ResourceInfo;
            long fileSize = fileInfo.Length; //actual size of the file to send

            int i = 0; //value used to calculate the actual progress of the file sending

            FileStream fs;
            byte[] datachunk; //buffer where to store the read bytes
            int numBytes = 1500; //number of bytes to read from file stream (maximum MTU for ethernet frame)
            int numRead = 0; //actually read bytes from filestream
            long numSend = 0; //number of bytes that have been actually sent

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

            /*
             * Send resource to the remote host
             */
            fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            datachunk = new byte[numBytes];

            /*
             * TODO: Invio dell'header del protocollo
             */

            /*
             * Send file by chunk and update progress bar
             */
            while (numSend != fileSize && this.Workers[WorkerID].CancellationPending != true)
            {
                //read a chunk
                numRead = fs.Read(datachunk, 0, numBytes);

                try
                {
                    if (numRead < numBytes)
                    {
                        //last chunk, or file smaller than a MTU
                        s.Send(datachunk, 0, numRead, SocketFlags.None);

                    }
                    else
                    {
                        s.Send(datachunk, 0, numBytes, SocketFlags.None);
                    }
                }
                catch (Exception SendExc)
                {
                    if (SendExc is ArgumentNullException || SendExc is ArgumentOutOfRangeException)
                    {
                        System.Windows.MessageBox.Show("ERROR: worker error in send due to argument exception.");
                        break;
                    }
                    else
                    {
                        System.Windows.MessageBox.Show("ERROR: worker error in send due to socket exception");
                        break;
                    }

                }

                numSend += numRead;

                /*
                 * Update the progress bar 
                 */
                (sender as BackgroundWorker).ReportProgress(i, wr.Recipient);
                i++;

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

            } else
            {
                //the worker correctly finished his job without errors
                if( this.numFinishedJobs == numWorkers)
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
