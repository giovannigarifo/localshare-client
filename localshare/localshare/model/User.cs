using System;
using System.ComponentModel;
using System.Web.Script.Serialization;

namespace localshare.model
{
    public class User : INotifyPropertyChanged
    {
        /*Properties with get/set-ters */
        public UInt32 UserId { get; set; }

        public string UserName { get; set; }

        public string Address { get; set; }

        public UInt32 PhotoVersion { get; set; }

        public Uri UserPhoto { get; set; }

        [ScriptIgnore]
        public int RowIndex { get; set; }

        [ScriptIgnore]
        public int ColIndex { get; set; }

        [ScriptIgnore]
        public int PercComplete { get; set; }

        [ScriptIgnore]
        public string MsgTimeRemaining { get; set; }


        /* event handlers */
        public event PropertyChangedEventHandler PropertyChanged;

        /*****************
         *  Constructors
         */

        public User(uint userId, string userName, string address, uint photoVersion, Uri userPhoto)
        {
            //obtained from serialized db object
            UserId = userId;
            UserName = userName;
            Address = address;
            PhotoVersion = photoVersion;
            UserPhoto = userPhoto;

            //locally generated attributes
            RowIndex = 0;
            ColIndex = 0;
            PercComplete = 0;
            MsgTimeRemaining = "Calculating remaining time statistics...";
        }

        public User()
        {
        }

        /*****************
         *  Methods
         */
        public override string ToString()
        {
            return "UserName: " + UserName + ". IpAddress: " + Address + ".";
        }

        //implementation of the method required by INotifyPropertyChanged interface
        public void NotifyPropertyChanged(string propName)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }


        //increment perComplete and notify for changed property
        public void updatePercComplete(int perc)
        {
            this.PercComplete = perc;

            this.NotifyPropertyChanged("PercComplete");
        }

        //update the Message to be showed inside the progressbar with the remaining time
        public void updateMsgTimeRemaining(int remainingTimeInSeconds)
        {
            if (remainingTimeInSeconds == -1)
                this.MsgTimeRemaining = "Waiting for statistics...";
            else if (remainingTimeInSeconds == 0)
                this.MsgTimeRemaining = "Finished!";
            else
                this.MsgTimeRemaining = "Remaining time in seconds: " + remainingTimeInSeconds;

            this.NotifyPropertyChanged("msgTimeRemaining");
        }


    }


}
