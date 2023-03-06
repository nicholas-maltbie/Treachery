
using Unity.Netcode;
using UnityEngine;

namespace nickmaltbie.Treachery.Interactive.Item
{
    public class PickupItem : NetworkBehaviour, IInteractive
    {
        public Sprite InteractiveIcon => throw new System.NotImplementedException();

        public string ObjectName => throw new System.NotImplementedException();

        public string InteractionText => throw new System.NotImplementedException();

        public bool CanInteract(GameObject source)
        {
            throw new System.NotImplementedException();
        }

        public void OnInteract(GameObject source)
        {
            throw new System.NotImplementedException();
        }

        public void SetFocusState(GameObject source, bool state)
        {
            throw new System.NotImplementedException();
        }
    }
}