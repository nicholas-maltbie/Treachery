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

using nickmaltbie.StateMachineUnity;
using nickmaltbie.StateMachineUnity.Attributes;
using nickmaltbie.StateMachineUnity.netcode;
using nickmaltbie.Treachery.Interactive.Health;
using UnityEngine;

using static nickmaltbie.Treachery.Player.PlayerAnimStates;

namespace nickmaltbie.Treachery.Player
{
    /// <summary>
    /// Have a character controller push any dynamic rigidbody it hits
    /// </summary>
    public class TargetDummy : NetworkSMAnim, IDamageListener
    {
        [InitialState]
        [Animation(IdleAnimState, 0.35f, true)]
        [AnimationTransition(typeof(OnHitEvent), typeof(HitReaction), 0.35f, true, 0.1f)]
        public class IdleState : State { }

        [Animation(HitReactionAnimState, 0.35f, true)]
        [Transition(typeof(OnHitEvent), typeof(HitReset))]
        [TransitionOnAnimationComplete(typeof(IdleState))]
        public class HitReaction : State { }

        [Animation(IdleAnimState, 0.1f, false)]
        [TransitionAfterTime(typeof(HitReaction), 0.05f)]
        public class HitReset : State { }

        [Animation(DyingAnimState, 0.35f, true)]
        [TransitionFromAnyState(typeof(PlayerDeath))]
        [TransitionOnAnimationComplete(typeof(DeadState))]
        [Transition(typeof(ReviveEvent), typeof(RevivingState))]
        public class DyingState : State { }

        [Animation(DeadAnimState, 0.35f, true)]
        [Transition(typeof(ReviveEvent), typeof(RevivingState))]
        public class DeadState : State { }

        [Animation(RevivingAnimState, 1.0f, true)]
        [TransitionOnAnimationComplete(typeof(IdleState), 0.35f)]
        public class RevivingState : State { }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            SetStateQuiet(typeof(IdleState));
        }

        public void OnDamage(IDamageable target, IDamageSource source, float previous, float current, float damage)
        {
            if (!IsServer)
            {
                return;
            }

            if (previous > 0 && current == 0)
            {
                RaiseEvent(PlayerDeath.Instance);
            }
            else
            {
                RaiseEvent(OnHitEvent.Instance);
            }
        }

        public void OnHeal(IDamageable target, IDamageSource source, float previous, float current, float amount)
        {
            if (!IsServer)
            {
                return;
            }

            if (previous == 0 && current > 0)
            {
                RaiseEvent(ReviveEvent.Instance);
            }
        }
    }
}
