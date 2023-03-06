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
using nickmaltbie.Treachery.Interactive.Stamina;
using UnityEngine;
using UnityEngine.InputSystem;

namespace nickmaltbie.Treachery.Action
{
    /// <summary>
    /// Action that can be performed by an IActionActor.
    /// </summary>
    public abstract class TimedConditionalAction<TAction> : ActorConditionalAction<TAction>
    {
        private bool performing = false;
        public float duration = 1.0f;
        protected float elapsed = Mathf.Infinity;
        public bool IsPerforming => performing && elapsed <= duration;

        /// <summary>
        /// Action to invoke when the player completed this action.
        /// Returns a bool, if true the action was interrupted, if false,
        /// the action completed normally.
        /// </summary>
        public EventHandler<bool> OnComplete;

        protected TimedConditionalAction(
            InputActionReference actionReference,
            IActionActor<TAction> actor,
            IStaminaMeter stamina,
            TAction actionType,
            float duration,
            float cooldown = 0.0f,
            float staminaCost = 0.0f,
            bool performWhileHeld = false)
            : base(actionReference, actor, stamina, actionType, cooldown, staminaCost, performWhileHeld)
        {
            this.duration = duration;
        }

        /// <inheritdoc/>
        public override void Update()
        {
            base.Update();
            if (performing)
            {
                ResetCooldown();
                elapsed += Time.deltaTime;
                if (elapsed >= duration)
                {
                    ResetCooldown();
                    performing = false;
                    OnComplete?.Invoke(this, false);
                }
            }
            else
            {
                elapsed = 0.0f;
            }
        }

        public bool Interrupt(bool restoreStamina = false)
        {
            if (performing)
            {
                performing = false;
                ResetCooldown();
                OnComplete?.Invoke(this, true);

                if (restoreStamina)
                {
                    this.stamina.RestoreStamina(Cost);
                }

                return true;
            }

            return false;
        }

        protected override void Perform()
        {
            performing = true;
        }
    }
}
