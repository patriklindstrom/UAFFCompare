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
        {   Debug.WriteLine(args[0]);
            Debug.WriteLine(args[1]);
            string fileA = args[0];
            string fileB = args[1];
            Debug.Assert(File.Exists(fileA));
            Debug.Assert(File.Exists(fileB));
            try
            {
                var fileStopwatch = Stopwatch.StartNew();
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine("Start reading {0}",fileA);
                using (StreamReader sr = new StreamReader(fileA))
                {
                    String line = sr.ReadToEnd();
                  //  Console.WriteLine(line);
                }
                fileStopwatch.Stop();
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.WriteLine("Done Reading {0} took {1} ms ", fileA, fileStopwatch.Elapsed.Milliseconds);
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine("Reading {0}", fileB);
                fileStopwatch.Restart();
                using (StreamReader sr = new StreamReader(fileB))
                {
                    String line = sr.ReadToEnd();
                //    Console.WriteLine(line);
                }
                fileStopwatch.Stop();
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.WriteLine("DoneReading {0} took {1} ms ", fileB, fileStopwatch.Elapsed.Milliseconds);
                Console.ForegroundColor=ConsoleColor.White;
                Console.WriteLine("Hit return to end program");
                Console.ReadLine();

            }
            catch (Exception e)
            {
                Console.WriteLine("The file could not be read:");
                Console.WriteLine(e.Message);
            }
        }
    }

    public class FileDictionaryDigger
    {
        private string Filecontent { get; set; }
        public FileDictionaryDigger(string filePath)
        {
            try
            {           
            var fileStopwatch = Stopwatch.StartNew();
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine("Start reading {0}", filePath);
            using (StreamReader sr = new StreamReader(filePath))
            {
                 Filecontent = sr.ReadToEnd();
                //  Console.WriteLine(line);
            }
            fileStopwatch.Stop();
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("Done Reading {0} took {1} ms ", filePath, fileStopwatch.Elapsed.Milliseconds);
            Console.ForegroundColor = ConsoleColor.DarkRed;
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("The file {0} could not be read:", filePath);
                Console.WriteLine(e.Message);
                Console.ForegroundColor = ConsoleColor.White;
            }
        }  

    } 
}
