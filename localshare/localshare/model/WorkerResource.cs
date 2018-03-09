using System;
using System.IO;


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

        private int workerID; //the position in the Workers array
        public int WorkerID { get; set; }

        public WorkerResource(string finalResourcePath, User recipient, int workerID)
        {
            FinalResourcePath = finalResourcePath;
            Recipient = recipient;

            //retrieve information about the actual file that must be sent
            try
            {
                ResourceInfo = new FileInfo(FinalResourcePath);
            }
            catch{ throw; }

        }
    }

}
