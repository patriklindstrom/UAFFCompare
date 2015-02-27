using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
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
        /// not A and B=> DiffFile  (see http://www.wolframalpha.com/input/?i=not+A+and+B ) 
        /// A and B => IntersectionFile (see http://www.wolframalpha.com/input/?i=A+and+B ) 
        /// NotaBene combined => (not A and B) or (A and B) (see http://www.wolframalpha.com/input/?i=%28not+A+and+B%29+or+%28A+and+B%29 )
        /// DiffFile+IntersctionFile => FileB
        /// </summary>
        /// <param name="args">-a fileA -b fileB -v optional verbose</param>
        private static void Main(string[] args)
        {
            var options = new Options();
            if (CommandLine.Parser.Default.ParseArguments(args, options))
            {
                var programStopwatch = Stopwatch.StartNew();
                var dictList = new List<FileDictionaryDigger>
                {
                    new FileDictionaryDigger(options.FileA,"A", options),
                    new FileDictionaryDigger(options.FileB,"B", options)
                };
                //Multithread the reading of files and making dictionary of all lines in file
                Parallel.ForEach(dictList, dl => dl.DigDictionary());
                //Give the sets nicer names
                var a = dictList.First(f => f.Name == "A");
                var b = dictList.First(f => f.Name == "B"); 
                //We need Compare to use set logic on keys only not Key and Value which is the default - odd that is the default and we have to override it.
                var keyOnly = new DictCompareOnKeyOnly();
                //Here comes the magic simple Except and Intersect and force it back to Dictionary.
                Dictionary<string, string> diffB = b.LineDictionary.Except(a.LineDictionary, keyOnly).ToDictionary(ld=>ld.Key,ld=>ld.Value);
                Dictionary<string, string> intersectAandB = b.LineDictionary.Intersect(a.LineDictionary, keyOnly).ToDictionary(ld => ld.Key, ld => ld.Value);
                VerboseConsoleStatisticsOutput(options,dictList, intersectAandB, diffB, programStopwatch); 
                //Here we save the output as text files
                var outPutList = new List<OutputObj>
                {
                    new OutputObj("DiffB",diffB, options),
                    new OutputObj("IntersectAandB",intersectAandB, options)
                };
                Parallel.ForEach(outPutList, oL => oL.Output());
                diffB.SaveValuesAsFile(Path.Combine(Path.GetDirectoryName(b.FilePath),"DiffB_" + DateTime.Now.ToString("yyyMMddTHHmmss") + ".csv"));
                intersectAandB.SaveValuesAsFile(Path.Combine(Path.GetDirectoryName(b.FilePath),"IntersectAandB" + DateTime.Now.ToString("yyyMMddTHHmmss") + ".csv"));
                VerboseConsoleEndMsg(options,programStopwatch); 
                programStopwatch.Stop();
            }
        }
        private static void VerboseConsoleStatisticsOutput(Options options, List<FileDictionaryDigger> dictList, Dictionary<string, string> ldIntersectAandB, Dictionary<string, string> ldDiffB, Stopwatch programStopwatch)
        {
            if (options.Verbose)
            {                           
            var a = dictList.First(f => f.Name == "A");
            var b = dictList.First(f => f.Name == "B"); 
            Console.WriteLine("File {0} ({4}) has {1} keys. {2} of them exist in file {3} ({5}) also",Path.GetFileName(b.FilePath), b.LineDictionary.Count(), ldIntersectAandB.Count(),Path.GetFileName(a.FilePath), b.Name, a.Name);
            Console.WriteLine("File {0} ({4}) has {1} keys. {2} of them do not exist in file {3} ({5})",Path.GetFileName(b.FilePath), b.LineDictionary.Count(), ldDiffB.Count(),Path.GetFileName(a.FilePath),b.Name, a.Name);
            Console.WriteLine("Same ({0}) + Diff ({1}) = {2} == Number of rows in ({5}) {3} ({4})  ", ldIntersectAandB.Count(),ldDiffB.Count(), ldIntersectAandB.Count() + ldDiffB.Count(), Path.GetFileName(b.FilePath),b.LineDictionary.Count(),b.Name);
            Console.WriteLine("Time after creating set operator {0} ms", programStopwatch.ElapsedMilliseconds);
            Console.ForegroundColor = ConsoleColor.White;
            }
        }
        private static void VerboseConsoleEndMsg(Options options, Stopwatch programStopwatch)
        {
            if (options.Verbose)
            {
                Console.WriteLine("Done ! hit any key to exit program. ExecutionTime was {0} ms",
                    programStopwatch.Elapsed.Milliseconds);
                Console.ReadLine();
            }
        }

    }

    public class OutputObj
    {

        public string Name { get; set; }
        public Dictionary<string, string> Dict { get; set; }
        public Options Opt { get; set; }

        public OutputObj(string name, Dictionary<string, string> dict, Options opt)
        {
            Name = name;
            Dict = dict;
            Opt = opt;
        }

        public void Output()
        {
            string dir = Path.GetDirectoryName(Opt.FileB);
            string ext = Path.GetExtension(Opt.FileB);
            if (dir != null)
            {
                Directory.Exists(dir);
                string fileName = Path.Combine(dir, Name + DateTime.Now.ToString("yyyMMddTHHmmss") + ext);
                Dict.SaveValuesAsFile(fileName);
            }
        }
    }

    public class FileDictionaryDigger
    {
        private const int OFFSET_UNIQUE_START_FIELD = 3;
        private string Filecontent { get; set; }
        public string Name { get; set; }
        public string FilePath
        {
            get { return _filePath ?? String.Empty; }
            set { _filePath = value; }
        }

        public Dictionary<string, string> LineDictionary { get; set; }
        public Options Option;
        private string _filePath;

        public FileDictionaryDigger(string filePath,string name, Options option)
        {
            Option = option;
            FilePath = filePath;
            Name = name;
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
                    Console.WriteLine("Done Reading {0} called file {3}. It took {1} ms and contained {2} rows", FilePath,fileStopwatch.Elapsed.Milliseconds, LineDictionary.Count,Name);
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

    #region Stuff to abstract the StreamText Reader into DataReader

    internal interface ILineReader
    {
        string ReadLine();
    }

    public class DataReader : ILineReader, IDisposable
    {
        public StreamReader StreamReader { get; set; }

        public DataReader(string path)
        {
            Debug.Assert(String.IsNullOrEmpty(path) == false);
            Debug.WriteLine(path);
            Debug.Assert(File.Exists(path));
            if (!(File.Exists(path)))
            {
                throw new ArgumentException("File do not exist");
            }
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

    #endregion


    #region Tool classes like extented method and argument parsers

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
                    Assembly.GetExecutingAssembly().GetName().Version.ToString()));
            usage.AppendLine("give help as param for help. Simple usage -a[fileA] -b[fileB] ");
            usage.AppendLine("Developed by Patrik Lindström 2015-02-25");
            return usage.ToString();
        }
    }
    #endregion

}

