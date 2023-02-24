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

using nickmaltbie.OpenKCC.Input;
using nickmaltbie.Treachery.Action;
using nickmaltbie.Treachery.Action.PlayerActions;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace nickmaltbie.Treachery.Player.Action
{
    [RequireComponent(typeof(IActionActor<PlayerAction>))]
    public abstract class AbstractActionBehaviour<TConditionalAction> : NetworkBehaviour where TConditionalAction : ActorConditionalAction<PlayerAction>
    {
        [SerializeField]
        public InputActionReference inputActionReference;

        [SerializeField]
        public float cooldown = 0.0f;

        [SerializeField]
        public float bufferTime = 0.05f;

        protected BufferedInput BufferedInput { get; private set; }
        private IActionActor<PlayerAction> _actor;

        public IActionActor<PlayerAction> Actor => _actor ??= GetComponent<IActionActor<PlayerAction>>();
        public TConditionalAction Action { get; private set; }

        public InputAction InputAction
        {
            get => BufferedInput.InputAction;
            set => BufferedInput.InputAction = value;
        }

        public abstract TConditionalAction SetupAction();
        public abstract void CleanupAction(TConditionalAction action);

        public virtual void OnValidate()
        {
            ValidateAction();
        }

        public virtual void Awake()
        {
            ValidateAction();
        }

        public void ValidateAction()
        {
            BufferedInput = new BufferedInput
            {
                inputActionReference = inputActionReference,
                cooldown = cooldown,
                bufferTime = bufferTime,
            };

            if (Action != null)
            {
                CleanupAction(Action);
            }

            Action = SetupAction();
        }

        public void Start()
        {
            if (IsOwner)
            {
                Action.Setup();
            }
        }

        public void Update()
        {
            if (IsOwner)
            {
                Action.Update();
            }
        }

        public void FixedUpdate()
        {
            if (IsOwner)
            {
                Action.AttemptIfPossible();
            }
        }
    }
}
