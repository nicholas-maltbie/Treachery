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

using System;
using nickmaltbie.OpenKCC.Character.Action;
using nickmaltbie.OpenKCC.Input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace nickmaltbie.Treachery.Action
{
    /// <summary>
    /// Action that can be performed by an IActionActor.
    /// </summary>
    public abstract class ActorConditionalAction<TAction> : ConditionalAction
    {
        public EventHandler OnPerform;
        private TAction actionType;

        protected InputActionReference inputActionReference;
        protected InputAction overrideInputAction;

        private IActionActor<TAction> actor;
        private float cooldown;
        private bool performWhileHeld;
        private float elapsedSincePerformed = Mathf.Infinity;

        public ActorConditionalAction(InputActionReference inputAction, IActionActor<TAction> actor, TAction actionType, float cooldown = 0.0f, bool performWhileHeld = false)
        {
            base.condition = Condition;
            this.inputActionReference = inputAction;
            this.actionType = actionType;
            this.actor = actor;
            this.cooldown = cooldown;
            this.performWhileHeld = performWhileHeld;
        }

        protected InputAction InputAction
        {
            get => overrideInputAction ?? inputActionReference.action;
            set => overrideInputAction = value;
        }

        public override void Update()
        {
            base.Update();
            elapsedSincePerformed += Time.deltaTime;
            if (performWhileHeld && (InputAction?.IsPressed() ?? false))
            {
                AttemptIfPossible();
            }
        }

        protected void ResetCooldown()
        {
            elapsedSincePerformed = 0.0f;
        }

        public bool AttemptIfPossible()
        {
            if (CanPerform)
            {
                Perform();
                OnPerform?.Invoke(this, EventArgs.Empty);
                ResetCooldown();
                return true;
            }

            return false;
        }

        protected abstract void Perform();

        public void Setup()
        {
            InputAction?.Enable();
            InputAction.performed += _ => AttemptIfPossible();
        }

        protected virtual bool Condition()
        {
            return elapsedSincePerformed >= cooldown && actor.CanPerform(actionType);
        }
    }
}
