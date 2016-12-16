﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;


namespace updater
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        WebClient webClient;               
        Stopwatch sw = new Stopwatch();

        public MainWindow(string version)
        {
            InitializeComponent();


            update(version);

        }


        private void update(string version)
        {

            string url = "https://github.com/hcmlab/nova/releases/download/" + version + "/nova.exe";


            DownloadFile(url, "nova.exe");



        }


        public void DownloadFile(string urlAddress, string location)
        {
            using (webClient = new WebClient())
            {
                webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(Completed);
                webClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(ProgressChanged);

                // The variable that will be holding the url address (making sure it starts with http://)
           //    Uri URL =// urlAddress.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ? new Uri(urlAddress) : new Uri("https://" + urlAddress);

                    Uri URL = new Uri(urlAddress);
                // Start the stopwatch which we will be using to calculate the download speed
                sw.Start();

                try
                {
                    // Start downloading the file
                    webClient.DownloadFileAsync(URL, location);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }


        // The event that will fire whenever the progress of the WebClient is changed
        private void ProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            // Calculate download speed and output it to labelSpeed.
            labelSpeed.Content = string.Format("{0} kb/s", (e.BytesReceived / 1024d / sw.Elapsed.TotalSeconds).ToString("0.00"));

            // Update the progressbar percentage only when the value is not the same.
            progressBar.Value = e.ProgressPercentage;

            // Show the percentage on our label.
            labelPerc.Content = e.ProgressPercentage.ToString() + "%";

            // Update the label with how much data have been downloaded so far and the total size of the file we are currently downloading
            labelDownloaded.Content = string.Format("{0} MB's / {1} MB's",
                (e.BytesReceived / 1024d / 1024d).ToString("0.00"),
                (e.TotalBytesToReceive / 1024d / 1024d).ToString("0.00"));
        }

        // The event that will trigger when the WebClient is completed
        private void Completed(object sender, AsyncCompletedEventArgs e)
        {
            // Reset the stopwatch.
            sw.Reset();




            System.Diagnostics.Process updateProcess = new System.Diagnostics.Process();
            updateProcess.StartInfo.FileName = "nova.exe";
            updateProcess.Start();


            //System.Diagnostics.Process deleteupdater = new System.Diagnostics.Process();
            //deleteupdater.StartInfo.FileName = AppDomain.CurrentDomain.BaseDirectory + "\\nova.exe";
            //deleteupdater.StartInfo.Arguments = "/ C choice / C Y / N / D Y / T 3 & Del " + AppDomain.CurrentDomain.BaseDirectory + "\\updater.exe";
            //deleteupdater.Start();


            ProcessStartInfo Info = new ProcessStartInfo();
            Info.Arguments = "/C choice /C Y /N /D Y /T 3 & Del " + "updater.exe";
            //Info.WindowStyle = ProcessWindowStyle.Hidden;
            //Info.CreateNoWindow = true;
            Info.FileName = "cmd.exe";
            Process.Start(Info);

            this.Close();
        }

    }
}
