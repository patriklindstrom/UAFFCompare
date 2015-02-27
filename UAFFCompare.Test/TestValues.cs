using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UAFFCompare.Test
{
    /// <summary>
    /// Handy MockValues to use wherever in tests.
    /// </summary>
   public static class Mv
    {
        public static string Line = "fum;fan;foo;bar;king;barter;28;496;8128;;;endisnear;really?";
        public static string ExpectedKey = "foo|bar|barter";
        public static int[] ColKeys = { 3, 4, 6 };
        public static char Splitchar = ';';
        public static string DataPath = "C:\\temp\\testpath";
        public static string Name = "TestName_Tore";

        public static string FileA = "D:\\DataImport\\TestFileA.csv}";
        public static string FileB = "D:\\DataImport\\TestFileB.csv}";
        public static bool IntersectAandB =true;
        public static bool Verbose;
        public static bool DiffB = true;
    }
}
