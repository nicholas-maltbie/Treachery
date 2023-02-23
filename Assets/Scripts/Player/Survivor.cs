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
using nickmaltbie.OpenKCC.CameraControls;
using nickmaltbie.OpenKCC.Character;
using nickmaltbie.OpenKCC.Character.Attributes;
using nickmaltbie.OpenKCC.Character.Config;
using nickmaltbie.OpenKCC.Character.Events;
using nickmaltbie.OpenKCC.Input;
using nickmaltbie.OpenKCC.netcode.Utils;
using nickmaltbie.OpenKCC.Utils;
using nickmaltbie.StateMachineUnity;
using nickmaltbie.StateMachineUnity.Attributes;
using nickmaltbie.StateMachineUnity.Event;
using nickmaltbie.StateMachineUnity.netcode;
using nickmaltbie.Treachery.Action;
using nickmaltbie.Treachery.Action.PlayerActions;
using nickmaltbie.Treachery.Interactive.Health;
using nickmaltbie.Treachery.Player.Action;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

using static nickmaltbie.Treachery.Player.PlayerAnimStates;

namespace nickmaltbie.Treachery.Player
{
    /// <summary>
    /// Have a character controller push any dynamic rigidbody it hits
    /// </summary>
    [RequireComponent(typeof(KCCMovementEngine))]
    [RequireComponent(typeof(Rigidbody))]
    [DefaultExecutionOrder(1000)]
    public class Survivor : NetworkSMAnim, IJumping, IDamageListener, IDamageSource, IActionActor<PlayerAction>, IMovementActor
    {
        public class BlockMovement : Attribute
        {
            public static bool IsMovementBlocked(Type type)
            {
                return Attribute.GetCustomAttribute(type, typeof(BlockMovement)) is BlockMovement;
            }
        }

        public class LockMovementAnimationAttribute : Attribute
        {
            public static bool IsMovementAnimationLocked(Type type)
            {
                return Attribute.GetCustomAttribute(type, typeof(LockMovementAnimationAttribute)) is LockMovementAnimationAttribute;
            }
        }

        [Header("Input Controls")]

        /// <summary>
        /// Action reference for moving the player.
        /// </summary>
        [Tooltip("Action reference for moving the player")]
        [SerializeField]
        public InputActionReference moveActionReference;

        /// <summary>
        /// Action reference for sprinting.
        /// </summary>
        [Tooltip("Action reference for moving the player")]
        [SerializeField]
        public InputActionReference sprintActionReference;

        /// <summary>
        /// Action reference for sprinting.
        /// </summary>
        [Tooltip("Action reference for player jumping")]
        [SerializeField]
        public InputActionReference jumpActionReference;

        /// <summary>
        /// Action reference for attacking.
        /// </summary>
        [Tooltip("Action reference for player attack move")]
        [SerializeField]
        public InputActionReference attackActionReference;

        [Header("Movement Settings")]

        /// <summary>
        /// Speed of player movement when walking.
        /// </summary>
        [Tooltip("Speed of player when walking")]
        [SerializeField]
        public float walkingSpeed = 7.5f;

        /// <summary>
        /// Speed of player movement when attacking.
        /// </summary>
        [Tooltip("Speed of player when attacking")]
        [SerializeField]
        public float attackSpeed = 3.5f;

        /// <summary>
        /// Speed of player when sprinting.
        /// </summary>
        [Tooltip("Speed of player when sprinting")]
        [SerializeField]
        public float sprintSpeed = 10.0f;

        /// <summary>
        /// Velocity of player jump.
        /// </summary>
        [Tooltip("Velocity of player jump.")]
        [SerializeField]
        public float jumpVelocity = 6.5f;

        /// <summary>
        /// Minimum delay between actions.
        /// </summary>
        public float minimumActionDelay = 1.0f;

        /// <summary>
        /// Action reference for jumping.
        /// </summary>
        private JumpActorAction jumpAction;

        /// <summary>
        /// Punch attack for the player.
        /// </summary>
        private PunchAttackAction punchAttack;

        /// <summary>
        /// Source of player viewpoint for attacks
        /// </summary>
        public Transform viewSource;

        /// <summary>
        /// Camera controls associated with the player.
        /// </summary>
        protected ICameraControls _cameraControls;

        /// <summary>
        /// Get the camera controls associated with the state machine.
        /// </summary>
        public ICameraControls CameraControls { get => _cameraControls ??= GetComponent<ICameraControls>(); internal set => _cameraControls = value; }

        /// <summary>
        /// Rotation of the plane the player is viewing
        /// </summary>
        private Quaternion HorizPlaneView => CameraControls != null ?
            CameraControls.PlayerHeading :
            Quaternion.Euler(0, transform.eulerAngles.y, 0);

        /// <summary>
        /// Input movement from player input updated each frame.
        /// </summary>
        public Vector3 InputMovement { get; private set; }

        /// <summary>
        /// Movement engine for controlling the kinematic character controller.
        /// </summary>
        protected KCCMovementEngine movementEngine;

        /// <summary>
        /// Override move action for testing.
        /// </summary>
        private InputAction overrideMoveAction;

        /// <summary>
        /// Override move action for testing.
        /// </summary>
        private InputAction overrideSprintAction;

        /// <summary>
        /// Override attack action for testing.
        /// </summary>
        private InputAction overrideAttackAction;

        /// <summary>
        /// Gets the move action associated with this kcc.
        /// </summary>
        public InputAction MoveAction
        {
            get => overrideMoveAction ?? moveActionReference?.action;
            set => overrideMoveAction = value;
        }

        /// <summary>
        /// Gets the move action associated with this kcc.
        /// </summary>
        public InputAction SprintAction
        {
            get => overrideSprintAction ?? sprintActionReference?.action;
            set => overrideSprintAction = value;
        }

        /// <summary>
        /// Gets the attack action associated with this player.
        /// </summary>
        public InputAction AttackAction
        {
            get => overrideAttackAction ?? attackActionReference?.action;
            set => overrideAttackAction = value;
        }

        /// <summary>
        /// Current velocity of the player.
        /// </summary>
        public Vector3 Velocity { get; protected set; }

        /// <summary>
        /// Animation movement for the player
        /// </summary>
        private NetworkVariable<Vector2> animationMove = new NetworkVariable<Vector2>(
            readPerm: NetworkVariableReadPermission.Everyone,
            writePerm: NetworkVariableWritePermission.Owner);

        /// <summary>
        /// One who deals damage is this.
        /// </summary>
        public GameObject Source => gameObject;

        [InitialState]
        [Animation(IdleAnimState, 0.35f, true)]
        [Transition(typeof(StartMoveInput), typeof(WalkingState))]
        [Transition(typeof(SteepSlopeEvent), typeof(SlidingState))]
        [Transition(typeof(LeaveGroundEvent), typeof(FallingState))]
        public class IdleState : State { }

        [Animation(JumpAnimState, 0.1f, true)]
        [TransitionFromAnyState(typeof(JumpEvent))]
        [TransitionOnAnimationComplete(typeof(FallingState), 0.15f, true)]
        [AnimationTransition(typeof(GroundedEvent), typeof(LandingState), 0.35f, true, 0.25f)]
        [Transition(typeof(SteepSlopeEvent), typeof(SlidingState))]
        [MovementSettings(SpeedConfig = nameof(walkingSpeed))]
        public class JumpState : State { }

        [Animation(LandingAnimState, 0.1f, true)]
        [TransitionOnAnimationComplete(typeof(IdleState), 0.25f, true)]
        [AnimationTransition(typeof(JumpEvent), typeof(JumpState), 0.35f, true)]
        [Transition(typeof(LeaveGroundEvent), typeof(FallingState))]
        [Transition(typeof(SteepSlopeEvent), typeof(SlidingState))]
        [MovementSettings(SpeedConfig = nameof(walkingSpeed))]
        [BlockAllAction]
        public class LandingState : State { }

        [Animation(WalkingAnimState, 0.1f, true)]
        [Transition(typeof(StopMoveInput), typeof(IdleState))]
        [Transition(typeof(SteepSlopeEvent), typeof(SlidingState))]
        [Transition(typeof(LeaveGroundEvent), typeof(FallingState))]
        [Transition(typeof(StartSprintEvent), typeof(SprintingState))]
        [MovementSettings(SpeedConfig = nameof(walkingSpeed))]
        public class WalkingState : State { }

        [Animation(SprintingAnimState, 0.1f, true)]
        [Transition(typeof(StopMoveInput), typeof(IdleState))]
        [Transition(typeof(SteepSlopeEvent), typeof(SlidingState))]
        [Transition(typeof(LeaveGroundEvent), typeof(FallingState))]
        [Transition(typeof(StopSprintEvent), typeof(WalkingState))]
        [MovementSettings(SpeedConfig = nameof(sprintSpeed))]
        public class SprintingState : State { }

        [Animation(SlidingAnimState, 0.35f, true)]
        [Transition(typeof(LeaveGroundEvent), typeof(FallingState))]
        [AnimationTransition(typeof(GroundedEvent), typeof(LandingState), 0.35f, true, 0.25f)]
        [MovementSettings(SpeedConfig = nameof(walkingSpeed))]
        public class SlidingState : State { }

        [Animation(FallingAnimState, 0.1f, true)]
        [Transition(typeof(SteepSlopeEvent), typeof(SlidingState))]
        [AnimationTransition(typeof(GroundedEvent), typeof(LandingState), 0.35f, true, 0.25f)]
        [TransitionAfterTime(typeof(LongFallingState), 2.0f)]
        [MovementSettings(SpeedConfig = nameof(walkingSpeed))]
        public class FallingState : State { }

        [Animation(LongFallingAnimState, 0.1f, true)]
        [Transition(typeof(SteepSlopeEvent), typeof(SlidingState))]
        [AnimationTransition(typeof(GroundedEvent), typeof(LandingState), 0.35f, true, 1.0f)]
        [MovementSettings(SpeedConfig = nameof(walkingSpeed))]
        public class LongFallingState : State { }

        [Animation(DodgeAnimState, 0.1f, true, 0.5f)]
        [TransitionFromAnyState(typeof(DodgeStart))]
        [Transition(typeof(DodgeStop), typeof(IdleState))]
        [OnFixedUpdate(nameof(DodgeMovement))]
        [OnEnterState(nameof(OnStartDodge))]
        [LockMovementAnimation]
        [BlockAllAction]
        public class DodgeState : State { }

        [Animation(BlockAnimState, 0.1f, true, 0.5f)]
        [BlockAllAction]
        public class GuardState : State { }

        [Animation(PunchingAnimState, 0.05f, true)]
        [TransitionOnAnimationComplete(typeof(IdleState), 0.35f, true)]
        [MovementSettings(SpeedConfig = nameof(attackSpeed))]
        [TransitionFromAnyState(typeof(PunchEvent))]
        [BlockAllAction]
        public class PunchingState : State { }

        [Animation(DyingAnimState, 0.35f, true)]
        [TransitionFromAnyState(typeof(PlayerDeath))]
        [TransitionOnAnimationComplete(typeof(DeadState))]
        [Transition(typeof(ReviveEvent), typeof(RevivingState))]
        [BlockAllAction]
        public class DyingState : State { }

        [Animation(DeadAnimState, 0.35f, true)]
        [Transition(typeof(ReviveEvent), typeof(RevivingState))]
        [BlockAllAction]
        public class DeadState : State { }

        [Animation(RevivingAnimState, 1.0f, true)]
        [TransitionOnAnimationComplete(typeof(IdleState), 0.35f)]
        [BlockAllAction]
        public class RevivingState : State { }

        /// <summary>
        /// Update the grounded state of the kinematic character controller.
        /// </summary>
        public void UpdateGroundedState()
        {
            if (movementEngine.GroundedState.Falling)
            {
                RaiseEvent(LeaveGroundEvent.Instance);
            }
            else if (movementEngine.GroundedState.Sliding)
            {
                RaiseEvent(SteepSlopeEvent.Instance);
            }
            else if (movementEngine.GroundedState.StandingOnGround)
            {
                RaiseEvent(GroundedEvent.Instance);
            }
        }

        public void Awake()
        {
            movementEngine = GetComponent<KCCMovementEngine>();
            jumpAction = new JumpActorAction(
                new BufferedInput()
                {
                    inputActionReference = jumpActionReference,
                    cooldown = 0.25f,
                    bufferTime = 0.05f,
                },
                this,
                movementEngine)
            {
                jumpVelocity = jumpVelocity,
                maxJumpAngle = 85.0f,
                jumpAngleWeightFactor = 0.0f,
            };
            jumpAction.OnPerform += (_, _) => ApplyJump(jumpVelocity * jumpAction.JumpDirection());

            punchAttack = new PunchAttackAction(
                new BufferedInput()
                {
                    inputActionReference = attackActionReference,
                    cooldown = 1.0f,
                    bufferTime = 0.05f,
                },
                this,
                GetComponent<Damageable>(),
                transform,
                CameraControls
            )
            {
                attackBaseOffset = viewSource.localPosition,
                coyoteTime = 0.0f,
            };
        }

        /// <summary>
        /// Configure kcc state machine operations.
        /// </summary>
        public override void Start()
        {
            base.Start();

            GetComponent<Rigidbody>().isKinematic = true;
            SetupInputs();

            SprintAction?.Enable();
            MoveAction?.Enable();
        }

        /// <summary>
        /// Setup inputs for the KCC
        /// </summary>
        public void SetupInputs()
        {
            if (IsOwner)
            {
                jumpAction?.Setup();
                punchAttack?.Setup();

                punchAttack.OnAttack += (e, attack) =>
                {
                    RaiseEvent(PunchEvent.Instance);

                    if (attack.target != null)
                    {
                        AttackServerRpc(NetworkAttackEvent.FromAttackEvent(attack, gameObject));
                    }
                };

                MoveAction?.Enable();
            }
        }

        public override void LateUpdate()
        {
            transform.position += movementEngine.ColliderCast.PushOutOverlapping(transform.position, transform.rotation, 100 * unityService.deltaTime);

            base.LateUpdate();
        }

        /// <summary>
        /// The the player's desired velocity for their current input value.
        /// </summary>
        /// <returns>Vector of player velocity based on input movement rotated by player view and projected onto the
        /// ground.</returns>
        public Vector3 GetDesiredMovement()
        {
            Vector3 rotatedMovement = HorizPlaneView * InputMovement;
            Vector3 projectedMovement = movementEngine.GetProjectedMovement(rotatedMovement);
            float speed = MovementSettingsAttribute.GetSpeed(CurrentState, this);
            Vector3 scaledMovement = projectedMovement * speed;
            return scaledMovement;
        }

        public override void FixedUpdate()
        {
            GetComponent<Rigidbody>().isKinematic = true;

            if (IsOwner)
            {
                jumpAction.AttemptIfPossible();
                punchAttack.AttemptIfPossible();

                if (!BlockMovement.IsMovementBlocked(CurrentState))
                {
                    movementEngine.MovePlayer(
                        GetDesiredMovement() * unityService.fixedDeltaTime,
                        Velocity * unityService.fixedDeltaTime);
                }
            }

            UpdateGroundedState();
            GetComponent<NetworkRelativeTransform>()?.UpdateState(movementEngine.RelativeParentConfig);

            // Apply gravity if needed
            if (movementEngine.GroundedState.Falling || movementEngine.GroundedState.Sliding)
            {
                Velocity += Physics.gravity * unityService.fixedDeltaTime;
            }
            else if (movementEngine.GroundedState.StandingOnGround)
            {
                Velocity = Vector3.zero;
            }

            base.FixedUpdate();
        }

        /// <summary>
        /// Teleport player to a given position.
        /// </summary>
        /// <param name="position">Position to teleport player to.</param>
        public void TeleportPlayer(Vector3 position)
        {
            movementEngine.TeleportPlayer(position);
        }

        /// <inheritdoc/>
        public override void Update()
        {
            if (IsOwner)
            {
                ReadPlayerInput();
            }

            AttachedAnimator.SetFloat("MoveX", animationMove.Value.x);
            AttachedAnimator.SetFloat("MoveY", animationMove.Value.y);

            base.Update();
        }

        /// <inheritdoc/>
        public void ApplyJump(Vector3 velocity)
        {
            if (IsOwner)
            {
                Velocity = velocity + movementEngine.GetGroundVelocity();
                RaiseEvent(JumpEvent.Instance);
            }
        }

        /// <summary>
        /// Read the current player input values.
        /// </summary>
        public void ReadPlayerInput()
        {
            bool denyMovement = PlayerInputUtils.playerMovementState == PlayerInputState.Deny;
            Vector2 moveVector = denyMovement ? Vector2.zero : MoveAction?.ReadValue<Vector2>() ?? Vector2.zero;
            InputMovement = new Vector3(moveVector.x, 0, moveVector.y);
            jumpAction.Update();
            punchAttack.Update();

            bool locked = LockMovementAnimationAttribute.IsMovementAnimationLocked(CurrentState);
            if (!locked)
            {
                UpdateAnimationState(moveVector);
            }

            bool moving = InputMovement.magnitude >= KCCUtils.Epsilon;
            IEvent moveEvent = moving ? StartMoveInput.Instance as IEvent : StopMoveInput.Instance as IEvent;
            RaiseEvent(moveEvent);

            if (moving)
            {
                if (SprintAction?.IsPressed() ?? false)
                {
                    RaiseEvent(StartSprintEvent.Instance);
                }
                else
                {
                    RaiseEvent(StopSprintEvent.Instance);
                }
            }
        }

        public void UpdateAnimationState(Vector3 moveVector, bool smooth = true)
        {
            float smoothValue = 4 * unityService.deltaTime;
            if (!smooth)
            {
                smoothValue = 1.0f;
            }

            float moveX = AttachedAnimator.GetFloat("MoveX");
            float moveY = AttachedAnimator.GetFloat("MoveY");
            moveX = Mathf.Lerp(moveX, moveVector.x, smoothValue);
            moveY = Mathf.Lerp(moveY, moveVector.y, smoothValue);
            animationMove.Value = new Vector2(moveX, moveY);
        }

        public void OnStartDodge()
        {
            Vector2 move = MoveAction?.ReadValue<Vector2>() ?? Vector2.zero;
            UpdateAnimationState(move, false);
        }

        public void DodgeMovement()
        {
            DodgeAction dodgeAction = GetComponent<DodgeActionBehaviour>()?.dodgeAction;
            Vector3 dodgeMovement = (dodgeAction?.DodgeDirection ?? Vector3.zero) * unityService.fixedDeltaTime;
            movementEngine.MovePlayer(
                dodgeMovement,
                Velocity * unityService.fixedDeltaTime);
        }

        [ServerRpc]
        public void AttackServerRpc(NetworkAttackEvent attack)
        {
            NetworkAttackEvent.ProcessEvent(attack);
        }

        public void OnDamage(IDamageable target, IDamageSource source, float previous, float current, float damage)
        {
            if (!IsLocalPlayer)
            {
                return;
            }

            if (previous > 0 && current == 0)
            {
                RaiseEvent(PlayerDeath.Instance);
            }
        }

        public void OnHeal(IDamageable target, IDamageSource source, float previous, float current, float amount)
        {
            if (!IsLocalPlayer)
            {
                return;
            }

            if (previous == 0 && current > 0)
            {
                RaiseEvent(ReviveEvent.Instance);
            }
        }

        public bool CanPerform(PlayerAction action)
        {
            if (!GetComponent<IDamageable>().IsAlive())
            {
                return false;
            }
            else if (PlayerInputUtils.playerMovementState == PlayerInputState.Deny)
            {
                return false;
            }
            else if (AbstractBlockActionAttribute.CheckBlocked(CurrentState, action))
            {
                return false;
            }

            return true;
        }
    }
}
