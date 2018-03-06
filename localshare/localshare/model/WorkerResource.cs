using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace localshare.model
{
    /// <summary>
    /// class that encapsulate the data needed by each worker to perform his tasks
    /// </summary>
    class WorkerResource
    {
        private string finalResourcePath;
        public string FinalResourcePath { get; set; }

        private User recipient;
        public User Recipient { get; set; }

        private FileInfo resourceInfo;
        public FileInfo ResourceInfo { get; set; }

        public WorkerResource(string finalResourcePath, User recipient)
        {
            FinalResourcePath = finalResourcePath;
            Recipient = recipient;

            //retrieve information about the actual file that must be sent
            try
            {
                ResourceInfo = new FileInfo(FinalResourcePath);
            }
            catch (Exception FileInfoExc)
            {
                System.Windows.MessageBox.Show("ERROR: WorkerResource Constructor: unable to retieve FileInfo");
            }

        }
    }

}
