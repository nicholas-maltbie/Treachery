
using nickmaltbie.OpenKCC.Input;
using nickmaltbie.Treachery.Action;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace nickmaltbie.Treachery.Player.Action
{
    public abstract class AbstractActionBehaviour<TAction> : NetworkBehaviour
    {
        [SerializeField]
        public InputActionReference inputActionReference;

        [SerializeField]
        public float cooldown = 0.0f;

        [SerializeField]
        public float bufferTime = 0.05f;

        protected BufferedInput bufferedInput;

        public InputAction InputAction
        {
            get => bufferedInput.InputAction;
            set => bufferedInput.InputAction = value;
        }

        public abstract ActorConditionalAction<TAction> Action { get; }

        public virtual void Awake()
        {
            bufferedInput = new BufferedInput
            {
                inputActionReference = inputActionReference,
                cooldown = cooldown,
                bufferTime = bufferTime,
            };
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
