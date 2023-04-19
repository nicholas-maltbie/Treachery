

using nickmaltbie.StateMachineUnity;
using nickmaltbie.StateMachineUnity.Attributes;
using nickmaltbie.StateMachineUnity.Event;
using nickmaltbie.StateMachineUnity.netcode;
using nickmaltbie.Treachery.Interactive.Health;
using nickmaltbie.Treachery.Player;
using UnityEngine;
using UnityEngine.AI;

using static nickmaltbie.Treachery.Enemy.Zombie.ZombieAnimations;
using static nickmaltbie.Treachery.Enemy.Zombie.ZombieEvents;

namespace nickmaltbie.Treachery.Enemy.Zombie
{
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(IDamageable))]
    public class ZombieEnemy : NetworkSMAnim
    {
        /// <summary>
        /// Distance at which the zombie will start chasing the player.
        /// </summary>
        public float aggroDistance = 5.0f;

        /// <summary>
        /// Distance at which the zombie will forget about the player.
        /// </summary>
        public float loseAggroDistance = 20.0f;

        /// <summary>
        /// Average time between when the zombie will start roaming.
        /// </summary>
        public float timeBetweenRoaming = 3.0f;

        /// <summary>
        /// Speed at which the zombie chases the player.
        /// </summary>
        public float chaseSpeed = 5.0f;

        /// <summary>
        /// Speed at which the zombie roams around.
        /// </summary>
        public float roamSpeed = 1.0f;

        /// <summary>
        /// Minimum time zombie will spend roaming.
        /// </summary>
        public float minRoamTime = 0.5f;

        /// <summary>
        /// Maximum time zombie will spend roaming.
        /// </summary>
        public float maxRoamTime = 3.0f;

        /// <summary>
        /// What the zombie is chasing.
        /// </summary>
        private GameObject zombieTarget;

        /// <summary>
        /// NavMeshAgent for controlling zombie movement.
        /// </summary>
        private NavMeshAgent navMeshAgent;

        /// <summary>
        /// Remaining time in player roaming action.
        /// </summary>
        private float roamRemainingTime;

        /// <summary>
        /// heading zombie is roaming towards
        /// </summary>
        private Quaternion roamHeading;

        /// <summary>
        /// Time until the zombie will start roaming again.
        /// </summary>
        private float timeToNextRoam;

        /// <summary>
        /// Zombie standing still and doing nothing.
        /// </summary>
        [InitialState]
        [OnEnterState(nameof(OnStartIdleState))]
        [Animation(ZombieIdleAnimState, 0.35f, true)]
        [OnUpdate(nameof(IdleZombieAction))]
        [Transition(typeof(StartRoamEvent), typeof(RoamingState))]
        [Transition(typeof(TargetIdentifiedEvent), typeof(ChaseState))]
        public class IdleState : State { }

        /// <summary>
        /// Zombie will randomly meander about and look for something to eat.
        /// </summary>
        [Animation(ZombieWalkingAnimState, 0.35f, true)]
        [OnEnterState(nameof(StartRoaming))]
        [OnUpdate(nameof(RoamingMovement))]
        [Transition(typeof(StopRoamEvent), typeof(IdleState))]
        [Transition(typeof(TargetIdentifiedEvent), typeof(ChaseState))]
        public class RoamingState : State { }

        /// <summary>
        /// Zombie has a target and is chasing after them.
        /// </summary>
        [Animation(ZombieRunningAnimState, 0.35f, true)]
        [Transition(typeof(TargetIdentifiedEvent), typeof(ChaseState))]
        [Transition(typeof(TargetLostEvent), typeof(IdleState))]
        [OnEnterState(nameof(OnStartChase))]
        [OnExitState(nameof(StopMovement))]
        [OnUpdate(nameof(ChaseTarget))]
        public class ChaseState : State { }

        /// <summary>
        /// Zombie is on top of their target and attempting to attack them.
        /// </summary>
        [Animation(ZombieAttackAnimState, 0.35f, true)]
        public class AttackState : State { }

        /// <summary>
        /// Zombie is stunned after being hit
        /// </summary>
        [Animation(ZombieReactionHitAnimState, 0.05f, true, 0.1f)]
        [TransitionFromAnyState(typeof(OnHitEvent))]
        [TransitionOnAnimationComplete(typeof(IdleState), 0.35f, true)]
        public class HitStunState : State { }

        /// <summary>
        /// Zombie has run out of life and entering the dying animation.
        /// </summary>
        [Animation(ZombieDyingAnimState, 0.35f, true)]
        [TransitionOnAnimationComplete(typeof(DeadState))]
        [TransitionFromAnyState(typeof(PlayerDeathEvent))]
        public class DyingState : State { }

        /// <summary>
        /// Zombie is dead (really dead) and lying on the ground.
        /// </summary>
        [Animation(ZombieDeadAnimState, 0.35f, true)]
        public class DeadState : State { }

        public override void Start()
        {
            gameObject.AddComponent<ReviveEventManager>();
            AttachedAnimator = GetComponentInChildren<Animator>();
            navMeshAgent = GetComponent<NavMeshAgent>();

            IDamageable damageable = GetComponent<IDamageable>();
            damageable.OnDamageEvent += OnDamage;

            base.Start();
        }

        /// <summary>
        /// Stop zombie movement.
        /// </summary>
        public void StopMovement()
        {
            navMeshAgent.ResetPath();
        }

        /// <summary>
        /// Chase toward the current zombie target
        /// </summary>
        public void ChaseTarget()
        {
            if (zombieTarget == null)
            {
                RaiseEvent(new TargetLostEvent());
            }

            navMeshAgent.SetDestination(zombieTarget.transform.position);
            navMeshAgent.speed = chaseSpeed;

            // If the target is more than lose aggro distance away, lose the target
            if (Vector3.Distance(transform.position, zombieTarget.transform.position) >= loseAggroDistance)
            {
                zombieTarget = null;
                RaiseEvent(new TargetLostEvent());
            }
        }

        public void OnDamage(object source, OnDamagedEvent onDamagedEvent)
        {
            if (!IsServer)
            {
                return;
            }

            if (onDamagedEvent.damageEvent.type == Interactive.Health.EventType.Damage)
            {
                base.RaiseEvent(OnHitEvent.Instance);
            }
        }

        public void RoamingMovement()
        {
            // Have the nav mesh agent move towards the target
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                roamHeading,
                navMeshAgent.angularSpeed * Time.deltaTime);
            navMeshAgent.Move(transform.forward * roamSpeed * Time.deltaTime);

            // Reduce time roaming
            roamRemainingTime -= Time.deltaTime;

            // If time is less than zero, return to idle state
            if (roamRemainingTime <= 0)
            {
                RaiseEvent(new StopRoamEvent());
            }

            // Check if something interesting has appeared nearby to eat.
            CheckForTargets();
        }

        /// <summary>
        /// Zombie action when Idle
        /// </summary>
        public void IdleZombieAction()
        {
            CheckForTargets();
            CheckRoaming();
        }

        /// <summary>
        /// When the zombie finds something to chase.
        /// </summary>
        public void OnStartChase(IEvent evt)
        {
            if (evt is TargetIdentifiedEvent targetIdentifiedEvent)
            {
                zombieTarget = targetIdentifiedEvent.target;
            }
        }

        /// <summary>
        /// check if the player might start roaming.
        /// </summary>
        public void CheckRoaming()
        {
            timeToNextRoam -= Time.deltaTime;
            if (timeToNextRoam <= 0)
            {
                RaiseEvent(new StartRoamEvent(
                    UnityEngine.Random.Range(minRoamTime, maxRoamTime),
                    Quaternion.Euler(0, UnityEngine.Random.Range(0, 360), 0)));
            }
        }

        /// <summary>
        /// Check if the zombie sees something it wants to munch.
        /// </summary>
        public void CheckForTargets()
        {
            GameObject target = FindTarget(aggroDistance);

            if (target != null)
            {
                RaiseEvent(new TargetIdentifiedEvent(target));
            }
        }

        public void OnStartIdleState()
        {
            // Check if we're already chasing something
            CheckPersistedTarget();

            // Setup time to next roam
            timeToNextRoam = Random.Range(0.0f, timeBetweenRoaming);
        }

        /// <summary>
        /// Check if the zombie was chasing something.
        /// </summary>
        public void CheckPersistedTarget()
        {
            if (zombieTarget != null)
            {
                // Ignore targets who are dead
                if (zombieTarget.GetComponent<IDamageable>() is IDamageable damageable)
                {
                    if (!damageable.IsAlive())
                    {
                        return;
                    }
                }

                RaiseEvent(new TargetIdentifiedEvent(zombieTarget));
            }
        }

        /// <summary>
        /// Start the zombie roaming towards an arbitrary target.
        /// </summary>
        /// <param name="evt">Event to move the zombie.</param>
        public void StartRoaming(IEvent evt)
        {
            if (evt is StartRoamEvent startRoam)
            {
                roamHeading = startRoam.heading;
                roamRemainingTime = startRoam.roamTime;
            }
        }

        /// <summary>
        /// Find something for the zombie to munch on.
        /// </summary>
        /// <param name="searchDist">Distance zombie is willing to search for a target.</param>
        /// <returns>Game object that the zombie wants to munch on.</returns>
        public GameObject FindTarget(float searchDist)
        {
            float closestDist = Mathf.Infinity;
            GameObject closest = null;

            foreach (GameObject target in GameObject.FindGameObjectsWithTag("Player"))
            {
                float dist = Vector3.Distance(target.transform.position, transform.position);
                
                // Ignore targets farther than aggro distance away
                if (dist > searchDist)
                {
                    continue;
                }

                // Ignore targets who are dead
                if (target.GetComponent<IDamageable>() is IDamageable damageable)
                {
                    if (!damageable.IsAlive())
                    {
                        continue;
                    }
                }

                // Select the closest target
                if (dist <= closestDist)
                {
                    closestDist = dist;
                    closest = target;
                }
            }

            return closest;
        }
    }
}