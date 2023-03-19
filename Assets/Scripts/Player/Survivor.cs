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
using nickmaltbie.OpenKCC.netcode.Utils;
using nickmaltbie.OpenKCC.Utils;
using nickmaltbie.StateMachineUnity;
using nickmaltbie.StateMachineUnity.Attributes;
using nickmaltbie.StateMachineUnity.Event;
using nickmaltbie.StateMachineUnity.netcode;
using nickmaltbie.Treachery.Action;
using nickmaltbie.Treachery.Action.PlayerActions;
using nickmaltbie.Treachery.Animation.Control;
using nickmaltbie.Treachery.Interactive.Health;
using nickmaltbie.Treachery.Interactive.Stamina;
using nickmaltbie.Treachery.Player.Action;
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
    [RequireComponent(typeof(Damageable))]
    [RequireComponent(typeof(StaminaMeter))]
    [DefaultExecutionOrder(1000)]
    public class Survivor : NetworkSMAnim, IJumping, IDamageSource, IActionActor<PlayerAction>, IMovementActor
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
        /// Speed of player when blocking.
        /// </summary>
        [Tooltip("Speed of player when blocking")]
        [SerializeField]
        public float blockSpeed = 2.5f;

        /// <summary>
        /// Minimum delay between actions.
        /// </summary>
        public float minimumActionDelay = 1.0f;

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

        private KCCMovementEngine _movementEngine;

        /// <summary>
        /// Movement engine for controlling the kinematic character controller.
        /// </summary>
        protected KCCMovementEngine MovementEngine => _movementEngine ??= GetComponent<KCCMovementEngine>();

        /// <summary>
        /// Override move action for testing.
        /// </summary>
        private InputAction overrideMoveAction;

        /// <summary>
        /// Gets the move action associated with this kcc.
        /// </summary>
        public InputAction MoveAction
        {
            get => overrideMoveAction ?? moveActionReference?.action;
            set => overrideMoveAction = value;
        }

        /// <summary>
        /// Current velocity of the player.
        /// </summary>
        public Vector3 Velocity { get; protected set; }

        /// <summary>
        /// One who deals damage is this.
        /// </summary>
        public GameObject Source => gameObject;

        /// <summary>
        /// Previous non zero movement for the player.
        /// </summary>
        public Vector3 PreviousNonZeroMovement { get; protected set; }

        /// <summary>
        /// Previous non zero input movement from player input updated each frame.
        /// </summary>
        public Vector3 LastInputMovement { get; private set; }

        /// <inheritdoc/>
        public Vector3 CameraBase => (CameraControls as SurvivorCameraController)?.InitialCameraPosition + transform.position ?? transform.position;

        /// <inheritdoc/>
        public IManagedCamera Camera => CameraControls as IManagedCamera;

        [InitialState]
        [Animation(IdleAnimState, 0.35f, true)]
        [Transition(typeof(StartMoveInput), typeof(WalkingState))]
        [Transition(typeof(BlockStart), typeof(BlockIdleState))]
        [Transition(typeof(SteepSlopeEvent), typeof(SlidingState))]
        [Transition(typeof(LeaveGroundEvent), typeof(FallingState))]
        [BlockAction(PlayerAction.Roll)]
        public class IdleState : State { }

        [Animation(JumpAnimState, 0.1f, true)]
        [TransitionFromAnyState(typeof(JumpEvent))]
        [TransitionOnAnimationComplete(typeof(FallingState), 0.15f, true)]
        [AnimationTransition(typeof(GroundedEvent), typeof(LandingState), 0.35f, true, 0.25f)]
        [Transition(typeof(SteepSlopeEvent), typeof(SlidingState))]
        [MovementSettings(SpeedConfig = nameof(walkingSpeed))]
        [BlockAllAction]
        public class JumpState : State { }

        [Animation(LandingAnimState, 0.1f, true)]
        [TransitionOnAnimationComplete(typeof(IdleState), 0.25f, true)]
        [AnimationTransition(typeof(JumpEvent), typeof(JumpState), 0.35f, true)]
        [Transition(typeof(LeaveGroundEvent), typeof(FallingState))]
        [Transition(typeof(SteepSlopeEvent), typeof(SlidingState))]
        [MovementSettings(SpeedConfig = nameof(walkingSpeed))]
        [BlockAction(PlayerAction.Punch, PlayerAction.Block)]
        public class LandingState : State { }

        [Animation(WalkingAnimState, 0.1f, true)]
        [Transition(typeof(StopMoveInput), typeof(IdleState))]
        [Transition(typeof(BlockStart), typeof(BlockMoveState))]
        [Transition(typeof(SteepSlopeEvent), typeof(SlidingState))]
        [Transition(typeof(LeaveGroundEvent), typeof(FallingState))]
        [Transition(typeof(StartSprintEvent), typeof(SprintingState))]
        [MovementSettings(SpeedConfig = nameof(walkingSpeed))]
        public class WalkingState : State { }

        [Animation(SprintingAnimState, 0.1f, true)]
        [Transition(typeof(StopMoveInput), typeof(IdleState))]
        [Transition(typeof(BlockStart), typeof(BlockMoveState))]
        [Transition(typeof(SteepSlopeEvent), typeof(SlidingState))]
        [Transition(typeof(LeaveGroundEvent), typeof(FallingState))]
        [Transition(typeof(StopSprintEvent), typeof(WalkingState))]
        [MovementSettings(SpeedConfig = nameof(sprintSpeed))]
        public class SprintingState : State { }

        [Animation(IdleAnimState, 0.1f, true)]
        [Transition(typeof(StartMoveInput), typeof(BlockMoveState))]
        [Transition(typeof(BlockStop), typeof(IdleState))]
        [BlockEnabled]
        [DamageMultiplier(multiplier = 0.5f)]
        public class BlockIdleState : State { }

        [Animation(WalkingAnimState, 0.1f, true)]
        [Transition(typeof(StopMoveInput), typeof(BlockIdleState))]
        [Transition(typeof(BlockStop), typeof(WalkingState))]
        [BlockEnabled]
        [MovementSettings(SpeedConfig = nameof(blockSpeed))]
        [DamageMultiplier(multiplier = 0.5f)]
        public class BlockMoveState : State { }

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
        [BlockAllAction]
        public class FallingState : State { }

        [Animation(LongFallingAnimState, 0.1f, true)]
        [Transition(typeof(SteepSlopeEvent), typeof(SlidingState))]
        [AnimationTransition(typeof(GroundedEvent), typeof(LandingState), 0.35f, true, 1.0f)]
        [MovementSettings(SpeedConfig = nameof(walkingSpeed))]
        [BlockAllAction]
        public class LongFallingState : State { }

        [Animation(DodgeAnimState, 0.1f, true)]
        [TransitionFromAnyState(typeof(DodgeStart))]
        [Transition(typeof(DodgeStop), typeof(IdleState))]
        [OnFixedUpdate(nameof(DodgeMovement))]
        [OnEnterState(nameof(SetAnimationInMoveDirection))]
        [LockMovementAnimation]
        [DamagePassthrough]
        [AllowAction(PlayerAction.Roll)]
        public class DodgeState : State { }

        [Animation(RollAnimState, 0.1f, true)]
        [TransitionFromAnyState(typeof(RollStart))]
        [Transition(typeof(RollStop), typeof(IdleState))]
        [OnFixedUpdate(nameof(RollMovement))]
        [OnEnterState(nameof(RotateTowardsMoveDirection))]
        [LockMovementAnimation]
        [DamagePassthrough]
        [BlockAllAction]
        public class RollState : State { }

        [Animation(BlockAnimState, 0.1f, true, 0.5f)]
        [BlockAllAction]
        public class GuardState : State { }

        [Animation(PunchingAnimState, 0.05f, true)]
        [TransitionOnAnimationComplete(typeof(IdleState), 0.35f, true)]
        [MovementSettings(SpeedConfig = nameof(attackSpeed))]
        [TransitionFromAnyState(typeof(PunchEvent))]
        [BlockAction(PlayerAction.Punch, PlayerAction.Roll)]
        [OnEnterState(nameof(RotateTowardsViewport))]
        public class PunchingState : State { }

        [Animation(DyingAnimState, 0.35f, true)]
        [TransitionFromAnyState(typeof(PlayerDeathEvent))]
        [TransitionOnAnimationComplete(typeof(DeadState))]
        [Transition(typeof(PlayerReviveEvent), typeof(RevivingState))]
        [BlockAllAction]
        public class DyingState : State { }

        [Animation(DeadAnimState, 0.35f, true)]
        [Transition(typeof(PlayerReviveEvent), typeof(RevivingState))]
        [Invulnerable]
        [BlockAllAction]
        public class DeadState : State { }

        [Animation(RevivingAnimState, 1.0f, true)]
        [TransitionOnAnimationComplete(typeof(IdleState), 0.35f)]
        [Invulnerable]
        [BlockAllAction]
        public class RevivingState : State { }

        /// <summary>
        /// Update the grounded state of the kinematic character controller.
        /// </summary>
        public void UpdateGroundedState()
        {
            if (MovementEngine.GroundedState.Falling)
            {
                RaiseEvent(LeaveGroundEvent.Instance);
            }
            else if (MovementEngine.GroundedState.Sliding)
            {
                RaiseEvent(SteepSlopeEvent.Instance);
            }
            else if (MovementEngine.GroundedState.StandingOnGround)
            {
                RaiseEvent(GroundedEvent.Instance);
            }
        }

        public void Awake()
        {
            gameObject.AddComponent<ReviveEventManager>();
        }

        /// <summary>
        /// Configure kcc state machine operations.
        /// </summary>
        public override void Start()
        {
            base.Start();

            GetComponent<Rigidbody>().isKinematic = true;
            SetupInputs();

            MoveAction?.Enable();
        }

        /// <summary>
        /// Setup inputs for the KCC
        /// </summary>
        public void SetupInputs()
        {
            if (IsOwner)
            {
                MoveAction?.Enable();
            }
        }

        public override void LateUpdate()
        {
            transform.position += MovementEngine.ColliderCast.PushOutOverlapping(
                transform.position,
                transform.rotation,
                100 * unityService.deltaTime,
                layerMask: MovementEngine.LayerMask);
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
            Vector3 projectedMovement = MovementEngine.GetProjectedMovement(rotatedMovement);
            float speed = MovementSettingsAttribute.GetSpeed(CurrentState, this);
            Vector3 scaledMovement = projectedMovement * speed;
            return scaledMovement;
        }

        public override void FixedUpdate()
        {
            GetComponent<Rigidbody>().isKinematic = true;

            if (IsOwner && !BlockMovement.IsMovementBlocked(CurrentState))
            {
                Vector3 movement = GetDesiredMovement();
                MovementEngine.MovePlayer(
                    movement * unityService.fixedDeltaTime,
                    Velocity * unityService.fixedDeltaTime);
                
                if (movement.magnitude >= KCCUtils.Epsilon)
                {
                    PreviousNonZeroMovement = movement;
                    LastInputMovement = InputMovement;
                }
            }

            UpdateGroundedState();
            GetComponent<NetworkRelativeTransform>()?.UpdateState(MovementEngine.RelativeParentConfig);

            // Apply gravity if needed
            if (MovementEngine.GroundedState.Falling || MovementEngine.GroundedState.Sliding)
            {
                Velocity += Physics.gravity * unityService.fixedDeltaTime;
            }
            else if (MovementEngine.GroundedState.StandingOnGround)
            {
                Velocity = Vector3.zero;
            }

            Damageable damageable = GetComponent<Damageable>();
            InvulnerableAttribute.UpdateDamageableState(CurrentState, damageable);
            DamagePassthroughAttribute.UpdateDamageableState(CurrentState, damageable);
            base.FixedUpdate();
        }

        /// <summary>
        /// Teleport player to a given position.
        /// </summary>
        /// <param name="position">Position to teleport player to.</param>
        public void TeleportPlayer(Vector3 position)
        {
            MovementEngine.TeleportPlayer(position);
        }

        /// <inheritdoc/>
        public override void Update()
        {
            if (IsOwner)
            {
                ReadPlayerInput();
            }

            BlockEnabledAttribute.UpdateBlockState(CurrentState, gameObject);
            DamageMultiplierAttribute.UpdateDamageMultiplier(CurrentState, gameObject);
            base.Update();
        }

        /// <inheritdoc/>
        public void ApplyJump(Vector3 velocity)
        {
            if (IsOwner)
            {
                Velocity = velocity + MovementEngine.GetGroundVelocity();
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

            bool locked = LockMovementAnimationAttribute.IsMovementAnimationLocked(CurrentState);
            GetComponent<RotateTowardsMovement>().Locked = locked;
            if (!locked)
            {
                GetComponent<RotateTowardsMovement>().UpdateAnimationState(HorizPlaneView * InputMovement);
            }

            bool moving = InputMovement.magnitude >= KCCUtils.Epsilon;
            IEvent moveEvent = moving ? StartMoveInput.Instance as IEvent : StopMoveInput.Instance as IEvent;
            RaiseEvent(moveEvent);
        }

        public void SetAnimationInMoveDirection()
        {
            GetComponent<RotateTowardsMovement>().UpdateAnimationState(HorizPlaneView * InputMovement, false);
        }

        public void DodgeMovement()
        {
            FixedMovementAction dodgeAction = GetComponent<DodgeActionBehaviour>()?.Action;
            Vector3 dodgeMovement = (dodgeAction?.MoveDirection ?? Vector3.zero) * unityService.fixedDeltaTime;
            MovementEngine.MovePlayer(
                dodgeMovement,
                Velocity * unityService.fixedDeltaTime);
        }

        public void RotateTowardsMoveDirection()
        {
            DodgeActionBehaviour dodge = GetComponent<DodgeActionBehaviour>();
            dodge.Action.Interrupt();
            RollActionBehaviour roll = GetComponent<RollActionBehaviour>();
            GetComponent<RotateTowardsMovement>().SetOverrideTargetHeading(roll.RollRotation, 360 * 3);
        }

        public void RotateTowardsViewport()
        {
            GetComponent<RotateTowardsMovement>().SetOverrideTargetHeading(HorizPlaneView.eulerAngles.y, 360 * 3);
        }

        public void RollMovement()
        {
            FixedMovementAction rollAction = GetComponent<RollActionBehaviour>()?.Action;
            Vector3 movement = (rollAction?.MoveDirection ?? Vector3.zero) * unityService.fixedDeltaTime;
            MovementEngine.MovePlayer(
                movement,
                Velocity * unityService.fixedDeltaTime);
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

        /// <inheritdoc/>
        public Vector3 LastDesiredMovement()
        {
            return PreviousNonZeroMovement;
        }
    }
}
