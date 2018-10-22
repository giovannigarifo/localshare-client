using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace localshare.model
{

    /*
    *   Object that represents the deserialized JSON db
    */
    public class OnlineUsers : INotifyPropertyChanged
    {
        public String MyUserName { get; set; }
        public UInt32 MyPhotoVersion { get; set; }
        public Int32 AppPort { get; set; } //tcp port
        public ObservableCollection<User> UserList { get; set; } //list of available remote users on LAN

        public OnlineUsers(string myUserName, uint myPhotoVersion, int appPort, ObservableCollection<User> userList)
        {
            this.MyUserName = myUserName;
            this.MyPhotoVersion = myPhotoVersion;
            this.AppPort = appPort;
            this.UserList = userList;
        }

        public OnlineUsers()
        {

        }

        public event PropertyChangedEventHandler PropertyChanged;

        //implementation of the method required by INotifyPropertyChanged interface
        public void NotifyPropertyChanged(string propName)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }
    }
}
