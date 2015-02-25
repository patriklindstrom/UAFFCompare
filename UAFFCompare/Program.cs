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
using CommandLine;
using CommandLine.Text;

namespace UAFFCompare
{
     
    class Program
    {     
        static void Main(string[] args)
        {

            var options = new Options();
            if (CommandLine.Parser.Default.ParseArguments(args, options))
            {

            
            string fileA = options.fileA;
            string fileB = options.fileB;
            ValidateArgs(fileA, fileB);
            var programStopwatch = Stopwatch.StartNew();
            var fDD = new List<FileDictionaryDigger>
            {
                new FileDictionaryDigger(fileA,options),
                new FileDictionaryDigger(fileB,options)
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
            if (options.Verbose)
            {
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
            }
            LDDiff.SaveValuesAsFile(System.IO.Path.Combine(Path.GetDirectoryName(fDD[0].FilePath), "UAFFDiffRows.csv"));
            LDIntersect.SaveValuesAsFile(System.IO.Path.Combine(Path.GetDirectoryName(fDD[0].FilePath), "UAFFCommonRows.csv"));
            if (options.Verbose)
            {
                Console.WriteLine("Done ! hit any key to exit program. ExecutionTime was {0} ms", programStopwatch.Elapsed.Milliseconds);
                Console.ReadLine();
            }
        }
        } 
        private static void ValidateArgs(string fileA,string fileB)
        {
            Debug.Assert(String.IsNullOrEmpty(fileA)==false);
            Debug.Assert(String.IsNullOrEmpty(fileB) == false);
            Debug.WriteLine(fileA);
            Debug.WriteLine(fileB);
            Debug.Assert(File.Exists(fileA));
            Debug.Assert(File.Exists(fileB));
            if (!(File.Exists(fileA) && File.Exists(fileB)))
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
        public Options Option;
        public FileDictionaryDigger( string filePath,Options option)
        {
            Option = option;
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
                if (Option.Verbose) { 
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine("Start reading {0}", FilePath);
                }
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
                 if (Option.Verbose) {
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.WriteLine("Done Reading {0} took {1} ms contains {2} rows", FilePath, fileStopwatch.Elapsed.Milliseconds, LineDictionary.Count);
                Console.ForegroundColor = ConsoleColor.DarkRed;
                 }
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
    public class Options
    {
        [Option('a', "fileA", Required = true, HelpText = "Input A csv file to read.")]
        public string fileA { get; set; }
        [Option('b', "fileb", Required = true, HelpText = "Input B csv file to read.")]
        public string fileB { get; set; }

        [Option('v', null, HelpText = "Print details during execution.")]
        public bool Verbose { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            // this without using CommandLine.Text
            //  or using HelpText.AutoBuild
            var usage = new StringBuilder();
            usage.AppendLine(String.Format("UAFFCompare Application takes Difference between two cvs files on columns 4,6,7 version {0}", System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString()));
            usage.AppendLine("give help as param for help. Simple usage -a[fileA] -b[fileB] ");
            usage.AppendLine("Developed by Patrik Lindström 2015-02-25");
            return usage.ToString();
        }
    }
   
}

