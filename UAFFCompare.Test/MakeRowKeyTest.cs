using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Rhino.Mocks;

namespace UAFFCompare.Test
{
    [TestFixture]
    public class MakeRowKeyTest
    {
        [Test]
        public void ShouldMakeAKeyString()
        { //Arragne
            var sut = MockRepository.GenerateMock<IDataChunk>();
            var expectedKey = "foo;bar;king";
            string line = "fum;fan;foo;bar;king;barter;28;496;8128;;;endisnear;really?";
            int[] colKeys = new[] {3, 4, 6};
            var sb = new StringBuilder();
            char splitchar = ';';
            //Act
            sut.BuildRowKey(ref sb, line, splitchar, colKeys);

            //Assert
            Assert.AreEqual(expectedKey,sb.ToString(), "TheRowKey should be generated from given columns");
        }
    }
}
