using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Glue
{
    class Program
    {
        static void Main(string[] args)
        {
            var desiredFiles = DesiredFiles;
            using (FileStream fs = new FileStream(OutputFile, FileMode.Create, FileAccess.Write, FileShare.Read))
            using (StreamWriter writer = new StreamWriter(fs, Encoding.UTF8))
            {
                writer.WriteLine("[");
                bool isFirst = true;
                foreach (FileInfo info in desiredFiles)
                {
                    Console.WriteLine($"Processing {info.Name} ({info.Length/1024}kb)");
                    if (isFirst)
                        isFirst = false;
                    else
                        writer.WriteLine(",");

                    using (StreamReader reader = info.OpenText())
                    {
                        while(!reader.EndOfStream)
                            writer.WriteLine(reader.ReadLine());
                    }
                }
                writer.WriteLine("]");
                writer.Flush();
            }

            Console.WriteLine("Done!");
            Console.ReadLine();
        }


        static string Location => ConfigurationManager.AppSettings["location"];
        private static string PhotosLocation => Path.Combine(Location, "photos\\");
        private static IEnumerable<FileInfo> DesiredFiles => new DirectoryInfo(PhotosLocation)
            .EnumerateFiles("*.json", SearchOption.TopDirectoryOnly)
            .Where(fi => Regex.IsMatch(fi.Name, "^[0-9]+\\.json$"));

        private static string OutputFile => Path.Combine(Location, "full-info.json");
    }
}
