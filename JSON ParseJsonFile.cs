using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ConsoleApplication1
{
    public class Parse
    {
        private static readonly string data = DateTime.Now.ToString("yyyy MM dd ");
        const string jsonDir = @"C:\Users\Roman\Desktop\Json\";
        const string pdfDir = @"C:\Users\Roman\Desktop\PDF\";
        const string csvDir = @"C:\Users\Roman\Desktop\";

        private static void Main(string[] args)
        {
            var fCount = Directory.GetFiles(jsonDir, "*", SearchOption.AllDirectories).Length;
            for (var i = 0; i < fCount; i++)
            {
                if (!File.Exists(jsonDir + "data" + i + ".json")) continue;
                var json = File.ReadAllText(jsonDir + "data" + i + ".json");
                var root = JArray.Parse(json);
                if (i == 0)
                {
                    var array = root[1].ToObject<Item[]>();
                    RenameAndSaveFile(pdfDir, csvDir: csvDir, array: array);
                }
                else
                {
                    var dict = root[1].ToObject<Dictionary<string, Item>>();
                    RenameAndSaveFile(pdfDir, csvDir: csvDir, dict: dict);
                }
            }
        }

        public class Item
        {
            [JsonProperty("docid")] public string docid;
            [JsonProperty("code")] public string code;
            [JsonProperty("name")] public string name;
            [JsonProperty("activitydate")] public string activitydate;
            [JsonProperty("status")] public string status;
            [JsonProperty("changes")] public string changes;
        }

        private static void RenameAndSaveFile(string pdfDir, string csvDir = null, IEnumerable<Item> array = null,
            Dictionary<string, Item> dict = null)
        {
            csvDir += data;

            if (array != null)
            {
                foreach (var keyValuePair in array)
                {
                    var first = keyValuePair.docid;
                    var second = keyValuePair.code;
                    var third = keyValuePair.name;

                    RenameFile(pdfDir, first, second, third);

                    if (csvDir != null)
                    {
                        SaveCsv(csvDir, $"{first};{second};{third}\r\n");
                    }
                }
            }
            else if (dict != null)
            {
                foreach (var keyValuePair in dict)
                {
                    var first = keyValuePair.Value.docid;
                    var second = keyValuePair.Value.code;
                    var third = keyValuePair.Value.name;

                    RenameFile(pdfDir, first, second, third);

                    if (csvDir != null)
                    {
                        SaveCsv(csvDir, $"{first};{second};{third}\r\n");
                    }
                }
            }
        }

        private static void SaveCsv(string csvPath, string csv)
        {
            File.AppendAllText(csvPath + "NormyBy.csv", csv, Encoding.UTF8);
        }

        private static void RenameFile(string path, string first, string second, string third)
        {
            try
            {
                if (File.Exists(path + first + ".pdf"))
                {
                    File.Move(path + first + ".pdf", path + $"{second} {third}" + ".pdf");
                }
            }
            catch (Exception)
            {
                try
                {
                    File.Move(path + first + ".pdf",
                        path + second.Replace("/", "-").Replace("*", "").Replace(":", "").Replace("\"", "") +
                        ".pdf");
                }
                catch (Exception)
                {
                    Console.WriteLine(second + first);
                }
            }
        }
    }
}
