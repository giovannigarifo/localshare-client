﻿using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows;

namespace localshare.model
{
    /// <summary>
    /// 
    /// ViewModel of the Application
    /// 
    /// it consist of a series of collection that are loaded at app startup,
    /// and some methods divided into:
    /// 
    /// - main methods: used to perform main tasks such as collection loading ecc
    /// 
    /// - support methods: used to perform secondary task (add a single element, ecc)
    ///
    /// </summary>
    public class DataModel : INotifyPropertyChanged
    {
        /****************
         *  Properties
         */

        FileSystemWatcher fsWatcher; //DB File watcher
        string dbPath; //path on file system of the db file
        OnlineUsers onlineUsers; //contains information about the logged user and a list of remote users

        public ObservableCollection<User> AvailableUsers { get; set; } //collection of available user on the LAN
        public ObservableCollection<User> SelectedUsers { get; set; } //collection of the selected users (selected by the user)
        
        public string resourcePath; //path of the file or directory to be shared with the selected users
        public Boolean resourcePath_isDirectory; //flag for testing if resource is a directory or not
        public string resourceName; //filename or directory name of the resource to be sent, shrinked from resourcePath
        public string compressedPath; //path of the compressed temporary file that is actually sended via the socket

        /****************
         *  Events 
         */
        public event PropertyChangedEventHandler PropertyChanged;

        /*****************
         *  Constructor 
         */
        public DataModel()
        {
            //instantiate watcher
            this.dbPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\QuickShare";
            this.fsWatcher = new FileSystemWatcher(this.dbPath, "*.db");
            this.fsWatcher.NotifyFilter = NotifyFilters.LastWrite;
            this.fsWatcher.Changed += dbFileChanged;

            //the first time read the db manually and load the user collection
            dbRead();
            this.AvailableUsers = onlineUsers.UserList;

            //instantiation of the selected users collection
            this.SelectedUsers = new ObservableCollection<User>();

            //retrieving path from arguments
            string[] args = Environment.GetCommandLineArgs();
            this.resourcePath = args[1];

            //shrinking filename or directory name from the path
            this.resourceName = Path.GetFileName(resourcePath);

            //check if the resourcePath corresponds to a directory or a file
            try
            {
                FileAttributes tpAttributes = File.GetAttributes(this.resourcePath);

                if (tpAttributes.HasFlag(FileAttributes.Directory))
                {
                    this.resourcePath_isDirectory = true;
                }
                else
                {
                    this.resourcePath_isDirectory = false;
                }

            } catch (Exception exc) {

                Console.WriteLine("[ERROR] unable to read resourcePath file attributes or to shrink resourcePath to filename/dirname");
                System.Windows.Application.Current.Shutdown();
            }

        }

        /// <summary>
        /// Read the DB from QuickShare.db and load all information in the collections
        /// </summary>
        private void dbRead()
        {
            //load data from db
            using (FileStream fs = new FileStream(this.dbPath + "\\QuickShare.db", FileMode.Open, FileAccess.Read))
            {
                JavaScriptSerializer deserializator = new JavaScriptSerializer();

                long fileLen = fs.Length;
                byte[] buf = new byte[fileLen];

                fs.Read(buf, 0, (int)fileLen);
                String jsonStr = Encoding.UTF8.GetString(buf); //whole db in json format
                onlineUsers = deserializator.Deserialize<OnlineUsers>(jsonStr);
            }

            for (int i = 0; i < onlineUsers.UserList.Count; i++)
            {
                User user = onlineUsers.UserList[i];

                user.RowIndex = i / 3; //row index in grid
                user.ColIndex = i % 3; //col index in grid

                if (user.PhotoVersion == 0)
                {
                    user.UserPhoto = new Uri("pack://application:,,,/Resources/DefaultAvatar.bmp", UriKind.RelativeOrAbsolute);

                } else user.UserPhoto = new Uri( this.dbPath + "\\Photos\\" + user.UserId + "-" + user.PhotoVersion);
                
                this.NotifyPropertyChanged("AvailableUsers");
            }
        }

        /// <summary>
        /// The database file has been changed by the server, update the collections to reflect the changes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dbFileChanged(object sender, FileSystemEventArgs e)
        {
            dbRead();
        }


        /***************
         *  Methods
         */

        //implementation of the method required by INotifyPropertyChanged interface
        public void NotifyPropertyChanged(string propName)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }


        //add a user to the SelectedUsers list
        public void AddSelectedUser(User u)
        {
            int auLen = this.SelectedUsers.Count();
            u.RowIndex = auLen;
            u.ColIndex = 0;
            
            this.SelectedUsers.Add(u);

            this.NotifyPropertyChanged("SelectedUsers");
        }

    }
}