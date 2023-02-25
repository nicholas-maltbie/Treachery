
using System;
using nickmaltbie.StateMachineUnity;
using nickmaltbie.Treachery.Interactive.Health;
using Unity.Netcode;
using UnityEngine;

namespace nickmaltbie.Treachery.Player
{
    [RequireComponent(typeof(IDamageable))]
    [RequireComponent(typeof(IStateMachine<Type>))]
    public class ReviveEventManager : NetworkBehaviour
    {
        /// <summary>
        /// Previous live state of the player.
        /// </summary>
        protected bool PreviousLivingState { get; set; }

        public void Start()
        {
            PreviousLivingState = GetComponent<Damageable>().IsAlive();
        }

        public void Update()
        {
            if (IsOwner)
            {
                bool currentLivingState = GetComponent<Damageable>().IsAlive();
                if (currentLivingState != PreviousLivingState)
                {
                    IStateMachine<Type> sm = GetComponent<IStateMachine<Type>>();
                    sm.RaiseEvent(currentLivingState ? PlayerReviveEvent.Instance : PlayerDeathEvent.Instance);
                }

                PreviousLivingState = currentLivingState;
            }
        }

    }
}