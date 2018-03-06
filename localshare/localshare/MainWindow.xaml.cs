using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO.Compression;

using localshare.model;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Net.NetworkInformation;
using System.Diagnostics;

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
 
            // 1) obtain a single archive for the resources to be sent

            if(dm.resourcePath_isDirectory == true)
            {
                //la risorsa da inviare è una directory, necessaria compressione prima di invio

                try
                {
                    string compressedPath = System.IO.Path.GetTempPath() + "localshare_tmp_" + Stopwatch.GetTimestamp().ToString() + ".zip";

                    ZipFile.CreateFromDirectory(dm.resourcePath, compressedPath, CompressionLevel.NoCompression, true);

                    dm.resourcePath = compressedPath; //update the path of the resource to be sent with the path of the compressed file

                } catch(Exception exc)
                {
                    if(exc is System.Security.SecurityException)
                    {
                        MessageBox.Show("ERROR: unable to retrieve the tmp folder path");

                    } else
                    {
                        MessageBox.Show("ERROR: unable to zip the directory");
                    }
                    
                }

            }
            
            // 2) for each selected user, fire a worker that send the resources to him and add him to the SelectedUsers collection
            
            BackgroundWorker[] Workers = new BackgroundWorker[uList.UsersListView.SelectedItems.Count];
            int i = 0;

            foreach (User u in uList.UsersListView.SelectedItems)
            {
                Workers[i] = new BackgroundWorker();

                Workers[i].WorkerReportsProgress = true;
                Workers[i].DoWork += WorkerJob; //registring WorkerJob as the last event handler for the RunWorkerAsync event
                Workers[i].ProgressChanged += WorkerJobProgressChanged; //event handler for the progresschanged event

                //aggregate all the needed resources for the worker
                WorkerResource wr = new WorkerResource(dm.resourcePath, u);

                Workers[i].RunWorkerAsync(wr); //fires the event with the WorkerResource as argument

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
         * action to take when the "Cancel" button has been clicked (event fired)
         */
        private void CancelBtnClicked(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
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
        void WorkerJob(object sender, DoWorkEventArgs e)
        {
            //retrieving all the needed resources from the arguments
            WorkerResource wr = (WorkerResource)e.Argument;

            User recipient = (User)wr.Recipient;
            string filePath = (string)wr.FinalResourcePath;
            FileInfo fileInfo = (FileInfo)wr.ResourceInfo;
            long fileSize = fileInfo.Length; //actual size of the file to send

            
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
                System.Windows.MessageBox.Show("ERROR: unable to connect to the remote endpoint");

                //TODO: notify user and thread destroy
            }

            /*
             * Send resource to the remote host
             */
            fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            datachunk = new byte[numBytes];

            /*
             * TODO: Invio dell'header del protocollo
             */

            while(numSend != fileSize)
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
                    if(SendExc is ArgumentNullException || SendExc is ArgumentOutOfRangeException)
                    {
                        System.Windows.MessageBox.Show("ERROR: worker error in send due to argument exception.");

                    } else
                    {
                        System.Windows.MessageBox.Show("ERROR: worker error in send due to socket exception");
                    }

                }

                numSend += numRead;
                
                /*
                 * Update the progress bar 
                 */
                //TODO: (sender as BackgroundWorker).ReportProgress(i);
            }


            // Release the socket and all the resources.
            s.Shutdown(SocketShutdown.Both);
            s.Close();

        }


        /// <summary>
        /// this event handler is used to update the progress bar in the view every time a progresschanged event is fired
        /// </summary>
        /// <param name="sender">the sender is the related worker</param>
        /// <param name="e">the progress percentage sended by the worker</param>
        void WorkerJobProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            //pbStatus.Value = e.ProgressPercentage;
        }


    }
}
