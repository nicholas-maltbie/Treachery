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
using nickmaltbie.Treachery.Interactive.Stamina;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.InputSystem.InputAction;

namespace nickmaltbie.Treachery.Action
{
    /// <summary>
    /// Action that can be performed by an IActionActor.
    /// </summary>
    public abstract class ActorConditionalAction<TAction> : ConditionalAction, IStaminaAction
    {
        public EventHandler OnPerform;
        private TAction actionType;

        protected InputActionReference inputActionReference;
        protected InputAction overrideInputAction;
        protected IActionActor<TAction> actor;
        protected IStaminaMeter stamina;

        protected float staminaCost;
        protected float cooldown;
        protected bool performWhileHeld;
        protected float elapsedSincePerformed = Mathf.Infinity;
        protected bool enabled = false;

        public ActorConditionalAction(
            InputActionReference inputAction,
            IActionActor<TAction> actor,
            IStaminaMeter stamina,
            TAction actionType,
            float cooldown = 0.0f,
            float staminaCost = 0.0f,
            bool performWhileHeld = false)
        {
            base.condition = Condition;
            inputActionReference = inputAction;
            this.actionType = actionType;
            this.actor = actor;
            this.cooldown = cooldown;
            this.performWhileHeld = performWhileHeld;
            this.stamina = stamina;
            this.staminaCost = staminaCost;
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
                stamina.SpendStamina(this);
                Perform();
                OnPerform?.Invoke(this, EventArgs.Empty);
                ResetCooldown();
                return true;
            }

            return false;
        }

        protected abstract void Perform();

        public void Cleanup()
        {
            InputAction.performed -= PerformAction;
            enabled = false;
        }

        private void PerformAction(CallbackContext context)
        {
            AttemptIfPossible();
        }

        public virtual void Setup()
        {
            InputAction?.Enable();
            enabled = true;
            InputAction.performed += PerformAction;
        }

        public virtual void SetActive(bool state)
        {
            enabled = state;
        }

        protected virtual bool Condition()
        {
            return elapsedSincePerformed >= cooldown &&
                stamina.HasEnoughStamina(this) &&
                actor.CanPerform(actionType) &&
                enabled;
        }

        protected virtual float StaminaCost => staminaCost;

        public virtual float Cost => StaminaCost;

        public virtual float CooldownTime => 0.0f;
    }
}
