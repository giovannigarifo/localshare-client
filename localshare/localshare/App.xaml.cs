using localshare.model;
using System;
using System.IO;
using System.Windows;

namespace localshare
{
    /// <summary>
    /// Logica di interazione per App.xaml
    /// 
    /// Entry Point dell'applicazione, lanciata dal metodo run() di Application (vedi implementazione classe per dettagli)
    /// 
    /// </summary>
    public partial class App : Application
    {
        MainWindow appWindow;
        DataModel dataModel;
        
        /// <summary>
        /// 
        /// This method is called when the startup event has been fired. 
        /// 
        /// It's responsible for the creation, setup and show of the main window of the application
        /// 
        /// </summary>
        /// <param name="sender">the object that fired the event</param>
        /// <param name="e">arguments of the event</param>
        private void ApplicationStartupHandler(object sender, StartupEventArgs e)
        {
            //create model
            dataModel = new DataModel();

            //create the startup event windows explicitly
            appWindow = new MainWindow(dataModel);

            //activity to be performed before showing the actual main window
            appWindow.Title = "LocalShare";

            /*
             * Check received argument from cmd line (file path)
             */
            //if no argument is received shutdown the application immediatly
            if (e.Args.Length != 1)
            {
                Console.WriteLine("[ERROR] wrong argument number.");
                Shutdown();
            }

            //if the filepath is not a valid system local path, shutdown the application
            try
            {
                FileInfo fi = new FileInfo(e.Args[0]);
            }
            catch (Exception)
            {
                Console.WriteLine("[ERROR] incorrect file path.");
                Shutdown();
            }

            //show the main window
            appWindow.Show();
        }


        /// <summary>
        /// 
        /// This method is called when the exit event has been fired. The exit event cannot be cancelled.
        /// 
        /// It's responsible to check if the application is exiting with status code equal to succes sor failure
        /// 
        /// </summary>
        /// <param name="sender">the object that fired the event</param>
        /// <param name="e">arguments of the event</param>
        private void Application_Exit(object sender, ExitEventArgs e)
        {
            if (e.ApplicationExitCode == 1) //failure
            {
                Console.WriteLine("[DEBUG] app exiting with status=1 (failure).");
            }
            else //success
            {
                Console.WriteLine("[ERROR] app exiting with status=0 (success).");
            }

            try
            {
                //delete the compressed file
                if (File.Exists(dataModel.compressedPath))
                    File.Delete(dataModel.compressedPath);

            } catch (Exception)
            {
                MessageBox.Show("There was a problem while closing the App. It's possible that some temporary file " +
                                "have not been deleted correctly, please check if there are files that begins with \"localshare\"" +
                                "in your tmp folder, or simply delete all your temporary files with the windows disk cleanup utility."
                                , "Alert", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            
        }
    }
}
