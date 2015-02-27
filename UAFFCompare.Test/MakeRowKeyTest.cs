using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SqlServer.Server;
using NUnit.Framework;
using Rhino.Mocks;

namespace UAFFCompare.Test
{
    [TestFixture]
    public class MakeDataChunkTest
    {
        [Test]
        public void ShouldMakeAKeyString()
        {
            //Arragne
            var mockOptions = MockRepository.GenerateStub<Options>();
            var sut = new DataChunk(dataPath: "aDataPath", name: "aTestName", option: mockOptions);
            var sb = new StringBuilder();
            //Act
            sut.BuildRowKey(ref sb, Mv.Line, Mv.Splitchar, Mv.ColKeys);

            //Assert
            Assert.AreEqual(Mv.ExpectedKey, sb.ToString(), "TheRowKey should be generated from given columns");
        }

        [Test]
        public void ShouldGenerateDataDictionary()
        {
            //Arrange
          //  var mockOptions = MockRepository.GenerateStub<Options>();
            var mockOptions = new Options
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
            var mockLineReader = MockRepository.GenerateStub<ILineReader>();
            mockLineReader.Stub(f => f.ReadLine()).Return(Mv.Line);
            //var expectedDict = new Dictionary<string, string> {{Mv.ExpectedKey, Mv.Line},{"foo","fum"}};
            var expectedDict = new Dictionary<string, string> {{Mv.ExpectedKey, Mv.Line}};
            var sut = new DataChunk(dataPath: Mv.DataPath, name: Mv.Name, option: mockOptions);
            //Act
            sut.GetDataContent(mockLineReader);
            var sb = new StringBuilder(Mv.ExpectedKey);
            // sut.AssertWasCalled(m => m.BuildRowKey(ref sb, line: Arg<string>.Is.Equal(Mv.Line), splitChar: Arg<char>.Is.Equal(Mv.Splitchar), keyColumns: Arg<int[]>.Is.Anything));            
            //Assert
            Assert.AreEqual(expectedDict.Except(sut.LineDictionary).Count(),
                sut.LineDictionary.Except(expectedDict).Count());
        }

        [Test]
        public void CheckCreationOfDataChunk()
        {
            //Arrange
            // did not work var mockOptions = MockRepository.GenerateStub<IOptions>();
            var opt = new Options
            {
                FileA = Mv.FileA
                ,
                FileB = Mv.FileB
                ,
                DiffB = Mv.DiffB
    ,                IntersectAandB = Mv.IntersectAandB
                ,
                Verbose = Mv.Verbose
                ,Fieldseparator = Mv.Splitchar
                ,Keycolumns = Mv.ColKeys
            };
            var mockLineReader = MockRepository.GenerateStub<ILineReader>();
            mockLineReader.Stub(f => f.ReadLine()).Return(Mv.Line);
            var expectedDict = new Dictionary<string, string> {{Mv.ExpectedKey, Mv.Line}};
            var sut = new DataChunk(dataPath: Mv.DataPath, name: Mv.Name, option: opt);
            //Act
            sut.GetDataContent(mockLineReader);
            var sb = new StringBuilder(Mv.ExpectedKey);
            //Assert
            Assert.AreEqual(Mv.DataPath, sut.DataPath, "DataPaht should be same");
            Assert.IsTrue(sut.LineDictionary.ContainsValue(Mv.Line), "Should contain expected Line");
            Assert.AreEqual(Mv.Name, sut.Name, "Name should be same");
            Assert.AreEqual(opt, sut.Option, "Option should be same");
            Assert.IsTrue(sut.LineDictionary.ContainsKey(Mv.ExpectedKey), "Dictionary should contain test key");
        }
    }
}
