using System;
using System.Threading;
using Npgsql;
using ServiceStack.Text;
using Features = System.Collections.Generic.Dictionary<string, bool>;

namespace Rooletochka {
    internal class Program {
        const int SLEEP_TIME = 1000;
        static object urlLocker = new object();

        public static Model CreateModel() {
            string connect = @"Server = 127.0.0.1; Port = 5432; User Id = s;
                Database = practice;";
            // Connection string for windows.
            // string connect = @"Server=127.0.0.1;Port=5432;User Id=postgres;
            //    Password=1111;Database=test_db;Preload Reader = true;";
            var connection = new NpgsqlConnection(connect);
            return new Model(connection);
        }

        // Example: analyze one url from database.
        //
        public static void Analyze(Model model) {
            string url = "";
            int siteId = -1;
            NpgsqlDataReader urlRow;

            // This part must be locked, otherwise threads will read
            // the same site.
            //
            lock (urlLocker) {
                urlRow = model.GetUrl();

                // If database haven't site to analyze.
                if (!urlRow.Read()) {
                    Console.WriteLine("All sites are processed or "
                        + "processing now.");
                    Thread.Sleep(SLEEP_TIME);
                    return;
                }
                siteId = urlRow.GetInt32(0);
                model.MarkSiteProcessed(siteId);
            }
            url = urlRow.GetString(1);
            Console.WriteLine(url);

            try {
                Analyzer analyzer = new Analyzer(url);
                int reportId = model.NewReport(siteId);
                report = analyzer.Analyze(reportId);
                report.PutIntoDB(model, siteId);
            }
            catch (InvalidOperationException ex) {
                Console.WriteLine("Analyze Error: {0}", ex.Message);
                model.MarkSiteFailed(siteId);
            }
            catch (Exception ex) {
                Console.WriteLine("Unknown error: " + ex.Message);
                model.MarkSiteFailed(siteId);
            }
        }

        // Function for execution at thread. Calls Analyze() function at
        // infinite cycle.
        public static void OneThread(object model) {
            Model castModel = (Model) model;
            while (true) { Analyze(castModel); }
        }

        // Example: grab information for .pdf generation from database
        // and write it to stdout.
        //
        public static void WriteReadyData(Model model) {
            NpgsqlDataReader urlRow = model.GetUrlForReport();

            // Check if database have site ready for .pdf generation.
            //
            if (!urlRow.Read()) { return; }

            int siteId = urlRow.GetInt32(0);
            int reportId = model.GetReportId(siteId);
            NpgsqlDataReader subpages = model.GetSubpages(reportId);

            while (subpages.Read()) {
                string featuresAddress = subpages.GetString(0);
                Console.WriteLine(featuresAddress);

                Features rules = subpages.GetString(1).FromJson<Features>();
                foreach (var rule in rules) {
                    Console.WriteLine(rule.Key + " = " + rule.Value);
                    if (!rule.Value) {
                        Console.WriteLine(model.Explain(rule.Key));
                    }
                }
            }
        }

        // Let's test!
        //
        private static void Main(string[] args) {
            const int THREADS_COUNT = 10;
            var model = CreateModel();
            for (int i = 0; i < THREADS_COUNT; i++) {
                new Thread(OneThread).Start((object) model);
            }

            // WriteReadyData(model);
        }
    }
}
