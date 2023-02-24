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

namespace nickmaltbie.Treachery.Action
{
    /// <summary>
    /// Action that can be performed by an IActionActor.
    /// </summary>
    public abstract class ActorConditionalAction<TAction> : ConditionalAction
    {
        public EventHandler OnPerform;
        private TAction actionType;
        private IActionActor<TAction> actor;

        protected BufferedInput BufferedInput { get; private set; }

        public ActorConditionalAction(BufferedInput bufferedInput, IActionActor<TAction> actor, TAction actionType)
        {
            base.condition = CanPerform;
            this.BufferedInput = bufferedInput;
            this.actionType = actionType;
            this.actor = actor;
        }

        public override void Update()
        {
            BufferedInput.Update();
            base.Update();
        }

        public bool AttemptIfPossible()
        {
            if (CanPerform() && BufferedInput.Pressed)
            {
                Perform();
                OnPerform?.Invoke(this, EventArgs.Empty);
                BufferedInput.Reset();
                return true;
            }

            return false;
        }

        protected abstract void Perform();

        public void Setup()
        {
            BufferedInput.InputAction?.Enable();
        }

        protected new virtual bool CanPerform()
        {
            return actor.CanPerform(actionType);
        }
    }
}
