
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Rhino.Mocks;
//  UAFFCompare.Test TestSetOperations.cs 2015 03 01
namespace UAFFCompare.Test
{
    [TestFixture]
    public class TestSetOperations
    {
        [Test]
        static  public void ShouldGenerateSetDifferenceBetweenAandB()
        {
            //Arrange
            var opt = new Options
            {
                FileA = Mv.FileA
                ,
                FileB = Mv.FileB
                ,
                DiffB = Mv.DiffB
                ,
                IntersectAandB = Mv.IntersectAandB
                ,
                Verbose = Mv.Verbose
                ,
                Fieldseparator = Mv.Splitchar
                ,
                Keycolumns = Mv.ColKeys
            }; 
            var sut = new SetOperator();
            var keyOnly = new DictCompareOnKeyOnly();
            Dictionary<string, string> mockSetA = new Dictionary<string, string>
            {
                {"fum;fan;foo", Mv.Line},
                {"fum;fan;fee", Mv.Line2},
                {"456;fan;faa", Mv.Line3}
            };
            Dictionary<string, string> mockSetB = new Dictionary<string, string>
            {
                {"fum;fan;foo", Mv.Line},
                {"fum;fan;fee", Mv.Line2},
                {"456;fan;faa", Mv.Line3},
                {"NewFi;foo;fum", Mv.Line4}
            };
            var expectedDict = new Dictionary<string, string> { { "NewFi;foo;fum", Mv.Line4 } };
            var mockChunckA = new DataChunk(dataPath: Mv.DataPath, name: Mv.Name, option: opt);
            mockChunckA.LineDictionary = mockSetA;
            var mockChunkB = new DataChunk(dataPath: Mv.DataPath, name: Mv.Name, option: opt);
            mockChunkB.LineDictionary = mockSetB;
            //Act
            var sutLineDict = sut.DiffB(a:mockChunckA, b:mockChunkB, keyOnly:keyOnly,options: opt);
            //Assert
           Assert.AreEqual(expectedDict.Except(sutLineDict).Count(),
             sutLineDict.Except(expectedDict).Count());
        }

        [Test]
        static public void ShouldGenerateSetDifferenceBetweenAand2()
        {
            //Arrange
            var opt = new Options
            {
                FileA = Mv.FileA
                ,
                FileB = Mv.FileB
                ,
                DiffB = Mv.DiffB
                ,
                IntersectAandB = Mv.IntersectAandB
                ,
                Verbose = Mv.Verbose
                ,
                Fieldseparator = Mv.Splitchar
                ,
                Keycolumns = Mv.ColKeys
            };
            var sut = new SetOperator();
            var keyOnly = new DictCompareOnKeyOnly();
            Dictionary<string, string> mockSetA = new Dictionary<string, string>
            {
                {"fum;fan;foo", Mv.Line},
                //{"fum;fan;fee", Mv.Line2},
                {"456;fan;faa", Mv.Line3}
            };
            Dictionary<string, string> mockSetB = new Dictionary<string, string>
            {
                {"fum;fan;foo", Mv.Line},
                {"fum;fan;fee", Mv.Line2},
                {"456;fan;faa", Mv.Line3},
                {"NewFi;foo;fum", Mv.Line4}
            };
            var expectedDict = new Dictionary<string, string> { { "NewFi;foo;fum", Mv.Line4 } ,{"fum;fan;fee", Mv.Line2} };
            var mockChunckA = new DataChunk(dataPath: Mv.DataPath, name: Mv.Name, option: opt);
            mockChunckA.LineDictionary = mockSetA;
            var mockChunkB = new DataChunk(dataPath: Mv.DataPath, name: Mv.Name, option: opt);
            mockChunkB.LineDictionary = mockSetB;
            //Act
            var sutLineDict = sut.DiffB(a: mockChunckA, b: mockChunkB, keyOnly: keyOnly, options: opt);
            //Assert
            Assert.AreEqual(expectedDict.Except(sutLineDict).Count(),
              sutLineDict.Except(expectedDict).Count());
        }
        [Test]
        static public void ShouldGenerateSetDifferenceBetweenAandB3()
        {
            //Arrange
            var opt = new Options
            {
                FileA = Mv.FileA
                ,
                FileB = Mv.FileB
                ,
                DiffB = Mv.DiffB
                ,
                IntersectAandB = Mv.IntersectAandB
                ,
                Verbose = Mv.Verbose
                ,
                Fieldseparator = Mv.Splitchar
                ,
                Keycolumns = Mv.ColKeys
            };
            var sut = new SetOperator();
            var keyOnly = new DictCompareOnKeyOnly();
            Dictionary<string, string> mockSetA = new Dictionary<string, string>
            {
                {"fum;fan;foo", Mv.Line},
                //{"fum;fan;fee", Mv.Line2},
                {"456;fan;faa", Mv.Line3}
            };
            Dictionary<string, string> mockSetB = new Dictionary<string, string>
            {
               // {"fum;fan;foo", Mv.Line},
                {"fum;fan;fee", Mv.Line2},
                {"456;fan;faa", Mv.Line3},
                {"NewFi;foo;fum", Mv.Line4}
            };
            var expectedDict = new Dictionary<string, string> { { "NewFi;foo;fum", Mv.Line4 }, { "fum;fan;fee", Mv.Line2 } };
            var mockChunckA = new DataChunk(dataPath: Mv.DataPath, name: Mv.Name, option: opt);
            mockChunckA.LineDictionary = mockSetA;
            var mockChunkB = new DataChunk(dataPath: Mv.DataPath, name: Mv.Name, option: opt);
            mockChunkB.LineDictionary = mockSetB;
            //Act
            var sutLineDict = sut.DiffB(a: mockChunckA, b: mockChunkB, keyOnly: keyOnly, options: opt);
            //Assert
            Assert.AreEqual(expectedDict.Except(sutLineDict).Count(),
              sutLineDict.Except(expectedDict).Count());
        }
    }
}