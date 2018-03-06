using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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

            //create the startup event windows explicitly
            MainWindow appWindow = new MainWindow();

            //activity to be performed before showing the actual main window
            appWindow.Title = "LocalShare via app.xaml.cs";

            /*
             * Check received argument from cmd line (file path)
             */
            //if no argument is received shutdown the application immediatly
            if (e.Args.Length != 1)
            {
                MessageBox.Show("wrong argument number");
                Shutdown();
            }

            //if the filepath is not a valid system local path, shutdown the application
            try
            {
                FileInfo fi = new FileInfo(e.Args[0]);

            }
            catch (Exception exc)
            {
                MessageBox.Show("incorrect file path");
                Shutdown();
            }
            

            //show the main window
            appWindow.Show();
        }

    }
}
