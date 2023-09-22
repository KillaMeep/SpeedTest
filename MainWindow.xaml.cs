using System;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace SpeedTestApp
{
    public partial class MainWindow : Window
    {
        private Dictionary<string, string> serverUrls = new Dictionary<string, string>
        {
            { "New York", "https://speedtest-ny.turnkeyinternet.net/100mb.bin" },
            { "California", "https://speedtest-ca.turnkeyinternet.net/100mb.bin" },
            { "Colorado", "https://speedtest-co.turnkeyinternet.net/100mb.bin" },
            { "Chicago", "https://speedtest-chi.turnkeyinternet.net/100mb.bin" },
            { "Miami", "https://speedtest-mia.turnkeyinternet.net/100mb.bin" },
            { "Amsterdam", "https://speedtest-ams.turnkeyinternet.net/100mb.bin" }
        };

        private double downloadSpeedMbps = 0;
        private long pingTimeMs = 0;
        private string tempFilePath = Path.Combine(Path.GetTempPath(), "100mb.bin");

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void StartTest_Click(object sender, RoutedEventArgs e)
        {
            downloadProgressBar.Value = 0;
            progressTextBlock.Text = "0%";
            downloadSpeedLabel.Content = "Download Speed: Waiting...";
            startTestButton.IsEnabled = false;

            // Ping all servers in parallel and get the best server
            string bestServer = await GetBestServerByPingAsync();
            serverRegion.Content = $"Nearest Server: {bestServer}";

            // Use the best server location for the speed test
            string fileUrl = serverUrls[bestServer];

            // Measure ping time to the selected server
            pingTimeLabel.Content = $"Ping Time: Testing...";
            pingTimeMs = MeasurePingTime(new Uri(fileUrl).Host);
            pingTimeLabel.Content = $"Ping Time: {pingTimeMs} ms";

            await MeasureDownloadSpeedAsync(fileUrl, 100 * 1024 * 1024);

            downloadSpeedLabel.Content = $"Download Speed (Avg):\n{downloadSpeedMbps:F2} MB/s\n{downloadSpeedMbps * 8:F2} Mbps";

            startTestButton.Content = "Run Again";
            startTestButton.IsEnabled = true;

            DeleteTempFile(tempFilePath);
        }

        private async Task<string> GetBestServerByPingAsync()
        {
            // Ping all servers in parallel
            var pingTasks = serverUrls.Select(async kvp =>
            {
                string serverName = kvp.Key;
                string serverUrl = kvp.Value;
                long pingTime = await MeasurePingTimeAsync(new Uri(serverUrl).Host);
                return new { ServerName = serverName, PingTime = pingTime };
            }).ToList();

            var pingResults = await Task.WhenAll(pingTasks);

            // Select the server with the lowest ping time
            var bestServer = pingResults.OrderBy(result => result.PingTime).FirstOrDefault();

            return bestServer?.ServerName ?? "Default Server";
        }

        private async Task<long> MeasurePingTimeAsync(string serverHostname)
        {
            try
            {
                Ping ping = new Ping();
                PingReply reply = await ping.SendPingAsync(serverHostname);

                if (reply.Status == IPStatus.Success)
                {
                    return reply.RoundtripTime;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error pinging server: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            // Return a high value if the ping fails
            return long.MaxValue;
        }

        private async Task MeasureDownloadSpeedAsync(string fileUrl, int fileSizeBytes)
        {
            using (var webClient = new WebClient())
            {
                Stopwatch stopwatch = new Stopwatch();
                long lastBytesReceived = 0;

                webClient.DownloadProgressChanged += (sender, e) =>
                {
                    if (e.BytesReceived == fileSizeBytes)
                    {
                        stopwatch.Stop();
                        progressTextBlock.Text = $"{e.BytesReceived / 1048576}MB/{fileSizeBytes / 1048576}MB ({(double)e.BytesReceived / fileSizeBytes * 100:F2}%)";
                    }
                    else
                    {
                        downloadProgressBar.Value = (double)e.BytesReceived / fileSizeBytes * 100;
                        progressTextBlock.Text = $"{e.BytesReceived / 1048576}MB/{fileSizeBytes / 1048576}MB ({(double)e.BytesReceived / fileSizeBytes * 100:F2}%)";

                        double currentSpeedMBps = e.BytesReceived / (stopwatch.Elapsed.TotalSeconds * 1048576);
                        downloadSpeedLabel.Content = $"Download Speed:\n{currentSpeedMBps:F2} MB/s\n{currentSpeedMBps * 8:F2} Mbps";

                        lastBytesReceived = e.BytesReceived;
                    }
                };

                stopwatch.Start();
                await webClient.DownloadFileTaskAsync(new Uri(fileUrl), tempFilePath);
                double elapsedSeconds = stopwatch.Elapsed.TotalSeconds;

                downloadSpeedMbps = fileSizeBytes / (elapsedSeconds * 1048576);
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
                return -1;
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