using XisfSharp.IO;

namespace XisfSharp.Tests;

[TestClass]
public class EndianConverterTests
{
    [TestMethod]
    public void ReverseEndiannessInPlace_ElementSize1_LeavesDataUnchanged()
    {
        byte[] data = [1, 2, 3, 4];

        EndianConverter.ReverseEndiannessInPlace(data, 1);

        data.ShouldBe(new byte[] { 1, 2, 3, 4 });
    }

    [TestMethod]
    [DataRow(2)]
    [DataRow(4)]
    [DataRow(8)]
    public void ReverseEndiannessInPlace_ReversesEachElement(int elementSize)
    {
        byte[] data = Enumerable.Range(0, elementSize * 3).Select(i => (byte)i).ToArray();
        byte[] expected = XisfBlockTestData.ReverseElements(data, elementSize);

        EndianConverter.ReverseEndiannessInPlace(data, elementSize);

        data.ShouldBe(expected);
    }

    [TestMethod]
    [DataRow(2)]
    [DataRow(4)]
    [DataRow(8)]
    public void ReverseEndiannessInPlace_TwiceRestoresOriginal(int elementSize)
    {
        byte[] original = Enumerable.Range(0, elementSize * 5).Select(i => (byte)(i * 7)).ToArray();
        byte[] data = (byte[])original.Clone();

        EndianConverter.ReverseEndiannessInPlace(data, elementSize);
        EndianConverter.ReverseEndiannessInPlace(data, elementSize);

        data.ShouldBe(original);
    }

    [TestMethod]
    public void ReverseEndiannessInPlace_LengthNotMultipleOfElementSize_Throws()
    {
        byte[] data = new byte[6];

        Should.Throw<ArgumentException>(() => EndianConverter.ReverseEndiannessInPlace(data, 4));
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(3)]
    [DataRow(16)]
    public void ReverseEndiannessInPlace_UnsupportedElementSize_Throws(int elementSize)
    {
        byte[] data = new byte[48];

        Should.Throw<ArgumentOutOfRangeException>(() => EndianConverter.ReverseEndiannessInPlace(data, elementSize));
    }
}
