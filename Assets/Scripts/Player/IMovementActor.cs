// Copyright (C) 2023 Nicholas Maltbie
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
// associated documentation files (the "Software"), to deal in the Software without restriction,
// including without limitation the rights to use, copy, modify, merge, publish, distribute,
// sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING
// BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
// CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
// ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using nickmaltbie.OpenKCC.CameraControls;
using UnityEngine;

namespace nickmaltbie.Treachery.Player
{
    public interface IMovementActor
    {
        /// <summary>
        /// Direction player is currently inputting.
        /// </summary>
        Vector3 InputMovement { get; }

        /// <summary>
        /// Previous non zero direction player has input.
        /// </summary>
        Vector3 LastInputMovement { get; }

        /// <summary>
        /// Desired movement of the player in world space.
        /// </summary>
        Vector3 GetDesiredMovement();

        /// <summary>
        /// Previous non zero desired movement of player in world space.
        /// </summary>
        Vector3 LastDesiredMovement();

        /// <summary>
        /// Where in world space is the player's camera origin coming from.
        /// </summary>
        Vector3 CameraBase { get; }

        /// <summary>
        /// What direction is the player currently looking.
        /// </summary>
        IManagedCamera Camera { get; }
    }
}
