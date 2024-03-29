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

using nickmaltbie.StateMachineUnity.Event;
using nickmaltbie.Treachery.Interactive.Stamina;
using UnityEngine;
using UnityEngine.InputSystem;

namespace nickmaltbie.Treachery.Action
{
    /// <summary>
    /// Action that can be performed by an IActionActor.
    /// </summary>
    public class ContinuousConditionalAction<TAction> : ActorConditionalAction<TAction>
    {
        protected IEvent raiseOnPerformed;
        protected IEvent raiseOnStopped;
        protected float staminaCostRate;
        protected float staminaRequiredToStart;
        public bool Performing { get; protected set; }

        public ContinuousConditionalAction(
            InputActionReference actionReference,
            IActionActor<TAction> actor,
            IStaminaMeter stamina,
            TAction actionType,
            float staminaCostRate,
            float staminaRequiredToStart,
            IEvent raiseOnPerformed = null,
            IEvent raiseOnStopped = null)
            : base(actionReference, actor, stamina, actionType, 0, staminaCostRate * Time.deltaTime, true)
        {
            this.raiseOnPerformed = raiseOnPerformed;
            this.raiseOnStopped = raiseOnStopped;
            this.staminaCostRate = staminaCostRate;
            this.staminaRequiredToStart = staminaRequiredToStart;
        }

        public override void Setup()
        {
            base.Setup();
            InputAction.canceled += _ => NotPerformed();
        }

        public override void Update()
        {
            if ((!InputAction?.IsPressed() ?? false) || !CanPerform)
            {
                NotPerformed();
            }
            else
            {
                base.Update();
            }
        }

        protected virtual void NotPerformed()
        {
            if (raiseOnStopped != null)
            {
                base.actor.RaiseEvent(raiseOnStopped);
            }

            Performing = false;
        }

        protected override void Perform()
        {
            if (raiseOnPerformed != null)
            {
                base.actor.RaiseEvent(raiseOnPerformed);
            }

            Performing = true;
        }

        protected override float StaminaCost => staminaCostRate * Time.deltaTime;

        protected override bool Condition()
        {
            if (Performing)
            {
                return base.Condition();
            }
            else
            {
                return base.Condition() && base.stamina.HasEnoughStamina(staminaRequiredToStart);
            }
        }
    }
}
