using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace UAFFCompare
{
     
    class Program
    {     
        static void Main(string[] args)
        {
            ValidateArgs(args);
            string fileA = args[0];
            string fileB = args[1];
            var programStopwatch = Stopwatch.StartNew();
            var fileDictionaryDiggers = new List<FileDictionaryDigger>
            {
                new FileDictionaryDigger(fileA),
                new FileDictionaryDigger(fileB)
            };
            //Multithread the reading of files and making dictionary of all lines in file
            Parallel.ForEach(fileDictionaryDiggers, fdd => fdd.DigDictionary());
           // var intersectDict =  fileDictionaryDiggers[0].LineDictionary.Intersect(fileDictionaryDiggers[0].LineDictionary);
            var intersectDict = fileDictionaryDiggers[0].LineDictionary.AsParallel().Intersect(fileDictionaryDiggers[0].LineDictionary.AsParallel());
            programStopwatch.Stop();
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Done ! hit any key to exit program. ExecutionTime was {0} ms", programStopwatch.Elapsed.Milliseconds);
            Console.ReadLine();
        }
        private static void ValidateArgs(string[] args)
        {
            Debug.Assert(args.Length == 2);
            Debug.WriteLine(args[0]);
            Debug.WriteLine(args[1]);
            if (args.Length != 2)
            {
                throw new ArgumentException("Two UAFF files path has to be given");
            }
            Debug.Assert(File.Exists(args[0]));
            Debug.Assert(File.Exists(args[1]));
            if (!(File.Exists(args[0]) && File.Exists(args[1])))
            {
                throw new ArgumentException("File do not exist");
            }
        }
    }

    public class FileDictionaryDigger
    {
        const int OFFSET_UNIQUE_START_FIELD = 3;
        private string Filecontent { get; set; }
        private string FilePath { get; set; }
        public Dictionary<string, string> LineDictionary { get; set; }
        public FileDictionaryDigger( string filePath)
        {
            FilePath = filePath;
            LineDictionary = new Dictionary<string, string>();
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
                using (var sr = new StreamReader(File.OpenRead(FilePath)))
                {
                   // Filecontent = sr.ReadToEnd();
                   //  Console.WriteLine(line);
                    string line;
                    string washedLine;
                    MD5 md5Hash = MD5.Create();
                    while ((line = sr.ReadLine()) != null)
                    {
                       var fieldArr= line.Split(';');
                        //Only choose fields that are unique. The 3 first fields are not part of the what makes the row unique according to rumours.
                       var uniqueFields = new ArraySegment<string>(fieldArr, OFFSET_UNIQUE_START_FIELD, fieldArr.Length - OFFSET_UNIQUE_START_FIELD);
                       //Remove the whitespace in fields and make it a long string again.
                       washedLine = String.Join("|", uniqueFields.Select(uF=>uF.Trim()));
                        //Calculate a small hash key that is unique for this row.
                       byte[] hashBytes = md5Hash.ComputeHash(Encoding.Default.GetBytes(washedLine));
                       var rowKey = System.BitConverter.ToString(hashBytes);
                        //add the line and its unique key to a dictionary.
                        LineDictionary.Add(rowKey, line);
                    }

                }
                fileStopwatch.Stop();
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.WriteLine("Done Reading {0} took {1} ms contains {2} rows", FilePath, fileStopwatch.Elapsed.Milliseconds, LineDictionary.Count);
                Console.ForegroundColor = ConsoleColor.DarkRed;
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("The file {0} could not be transformed to Dictionary structure:", FilePath);
                Console.WriteLine(e.Message);
                Console.ForegroundColor = ConsoleColor.White;
            }
        }
    } 
}
