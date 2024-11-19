//
// Copyright (c) 2024 SICK AG
// SPDX-License-Identifier: MIT
//

namespace Compact;

using Helpers = CompactConverterHelpers;
/// <summary>
/// Deserialize a Compact byte array into a ordered type.
/// </summary>
public static class CompactDeserializer
{
  private const UInt32 ExpectedCommandId = 1;
  private const UInt32 ExpectedTelegramVersion = 4;
  private const UInt32 ExpectedStartOfFrame = 0x02020202;

  /// <summary>
  /// The header is the first part of the compact frame. It has always a length of 32 bytes. In addition to the general information, it contains the length of the first module.
  /// </summary>
  /// <param name="buffer">The buffer containing the raw data.</param>
  /// <param name="currentIndex">The current index in the buffer. Will be incremented by the size of the header.</param>
  /// <returns>The deserialized header.</returns>
  /// <exception cref="ArgumentException">Thrown when the start of frame, command ID, or version is invalid, or if the packet contains no data.</exception>
  private static Header DeserializeHeader(ReadOnlySpan<byte> buffer, ref int currentIndex)
  {
    var startOfFrame = Helpers.DeserializePrimitiveAndIncrementIndex<UInt32>(buffer, ref currentIndex);
    if (startOfFrame != ExpectedStartOfFrame)
    {
      throw new ArgumentException("Start of frame missing");
    }

    var commandId = Helpers.DeserializePrimitiveAndIncrementIndex<UInt32>(buffer, ref currentIndex);
    if (commandId != ExpectedCommandId)
    {
      throw new ArgumentException($"Command ID invalid: expected {ExpectedCommandId}, got {commandId}");
    }

    var telegramCounter = Helpers.DeserializePrimitiveAndIncrementIndex<UInt64>(buffer, ref currentIndex);
    var timestampTransmit = Helpers.DeserializePrimitiveAndIncrementIndex<UInt64>(buffer, ref currentIndex);
    var version = Helpers.DeserializePrimitiveAndIncrementIndex<UInt32>(buffer, ref currentIndex);
    if (version != ExpectedTelegramVersion)
    {
      throw new ArgumentException($"Telegram version invalid: expected {ExpectedTelegramVersion}, got {version}");
    }

    var sizeOfFirstModule = Helpers.DeserializePrimitiveAndIncrementIndex<UInt32>(buffer, ref currentIndex);
    if (sizeOfFirstModule == 0)
    {
      throw new ArgumentException("Packet contains no data");
    }

    return new Header
    {
      StartOfFrame = startOfFrame,
      CommandId = commandId,
      TelegramCounter = telegramCounter,
      TimestampTransmit = timestampTransmit,
      Version = version,
      SizeOfFirstModule = sizeOfFirstModule
    };
  }

  /// <summary>
  /// Deserializes the metadata of a module.
  /// Each module has a MetaData section that contains important information about the measured data. See <cref="ModuleMetaData" /> for details.
  /// </summary>
  /// <param name="buffer">The buffer containing the raw data.</param>
  /// <param name="currentIndex">The current index in the buffer. Will be incremented by the size of the meta data.</param>
  /// <returns>The deserialized module metadata.</returns>
  private static ModuleMetaData DeserializeModuleMetaData(ReadOnlySpan<byte> buffer, ref int currentIndex)
  {
    var segmentCounter = Helpers.DeserializePrimitiveAndIncrementIndex<UInt64>(buffer, ref currentIndex);
    var frameNumber = Helpers.DeserializePrimitiveAndIncrementIndex<UInt64>(buffer, ref currentIndex);
    var senderId = Helpers.DeserializePrimitiveAndIncrementIndex<UInt32>(buffer, ref currentIndex);
    var numberOfLayersInModule = Helpers.DeserializePrimitiveAndIncrementIndex<UInt32>(buffer, ref currentIndex);
    var numberOfBeamsPerScan = Helpers.DeserializePrimitiveAndIncrementIndex<UInt32>(buffer, ref currentIndex);
    var numberOfEchoes = Helpers.DeserializePrimitiveAndIncrementIndex<UInt32>(buffer, ref currentIndex);
    var timestampsStart = Helpers.DeserializeVectorAndIncrementIndex<UInt64>(buffer, ref currentIndex, numberOfLayersInModule);
    var timestampsStop = Helpers.DeserializeVectorAndIncrementIndex<UInt64>(buffer, ref currentIndex, numberOfLayersInModule);
    var phis = Helpers.DeserializeVectorAndIncrementIndex<float>(buffer, ref currentIndex, numberOfLayersInModule);
    var thetaStart = Helpers.DeserializeVectorAndIncrementIndex<float>(buffer, ref currentIndex, numberOfLayersInModule);
    var thetaStop = Helpers.DeserializeVectorAndIncrementIndex<float>(buffer, ref currentIndex, numberOfLayersInModule);
    var distanceScalingFactor = Helpers.DeserializePrimitiveAndIncrementIndex<float>(buffer, ref currentIndex);
    var nextModuleSize = Helpers.DeserializePrimitiveAndIncrementIndex<UInt32>(buffer, ref currentIndex);
    _ = Helpers.DeserializePrimitiveAndIncrementIndex<byte>(buffer, ref currentIndex);
    var echoContent = Helpers.DeserializePrimitiveAndIncrementIndex<byte>(buffer, ref currentIndex);
    var beamContent = Helpers.DeserializePrimitiveAndIncrementIndex<byte>(buffer, ref currentIndex);
    _ = Helpers.DeserializePrimitiveAndIncrementIndex<byte>(buffer, ref currentIndex);

    return new ModuleMetaData
    {
      SegmentCounter = segmentCounter,
      FrameNumber = frameNumber,
      SenderId = senderId,
      NumberOfLayersInModule = numberOfLayersInModule,
      NumberOfBeamsPerScan = numberOfBeamsPerScan,
      NumberOfEchoes = numberOfEchoes,
      TimestampsStart = timestampsStart,
      TimestampsStop = timestampsStop,
      Phis = phis,
      ThetaStart = thetaStart,
      ThetaStop = thetaStop,
      DistanceScalingFactor = distanceScalingFactor,
      NextModuleSize = nextModuleSize,
      EchoContent = (EchoContent)echoContent,
      BeamContent = (BeamContent)beamContent
    };
  }

  /// <summary>
  /// Deserializes the scan data.
  /// See `CompactSegmentTypes.cs` for details.
  /// </summary>
  /// <param name="buffer">The buffer containing the rwa data.</param>
  /// <param name="currentIndex">The current index in the buffer. Will be incremented by the size of the scan data block.</param>
  /// <param name="metaData">The metadata of the module.</param>
  /// <returns>The deserialized module data.</returns>
  private static ModuleData DeserializeModuleData(ReadOnlySpan<byte> buffer, ref int currentIndex, ModuleMetaData metaData)
  {
    var data = new ModuleData(metaData);

    for (UInt32 beamIndex = 0; beamIndex < metaData.NumberOfBeamsPerScan; ++beamIndex)
    {
      var beamsForCurrentBeamIndex = new BeamAllLayers();
      for (UInt32 layerIndex = 0; layerIndex < metaData.NumberOfLayersInModule; ++layerIndex)
      {
        var beam = new Beam();
        for (UInt32 echoIndex = 0; echoIndex < metaData.NumberOfEchoes; ++echoIndex)
        {
          var echo = new Echo();
          if ((metaData.EchoContent & EchoContent.Distance) != EchoContent.None)
          {
            echo.Distance = Helpers.DeserializePrimitiveAndIncrementIndex<UInt16>(buffer, ref currentIndex);
          }
          if ((metaData.EchoContent & EchoContent.Rssi) != EchoContent.None)
          {
            echo.Rssi = Helpers.DeserializePrimitiveAndIncrementIndex<UInt16>(buffer, ref currentIndex);
          }

          beam.Echoes.Add(echo);
        }

        if ((metaData.BeamContent & BeamContent.Properties) != BeamContent.None)
        {
          beam.Properties = Helpers.DeserializePrimitiveAndIncrementIndex<byte>(buffer, ref currentIndex);
        }

        if ((metaData.BeamContent & BeamContent.Theta) != BeamContent.None)
        {
          beam.Theta = Helpers.DeserializePrimitiveAndIncrementIndex<Int16>(buffer, ref currentIndex);
        }

        beamsForCurrentBeamIndex.Add(beam);
      }
      data.Beams.Add(beamsForCurrentBeamIndex);
    }

    return data;
  }

  /// <summary>
  /// Deserializes all modules in a segment.
  /// </summary>
  /// <param name="buffer">The buffer containing the raw data.</param>
  /// <param name="currentIndex">The current index in the buffer. Will be incremented by the size of the deserialized data.</param>
  /// <param name="nextModuleSize">The size of the next module.</param>
  /// <returns>A list of deserialized module data.</returns>
  /// <exception cref="ArgumentException">Thrown when the module size does not match the expected size.</exception>
  private static List<ModuleData> DeserializeModules(ReadOnlySpan<byte> buffer, ref int currentIndex, UInt32 nextModuleSize)
  {
    var ret = new List<ModuleData>();
    while (nextModuleSize != 0)
    {
      var startIndex = currentIndex;

      var metaData = DeserializeModuleMetaData(buffer, ref currentIndex);
      var moduleData = DeserializeModuleData(buffer, ref currentIndex, metaData);
      ret.Add(moduleData);

      var moduleSize = currentIndex - startIndex;
      if (moduleSize != nextModuleSize)
      {
        throw new ArgumentException("Next Module Size: " + (int)nextModuleSize + ", Module Size: " + moduleSize);
      }

      nextModuleSize = metaData.NextModuleSize;
    }
    return ret;
  }

  /// <summary>
  /// Converts a Compact binary telegram.
  /// </summary>
  /// <param name="buffer">The buffer containing the raw data.</param>
  /// <returns>The deserialized CompactSegment.</returns>
  public static CompactSegment Convert(ReadOnlySpan<byte> buffer)
  {
    Helpers.EnsureChecksumIsValid(buffer);

    int currentIndex = 0;
    var header = DeserializeHeader(buffer, ref currentIndex);
    var modules = DeserializeModules(buffer, ref currentIndex, header.SizeOfFirstModule);

    return new CompactSegment(header, modules);
  }
}
