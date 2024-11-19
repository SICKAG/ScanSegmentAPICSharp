//
// Copyright (c) 2024 SICK AG
// SPDX-License-Identifier: MIT
//
using Compact;
using System.Net;
using System.Net.Sockets;

static class Example
{
  /// <summary>
  /// Receive `numberOfSegments` Compact segments on the given UDP `port` and convert the received data into CompactSegment objects.
  /// </summary>
  /// <remarks>
  /// This example does not include exception handling. Any exceptions will be reported to the console and the program will exit.
  /// </remarks>
  /// <param name="port">Port for incoming data.</param>
  /// <param name="numberOfSegments">The number of segments to read.</param>
  /// <returns>An enumerable CompactSegment objects</returns>
  private static IEnumerable<CompactSegment> ReceiveCompactSegments(int port, int numberOfSegments)
  {
    using var udpClient = new UdpClient(port);

    Console.WriteLine($"Waiting to receive {numberOfSegments} Compact segments on UDP port {port}.");
    Console.WriteLine("Press Ctrl+C to cancel.");

    var ep = new IPEndPoint(IPAddress.Any, port);
    for (var i = 0; i < numberOfSegments; i++)
    {
      // Receive UDP packet and convert to compact frame
      var receivedData = udpClient.Receive(ref ep);
      Console.WriteLine($"Received segment {i + 1}.");
      yield return CompactDeserializer.Convert(receivedData);
    }
  }

  /// <summary>
  /// An example how to receive and parse Compact telegrams from a SICK sensor.
  /// </summary>
  public static void Main()
  {
    // Receive and collect 20 Compact segments
    var segments = ReceiveCompactSegments(port: 2115, numberOfSegments: 20).ToList();

    // Display information about the first 5 frames
    foreach (var segment in segments.Take(5))
    {
      try
      {
        var frameNumber = segment.Modules[0].MetaData.FrameNumber;
        var segmentCounter = segment.Modules[0].MetaData.SegmentCounter;
        var startAngle = segment.Modules[0].MetaData.ThetaStart[0]; // Start angle of the first scan in the first module
        var distance = segment.Modules[0].Beams[6][0].Echoes[0].Distance; // Distance of the first echo of the 7th beam in the first layer of the first module
        Console.WriteLine($"Frame number: {frameNumber}, segment counter: {segmentCounter:D3}, start angle: {startAngle:F4} rad, distance: {distance} mm");
      }
      catch (Exception e)
      {
        Console.WriteLine(e);
      }

    }
  }
}
