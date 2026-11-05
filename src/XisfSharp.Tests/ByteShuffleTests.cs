using XisfSharp.IO;

namespace XisfSharp.Tests;

[TestClass]
public class ByteShuffleTests
{
    private static byte[] RandomBytes(int count, int seed)
    {
        byte[] bytes = new byte[count];
        new Random(seed).NextBytes(bytes);
        return bytes;
    }

    // Counts around the 16-byte vector width exercise both the vectorized loop and the scalar remainder.
    public static IEnumerable<object[]> Cases() =>
        from itemSize in new[] { 1, 2, 3, 4, 8 }
        from count in new[] { 0, 1, 15, 16, 17, 63, 64, 1000 }
        select new object[] { itemSize, count };

    [TestMethod]
    [DynamicData(nameof(Cases))]
    public void Interleave_MatchesDefinition(int itemSize, int count)
    {
        byte[][] planes = Enumerable.Range(0, itemSize).Select(j => RandomBytes(count, j)).ToArray();
        byte[] output = new byte[count * itemSize];

        ByteShuffle.Interleave(planes, count, output);

        for (int i = 0; i < count; i++)
            for (int j = 0; j < itemSize; j++)
                output[i * itemSize + j].ShouldBe(planes[j][i]);
    }

    [TestMethod]
    [DynamicData(nameof(Cases))]
    public void ExtractPlane_MatchesDefinition(int itemSize, int count)
    {
        byte[] items = RandomBytes(count * itemSize, 99);
        byte[] plane = new byte[count];

        for (int j = 0; j < itemSize; j++)
        {
            ByteShuffle.ExtractPlane(items, itemSize, j, plane);

            for (int i = 0; i < count; i++)
                plane[i].ShouldBe(items[i * itemSize + j]);
        }
    }

    [TestMethod]
    [DynamicData(nameof(Cases))]
    public void ExtractPlane_ThenInterleave_RestoresItems(int itemSize, int count)
    {
        byte[] items = RandomBytes(count * itemSize, 7);
        byte[][] planes = Enumerable.Range(0, itemSize).Select(_ => new byte[count]).ToArray();

        for (int j = 0; j < itemSize; j++)
            ByteShuffle.ExtractPlane(items, itemSize, j, planes[j]);

        byte[] output = new byte[items.Length];
        ByteShuffle.Interleave(planes, count, output);

        output.ShouldBe(items);
    }

    [TestMethod]
    [DataRow(2)]
    [DataRow(4)]
    [DataRow(8)]
    public void Shuffle_MatchesIndependentImplementation(int itemSize)
    {
        // A length that is not a whole number of items checks that trailing bytes are kept unshuffled.
        byte[] data = RandomBytes(1000 * itemSize + itemSize - 1, 3);

        XisfUtil.Shuffle(data, itemSize).ShouldBe(XisfBlockTestData.Shuffle(data, itemSize));
    }
}
