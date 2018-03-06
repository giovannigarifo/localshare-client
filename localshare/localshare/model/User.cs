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


        /*constructor*/
        public User( string UserName, Uri PhotoPath, string IpAddress, int RowIndex, int ColIndex )
        {
            this.UserName = UserName;
            this.PhotoPath = PhotoPath; //todo: CHECK IF VALID PATH
            this.IpAddress = IpAddress;
            this.RowIndex = RowIndex;
            this.ColIndex = ColIndex;
        }

        /*methods*/
        public override string ToString()
        {
            return "UserName: " + UserName + ". PhotoPath: " + PhotoPath + ". IpAddress: " + IpAddress + ".";
        }



       
    }


}
