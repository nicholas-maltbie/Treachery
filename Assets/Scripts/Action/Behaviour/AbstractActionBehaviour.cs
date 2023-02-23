
using Unity.Netcode;

namespace nickmaltbie.Treachery.Action.Behaviour
{
    public abstract class AbstractActionBehaviour<TAction> : NetworkBehaviour
    {
        public abstract ActorConditionalAction<TAction> Action { get; }

        public void Setup()
        {
            Action?.Setup();
        }

        public void Update()
        {
            if (IsOwner)
            {
                Action?.Update();
            }
        }

        public void FixedUpdate()
        {
            if (IsOwner)
            {
                Action?.AttemptIfPossible();
            }
        }
    }
}