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

using System.Collections.Generic;
using nickmaltbie.StateMachineUnity;
using nickmaltbie.StateMachineUnity.Attributes;
using nickmaltbie.StateMachineUnity.Event;
using nickmaltbie.StateMachineUnity.netcode;
using nickmaltbie.Treachery.Interactive.Health;
using nickmaltbie.Treachery.Interactive.Hitbox;
using nickmaltbie.Treachery.Player;
using nickmaltbie.Treachery.Utils;
using UnityEngine;
using UnityEngine.AI;

using static nickmaltbie.Treachery.Enemy.Zombie.ZombieAnimations;
using static nickmaltbie.Treachery.Enemy.Zombie.ZombieEvents;

namespace nickmaltbie.Treachery.Enemy.Zombie
{
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(IDamageable))]
    public class ZombieEnemy : NetworkSMAnim, IDamageSource
    {
        /// <summary>
        /// Cooldown between notifications of nearby zombies in seconds.
        /// </summary>
        public const float notifyCooldown = 1.0f;

        /// <summary>
        /// Tag for what the zombie will target.
        /// </summary>
        public const string ZombieTargetTag = "Player";

        /// <summary>
        /// Physics layer for enemies.
        /// </summary>
        public static int EnemyLayerMask => 1 << LayerMask.NameToLayer("Enemy");

        /// <summary>
        /// All zombies spawned in game right now.
        /// </summary>
        public static LinkedList<ZombieEnemy> AllZombies = new LinkedList<ZombieEnemy>();

        /// <summary>
        /// Node for this zombie in the list of all zombies.
        /// </summary>
        private LinkedListNode<ZombieEnemy> myNode;

        /// <summary>
        /// Distance at which the zombie will start chasing the player.
        /// </summary>
        public float aggroDistance = 5.0f;

        /// <summary>
        /// Distance at which the zombie will forget about the player.
        /// </summary>
        public float loseAggroDistance = 20.0f;

        /// <summary>
        /// Notify zombies within this range when chasing an enemy.
        /// </summary>
        public float notifyRange = 10.0f;

        /// <summary>
        /// Chance a zombie will stumble towards players.
        /// </summary>
        public float probabilityRoamTowardsPlayer = 0.85f;

        /// <summary>
        /// Average time between when the zombie will start roaming.
        /// </summary>
        public float timeBetweenRoaming = 3.0f;

        /// <summary>
        /// Speed at which the zombie chases the player.
        /// </summary>
        public float chaseSpeed = 5.0f;

        /// <summary>
        /// Speed zombie moves while attacking.
        /// </summary>
        public float attackSpeed = 4.0f;

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
        /// Time between starting attack animation and
        /// attacking the player.
        /// </summary>
        public float timeToAttack = 0.25f;

        /// <summary>
        /// Distance from which zombie can attack a player.
        /// </summary>
        public float attackRange = 1.0f;

        /// <summary>
        /// Damage zombie deals during attack
        /// </summary>
        public float attackDamage = 5.0f;

        /// <summary>
        /// Minimum time between attacks.
        /// </summary>
        public float attackCooldown = 1.0f;

        /// <summary>
        /// Attack base transform to draw attack raycast from.
        /// </summary>
        private Transform attackBase;

        /// <summary>
        /// Last time the zombie attacked at.
        /// </summary>
        private float lastAttackTime = Mathf.NegativeInfinity;

        /// <summary>
        /// What the zombie is chasing.
        /// </summary>
        private GameObject zombieTarget;

        /// <summary>
        /// Is the zombie roaming towards a player.
        /// </summary>
        private bool roamingTowardsPlayer;

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
        /// Has the zombie attacked this animation.
        /// </summary>
        private bool attacked;

        /// <summary>
        /// Damageable object for the zombie.
        /// </summary>
        private IDamageable damageable;

        /// <summary>
        /// Animation state when chasing someone
        /// </summary>
        /// <value></value>
        public string ChaseAnimationState { get; private set; } = ZombieRunningAnimState;

        /// <summary>
        /// Source of damage for this game object.
        /// </summary>
        public GameObject Source => gameObject;

        /// <summary>
        /// Time it takes the zombie to jump over a gap.
        /// </summary>
        private float jumpLength = 1.0f;

        /// <summary>
        /// Time until the next notification
        /// </summary>
        private float timeToNotify = notifyCooldown;

        /// <summary>
        /// Zombie standing still and doing nothing.
        /// </summary>
        [InitialState]
        [OnEnterState(nameof(OnStartIdleState))]
        [Animation(ZombieIdleAnimState, 0.35f, true)]
        [OnUpdate(nameof(IdleZombieAction))]
        [Transition(typeof(StartRoamEvent), typeof(RoamingState))]
        [Transition(typeof(TargetIdentifiedEvent), typeof(ChaseState))]
        [Transition(typeof(OnHitEvent), typeof(HitStunState))]
        public class IdleState : State { }

        /// <summary>
        /// Zombie will randomly meander about and look for something to eat.
        /// </summary>
        [Animation(ZombieWalkingAnimState, 0.35f, true)]
        [Transition(typeof(ZombieAttackEvent), typeof(AttackState))]
        [Transition(typeof(JumpStartEvent), typeof(JumpingState))]
        [OnEnterState(nameof(StartRoaming))]
        [OnUpdate(nameof(RoamingMovement))]
        [OnExitState(nameof(ClearTarget))]
        [Transition(typeof(StopRoamEvent), typeof(IdleState))]
        [Transition(typeof(TargetIdentifiedEvent), typeof(ChaseState))]
        [Transition(typeof(OnHitEvent), typeof(HitStunState))]
        public class RoamingState : State { }

        /// <summary>
        /// Zombie has a target and is chasing after them.
        /// </summary>
        [DynamicAnimation(nameof(ChaseAnimationState), 0.25f, true)]
        [Transition(typeof(TargetIdentifiedEvent), typeof(ChaseState))]
        [Transition(typeof(ZombieAttackEvent), typeof(AttackState))]
        [Transition(typeof(TargetLostEvent), typeof(IdleState))]
        [Transition(typeof(JumpStartEvent), typeof(JumpingState))]
        [OnEnterState(nameof(OnStartChase))]
        [OnExitState(nameof(StopMovement))]
        [OnUpdate(nameof(ChaseTarget))]
        [Transition(typeof(OnHitEvent), typeof(HitStunState))]
        public class ChaseState : State { }

        /// <summary>
        /// Zombie is on top of their target and attempting to attack them.
        /// </summary>
        [Animation(ZombieAttackAnimState, 0.25f, true)]
        [Transition(typeof(OnHitEvent), typeof(HitStunState))]
        [TransitionOnAnimationComplete(typeof(IdleState), 0.35f, true)]
        [OnUpdate(nameof(ZombieAttackAction))]
        [OnEnterState(nameof(OnEnterAttackState))]
        [OnExitState(nameof(OnFinishAttack))]
        public class AttackState : State { }

        [Animation(ZombieJumpAnimState, 0.25f, true)]
        [Transition(typeof(JumpEndEvent), typeof(IdleState))]
        [OnUpdate(nameof(OnJump))]
        [OnEnterState(nameof(OnStartJump))]
        public class JumpingState : State { }

        /// <summary>
        /// Zombie is stunned after being hit
        /// </summary>
        [Animation(ZombieReactionHitAnimState, 0.05f, true, 0.1f)]
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

        private OffMeshLinkData jumpLink;

        public void Awake()
        {
            AttachedAnimator = GetComponentInChildren<Animator>();
            navMeshAgent = GetComponent<NavMeshAgent>();
            damageable = GetComponent<IDamageable>();
            attackBase = AttachedAnimator.GetBoneTransform(HumanBodyBones.Chest);
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
        }

        public override void Start()
        {
            gameObject.AddComponent<ReviveEventManager>();
            damageable.OnDamageEvent += OnDamage;

            base.Start();
        }

        public override void Update()
        {
            if (IsServer)
            {
                navMeshAgent.enabled = damageable.IsAlive();
                navMeshAgent.stoppingDistance = Mathf.Max(0.5f, attackRange / 2);

                if (!damageable.IsAlive() &&
                    (CurrentState != typeof(DyingState) &&
                     CurrentState != typeof(DeadState)))
                {
                    RaiseEvent(PlayerDeathEvent.Instance);
                }

                if (navMeshAgent.enabled && navMeshAgent.isOnOffMeshLink)
                {
                    RaiseEvent(new JumpStartEvent(navMeshAgent.currentOffMeshLinkData));
                }
            }

            base.Update();
        }

        public void OnStartJump(IEvent jmp)
        {
            var jumpEvent = jmp as JumpStartEvent;
            jumpLink = jumpEvent.linkData;

            deltaTimeInCurrentState = 0.0f;
            jumpLength = Mathf.Min(1.0f, Vector3.Distance(jumpLink.startPos, jumpLink.endPos) / 2.0f);
        }

        public override void OnEnable()
        {
            base.OnEnable();
            myNode = AllZombies.AddFirst(this);
        }

        public override void OnDisable()
        {
            base.OnDisable();
            AllZombies.Remove(myNode);
        }

        public void OnJump()
        {
            if (!IsServer)
            {
                return;
            }

            // lerp from link start to link end in time to animation
            float tlerp = MathUtils.SmoothValue(Mathf.Clamp(deltaTimeInCurrentState / jumpLength, 0, 1.0f));

            // straight line from startlink to endlink
            var newPos = Vector3.Lerp(jumpLink.startPos, jumpLink.endPos, tlerp);

            // add the 'hop'
            newPos.y += 1.0f * Mathf.Sin(Mathf.PI * tlerp);

            // Update transform position
            transform.position = newPos;

            if (tlerp >= 1.0f)
            {
                RaiseEvent(new JumpEndEvent());
            }
        }

        public void ClearTarget()
        {
            zombieTarget = null;
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
            if (!IsServer)
            {
                return;
            }

            if (zombieTarget == null)
            {
                RaiseEvent(new TargetLostEvent());
                return;
            }

            navMeshAgent.SetDestination(zombieTarget.transform.position);
            navMeshAgent.speed = chaseSpeed;

            float dist = Vector3.Distance(transform.position, zombieTarget.transform.position);

            // If the target is more than lose aggro distance away, lose the target
            if (dist >= loseAggroDistance)
            {
                zombieTarget = null;
                RaiseEvent(new TargetLostEvent());
            }

            // If the target is within attack range, then attack the player
            if (dist <= attackRange)
            {
                // set animation to just idle
                ChaseAnimationState = ZombieChaseIdleAnimState;

                // Rotate towards target
                RotateTowardsTarget();

                // Only allow the attack if it has been at least cooldown since previous attack
                if (Time.time >= lastAttackTime + attackCooldown)
                {
                    RaiseEvent(new ZombieAttackEvent());
                }
            }
            else
            {
                // maintain chase animation
                ChaseAnimationState = ZombieRunningAnimState;
            }

            // Notify other zombies within a notify radius that there
            // is fresh meat nearby
            timeToNotify -= Time.deltaTime;
            if (timeToNotify <= 0 && zombieTarget != null)
            {
                NotifyNearbyZombies();
                timeToNotify = UnityEngine.Random.Range(notifyCooldown, 2 * notifyCooldown);
            }

            // Check if there is a closer target
            GameObject closest = FindTarget(loseAggroDistance);
            if (closest != zombieTarget)
            {
                RaiseEvent(new TargetIdentifiedEvent(closest));
            }
        }

        /// <summary>
        /// Notify zombies within a notify radius.
        /// </summary>
        public void NotifyNearbyZombies()
        {
            foreach (ZombieEnemy zom in AllZombies)
            {
                if (Vector3.Distance(zom.transform.position, transform.position) <= notifyRange)
                {
                    if (zom.CurrentState == typeof(IdleState) || zom.CurrentState == typeof(RoamingState))
                    {
                        zom.RaiseEvent(new TargetIdentifiedEvent(zombieTarget));
                    }
                }
            }
        }

        /// <summary>
        /// When the zombie is damaged, react to the damage.
        /// </summary>
        /// <param name="source">Damage source for the event.</param>
        /// <param name="onDamagedEvent">Event for the damage.</param>
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

        /// <summary>
        /// When the zombie starts attacking.
        /// </summary>
        public void OnEnterAttackState()
        {
            attacked = false;
            navMeshAgent.speed = 0.0f;
        }

        /// <summary>
        /// Last time the player attacked at.
        /// </summary>
        public void OnFinishAttack()
        {
            lastAttackTime = Time.time;
        }

        private void RotateTowardsTarget()
        {
            if (zombieTarget == null)
            {
                return;
            }

            Vector3 look = Vector3.ProjectOnPlane(zombieTarget.transform.position - transform.position, Vector3.up).normalized;
            if (look.magnitude >= 0.001f)
            {
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    Quaternion.LookRotation(look, Vector3.up),
                    navMeshAgent.angularSpeed * Time.deltaTime);
            }
        }

        /// <summary>
        /// Action to run while in the attack state.
        /// </summary>
        public void ZombieAttackAction()
        {
            if (!IsServer)
            {
                return;
            }

            // Rotate towards target
            RotateTowardsTarget();

            // Still chase the target
            _ = Vector3.Distance(transform.position, zombieTarget.transform.position);
            navMeshAgent.speed = attackSpeed;
            navMeshAgent.SetDestination(zombieTarget.transform.position);

            if (base.deltaTimeInCurrentState >= timeToAttack && !attacked)
            {
                attacked = true;

                // Attack forward towards the target
                Vector3 attackStart = attackBase.transform.position;
                Vector3 attackTarget = zombieTarget.transform.position;

                if (zombieTarget.GetComponentInChildren<Animator>() is Animator anim)
                {
                    attackTarget = anim.GetBoneTransform(HumanBodyBones.Chest).transform.position;
                }

                Vector3 attackDir = (attackTarget - attackStart).normalized;
                IEnumerable<RaycastHit> hits = Physics.RaycastAll(attackStart, attackDir, attackRange * 3.0f, IHitbox.HitLayerMaskComputation, QueryTriggerInteraction.Collide);

                var hitbox = IHitbox.GetFirstValidHit(hits, damageable, out RaycastHit hit, out bool didHit, layerMaskIgnore: EnemyLayerMask);
                bool playerTarget = didHit && ((hitbox?.Source as Component)?.CompareTag(ZombieTargetTag) ?? false);
                if (didHit && hitbox != null && playerTarget)
                {
                    DamageEvent damageEvent = IHitbox.DamageEventFromHit(hit, hitbox, attackDamage, -attackDir, DamageType.Slashing);
                    damageEvent.damageSource = this;
                    NetworkDamageEvent.ProcessEvent(damageEvent);
                }
            }
        }

        /// <summary>
        /// Apply roaming movement for the player.
        /// </summary>
        public void RoamingMovement()
        {
            if (!IsServer)
            {
                return;
            }

            if (roamingTowardsPlayer && zombieTarget != null)
            {
                navMeshAgent.speed = roamSpeed;
                navMeshAgent.SetDestination(zombieTarget.transform.position);
            }
            else
            {
                // Have the nav mesh agent move towards the target
                RotateTowardsTarget();
                navMeshAgent.Move(roamHeading * Vector3.forward * roamSpeed * Time.deltaTime);
            }

            // Reduce time roaming
            roamRemainingTime -= Time.deltaTime;

            // If time is less than zero, return to idle state
            if (roamRemainingTime <= 0)
            {
                zombieTarget = null;
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
            if (IsServer)
            {
                CheckForTargets();
                CheckRoaming();
            }
        }

        /// <summary>
        /// When the zombie finds something to chase.
        /// </summary>
        public void OnStartChase(IEvent evt)
        {
            if (evt is TargetIdentifiedEvent targetIdentifiedEvent)
            {
                zombieTarget = targetIdentifiedEvent.target;
                timeToNotify = Random.Range(0.1f, 0.25f);
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

        /// <summary>
        /// Check current parameters when entering idle state.
        /// </summary>
        public void OnStartIdleState()
        {
            // Check if we're already chasing something
            CheckPersistedTarget();

            // Setup time to next roam
            timeToNextRoam = UnityEngine.Random.Range(timeBetweenRoaming, timeBetweenRoaming * 2.0f);
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
                roamingTowardsPlayer = Random.Range(0.0f, 1.0f) < probabilityRoamTowardsPlayer;
                if (roamingTowardsPlayer)
                {
                    zombieTarget = FindTarget(Mathf.Infinity);
                }
                else
                {
                    zombieTarget = null;
                }
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

            foreach (GameObject target in GameObject.FindGameObjectsWithTag(ZombieTargetTag))
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
