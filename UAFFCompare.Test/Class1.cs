using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace UAFFCompare.Test
{
    [TestFixture]
    public class MakeRowKeyTest
    {
        [Test]
        public void ShouldMakeAKeyString()
        {
            var opt = new Options();
            var sut = new DataChunk("foo", "Test", opt);

        }
    }
}
