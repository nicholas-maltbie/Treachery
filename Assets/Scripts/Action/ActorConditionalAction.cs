
using System;
using nickmaltbie.OpenKCC.Character.Action;
using nickmaltbie.OpenKCC.Input;
using UnityEngine;

namespace nickmaltbie.Treachery.Action
{
    /// <summary>
    /// Action that can be performed by an IActionActor.
    /// </summary>
    public abstract class ActorConditionalAction<TAction> : ConditionalAction
    {
        protected BufferedInput bufferedInput;

        private TAction actionType;

        private IActionActor<TAction> actor;

        public EventHandler OnPerform;

        public ActorConditionalAction(BufferedInput bufferedInput, IActionActor<TAction> actor, TAction actionType)
        {
            base.condition = this.CanPerform;
            this.bufferedInput = bufferedInput;
            this.actionType = actionType;
            this.actor = actor;
        }

        public override void Update()
        {
            bufferedInput.Update();
            base.Update();
        }

        public bool AttemptIfPossible()
        {
            if (CanPerform() && bufferedInput.Pressed)
            {
                Perform();
                OnPerform?.Invoke(this, EventArgs.Empty);
                bufferedInput.Reset();
                return true;
            }

            return false;
        }

        protected abstract void Perform();

        public void Setup()
        {
            bufferedInput.InputAction?.Enable();
        }

        protected new virtual bool CanPerform()
        {
            return actor.CanPerform(actionType);
        }
    }
}