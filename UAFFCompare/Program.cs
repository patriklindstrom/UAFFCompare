using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace UAFFCompare
{
    class Program
    {
        static void Main(string[] args)
        {   Debug.Assert(args.Length==2);
            Debug.WriteLine(args[0]);
            Debug.WriteLine(args[1]);
            if (args.Length != 2)
            {
                throw new ArgumentException("Two UAFF files path has to be given");
            }
            string fileA = args[0];
            string fileB = args[1];
            Debug.Assert(File.Exists(fileA));
            Debug.Assert(File.Exists(fileB));
            var programStopwatch = Stopwatch.StartNew();
 
            Parallel.ForEach(new List<FileDictionaryDigger>
            {
                new FileDictionaryDigger(fileA),
                new FileDictionaryDigger(fileB)
            }, fdd => fdd.DigDictionary());

            programStopwatch.Stop();
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Done ! hit any key to exit program. ExecutionTime was {0} ms", programStopwatch.Elapsed.Milliseconds);
            Console.ReadLine();
        }
    }

    public class FileDictionaryDigger
    {
        private string Filecontent { get; set; }
        private string FilePath { get; set; }
        public Dictionary<string, string> LineDictionary { get; set; }
        public FileDictionaryDigger( string filePath)
        {
            FilePath = filePath;
        }

        public void DigDictionary()
        {
            GetFileContent();
        }

        private void GetFileContent()
        {
            try
            {
                var fileStopwatch = Stopwatch.StartNew();
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine("Start reading {0}", FilePath);
                using (var sr = new StreamReader(FilePath))
                {
                    Filecontent = sr.ReadToEnd();
                    //  Console.WriteLine(line);
                }
                fileStopwatch.Stop();
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.WriteLine("Done Reading {0} took {1} ms ", FilePath, fileStopwatch.Elapsed.Milliseconds);
                Console.ForegroundColor = ConsoleColor.DarkRed;
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("The file {0} could not be read:", FilePath);
                Console.WriteLine(e.Message);
                Console.ForegroundColor = ConsoleColor.White;
            }
        }
    } 
}
