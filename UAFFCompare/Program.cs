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
            var fDD = new List<FileDictionaryDigger>
            {
                new FileDictionaryDigger(fileA),
                new FileDictionaryDigger(fileB)
            };
            //Multithread the reading of files and making dictionary of all lines in file
            Parallel.ForEach(fDD, fdd => fdd.DigDictionary());
           // var intersectDict =  fileDictionaryDiggers[0].LineDictionary.Intersect(fileDictionaryDiggers[0].LineDictionary);
           // var intersectDict = fileDictionaryDiggers[0].LineDictionary.AsParallel().Intersect(fileDictionaryDiggers[0].LineDictionary.AsParallel());
            var LDDiff = new Dictionary<string, string>();
            var LDIntersect = new Dictionary<string, string>();
            foreach (var line in fDD[0].LineDictionary)
            {
                if (fDD[1].LineDictionary.ContainsKey(line.Key))
                {
                    LDIntersect.Add(line.Key, line.Value); 
                }
                else
                {
                    LDDiff.Add(line.Key, line.Value);
                }         
            }
            programStopwatch.Stop();
            #region Console output Answere
            Console.WriteLine("File {0} has {1} keys. {2} of them exist in file {3} also"
                       , Path.GetFileName(fDD[0].FilePath), fDD[0].LineDictionary.Count()
                       , LDIntersect.Count, Path.GetFileName(fDD[1].FilePath));
            Console.WriteLine("File {0} has {1} keys. {2} of them do not exist in file {3}"
                               , Path.GetFileName(fDD[0].FilePath), fDD[0].LineDictionary.Count()
                               , LDDiff.Count, Path.GetFileName(fDD[1].FilePath));
            Console.WriteLine("Same ({0}) + Diff ({1}) = {2} == Number of rows in {3} ({4})  "
                               , LDIntersect.Count, LDDiff.Count, LDIntersect.Count + LDDiff.Count,
                               Path.GetFileName(fDD[0].FilePath), fDD[0].LineDictionary.Count());
            Console.ForegroundColor = ConsoleColor.White; 
            #endregion
            LDDiff.SaveValuesAsFile(System.IO.Path.Combine(Path.GetDirectoryName(fDD[0].FilePath), "UAFFDiffRows.csv"));
            LDIntersect.SaveValuesAsFile(System.IO.Path.Combine(Path.GetDirectoryName(fDD[0].FilePath), "UAFFCommonRows.csv"));
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
        public string FilePath { get; set; }
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
                    string line;
                    var rowKey = new StringBuilder();
                    while ((line = sr.ReadLine()) != null)
                    {
                       var fieldArr= line.Split(';');
                       //Fields 4,6,7 makes the row unique according to rumours. Not that fieldArr is nollbased so it is: 3,5,6
                        rowKey.Append(fieldArr[3]).Append("|").Append(fieldArr[5]).Append("|").Append(fieldArr[6]);
                        LineDictionary.Add(rowKey.ToString(), line);
                        rowKey.Clear();
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
    public static class Dictionary
    {
    public static void SaveValuesAsFile(this Dictionary<string, string> coll,string filePath){
        using (StreamWriter writer = new StreamWriter(filePath))
            foreach (var item in coll)
                writer.WriteLine("{0}", item.Value);
            } 
    }
}
