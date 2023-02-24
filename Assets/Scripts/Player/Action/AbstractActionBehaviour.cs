
using nickmaltbie.OpenKCC.Input;
using nickmaltbie.Treachery.Action;
using nickmaltbie.Treachery.Action.PlayerActions;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace nickmaltbie.Treachery.Player.Action
{
    [RequireComponent(typeof(IActionActor<PlayerAction>))]
    public abstract class AbstractActionBehaviour<TConditionalAction> : NetworkBehaviour where TConditionalAction : ActorConditionalAction<PlayerAction>
    {
        [SerializeField]
        public InputActionReference inputActionReference;

        [SerializeField]
        public float cooldown = 0.0f;

        [SerializeField]
        public float bufferTime = 0.05f;

        protected BufferedInput BufferedInput { get; private set; }
        private IActionActor<PlayerAction> _actor;

        public IActionActor<PlayerAction> Actor => _actor ??= GetComponent<IActionActor<PlayerAction>>();
        public TConditionalAction Action { get; private set; }

        public InputAction InputAction
        {
            get => BufferedInput.InputAction;
            set => BufferedInput.InputAction = value;
        }

        public abstract TConditionalAction SetupAction();
        public abstract void CleanupAction(TConditionalAction action);

        public virtual void OnValidate()
        {
            ValidateAction();
        }

        public virtual void Awake()
        {
            ValidateAction();
        }

        public void ValidateAction()
        {
            BufferedInput = new BufferedInput
            {
                inputActionReference = inputActionReference,
                cooldown = cooldown,
                bufferTime = bufferTime,
            };

            if (Action != null)
            {
                CleanupAction(Action);
            }

            Action = SetupAction();
        }

        public void Start()
        {
            if (IsOwner)
            {
                Action.Setup();
            }
        }

        public void Update()
        {
            if (IsOwner)
            {
                Action.Update();
            }
        }

        public void FixedUpdate()
        {
            if (IsOwner)
            {
                Action.AttemptIfPossible();
            }
        }
    }
}
