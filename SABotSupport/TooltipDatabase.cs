using CsvHelper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Windows.Forms;

namespace SABotSupport
{
    internal class TooltipData
    {
        public string Item { get; set; }
        public string Description { get; set; }
    }

    internal static class TooltipDatabase
    {
        private static readonly Dictionary<string, string> database = new();
        private static readonly BackgroundWorker worker = new();
        internal static volatile bool IsDatabaseLoaded = false;

        internal static void LoadDatabase(string path)
        {
            if (worker.IsBusy)
                return;

            worker.DoWork += DoWork;
            worker.RunWorkerCompleted += WorkCompleted;
            worker.RunWorkerAsync(path);
        }

        internal static string GetTooltip(string identifier)
        {
            if (!IsDatabaseLoaded)
                return "資料庫讀取中";

            identifier = identifier.Replace(",", String.Empty);

            return database.TryGetValue(identifier, out string description) ? description : "沒有資料";
        }

        internal static IEnumerable<(string item, string description)> DebugDumpDictionary()
        {
            if (!IsDatabaseLoaded)
                throw new ApplicationException("Database not yet loaded");

            foreach (var pair in database)
                yield return (pair.Key, pair.Value);
        }

        private static void DoWork(object sender, DoWorkEventArgs e)
        {
            using var reader = new StreamReader(e.Argument as string);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

            csv.Configuration.AllowComments = true;
            csv.Configuration.MissingFieldFound = new Action<string[], int, ReadingContext>(
                delegate (string[] headerNames, int index, ReadingContext context)
                {
                    MessageBox.Show($"找不到區隔逗號:{Environment.NewLine}{Environment.NewLine}第{context.Row}行: {context.RawRecord}",
                        "Parse error in tooltipData.csv", MessageBoxButtons.OK, MessageBoxIcon.Error);
                });
            var records = csv.GetRecords<TooltipData>();

            database.Clear();

            foreach (var record in records)
            {
                if (database.ContainsKey(record.Item))
                {
                    MessageBox.Show($"找到重複的紀錄: {record.Item}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    continue;
                }

                database.Add(record.Item, record.Description);
            }
        }

        private static void WorkCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            worker.DoWork -= DoWork;
            worker.RunWorkerCompleted -= WorkCompleted;
            IsDatabaseLoaded = true;
        }
    }
}
