//
// Copyright (c) 2024 SICK AG
// SPDX-License-Identifier: MIT
//

namespace Compact;

[Flags]
public enum EchoContent : byte
{
  None = 0x00,
  Distance = 0x01,
  Rssi = 0x02,
  All = Distance | Rssi
}

[Flags]
public enum BeamContent : byte
{
  None = 0x00,
  Properties = 0x01,
  Theta = 0x02,
  All = Properties | Theta
}

public class Header
{
  /// <summary>
  /// StartOfFrame has always the value 0x02020202.
  /// </summary>
  public UInt32 StartOfFrame { get; init; } = 0;
  /// <summary>
  /// CommandID has always the value 1.
  /// </summary>
  public UInt32 CommandId { get; init; } = 0;
  /// <summary>
  /// Counts the packets sent since the device was switched on.
  /// </summary>
  public UInt64 TelegramCounter { get; init; } = 0;
  /// <summary>
  /// Sensor system time in microseconds since 1.1.1970 00:00 in UTC, if there is no other timeserver.
  /// </summary>
  public UInt64 TimestampTransmit { get; init; } = 0;
  /// <summary>
  /// Version must be 4.
  /// </summary>
  public UInt32 Version { get; init; } = 0;
  /// <summary>
  /// The size of the first module.
  /// </summary>
  public UInt32 SizeOfFirstModule { get; init; } = 0;
}

public class ModuleMetaData
{
  /// <summary>
  /// Counts how many segments are required for one revolution.
  /// </summary>
  public UInt64 SegmentCounter { get; init; } = 0;
  /// <summary>
  /// Counts the revolutions since the device was switched on.
  /// </summary>
  public UInt64 FrameNumber { get; init; } = 0;
  /// <summary>
  /// Serial number of the device.
  /// </summary>
  public UInt32 SenderId { get; init; } = 0;
  /// <summary>
  /// Number of layers in one beam.
  /// </summary>
  public UInt32 NumberOfLayersInModule { get; init; } = 0;
  /// <summary>
  /// Number of beams in one module.
  /// </summary>
  public UInt32 NumberOfBeamsPerScan { get; set; } = 0; // Must be settable for the collector to work
  /// <summary>
  /// Number of echoes in one layer.
  /// </summary>
  public UInt32 NumberOfEchoes { get; init; } = 0;
  /// <summary>
  /// Array with the timestamp, when beam starts recording.
  /// </summary>
  public List<UInt64> TimestampsStart { get; init; } = [];
  /// <summary>
  /// Array with the timestamp, when beam finishes recording.
  /// </summary>
  public List<UInt64> TimestampsStop { get; set; } = []; // Must be settable for the collector to work
  /// <summary>
  /// Array with elevation angles in radians.
  /// </summary>
  public List<float> Phis { get; init; } = [];
  /// <summary>
  /// Array with azimuth angles in radians of the first beam of each scan.
  /// </summary>
  public List<float> ThetaStart { get; init; } = [];
  /// <summary>
  /// Array with azimuth angles in radians of the last beam of each scan.
  /// </summary>
  public List<float> ThetaStop { get; set; } = []; // Must be settable for the collector to work
  /// <summary>
  /// Required for displaying distance values over 65535mm. Distance = raw_distance * DistanceScalingFactor.
  /// </summary>
  public float DistanceScalingFactor { get; init; } = 1.0f;
  /// <summary>
  /// Size for next module. If 0, then this was the last module.
  /// </summary>
  public UInt32 NextModuleSize { get; init; } = 0;
  /// <summary>
  /// The availability of the segment
  /// </summary>
  public bool Availability { get; init; } = false;
  /// <summary>
  /// The individual bits of this byte describe which data is available in that part of the measurement data that is recorded per echo, e.g., distance or RSSI.
  /// </summary>
  public EchoContent EchoContent { get; init; } = EchoContent.None;
  /// <summary>
  /// The individual bits of this byte describe which data is available in that part of the measurement data that is recorded per beam, e.g., azimuth angle or beam properties.
  /// </summary>
  public BeamContent BeamContent { get; init; } = BeamContent.None;
}

public class Echo
{
  /// <summary>
  /// Distance value.
  /// </summary>
  public UInt16 Distance { get; set; } = 0;
  /// <summary>
  /// RSSI (Received Signal Strength Indicator) shows the strength of the received signal.
  /// High value means high signal strength, low value means low signal strength.
  /// </summary>
  public UInt16 Rssi { get; set; } = 0;
}

/// <summary>
/// A beam is a measuring beam with specific properties, such as distance, remission, ...
/// </summary>
public class Beam
{
  /// <summary>
  /// List of echoes.
  /// </summary>
  public List<Echo> Echoes { get; init; } = [];
  /// <summary>
  /// Theta value.
  /// </summary>
  public Int16 Theta { get; set; } = 0;
  /// <summary>
  /// Additional properties for a beam.
  /// Bit 0: if bit is set, a reflector is detected for one of the echoes.
  /// </summary>
  public byte Properties { get; set; } = 0;
}

/// <summary>
/// Contains all beams that are measured in a segment.
/// </summary>
public class BeamAllLayers : List<Beam>
{ }

/// <summary>
/// A module contains the measured data.
/// It is therefore divided into two parts: The ModuleMetaData and the beams.
/// The MetaData contains the general information of a module.
/// The Beams contain the measurement data of the different layers.
/// </summary>
/// <param name="metaData">General information about the module.</param>
public class ModuleData(ModuleMetaData metaData)
{
  public ModuleMetaData MetaData { get; } = metaData;
  public List<BeamAllLayers> Beams { get; } = [];
}

/// <summary>
/// Contains all received data from sensor.
/// </summary>
public class CompactSegment
{
  public Header Header { get; init; }
  public List<ModuleData> Modules { get; }

  public CompactSegment(Header header, List<ModuleData> modules)
  {
    Header = header;
    Modules = modules;
  }
}
