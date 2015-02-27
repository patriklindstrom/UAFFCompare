using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CommandLine;

namespace UAFFCompare
{
    /// <summary>
    /// We want the following set operations: 
    /// not A and B => DiffFile  (see http://www.wolframalpha.com/input/?i=not+A+and+B ) (Venn diagram http://www.wolframalpha.com/share/clip?f=d41d8cd98f00b204e9800998ecf8427e41kvo33uui)
    /// A and B => IntersectionFile (see http://www.wolframalpha.com/input/?i=A+and+B ) ( Venn diagram  http://www.wolframalpha.com/share/clip?f=d41d8cd98f00b204e9800998ecf8427e7e2qko5194 )
    /// NotaBene combined => (not A and B) or (A and B) (see http://www.wolframalpha.com/input/?i=%28not+A+and+B%29+or+%28A+and+B%29 ) (Venn Diagram http://www.wolframalpha.com/share/clip?f=d41d8cd98f00b204e9800998ecf8427eguh00j5eik)
    /// DiffFile+IntersctionFile => FileB
    /// </summary>
    internal class Program
    {
        /// <summary>
        /// Takes two parameters file A and File B. Set A is contained in B also.
        /// A is file n and B is file n+1
        /// </summary>
        /// <param name="args">-a fileA -b fileB -v optional verbose</param>
        private static void Main(string[] args)
        {
            var options = new Options();
            if (Parser.Default.ParseArguments(args, options))
            {
                var programStopwatch = Stopwatch.StartNew();
                var chunkList = new List<DataChunk>
                {
                    new DataChunk(dataPath:options.FileA,name:"A", option:options),
                    new DataChunk(dataPath:options.FileB,name:"B", option:options)
                };
                //Multithread the reading of files and making dictionary of all lines in file
                Parallel.ForEach(chunkList, dl => dl.GetDataContent(new FileLineReader(dl.DataPath)));
                //Give the sets nicer names
                var a = chunkList.First(f => f.Name == "A");
                var b = chunkList.First(f => f.Name == "B"); 
                //We need Compare to use set logic on keys only not Key and Value which is the default - odd that is the default and we have to override it.
                var keyOnly = new DictCompareOnKeyOnly();
                //Here comes the magic simple Except and Intersect and force it back to Dictionary.
                Dictionary<string, string> diffB = b.LineDictionary.Except(a.LineDictionary, keyOnly).ToDictionary(ld=>ld.Key,ld=>ld.Value);
                Dictionary<string, string> intersectAandB = b.LineDictionary.Intersect(a.LineDictionary, keyOnly).ToDictionary(ld => ld.Key, ld => ld.Value);
                VerboseConsoleStatisticsOutput(options,chunkList, intersectAandB, diffB, programStopwatch); 
                //Here we save the output as text files
                var outPutList = new List<OutputObj>
                {
                    new OutputObj(name:"DiffB",dict:diffB,opt: options),
                    new OutputObj(name:"IntersectAandB",dict:intersectAandB, opt:options)
                };
                //Multithread the output of files
                Parallel.ForEach(outPutList, oL => oL.Output());
                VerboseConsoleEndMsg(options,programStopwatch); 
                programStopwatch.Stop();
            }
        }
        private static void VerboseConsoleStatisticsOutput(Options options, List<DataChunk> dictList, Dictionary<string, string> ldIntersectAandB, Dictionary<string, string> ldDiffB, Stopwatch programStopwatch)
        {
            if (options.Verbose)
            {                           
            var a = dictList.First(f => f.Name == "A");
            var b = dictList.First(f => f.Name == "B");
            Console.WriteLine("File {0} ({4}) has {1} keys. {2} of them exist in file {3} ({5}) also", Path.GetFileName(b.DataPath), b.LineDictionary.Count(), ldIntersectAandB.Count(), Path.GetFileName(a.DataPath), b.Name, a.Name);
            Console.WriteLine("File {0} ({4}) has {1} keys. {2} of them do not exist in file {3} ({5})", Path.GetFileName(b.DataPath), b.LineDictionary.Count(), ldDiffB.Count(), Path.GetFileName(a.DataPath), b.Name, a.Name);
            Console.WriteLine("Same ({0}) + Diff ({1}) = {2} == Number of rows in ({5}) {3} ({4})  ", ldIntersectAandB.Count(), ldDiffB.Count(), ldIntersectAandB.Count() + ldDiffB.Count(), Path.GetFileName(b.DataPath), b.LineDictionary.Count(), b.Name);
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

    public interface IDataChunk
    {
        string Name { get; set; }
        string DataPath { get; set; }
        IOptions Option { get; set; }
        Dictionary<string, string> LineDictionary { get; set; }
        void GetDataContent(ILineReader dr);
        void BuildRowKey(ref StringBuilder rowKey, string line, char splitChar, int[] keyColumns);
    }

    public class DataChunk : IDataChunk
    {
        public string Name { get; set; }
        public string DataPath  { get; set; }
        public Dictionary<string,string> LineDictionary { get; set; }
        public IOptions Option { get; set; }

        public DataChunk(string dataPath,string name, IOptions option)
        {
            Option = option;
            DataPath = dataPath;
            Name = name;
            LineDictionary = new Dictionary<string, string>();
        }

        public void GetDataContent(ILineReader dr)
        {
            int i = 0; //rowcounter to see where error occured
            try
            {
                var fileStopwatch = Stopwatch.StartNew();
                #region Verbose output

                if (Option.Verbose)
                {
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.WriteLine("Start reading {0}", DataPath);
                }

                #endregion
                using (dr)
                {
                    string line;
                    var rowKey = new StringBuilder();
                    var colKeys =Option.Keycolumns;
                    var sepChar = Option.Fieldseparator;
                    while ((line = dr.ReadLine()) != null)
                    {
                        i += 1;
                        
                        //Fields 4,6,7 makes the row unique according to rumours. Not that fieldArr is nollbased so it is: 3,5,6                   
                        BuildRowKey(ref rowKey, line, sepChar, colKeys);
                        LineDictionary.Add(rowKey.ToString(), line);
                        rowKey.Clear();
                    }
                }
                fileStopwatch.Stop();
                #region Verbose output

                if (Option.Verbose)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                    Console.WriteLine("Done Reading {0} called file {3}. It took {1} ms and contained {2} rows", DataPath, fileStopwatch.Elapsed.Milliseconds, LineDictionary.Count, Name);
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                }

                #endregion
            }
                #region Catch if error

            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("The file {0} could not be transformed to Dictionary structure error in line {1}:",
                    DataPath, i);
                Console.WriteLine(e.Message);
                Console.ForegroundColor = ConsoleColor.White;
            }

            #endregion
        }

        public void BuildRowKey(ref StringBuilder rowKey, string line, char splitChar, int[] keyColumns)
     {
         var fieldArr = line.Split(splitChar);
            for (int index = 0; index < keyColumns.Length-1; index++)
            {
                rowKey.Append(fieldArr[keyColumns[index] - 1]).Append("|");
            }
            rowKey.Append(fieldArr[keyColumns.Last() - 1]);
     }
    }

    #region Output to file or whateever Abstraktion

    public interface IOutputObj
    {
        string Name { get; set; }
        Dictionary<string, string> Dict { get; set; }
        Options Opt { get; set; }
        void Output();
    }

    public class OutputObj : IOutputObj
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
            if (!String.IsNullOrEmpty(dir))
            {
                Directory.Exists(dir);
                string fileName = Path.Combine(dir, Name + "_" + DateTime.Now.ToString("yyyMMddTHHmmss") + ext);
                Dict.SaveValuesAsFile(fileName);
            }
        }
    }

    #endregion

    #region Stuff to abstract the StreamText Reader into DataReader

    public interface ILineReader : IDisposable
    {
        string ReadLine();
    }

    public class FileLineReader : ILineReader, IDisposable
    {
        public StreamReader StreamReader { get; set; }

        public FileLineReader(string path)
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

    public interface IOptions
    {
        string FileA { get; set; }
        string FileB { get; set; }
        char Fieldseparator { get; set; }
        bool DiffB { get; set; }
        bool IntersectAandB { get; set; }
        bool Verbose { get; set; }
        int[] Keycolumns { get; set; }
        string GetUsage();
    }

    public class Options : IOptions
    {
        // Good test parameters for deafult meaning: verbose fileA and fileB key is in combination of column [4,6,7] - where first column is called 1. Separator char is semikolon 
        //-v -a"s:\Darkcompare\UAFF#.140206.TXT"  -b"s:\Darkcompare\UAFF#.140603.TXT" -k4 6 7 -s;
        [Option('a', "fileA", Required = true, HelpText = "Input A csv file to read.")]
        public string FileA { get; set; }

        [Option('b', "fileb", Required = true, HelpText = "Input B csv file to read.")]
        public string FileB { get; set; }


        [Option('d', "DiffB", Required = false, HelpText = "Calculate and output intersectAandB csv file.")]
        public bool DiffB { get; set; }

        [Option('i', "IntersectAandB", Required = false, HelpText = "Calculate and output intersectAandB csv file.")]
        public bool IntersectAandB { get; set; }
        [OptionArray('k', "keycolumns", Required = false, DefaultValue = new int[] {4, 6, 7}, HelpText = "What columns combined are the key of every row.")]
        public int[] Keycolumns { get; set; }
        [Option('v', null, Required = false,HelpText = "Print details during execution.")]
        public bool Verbose { get; set; }
        [Option('s', "fieldseparator", Required = false,DefaultValue = ';', HelpText = "Char that separates every column")]
        public char Fieldseparator { get; set; }
        [Option('e', "version",Required = false, HelpText = "Prints version number of program.")]
        public string Version
        {
            get { return Assembly.GetExecutingAssembly().GetName().Version.ToString();
            }
          
        }

        [HelpOption]
        public string GetUsage()
        {
            // this without using CommandLine.Text
            //  or using HelpText.AutoBuild
            var usage = new StringBuilder();
            usage.AppendLine(
                String.Format(
                    "UAFFCompare Application takes Difference between two cvs files on columns 4,6,7 version {0}",
                    Assembly.GetExecutingAssembly().GetName().Version));
            usage.AppendLine("give help as param for help. Simple usage -a[fileA] -b[fileB] ");
            usage.AppendLine("Developed by Patrik Lindström 2015-02-25");
            return usage.ToString();
        }
    }
    #endregion

}

