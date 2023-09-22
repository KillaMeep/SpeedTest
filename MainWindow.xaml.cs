using System;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Windows;
using System.IO; 

namespace SpeedTestApp
{
    public partial class MainWindow : Window
    {
        private double downloadSpeedMbps = 0; // Variable to store download speed result
        private long pingTimeMs = 0;          // Variable to store ping time result
        private string tempFilePath = Path.Combine(Path.GetTempPath(), "tempFile.zip");

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void StartTest_Click(object sender, RoutedEventArgs e)
        {
            // Add the code to start the speed test when the button is clicked
            string fileUrl = "http://link.testfile.org/150MB";
            int downloadFileSizeBytes = 150 * 1024 * 1024;

            // Reset progress bar, total progress, and download speed label
            downloadProgressBar.Value = 0;
            progressTextBlock.Text = "0%";
            downloadSpeedLabel.Content = "Download Speed: Waiting...";
            startTestButton.IsEnabled = false; // Disable the button while the test is running

            // Measure ping time after the download is complete
            pingTimeLabel.Content = $"Ping Time: Testing... ";
            pingTimeMs = MeasurePingTime("www.google.com"); // Replace with your desired server
            pingTimeLabel.Content = $"Ping Time: {pingTimeMs} ms";

            // Start the download and update progress
            await MeasureDownloadSpeedAsync(fileUrl, downloadFileSizeBytes);

            // Update the labels with the test results and average speed
            downloadSpeedLabel.Content = $"Download Speed (Avg):\n{downloadSpeedMbps:F2} MB/s\n{downloadSpeedMbps*8:F2} Mbps";

            // Enable the button to run the test again
            startTestButton.Content = "Run Again";
            startTestButton.IsEnabled = true; // Enable the button for the next test

            // Delete the temporary file after the tests are completed
            DeleteTempFile(tempFilePath);
        }

        private async Task MeasureDownloadSpeedAsync(string fileUrl, int fileSizeBytes)
        {
            using (var webClient = new WebClient())
            {
                Stopwatch stopwatch = new Stopwatch();
                long lastBytesReceived = 0; // Store the bytes received in the previous event

                webClient.DownloadProgressChanged += (sender, e) =>
                {
                    if (e.BytesReceived == fileSizeBytes)
                    {
                        stopwatch.Stop();
                    }
                    else
                    {
                        // Update the progress bar value
                        downloadProgressBar.Value = (double)e.BytesReceived / fileSizeBytes * 100;

                        // Update the total progress text in MB
                        progressTextBlock.Text = $"{e.BytesReceived / 1048576}MB/{fileSizeBytes / 1048576}MB ({(double)e.BytesReceived / fileSizeBytes * 100:F2}%)";

                        // Calculate current download speed in MB/s
                        double currentSpeedMBps = e.BytesReceived / (stopwatch.Elapsed.TotalSeconds * 1048576); // MB/s
                        downloadSpeedLabel.Content = $"Download Speed:\n{currentSpeedMBps:F2} MB/s\n{currentSpeedMBps*8:F2} Mbps";


                        // Update the last bytes received
                        lastBytesReceived = e.BytesReceived;
                    }
                };

                stopwatch.Start();
                await webClient.DownloadFileTaskAsync(new Uri(fileUrl), tempFilePath);
                double elapsedSeconds = stopwatch.Elapsed.TotalSeconds;

                // Calculate average download speed in MB/s
                downloadSpeedMbps = fileSizeBytes / (elapsedSeconds * 1048576); // MB/s
            }
        }

        private long MeasurePingTime(string serverAddress)
        {
            Ping ping = new Ping();
            PingReply reply = ping.Send(serverAddress);

            if (reply.Status == IPStatus.Success)
            {
                return reply.RoundtripTime;
            }
            else
            {
                return -1; // Ping failed
            }
        }

        private void DeleteTempFile(string filePath)
        {
            try
            {
                System.IO.File.Delete(filePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting temporary file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
