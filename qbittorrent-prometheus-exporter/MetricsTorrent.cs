using Prometheus;
using QBittorrent.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace qbittorrent_prometheus_exporter
{
    class MetricsTorrent
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

        private CLOptions options;
        private bool hasUpdated = false;
        private long? lastBytesDownloaded = null;
        private long? lastBytesUploaded = null;

        public string Hash { get; private set; }

        public string TorrentName { get; set; }

        public bool TorrentDeleted { get; private set; }

        public MetricsTorrent(TorrentInfo trackedTorrent, CLOptions options)
        {
            TorrentName = trackedTorrent.Name;
            Hash = trackedTorrent.Hash;
            this.options = options;
            TorrentDeleted = false;
        }

        public void BeginPoll()
        {
            hasUpdated = false;
        }

        public void EndPoll()
        {
            // If this torrent was never sent an update, then it's no longer in the user's
            // download/upload queue.  Clear any gauges left on it.
            if (!hasUpdated)
            {
                try
                {
                    MetricDownloadSpeed.WithLabels(SanitizeString(TorrentName), Hash).Remove();
                    MetricUploadSpeed.WithLabels(SanitizeString(TorrentName), Hash).Remove();
                    MetricTotalSize.WithLabels(SanitizeString(TorrentName), Hash).Remove();
                    MetricRemainingSize.WithLabels(SanitizeString(TorrentName), Hash).Remove();
                    MetricPeersConnected.WithLabels(SanitizeString(TorrentName), Hash).Remove();
                    MetricPeersInSwarm.WithLabels(SanitizeString(TorrentName), Hash).Remove();
                    MetricSeedsConnected.WithLabels(SanitizeString(TorrentName), Hash).Remove();
                    MetricSeedsInSwarm.WithLabels(SanitizeString(TorrentName), Hash).Remove();
                    MetricRatio.WithLabels(SanitizeString(TorrentName), Hash).Remove();
                    MetricProgress.WithLabels(SanitizeString(TorrentName), Hash).Remove();
                    MetricBytesDownloaded.WithLabels(SanitizeString(TorrentName), Hash).Remove();
                    MetricBytesUploaded.WithLabels(SanitizeString(TorrentName), Hash).Remove();

                    TorrentDeleted = true;
                }
                catch { }
            }
        }

        public void UpdateStatus(TorrentInfo info)
        {
            if (info.Downloaded.HasValue)
            {
                // MetricBytesDownloaded
                if (lastBytesDownloaded.HasValue)
                {
                    var increaseAmt = info.Downloaded.Value - lastBytesDownloaded.Value;
                    MetricBytesDownloaded.WithLabels(SanitizeString(TorrentName), Hash).Inc(increaseAmt);
                    if (options.Verbose) Console.WriteLine($"{MetricBytesDownloadedCounterName} with torrent = \"{SanitizeString(TorrentName)}\" increased by {increaseAmt}.");
                }

                lastBytesDownloaded = info.Downloaded;
            }

            if (info.Uploaded.HasValue)
            {
                // MetricBytesUploaded
                if (lastBytesUploaded.HasValue)
                {
                    var increaseAmt = info.Uploaded.Value - lastBytesUploaded.Value;
                    MetricBytesUploaded.WithLabels(SanitizeString(TorrentName), Hash).Inc(increaseAmt);
                    if (options.Verbose) Console.WriteLine($"{MetricBytesUploadedCounterName} with torrent = \"{SanitizeString(TorrentName)}\" increased by {increaseAmt}.");
                }

                lastBytesUploaded = info.Uploaded;
            }

            MetricDownloadSpeed.WithLabels(SanitizeString(TorrentName), Hash).Set(info.DownloadSpeed);
            if (options.Verbose) Console.WriteLine($"{MetricDownloadSpeedGaugeName} with torrent = \"{SanitizeString(TorrentName)}\" set to {info.DownloadSpeed}.");
            MetricUploadSpeed.WithLabels(SanitizeString(TorrentName), Hash).Set(info.UploadSpeed);
            if (options.Verbose) Console.WriteLine($"{MetricUploadSpeedGaugeName} with torrent = \"{SanitizeString(TorrentName)}\" set to {info.UploadSpeed}.");

            MetricTotalSize.WithLabels(SanitizeString(TorrentName), Hash).Set(info.TotalSize ?? 0);
            if (options.Verbose) Console.WriteLine($"{MetricTotalSizeGaugeName} with torrent = \"{SanitizeString(TorrentName)}\" set to {info.Size}.");
            MetricRemainingSize.WithLabels(SanitizeString(TorrentName), Hash).Set(info.IncompletedSize ?? 0);
            if (options.Verbose) Console.WriteLine($"{MetricRemainingSizeGaugeName} with torrent = \"{SanitizeString(TorrentName)}\" set to {info.IncompletedSize ?? 0}.");
            MetricPeersConnected.WithLabels(SanitizeString(TorrentName), Hash).Set(info.ConnectedLeechers);
            if (options.Verbose) Console.WriteLine($"{MetricPeersConnectedGaugeName} with torrent = \"{SanitizeString(TorrentName)}\" set to {info.ConnectedLeechers}.");
            MetricPeersInSwarm.WithLabels(SanitizeString(TorrentName), Hash).Set(info.TotalLeechers);
            if (options.Verbose) Console.WriteLine($"{MetricPeersInSwarmGaugeName} with torrent = \"{SanitizeString(TorrentName)}\" set to {info.TotalLeechers}.");
            MetricSeedsConnected.WithLabels(SanitizeString(TorrentName), Hash).Set(info.ConnectedSeeds);
            if (options.Verbose) Console.WriteLine($"{MetricSeedsConnectedGaugeName} with torrent = \"{SanitizeString(TorrentName)}\" set to {info.ConnectedSeeds}.");
            MetricSeedsInSwarm.WithLabels(SanitizeString(TorrentName), Hash).Set(info.TotalSeeds);
            if (options.Verbose) Console.WriteLine($"{MetricSeedsInSwarmGaugeName} with torrent = \"{SanitizeString(TorrentName)}\" set to {info.TotalSeeds}.");
            MetricRatio.WithLabels(SanitizeString(TorrentName), Hash).Set(info.Ratio);
            if (options.Verbose) Console.WriteLine($"{MetricRatioGaugeName} with torrent = \"{SanitizeString(TorrentName)}\" set to {info.Ratio}.");
            MetricProgress.WithLabels(SanitizeString(TorrentName), Hash).Set(info.Progress);
            if (options.Verbose) Console.WriteLine($"{MetricProgressGaugeName} with torrent = \"{SanitizeString(TorrentName)}\" set to {info.Progress}.");

            hasUpdated = true;
        }
        static string SanitizeString(string input)
        {
            return input.Replace("\"", "").Replace("\\", "");
        }
    }
}
