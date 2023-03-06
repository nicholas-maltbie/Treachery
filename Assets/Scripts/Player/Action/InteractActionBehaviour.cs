
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
using nickmaltbie.Treachery.Action.PlayerActions;
using nickmaltbie.Treachery.Interactive;
using nickmaltbie.Treachery.Interactive.Health;
using Unity.Netcode;
using UnityEngine;

namespace nickmaltbie.Treachery.Player.Action
{
    /// <summary>
    /// Dodge action that can be performed by a player.
    /// </summary>
    [RequireComponent(typeof(ICameraControls))]
    public class InteractActionBehaviour : AbstractActionBehaviour<InteractAction>
    {
        [SerializeField]
        public Transform viewSource;

        [HideInInspector]
        [SerializeField]
        private Vector3 viewOffset;

        public float interactRange = 2.0f;
        public float interactRadius = 0.1f;

        public IInteractive Focus => (Action as InteractAction).Focus;

        public override void OnValidate()
        {
            base.OnValidate();
            viewOffset = viewSource.localPosition;
        }

        public override InteractAction SetupAction()
        {
            return new InteractAction(
                inputActionReference,
                Actor,
                Stamina,
                gameObject,
                transform,
                viewOffset,
                GetComponent<ICameraControls>())
            {
                interactRange = interactRange,
                interactRadius = interactRadius,
            };
        }

        public override void CleanupAction(InteractAction action)
        {

        }
    }
}
