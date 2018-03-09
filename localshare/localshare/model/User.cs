using System;
using System.ComponentModel;

namespace localshare.model
{
    class User : INotifyPropertyChanged
    {
        /*Properties with get/set-ters */

        private string userName;
        public string UserName
        {
            get { return this.userName; }
            set { this.userName = value; }
        }

        private Uri photoPath;
        public Uri PhotoPath
        {
            get { return this.photoPath; }
            set { this.photoPath = value; }
        }

        private string ipAddress;
        public string IpAddress
        {
            get { return this.ipAddress; }
            set { this.ipAddress = value; }
        }

        private int rowIndex;
        public int RowIndex
        {
            get { return this.rowIndex; }
            set { this.rowIndex = value; }
        }

        private int colIndex;
        public int ColIndex
        {
            get { return this.colIndex; }
            set { this.colIndex = value; }
        }

        private int percComplete;
        public int PercComplete
        {
            get { return this.percComplete; }
            set { this.percComplete = value; }
        }

        /* event handlers */
        public event PropertyChangedEventHandler PropertyChanged;


        /*constructor*/
        public User( string UserName, Uri PhotoPath, string IpAddress, int RowIndex, int ColIndex )
        {
            this.UserName = UserName;
            this.PhotoPath = PhotoPath; //todo: CHECK IF VALID PATH
            this.IpAddress = IpAddress;
            this.RowIndex = RowIndex;
            this.ColIndex = ColIndex;
            this.PercComplete = 0;
        }

        /*methods*/
        public override string ToString()
        {
            return "UserName: " + UserName + ". PhotoPath: " + PhotoPath + ". IpAddress: " + IpAddress + ".";
        }

        //implementation of the method required by INotifyPropertyChanged interface
        public void NotifyPropertyChanged(string propName)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }


        //Create a User and add him to the AvailableUsers list
        public void IncrementPercComplete(int perc)
        {
            this.PercComplete = perc;

            this.NotifyPropertyChanged("PercComplete");
        }

    }


}
