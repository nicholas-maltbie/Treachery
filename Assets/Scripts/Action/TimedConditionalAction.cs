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
using nickmaltbie.OpenKCC.Input;
using UnityEngine;

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
            BufferedInput bufferedInput,
            IActionActor<TAction> actor,
            TAction actionType,
            float duration)
            : base(bufferedInput, actor, actionType)
        {
            this.duration = duration;
        }

        /// <inheritdoc/>
        public override void Update()
        {
            base.Update();
            if (performing)
            {
                elapsed += Time.deltaTime;
                if (elapsed >= duration)
                {
                    BufferedInput.Reset();
                    performing = false;
                    OnComplete?.Invoke(this, false);
                }
            }
            else
            {
                elapsed = 0.0f;
            }
        }

        public bool Interrupt()
        {
            if (performing)
            {
                performing = false;
                elapsed = 0.0f;
                OnComplete?.Invoke(this, true);
                BufferedInput.Reset();
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
