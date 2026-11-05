using XisfSharp.IO;

namespace XisfSharp.Tests;

[TestClass]
public class SubblockTableTests
{
    [TestMethod]
    public void Parse_ComputesOffsetsAndTotals()
    {
        var table = SubblockTable.Parse("10,100:20,200:30,300");

        table.Count.ShouldBe(3);
        table.CompressedSize.ShouldBe(60);
        table.UncompressedSize.ShouldBe(600);
        table.MaxUncompressedSubblockSize.ShouldBe(300);

        table[0].ShouldBe(new Subblock(0, 10, 0, 100));
        table[1].ShouldBe(new Subblock(10, 20, 100, 200));
        table[2].ShouldBe(new Subblock(30, 30, 300, 300));
    }

    [TestMethod]
    public void Parse_SizesLargerThanInt32_AreSupported()
    {
        var table = SubblockTable.Parse("4294967294,4294967294:1298513199,4037566850");

        table.CompressedSize.ShouldBe(5593480493L);
        table.UncompressedSize.ShouldBe(8332534144L);
        table[1].UncompressedOffset.ShouldBe(4294967294L);
    }

    [TestMethod]
    public void Parse_ToleratesWhitespaceAroundEntries()
    {
        var table = SubblockTable.Parse(" 10 , 100 : 20 , 200 ");

        table.Count.ShouldBe(2);
        table.UncompressedSize.ShouldBe(300);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("10")]
    [DataRow("10,x")]
    [DataRow("10,20,30")]
    [DataRow("0,5")]
    [DataRow("5,0")]
    [DataRow("-1,5")]
    [DataRow("+1,5")]
    [DataRow("10,20:")]
    [DataRow("10,20::30,40")]
    public void Parse_InvalidValue_Throws(string value)
    {
        Should.Throw<XisfException>(() => SubblockTable.Parse(value));
    }

    [TestMethod]
    public void Subblock_EqualSizes_IsStored()
    {
        var table = SubblockTable.Parse("100,100:50,200");

        table[0].IsStored.ShouldBeTrue();
        table[1].IsStored.ShouldBeFalse();
    }

    [TestMethod]
    public void Single_SpansWholeBlock()
    {
        var table = SubblockTable.Single(123, 456);

        table.Count.ShouldBe(1);
        table[0].ShouldBe(new Subblock(0, 123, 0, 456));
    }

    [TestMethod]
    [DataRow(0L, 0)]
    [DataRow(99L, 0)]
    [DataRow(100L, 1)]
    [DataRow(299L, 1)]
    [DataRow(300L, 2)]
    [DataRow(599L, 2)]
    public void IndexOfUncompressedOffset_ReturnsContainingSubblock(long offset, int expectedIndex)
    {
        var table = SubblockTable.Parse("10,100:20,200:30,300");

        table.IndexOfUncompressedOffset(offset).ShouldBe(expectedIndex);
    }

    [TestMethod]
    [DataRow(-1L)]
    [DataRow(600L)]
    public void IndexOfUncompressedOffset_OutOfRange_Throws(long offset)
    {
        var table = SubblockTable.Parse("10,100:20,200:30,300");

        Should.Throw<ArgumentOutOfRangeException>(() => table.IndexOfUncompressedOffset(offset));
    }

    [TestMethod]
    public void Validate_MatchingSizes_DoesNotThrow()
    {
        var table = SubblockTable.Parse("10,100:20,200");

        Should.NotThrow(() => table.Validate(30, 300));
    }

    [TestMethod]
    [DataRow(31L, 300L)]
    [DataRow(30L, 301L)]
    public void Validate_MismatchedSizes_Throws(long compressedSize, long uncompressedSize)
    {
        var table = SubblockTable.Parse("10,100:20,200");

        Should.Throw<XisfException>(() => table.Validate(compressedSize, uncompressedSize));
    }
}
