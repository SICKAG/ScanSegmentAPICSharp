using Compact;
using Xunit;

namespace CompactTests;

public class CompactDeserializerTests()
{
  [Fact]
  public void Convert_ValidValue()
  {
    var bytes = TestData.CompactTestData();
    var expectedTimeStampStart = new List<int> { 629095889, 629097417, 629097834, 629093803, 629097834, 629097279, 629095889 };
    var expectedTimeStampStop = new List<int> { 629099917, 629101444, 629101861, 629097834, 629101861, 629101305, 629099917 };
    CompactSegment CompactSegment = CompactDeserializer.Convert((ReadOnlySpan<byte>)bytes.ToArray());
    Console.WriteLine(CompactSegment.Header.StartOfFrame);

    Assert.Equal((UInt32)Convert.ToInt32("0x02020202", 16), CompactSegment.Header.StartOfFrame);
    Assert.Equal((UInt32)1, CompactSegment.Header.CommandId);
    Assert.Equal((UInt64)3703, CompactSegment.Header.TelegramCounter);
    Assert.Equal((UInt64)629160554, CompactSegment.Header.TimestampTransmit);
    Assert.Equal((UInt32)4, CompactSegment.Header.Version);
    Assert.Equal((UInt32)3390, CompactSegment.Header.SizeOfFirstModule);
    Assert.Equal((UInt64)1, CompactSegment.Modules[0].MetaData.SegmentCounter);
    Assert.Equal((UInt64)12441, CompactSegment.Modules[0].MetaData.FrameNumber);
    Assert.Equal((UInt32)23350001, CompactSegment.Modules[0].MetaData.SenderId);
    Assert.Equal((UInt32)7, CompactSegment.Modules[0].MetaData.NumberOfLayersInModule);
    Assert.Equal((UInt32)30, CompactSegment.Modules[0].MetaData.NumberOfBeamsPerScan);
    Assert.Equal((UInt32)3, CompactSegment.Modules[0].MetaData.NumberOfEchoes);
    Assert.Equal((UInt32)1, CompactSegment.Modules[0].MetaData.DistanceScalingFactor);
    Assert.Equal((UInt32)3390, CompactSegment.Modules[0].MetaData.NextModuleSize);

    for (int i = 0; i < CompactSegment.Modules[0].MetaData.NumberOfLayersInModule; i++)
    {
      Assert.Equal((UInt64)expectedTimeStampStart[i], CompactSegment.Modules[0].MetaData.TimestampsStart[i]);
      Assert.Equal((UInt64)expectedTimeStampStop[i], CompactSegment.Modules[0].MetaData.TimestampsStop[i]);
    }

    Assert.Equal(CompactSegment.Modules[0].MetaData.NumberOfBeamsPerScan, (UInt32)CompactSegment.Modules[0].Beams.Count);
    Assert.Equal(CompactSegment.Modules[0].MetaData.NumberOfEchoes, (UInt32)CompactSegment.Modules[0].Beams[0][0].Echoes.Count);
  }
}
