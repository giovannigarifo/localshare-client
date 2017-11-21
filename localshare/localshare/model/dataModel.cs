using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        /* properties */

        //collection of user available on the LAN
        private ObservableCollection<User> availableUsers;
        public ObservableCollection<User> AvailableUsers
        {
            get { return this.availableUsers; }
            set { this.availableUsers = value; }

        } 

        //path of the file or directory to be shared
        public string targetPath;


        /* events*/
        public event PropertyChangedEventHandler PropertyChanged;


        /* constructor */
        public DataModel()
        {
            //populating users collection
            this.AvailableUsers = new ObservableCollection<User>();

            User u1 = new User("User_name_1", "C:\\photo\\path\\prova1", "192.168.0.1");
            User u2 = new User("User_name_2", "C:\\photo\\path\\prova2", "192.168.0.2");
            User u3 = new User("User_name_3", "C:\\photo\\path\\prova3", "192.168.0.3");

            this.AvailableUsers.Add(u1);
            this.AvailableUsers.Add(u2);
            this.AvailableUsers.Add(u3);

            //retrieving path for arguments
            string[] args = Environment.GetCommandLineArgs();
            this.targetPath = args[1];
        }


        /* methods */

        //implementation of the method required by INotifyPropertyChanged interface
        public void NotifyPropertyChanged(string propName)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }


        //add a user to the AvailableUsers list
        public void AddAvailableUser(User u)
        {
            AvailableUsers.Add(u);
            this.NotifyPropertyChanged("AvailableUsers");
        }
        
        


    }
}
