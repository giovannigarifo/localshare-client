using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    class DataModel : INotifyPropertyChanged
    {

        /****************
         *  Properties
         */

        //collection of available user on the LAN
        private ObservableCollection<User> availableUsers;
        public ObservableCollection<User> AvailableUsers
        {
            get { return this.availableUsers; }
            set { this.availableUsers = value; }
        }

        //collection of the selected users
        private ObservableCollection<User> selectedUsers;
        public ObservableCollection<User> SelectedUsers
        {
            get { return this.selectedUsers; }
            set { this.selectedUsers = value; }
        }

        //path of the file or directory to be shared with the selected users
        public string resourcePath; //path of the resource selected by the user (received as cmd line arg)
        public Boolean resourcePath_isDirectory;


        /****************
         *  Events 
         */

        public event PropertyChangedEventHandler PropertyChanged;



        /*****************
         *  Constructor 
         */
        public DataModel()
        {
            //populating available users collection
            this.AvailableUsers = new ObservableCollection<User>();

            this.AddAvailableUser("user_name_1", "C:\\Users\\giova\\Workspace\\PDS-malnati\\localshare-client\\localshare\\localshare\\resources\\profile-pic.png", "192.168.0.1");
            this.AddAvailableUser("User_name_2", "C:\\Users\\giova\\Workspace\\PDS-malnati\\localshare-client\\localshare\\localshare\\resources\\profile-pic.png", "192.168.0.2");
            this.AddAvailableUser("User_name_3", "C:\\Users\\giova\\Workspace\\PDS-malnati\\localshare-client\\localshare\\localshare\\resources\\profile-pic.png", "192.168.0.3");
            this.AddAvailableUser("User_name_4", "C:\\Users\\giova\\Workspace\\PDS-malnati\\localshare-client\\localshare\\localshare\\resources\\profile-pic.png", "192.168.0.4");
            this.AddAvailableUser("User_name_5", "C:\\Users\\giova\\Workspace\\PDS-malnati\\localshare-client\\localshare\\localshare\\resources\\profile-pic.png", "192.168.0.5");
            this.AddAvailableUser("User_name_6", "C:\\Users\\giova\\Workspace\\PDS-malnati\\localshare-client\\localshare\\localshare\\resources\\profile-pic.png", "192.168.0.6");
            this.AddAvailableUser("User_name_7", "C:\\Users\\giova\\Workspace\\PDS-malnati\\localshare-client\\localshare\\localshare\\resources\\profile-pic.png", "192.168.0.7");
            this.AddAvailableUser("User_name_8", "C:\\Users\\giova\\Workspace\\PDS-malnati\\localshare-client\\localshare\\localshare\\resources\\profile-pic.png", "192.168.0.8");
            this.AddAvailableUser("User_name_9", "C:\\Users\\giova\\Workspace\\PDS-malnati\\localshare-client\\localshare\\localshare\\resources\\profile-pic.png", "192.168.0.9");
            this.AddAvailableUser("User_name_10", "C:\\Users\\giova\\Workspace\\PDS-malnati\\localshare-client\\localshare\\localshare\\resources\\profile-pic.png", "192.168.0.10");



            //instantiation of the selected users collection
            this.SelectedUsers = new ObservableCollection<User>();

            //retrieving path for arguments
            string[] args = Environment.GetCommandLineArgs();
            this.resourcePath = args[1];

            //check if the resourcePath corresponds to a directory or a file

            try
            {
                FileAttributes tpAttributes = File.GetAttributes(this.resourcePath);

                if (tpAttributes.HasFlag(FileAttributes.Directory))
                    this.resourcePath_isDirectory = true;
                else
                    this.resourcePath_isDirectory = false;

            } catch (Exception exc) {

                MessageBox.Show("ERROR: unable to read resourcePath file attributes");
            }

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


        //Create a User and add him to the AvailableUsers list
        public void AddAvailableUser(string UserName, string PhotoPath, string IpAddress)
        {
            int auLen = this.AvailableUsers.Count();
            int row = auLen / 3; //row index in grid
            int col = auLen % 3; //col index in grid

            this.AvailableUsers.Add(new User(UserName, new Uri(PhotoPath), IpAddress, row, col));

            this.NotifyPropertyChanged("AvailableUsers");
        }

        //add a user to the SelectedUsers list
        public void AddSelectedUser(User u)
        {
            int auLen = this.SelectedUsers.Count();

            this.SelectedUsers.Add(new User(u.UserName, u.PhotoPath, u.IpAddress, auLen, 0));

            this.NotifyPropertyChanged("SelectedUsers");
        }

    }

}
