//---------------------------------------------------------------------------
//<copyright file="Dht11Sensor.cs">
//
// Copyright 2011 Stanislav "CW" Simicek
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
//</copyright>
//---------------------------------------------------------------------------
namespace CW.NETMF.Sensors
{
  using Microsoft.SPOT;
  using Microsoft.SPOT.Hardware;

  /// <summary>
  /// Represents an instance of the DHT11 sensor.
  /// </summary>
  /// <remarks>
  /// Humidity measurement range 20 - 90%, accuracy ±4% at 25°C, ±5% at full range.
  /// Temperature measurement range 0 - 50°C, accuracy ±1-2°C.
  /// </remarks>
  public class Dht11Sensor : DhtSensor
  {
    /// <summary>
    /// Initialize a new instance of the <see cref="Dht11Sensor"/> class.
    /// </summary>
    /// <param name="pin1">The identifier for the sensor's data bus port.</param>
    /// <param name="pin2">The identifier for the sensor's data bus port.</param>
    /// <param name="pullUp">The pull-up resistor type.</param>
    /// <remarks>
    /// The ports identified by <paramref name="pin1"/> and <paramref name="pin2"/>
    /// must be wired together.
    /// </remarks>
    public Dht11Sensor(Cpu.Pin pin1, Cpu.Pin pin2, PullUpResistor pullUp) : base(pin1, pin2, pullUp)
    {
      // This constructor is intentionally left blank.
    }

    protected override int StartDelay
    {
      get
      {
        return 18;  // At least 18 ms
      }
    }

    protected override void Convert(byte[] data)
    {
      Debug.Assert(data != null);
      Debug.Assert(data.Length == 4);
      // DHT11 has 8-bit resolution, so the decimal part is always zero.
      Debug.Assert(data[1] == 0, "Humidity decimal part should be zero.");
      Debug.Assert(data[3] == 0, "Temperature decimal part should be zero.");

      Humidity    = (float)data[0];
      Temperature = (float)data[2];
    }
  }
}
