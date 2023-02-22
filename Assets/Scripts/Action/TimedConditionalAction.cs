
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
        private float elapsed = Mathf.Infinity;
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
            float duration) :
            base(bufferedInput, actor, actionType)
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
                    bufferedInput.Reset();
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
                bufferedInput.Reset();
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