
using nickmaltbie.StateMachineUnity.Event;
using UnityEngine;

namespace nickmaltbie.Treachery.Enemy.Zombie
{
    public static class ZombieEvents
    {
        public class TargetIdentifiedEvent : IEvent
        {
            public readonly GameObject target;

            public TargetIdentifiedEvent(GameObject target)
            {
                this.target = target;
            }
        }

        public class TargetLostEvent : IEvent { }
        public class StopRoamEvent : IEvent { }

        public class StartRoamEvent : IEvent
        {
            public readonly float roamTime;
            public readonly Quaternion heading;

            public StartRoamEvent(float roamTime, Quaternion heading)
            {
                this.roamTime = roamTime;
                this.heading = heading;
            }
        }

        public class ZombieAttackEvent : IEvent { }
    }
}