using System;
using System.IO;


namespace localshare.model
{
    /// <summary>
    /// class that encapsulate the data needed by each worker to perform his tasks
    /// </summary>
    class WorkerResource
    {
        private string compressedPath;
        public string CompressedPath { get; set; }

        private string resourceName; //filename or directory name of the resource that the user want to send (not the compressed tmp file!)
        public string ResourceName { get; set; }

        private User recipient;
        public User Recipient { get; set; }

        private FileInfo compressedFileInfo;
        public FileInfo CompressedFileInfo { get; set; }

        private int workerID; //the position in the Workers array
        public int WorkerID { get; set; }

        private int remainingTimeInSeconds; //where the worker stores the calculated remaining time after each chunk sended
        public int RemainingTimeInSeconds { get; set; }

        /*****************
         *  Constructor 
         */
        public WorkerResource(string compressedPath, string resourceName, User recipient, int workerID)
        {
            this.CompressedPath = compressedPath;
            this.Recipient = recipient;
            this.ResourceName = resourceName;

            //retrieve information about the actual file that must be sent
            try
            {
                CompressedFileInfo = new FileInfo(CompressedPath);
            }
            catch{ throw; }

        }
    }

}
