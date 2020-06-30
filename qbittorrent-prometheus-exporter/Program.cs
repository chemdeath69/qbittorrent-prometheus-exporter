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
        static void Main(string[] args)
        {
            List<string> removeHashes = new List<string>();
            Dictionary<string, MetricsTorrent> torrentMetrics = new Dictionary<string, MetricsTorrent>();

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
                        foreach (var trackedTorrent in torrentMetrics)
                        {
                            trackedTorrent.Value.BeginPoll();
                        }

                        // summarize all sub-torrent data
                        foreach (var torrent in response)
                        {
                            if (torrentMetrics.ContainsKey(torrent.Hash))
                            {
                                torrentMetrics[torrent.Hash].UpdateStatus(torrent);
                            }
                            else
                            {
                                var newTorrent = new MetricsTorrent(torrent, options);
                                torrentMetrics.Add(torrent.Hash, newTorrent);
                                newTorrent.UpdateStatus(torrent);
                            }                            
                        }
                        
                        foreach (var trackedTorrent in torrentMetrics)
                        {
                            trackedTorrent.Value.EndPoll();
                            if (trackedTorrent.Value.TorrentDeleted)
                                removeHashes.Add(trackedTorrent.Value.Hash);
                        }
                        foreach (var item in removeHashes)
                        {
                            torrentMetrics.Remove(item);
                        }
                        removeHashes.Clear();
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
    }
}
