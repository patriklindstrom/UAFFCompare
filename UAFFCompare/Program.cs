using System;
using System.Collections.Concurrent;
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
    internal class Program
    {
        /// <summary>
        /// Takes two parameters file A and File B. Set A is contained in B also.
        /// A is file n and B is file n+1
        ///  We want the difference from (in bool notation): 
        /// (A'B)+(AB) => DiffFile
        /// (AB) => IntersectionFile
        /// NotaBene DiffFile+IntersctionFile => FileA
        /// </summary>
        /// <param name="args">-a fileA -b fileB -v optional verbose</param>
        private static void Main(string[] args)
        {
            var options = new Options();
            if (CommandLine.Parser.Default.ParseArguments(args, options))
            {
                string fileA = options.FileA;
                string fileB = options.FileB;
                ValidateArgs(fileA, fileB);
                var programStopwatch = Stopwatch.StartNew();
                var fDD = new List<FileDictionaryDigger>
                {
                    new FileDictionaryDigger(fileA, options),
                    new FileDictionaryDigger(fileB, options)
                };
                //Multithread the reading of files and making dictionary of all lines in file
                Parallel.ForEach(fDD, fdd => fdd.DigDictionary());
                var keyOnly = new DictCompareOnKeyOnly();
                Dictionary<string, string> ldDiffB = fDD[1].LineDictionary.Except(fDD[0].LineDictionary, keyOnly).ToDictionary(ld=>ld.Key,ld=>ld.Value);
                Dictionary<string, string> ldIntersectAandB = fDD[1].LineDictionary.Intersect(fDD[0].LineDictionary, keyOnly).ToDictionary(ld => ld.Key, ld => ld.Value);
                programStopwatch.Stop();
                if (options.Verbose)
                {
                    #region Console output Answer

                    Console.WriteLine("File {0} has {1} keys. {2} of them exist in file {3} also"
                        , Path.GetFileName(fDD[0].FilePath), fDD[0].LineDictionary.Count()
                        , ldIntersectAandB.Count(), Path.GetFileName(fDD[1].FilePath));
                    Console.WriteLine("File {0} has {1} keys. {2} of them do not exist in file {3}"
                        , Path.GetFileName(fDD[0].FilePath), fDD[0].LineDictionary.Count()
                        , ldDiffB.Count(), Path.GetFileName(fDD[1].FilePath));
                    Console.WriteLine("Same ({0}) + Diff ({1}) = {2} == Number of rows in {3} ({4})  "
                        , ldIntersectAandB.Count(), ldDiffB.Count(), ldIntersectAandB.Count() + ldDiffB.Count(),
                        Path.GetFileName(fDD[0].FilePath), fDD[0].LineDictionary.Count());
                    Console.ForegroundColor = ConsoleColor.White;
                    #endregion
                }
                ldDiffB.SaveValuesAsFile(System.IO.Path.Combine(Path.GetDirectoryName(fDD[0].FilePath),
                    "UAFFDiffRows.csv"));
                ldIntersectAandB.SaveValuesAsFile(System.IO.Path.Combine(Path.GetDirectoryName(fDD[0].FilePath),
                    "UAFFCommonRows.csv"));
                #region Console output Done

                if (options.Verbose)
                {
                    Console.WriteLine("Done ! hit any key to exit program. ExecutionTime was {0} ms",
                        programStopwatch.Elapsed.Milliseconds);
                    Console.ReadLine();
                }

                #endregion

            }
        }

        private static void ValidateArgs(string fileA, string fileB)
        {
            Debug.Assert(String.IsNullOrEmpty(fileA) == false);
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
        private const int OFFSET_UNIQUE_START_FIELD = 3;
        private string Filecontent { get; set; }

        public string FilePath
        {
            get { return _filePath ?? String.Empty; }
            set { _filePath = value; }
        }

        public Dictionary<string, string> LineDictionary { get; set; }
        public Options Option;
        private string _filePath;

        public FileDictionaryDigger(string filePath, Options option)
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
            int i = 0; //rowcounter to see where error occured
            try
            {
                var fileStopwatch = Stopwatch.StartNew();
                #region Verbose output

                if (Option.Verbose)
                {
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.WriteLine("Start reading {0}", FilePath);
                }

                #endregion
                using (var sr = new DataReader(FilePath))
                {
                    string line;
                    var rowKey = new StringBuilder();
                    while ((line = sr.ReadLine()) != null)
                    {
                        i += 1;
                        var fieldArr = line.Split(';');
                        //Fields 4,6,7 makes the row unique according to rumours. Not that fieldArr is nollbased so it is: 3,5,6
                        rowKey.Append(fieldArr[3]).Append("|").Append(fieldArr[5]).Append("|").Append(fieldArr[6]);
                        LineDictionary.Add(rowKey.ToString(), line);
                        rowKey.Clear();
                    }
                }
                fileStopwatch.Stop();
                #region Verbose output

                if (Option.Verbose)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                    Console.WriteLine("Done Reading {0} took {1} ms contains {2} rows", FilePath,
                        fileStopwatch.Elapsed.Milliseconds, LineDictionary.Count);
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                }

                #endregion
            }
                #region Catch if error

            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("The file {0} could not be transformed to Dictionary structure error in line {1}:",
                    FilePath, i);
                Console.WriteLine(e.Message);
                Console.ForegroundColor = ConsoleColor.White;
            }

            #endregion
        }
    }
    public static class Dictionary
    {
        /// <summary>
        /// Extended method that saves the value part of a string string dictionary as rows in a textfile
        /// </summary>
        /// <param name="dict">this dictionary</param>
        /// <param name="filePath">The path to where the files should be stored eg: c:\temp</param>
        public static void SaveValuesAsFile(this Dictionary<string, string> dict, string filePath)
        {
            using (StreamWriter writer = new StreamWriter(filePath))
                foreach (var item in dict)
                    writer.WriteLine("{0}", item.Value);
        }
    }
    public class DictCompareOnKeyOnly : IEqualityComparer<KeyValuePair<string, string>>
    {
        public bool Equals(KeyValuePair<string, string> x, KeyValuePair<string, string> y)
        {
            return x.Key.Equals(y.Key);
        }

        public int GetHashCode(KeyValuePair<string, string> obj)
        {
            return obj.Key.GetHashCode();
        }
    }
    public class Options
    {
        [Option('a', "fileA", Required = true, HelpText = "Input A csv file to read.")]
        public string FileA { get; set; }

        [Option('b', "fileb", Required = true, HelpText = "Input B csv file to read.")]
        public string FileB { get; set; }


        [Option('d', "DiffB", Required = false, HelpText = "Calculate and output intersectAandB csv file.")]
        public string DiffB { get; set; }

        [Option('i', "IntersectAandB", Required = false, HelpText = "Calculate and output intersectAandB csv file.")]
        public string IntersectAandB { get; set; }

        [Option('v', null, HelpText = "Print details during execution.")]
        public bool Verbose { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            // this without using CommandLine.Text
            //  or using HelpText.AutoBuild
            var usage = new StringBuilder();
            usage.AppendLine(
                String.Format(
                    "UAFFCompare Application takes Difference between two cvs files on columns 4,6,7 version {0}",
                    System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString()));
            usage.AppendLine("give help as param for help. Simple usage -a[fileA] -b[fileB] ");
            usage.AppendLine("Developed by Patrik Lindström 2015-02-25");
            return usage.ToString();
        }
    }

    interface ILineReader
    {
      string  ReadLine();
    }

    public class DataReader : ILineReader, IDisposable
    {
        public StreamReader StreamReader { get; set; }
        public DataReader(string path)
        {
            StreamReader = new StreamReader(File.OpenRead(path));
        }
        public string ReadLine()
        {
            return StreamReader.ReadLine();
        }

        public void Dispose()
        {
            StreamReader.Dispose();
        }
    }
}

