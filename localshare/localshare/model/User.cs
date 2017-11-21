/* Model*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace localshare.model
{
    class User
    {
        /*Properties with get/set-ters */

        private string userName;
        public string UserName
        {
            get { return this.userName; }
            set { this.userName = value; }
        }

        private string photoPath;
        public string PhotoPath
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

        /*constructor*/
        public User( string UserName, string PhotoPath, string IpAddress)
        {
            this.UserName = UserName;
            this.PhotoPath = PhotoPath;
            this.IpAddress = IpAddress;
        }

        /*methods*/
        public override string ToString()
        {
            return "UserName: " + UserName + ". PhotoPath: " + PhotoPath + ". IpAddress: " + IpAddress + ".";
        }



       
    }


}
