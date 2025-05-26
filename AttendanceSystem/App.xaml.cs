using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;

namespace SchoolAttendance
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // Check if another instance is running
            if (IsAlreadyRunning())
            {
                MessageBox.Show($"(ApplicationName) is already running.\n" +
                              "Please close the existing instance before starting a new one.",
                              "(ApplicationName)",
                              MessageBoxButton.OK,
                              MessageBoxImage.Exclamation);

                // Exit this instance
                Current.Shutdown();
                return;
            }

            base.OnStartup(e);
        }

        private bool IsAlreadyRunning()
        {
            Process currentProcess = Process.GetCurrentProcess();
            string currentProcessPath = currentProcess.MainModule.FileName;

            foreach (Process process in Process.GetProcessesByName(currentProcess.ProcessName))
            {
                if (process.Id != currentProcess.Id)
                {
                    try
                    {
                        if (process.MainModule.FileName == currentProcessPath)
                        {
                            return true;
                        }
                    }
                    catch
                    {
                        // Some processes might not be accessible
                        continue;
                    }
                }
            }
            return false;
        }

    }
}
