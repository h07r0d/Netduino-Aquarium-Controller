//---------------------------------------------------------------------------
//<copyright file="PullUpResistor.cs">
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
namespace CW.NETMF
{
  using Microsoft.SPOT.Hardware;

  /// <summary>
  /// Specifies the various pull-up resistor types.
  /// </summary>
  public enum PullUpResistor
  {
    /// <summary>
    /// A value that represents an external pull-up resistor.
    /// </summary>
    External = Port.ResistorMode.Disabled,

    /// <summary>
    /// A value that represents an internal pull-up resistor.
    /// </summary>
    Internal = Port.ResistorMode.PullUp
  }
}
