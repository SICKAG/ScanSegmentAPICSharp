//
// Copyright (c) 2024 SICK AG
// SPDX-License-Identifier: MIT
//
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Compact;

/// <summary>
/// Provides helper methods for deserializing data from a byte buffer in Compact format.
/// </summary>
public static class CompactConverterHelpers
{
  /// <summary>
  /// Deserializes a primitive type from the buffer and increments the current index.
  /// </summary>
  /// <typeparam name="T">The type of the primitive to deserialize.</typeparam>
  /// <param name="buffer">The buffer containing the data.</param>
  /// <param name="currentIndex">The current index in the buffer, which will be incremented.</param>
  /// <returns>The deserialized primitive value.</returns>
  /// <exception cref="ArgumentException">Thrown when the index is out of bounds.</exception>
  public static T DeserializePrimitiveAndIncrementIndex<T>(ReadOnlySpan<byte> buffer, ref int currentIndex) where T : struct
  {
    var typeSize = Unsafe.SizeOf<T>();
    if (currentIndex + typeSize > buffer.Length)
    {
      throw new ArgumentException("index out of bounds");
    }

    var ret = MemoryMarshal.Read<T>(buffer.Slice(currentIndex, typeSize));
    currentIndex += typeSize;
    return ret;
  }

  /// <summary>
  /// Deserializes a vector of primitive types from the buffer and increments the current index.
  /// </summary>
  /// <typeparam name="T">The type of the primitives to deserialize.</typeparam>
  /// <param name="buffer">The buffer containing the data.</param>
  /// <param name="currentIndex">The current index in the buffer, which will be incremented.</param>
  /// <param name="numberOfElements">The number of elements to deserialize.</param>
  /// <returns>A list of deserialized primitive values.</returns>
  /// <exception cref="ArgumentException">Thrown when the index is out of bounds.</exception>
  public static List<T> DeserializeVectorAndIncrementIndex<T>(ReadOnlySpan<byte> buffer, ref int currentIndex, UInt32 numberOfElements) where T : struct
  {
    var typeSize = Unsafe.SizeOf<T>();
    if (currentIndex + typeSize * numberOfElements > buffer.Length)
    {
      throw new ArgumentException("index out of bounds");
    }

    var ret = new List<T>((int)numberOfElements);
    for (UInt32 i = 0; i < numberOfElements; ++i)
    {
      ret.Add(DeserializePrimitiveAndIncrementIndex<T>(buffer, ref currentIndex));
    }
    return ret;
  }

  /// <summary>
  /// Ensures that the checksum of the buffer is valid.
  /// </summary>
  /// <param name="buffer">The buffer containing the data and checksum.</param>
  /// <exception cref="ArgumentException">Thrown when the buffer is too short or the checksum is invalid.</exception>
  public static void EnsureChecksumIsValid(ReadOnlySpan<byte> buffer)
  {
    if (buffer.Length < sizeof(UInt32) + 1)
    {
      throw new ArgumentException("Compact telegram is too short to contain a checksum and data");
    }
    var checksumStartIndex = buffer.Length - sizeof(UInt32);

    var hash = System.IO.Hashing.Crc32.Hash(buffer[..checksumStartIndex]);
    var expectedChecksum = BitConverter.ToUInt32(hash);
    var givenChecksum = DeserializePrimitiveAndIncrementIndex<UInt32>(buffer, ref checksumStartIndex);
    if (givenChecksum != expectedChecksum)
    {
      throw new ArgumentException($"Compact checksum is invalid. Expected 0x{expectedChecksum:X}, got 0x{givenChecksum:X}");
    }
  }
}
