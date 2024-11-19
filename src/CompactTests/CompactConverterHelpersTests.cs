using Compact;
using Xunit;

namespace CompactTests;

public class CompactConverterHelperTests
{
  [Fact]
  public void DeserializePrimitiveAndIncrementIndex_DeserializesIntValueAndIncrementsIndex()
  {
    var buffer = new ReadOnlyMemory<byte>([0x01, 0x02, 0x03, 0x04]);

    var currentIndex = 0;
    var result = CompactConverterHelpers.DeserializePrimitiveAndIncrementIndex<int>(buffer.Span, ref currentIndex);

    Assert.Equal(67_305_985, result);
    Assert.Equal(4, currentIndex);
  }

  [Fact]
  public void DeserializePrimitiveAndIncrementIndex_DeserializesLongValueAndIncrementsIndex()
  {
    var buffer = new ReadOnlyMemory<byte>([0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08]);

    var currentIndex = 0;
    var result = CompactConverterHelpers.DeserializePrimitiveAndIncrementIndex<long>(buffer.Span, ref currentIndex);

    Assert.Equal(578437695752307201, result);
    Assert.Equal(8, currentIndex);
  }

  [Fact]
  public void DeserializePrimitiveAndIncrementIndex_DeserializesFloatValueAndIncrementsIndex()
  {
    var buffer = new ReadOnlyMemory<byte>([0x01, 0x02, 0x03, 0x04]);

    var currentIndex = 0;
    var result = CompactConverterHelpers.DeserializePrimitiveAndIncrementIndex<float>(buffer.Span, ref currentIndex);

    Assert.Equal(1.5399896E-36f, result);
    Assert.Equal(4, currentIndex);
  }

  [Fact]
  public void DeserializePrimitiveAndIncrementIndex_DeserializesDoubleValueAndIncrementsIndex()
  {
    var buffer = new ReadOnlyMemory<byte>([0x32, 0x10, 0x54, 0x41, 0x09, 0x4c, 0x8b, 0x40]);

    var currentIndex = 0;
    var result = CompactConverterHelpers.DeserializePrimitiveAndIncrementIndex<double>(buffer.Span, ref currentIndex);

    Assert.Equal(873.50451913523125, result);
    Assert.Equal(8, currentIndex);
  }

  [Fact]
  public void DeserializePrimitiveAndIncrementIndex_FailsWhenTypeIsTooLargeAndDoesNotChangeIndex()
  {
    var buffer = new ReadOnlyMemory<byte>([0x01, 0x02, 0x03]);

    var currentIndex = 0;

    Assert.Throws<ArgumentException>(() => CompactConverterHelpers.DeserializePrimitiveAndIncrementIndex<int>(buffer.Span, ref currentIndex));
    Assert.Equal(0, currentIndex);
  }

  [Fact]
  public void DeserializeVectorAndIncrementIndex_DeserializesIntVectorAndIncrementsIndex()
  {
    var buffer = new ReadOnlyMemory<byte>([0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08]);
    var numberOfElements = 2u;

    var currentIndex = 0;
    var result = CompactConverterHelpers.DeserializeVectorAndIncrementIndex<int>(buffer.Span, ref currentIndex, numberOfElements);

    Assert.Equal([67305985, 134678021], result);
    Assert.Equal(8, currentIndex);
  }

  [Fact]
  public void DeserializeVectorAndIncrementIndex_DeserializesLongVectorAndIncrementsIndex()
  {
    var buffer = new ReadOnlyMemory<byte>([0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f, 0x10]);
    var numberOfElements = 2u;

    var currentIndex = 0;
    var result = CompactConverterHelpers.DeserializeVectorAndIncrementIndex<long>(buffer.Span, ref currentIndex, numberOfElements);

    Assert.Equal([578437695752307201, 1157159078456920585], result);
    Assert.Equal(16, currentIndex);
  }

  [Fact]
  public void DeserializeVectorAndIncrementIndex_DeserializesFloatVectorAndIncrementsIndex()
  {
    var buffer = new ReadOnlyMemory<byte>([0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08]);
    var numberOfElements = 2u;

    var currentIndex = 0;
    var result = CompactConverterHelpers.DeserializeVectorAndIncrementIndex<float>(buffer.Span, ref currentIndex, numberOfElements);

    Assert.Equal([1.5399896E-36f, 4.06321607E-34f], result);
    Assert.Equal(8, currentIndex);
  }

  [Fact]
  public void DeserializeVectorAndIncrementIndex_DeserializesDoubleVectorAndIncrementsIndex()
  {
    var buffer = new ReadOnlyMemory<byte>([0x32, 0x10, 0x54, 0x41, 0x09, 0x4c, 0x8b, 0x40, 0x32, 0x10, 0x54, 0x41, 0x09, 0x4c, 0x8b, 0x40]);
    var numberOfElements = 2u;

    var currentIndex = 0;
    var result = CompactConverterHelpers.DeserializeVectorAndIncrementIndex<double>(buffer.Span, ref currentIndex, numberOfElements);

    Assert.Equal([873.50451913523125, 873.50451913523125], result);
    Assert.Equal(16, currentIndex);
  }

  [Fact]
  public void DeserializeVectorAndIncrementIndex_FailsWhenTypeIsTooLargeAndDoesNotChangeIndex()
  {
    var buffer = new ReadOnlyMemory<byte>([0x01, 0x02, 0x03]);
    var numberOfElements = 2u;

    var currentIndex = 0;

    Assert.Throws<ArgumentException>(() => CompactConverterHelpers.DeserializeVectorAndIncrementIndex<int>(buffer.Span, ref currentIndex, numberOfElements));
    Assert.Equal(0, currentIndex);
  }

  [Fact]
  public void EnsureChecksumIsValid_SingleValidChecksum()
  {
    var buffer = new ReadOnlyMemory<byte>([0x64, 0xcc, 0x4a, 0xdd, 0x98]);

    CompactConverterHelpers.EnsureChecksumIsValid(buffer.Span);
  }

  [Fact]
  public void EnsureChecksumIsValid_MultipleValidChecksums()
  {
    var buffer = new ReadOnlyMemory<byte>([0x64, 0xcc, 0x4a, 0xdd, 0x98, 0x64, 0x5c, 0x52, 0xfd, 0x8c]);

    CompactConverterHelpers.EnsureChecksumIsValid(buffer.Span);
  }

  [Fact]
  public void EnsureChecksumIsValid_InvalidChecksum()
  {
    var buffer = new ReadOnlyMemory<byte>([0x64, 0xcc, 0x4a, 0xdd, 0x99]);

    Assert.Throws<ArgumentException>(() => CompactConverterHelpers.EnsureChecksumIsValid(buffer.Span));
  }

  [Fact]
  public void EnsureChecksumIsValid_BufferIsTooSmall()
  {
    var buffer = new ReadOnlyMemory<byte>([0x64, 0xcc, 0x4a, 0xdd]);

    Assert.Throws<ArgumentException>(() => CompactConverterHelpers.EnsureChecksumIsValid(buffer.Span));
  }
}
