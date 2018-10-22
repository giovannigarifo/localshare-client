using System;
using System.IO;


namespace localshare.model
{
    /// <summary>
    /// class that encapsulate the data needed by each worker to perform his tasks
    /// </summary>
    class WorkerResource
    {
        public string CompressedPath { get; set; }

        //filename or directory name of the resource that the user want to send (not the compressed tmp file!)
        public string ResourceName { get; set; }

        public User Recipient { get; set; }

        public FileInfo CompressedFileInfo { get; set; }

        //the position in the SendingWorkers array
        public int WorkerID { get; set; }

        //where the worker stores the calculated remaining time after each chunk sent
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
