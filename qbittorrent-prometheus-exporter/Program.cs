using CommandLine;
using Prometheus;
using QBittorrent.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace qbittorrent_prometheus_exporter
{
    /// <summary>
    /// Options read in using the Command Line
    /// </summary>
    public class CLOptions
    {
        [Option('v', "verbose", Required = false, HelpText = "Set output to verbose messages.", Default = false)]
        public bool Verbose { get; set; }

        [Option('i', "qbittorrent-ip-address", Required = false, HelpText = "Set qbittorrent WebAPI address.", Default = "127.0.0.1")]
        public string QbittorrentIpAddress { get; set; }

        [Option('p', "qbittorrent-port", Required = false, HelpText = "Set qbittorrent WebAPI port.", Default = 8080)]
        public ushort QbittorrentPort { get; set; }

        [Option('a', "listen-address", Required = false, HelpText = "Listen Address for prometheus scraper.", Default = "127.0.0.1")]
        public string PrometheusAddress { get; set; }

        [Option('l', "listen-port", Required = false, HelpText = "Set listen port for prometheus scraper.", Default = 8091)]
        public ushort PrometheusPort { get; set; }

        [Option('u', "qbittorrent-username", Required = true, HelpText = "Username to connect to qbittorrent webapi.")]
        public string QbittorrentUsername { get; set; }

        [Option('w', "qbittorrent-password", Required = true, HelpText = "Password to conenct to qbittorrent webapi.")]
        public string QbittorrentPassword { get; set; }

        [Option("poll-speed-seconds", Required = false, HelpText = "The number of seconds between qbittorrent poll", Default = 10)]
        public long QbitorrentPollSeconds { get; set; }
    }

    class Program
    {
        #region Metrics    
        /// <summary>
        /// Definitions of all metrics used in this exporter.
        /// </summary>
        private static readonly string MetricBytesDownloadedCounterName = "qbittorrent_total_bytes_downloaded";
        private static readonly Counter MetricBytesDownloaded =
            Metrics.CreateCounter(MetricBytesDownloadedCounterName
                , "The total number of bytes downloaded using the Qbittorrent instance"
                , new CounterConfiguration
                {
                    LabelNames = new[] { "torrent", "hash" }
                });

        private static readonly string MetricBytesUploadedCounterName = "qbittorrent_total_bytes_uploaded";
        private static readonly Counter MetricBytesUploaded =
            Metrics.CreateCounter(MetricBytesUploadedCounterName
                , "The total number of bytes uploaded using the Qbittorrent instance"
                , new CounterConfiguration
                {
                    LabelNames = new[] { "torrent", "hash" }
                });

        private static readonly string MetricDownloadSpeedGaugeName = "qbittorrent_current_download_speed";
        private static readonly Gauge MetricDownloadSpeed =
            Metrics.CreateGauge(MetricDownloadSpeedGaugeName
                , "The current download speed using the Qbittorrent instance"
                , new GaugeConfiguration
                {
                    LabelNames = new[] { "torrent", "hash" }
                });

        private static readonly string MetricUploadSpeedGaugeName = "qbittorrent_current_upload_speed";
        private static readonly Gauge MetricUploadSpeed =
            Metrics.CreateGauge(MetricUploadSpeedGaugeName
                , "The current upload speed using the Qbittorrent instance"
                , new GaugeConfiguration
                {
                    LabelNames = new[] { "torrent", "hash" }
                });

        private static readonly string MetricTotalSizeGaugeName = "qbittorrent_torrent_total_size";
        private static readonly Gauge MetricTotalSize =
            Metrics.CreateGauge(MetricTotalSizeGaugeName
                , "The current total torrent size using the Qbittorrent instance"
                , new GaugeConfiguration
                {
                    LabelNames = new[] { "torrent", "hash" }
                });

        private static readonly string MetricRemainingSizeGaugeName = "qbittorrent_torrent_remaining_size";
        private static readonly Gauge MetricRemainingSize =
            Metrics.CreateGauge(MetricRemainingSizeGaugeName
                , "The current remaining torrent size using the Qbittorrent instance"
                , new GaugeConfiguration
                {
                    LabelNames = new[] { "torrent", "hash" }
                });

        private static readonly string MetricAvailableGaugeName = "qbittorrent_torrent_available_amount";
        private static readonly Gauge MetricAvailable =
            Metrics.CreateGauge(MetricAvailableGaugeName
                , "The availability of a torrent with less than 1 being unfulfilled"
                , new GaugeConfiguration
                {
                    LabelNames = new[] { "torrent", "hash" }
                });

        private static readonly string MetricPeersConnectedGaugeName = "qbittorrent_torrent_peers_connected";
        private static readonly Gauge MetricPeersConnected =
            Metrics.CreateGauge(MetricPeersConnectedGaugeName
                , "The number of peers connected for a specific torrent"
                , new GaugeConfiguration
                {
                    LabelNames = new[] { "torrent", "hash" }
                });

        private static readonly string MetricPeersInSwarmGaugeName = "qbittorrent_torrent_peers_in_swarm";
        private static readonly Gauge MetricPeersInSwarm =
            Metrics.CreateGauge(MetricPeersInSwarmGaugeName
                , "The number of peers in swarm for a specific torrent"
                , new GaugeConfiguration
                {
                    LabelNames = new[] { "torrent", "hash" }
                });

        private static readonly string MetricSeedsConnectedGaugeName = "qbittorrent_torrent_seeds_connected";
        private static readonly Gauge MetricSeedsConnected =
            Metrics.CreateGauge(MetricSeedsConnectedGaugeName
                , "The number of seeds connected for a specific torrent"
                , new GaugeConfiguration
                {
                    LabelNames = new[] { "torrent", "hash" }
                });

        private static readonly string MetricSeedsInSwarmGaugeName = "qbittorrent_torrent_seeds_in_swarm";
        private static readonly Gauge MetricSeedsInSwarm =
            Metrics.CreateGauge(MetricSeedsInSwarmGaugeName
                , "The number of seeds in swarm for a specific torrent"
                , new GaugeConfiguration
                {
                    LabelNames = new[] { "torrent", "hash" }
                });

        private static readonly string MetricRatioGaugeName = "qbittorrent_torrent_ratio";
        private static readonly Gauge MetricRatio =
            Metrics.CreateGauge(MetricRatioGaugeName
                , "The current ratio for a specific torrent"
                , new GaugeConfiguration
                {
                    LabelNames = new[] { "torrent", "hash" }
                });

        private static readonly string MetricProgressGaugeName = "qbittorrent_torrent_progress";
        private static readonly Gauge MetricProgress =
            Metrics.CreateGauge(MetricProgressGaugeName
                , "The current progress for a specific torrent"
                , new GaugeConfiguration
                {
                    LabelNames = new[] { "torrent", "hash" }
                });
        #endregion

        static void Main(string[] args)
        {
            // Note:  This is a really simple implementation, but it works.
            Dictionary<string, long> LastBytesDownloadedState = new Dictionary<string, long>();
            Dictionary<string, long> LastBytesUploadedState = new Dictionary<string, long>();

            CLOptions options = null;
            Parser.Default.ParseArguments<CLOptions>(args)
                .WithParsed<CLOptions>(o =>
                {
                    options = o;
                });

            Console.WriteLine($"Starting Prometheus scrape listener on {options.PrometheusAddress}:{options.PrometheusPort}");
            var promServer = new MetricServer(options.PrometheusAddress, options.PrometheusPort);
            promServer.Start();

            Console.WriteLine($"Creating qbittorrent WebAPI client on {options.QbittorrentIpAddress}:{options.QbittorrentPort}");
            QBittorrentClient client = new QBittorrentClient(new Uri($"http://{options.QbittorrentIpAddress}:{options.QbittorrentPort}"));

            //UTorrentClient client = new UTorrentClient(options.QbittorrentIpAddress, options.QbittorrentPort,
            //    options.QbittorrentUsername, options.QbittorrentPassword);
            bool reconnectRequired = true;
            for (; ; )
            {
                try
                {
                    if (reconnectRequired)
                    {
                        try { var logoutTask = client.LogoutAsync(); logoutTask.Wait(); } catch { }
                        var loginTask = client.LoginAsync(options.QbittorrentUsername, options.QbittorrentPassword);
                        loginTask.Wait();
                    }
                    reconnectRequired = false;

                    if (options.Verbose) Console.WriteLine($"Processing Qbittorrent Poll at {DateTime.Now}");
                    var getTorrentListTask = client.GetTorrentListAsync();
                    getTorrentListTask.Wait();
                    var response = getTorrentListTask.Result;

                    if (response != null)
                    {
                        // summarize all sub-torrent data
                        foreach (var torrent in response)
                        {
                            if (torrent.Downloaded.HasValue)
                            {
                                // MetricBytesDownloaded
                                if (LastBytesDownloadedState.ContainsKey(torrent.Hash))
                                {
                                    var increaseAmt = torrent.Downloaded.Value - LastBytesDownloadedState[torrent.Hash];
                                    MetricBytesDownloaded.WithLabels(SanitizeString(torrent.Name), torrent.Hash).Inc(increaseAmt);
                                    if (options.Verbose) Console.WriteLine($"{MetricBytesDownloadedCounterName} with torrent = \"{SanitizeString(torrent.Name)}\" increased by {increaseAmt}.");
                                }

                                LastBytesDownloadedState[torrent.Hash] = torrent.Downloaded.Value;
                            }

                            if (torrent.Uploaded.HasValue)
                            {
                                // MetricBytesUploaded
                                if (LastBytesUploadedState.ContainsKey(torrent.Hash))
                                {
                                    var increaseAmt = torrent.Uploaded.Value - LastBytesUploadedState[torrent.Hash];
                                    MetricBytesUploaded.WithLabels(SanitizeString(torrent.Name), torrent.Hash).Inc(increaseAmt);
                                    if (options.Verbose) Console.WriteLine($"{MetricBytesUploadedCounterName} with torrent = \"{SanitizeString(torrent.Name)}\" increased by {increaseAmt}.");
                                }

                                LastBytesUploadedState[torrent.Hash] = torrent.Uploaded.Value;
                            }

                            MetricDownloadSpeed.WithLabels(SanitizeString(torrent.Name), torrent.Hash).Set(torrent.DownloadSpeed);
                            if (options.Verbose) Console.WriteLine($"{MetricDownloadSpeedGaugeName} with torrent = \"{SanitizeString(torrent.Name)}\" set to {torrent.DownloadSpeed}.");
                            MetricUploadSpeed.WithLabels(SanitizeString(torrent.Name), torrent.Hash).Set(torrent.UploadSpeed);
                            if (options.Verbose) Console.WriteLine($"{MetricUploadSpeedGaugeName} with torrent = \"{SanitizeString(torrent.Name)}\" set to {torrent.UploadSpeed}.");

                            MetricTotalSize.WithLabels(SanitizeString(torrent.Name), torrent.Hash).Set(torrent.TotalSize??0);
                            if (options.Verbose) Console.WriteLine($"{MetricTotalSizeGaugeName} with torrent = \"{SanitizeString(torrent.Name)}\" set to {torrent.Size}.");
                            MetricRemainingSize.WithLabels(SanitizeString(torrent.Name), torrent.Hash).Set(torrent.IncompletedSize??0);
                            if (options.Verbose) Console.WriteLine($"{MetricRemainingSizeGaugeName} with torrent = \"{SanitizeString(torrent.Name)}\" set to {torrent.IncompletedSize??0}.");
                            MetricPeersConnected.WithLabels(SanitizeString(torrent.Name), torrent.Hash).Set(torrent.ConnectedLeechers);
                            if (options.Verbose) Console.WriteLine($"{MetricPeersConnectedGaugeName} with torrent = \"{SanitizeString(torrent.Name)}\" set to {torrent.ConnectedLeechers}.");
                            MetricPeersInSwarm.WithLabels(SanitizeString(torrent.Name), torrent.Hash).Set(torrent.TotalLeechers);
                            if (options.Verbose) Console.WriteLine($"{MetricPeersInSwarmGaugeName} with torrent = \"{SanitizeString(torrent.Name)}\" set to {torrent.TotalLeechers}.");
                            MetricSeedsConnected.WithLabels(SanitizeString(torrent.Name), torrent.Hash).Set(torrent.ConnectedSeeds);
                            if (options.Verbose) Console.WriteLine($"{MetricSeedsConnectedGaugeName} with torrent = \"{SanitizeString(torrent.Name)}\" set to {torrent.ConnectedSeeds}.");
                            MetricSeedsInSwarm.WithLabels(SanitizeString(torrent.Name), torrent.Hash).Set(torrent.TotalSeeds);
                            if (options.Verbose) Console.WriteLine($"{MetricSeedsInSwarmGaugeName} with torrent = \"{SanitizeString(torrent.Name)}\" set to {torrent.TotalSeeds}.");
                            MetricRatio.WithLabels(SanitizeString(torrent.Name), torrent.Hash).Set(torrent.Ratio);
                            if (options.Verbose) Console.WriteLine($"{MetricRatioGaugeName} with torrent = \"{SanitizeString(torrent.Name)}\" set to {torrent.Ratio}.");
                            MetricProgress.WithLabels(SanitizeString(torrent.Name), torrent.Hash).Set(torrent.Progress);
                            if (options.Verbose) Console.WriteLine($"{MetricProgressGaugeName} with torrent = \"{SanitizeString(torrent.Name)}\" set to {torrent.Progress}.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Fatal Error: {ex.Message}");
                    reconnectRequired = true;
                }

                System.Threading.Thread.Sleep(TimeSpan.FromSeconds(options.QbitorrentPollSeconds));
            }
        }

        static string SanitizeString(string input)
        {
            return input.Replace("\"", "").Replace("\\", "");
        }
    }
}
