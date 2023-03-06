
using nickmaltbie.Treachery.Equipment;
using Unity.Netcode;
using UnityEngine;

namespace nickmaltbie.Treachery.Interactive.Item
{
    public class PickupItem : NetworkBehaviour, IInteractive
    {
        [SerializeField]
        public GameObject _equipment;

        public IEquipment Equipment => _equipment.GetComponent<IEquipment>();

        public Sprite InteractiveIcon => Equipment.ItemIcon;

        public string ObjectName => Equipment.ItemName;
        public bool grabbed = false;

        public string InteractionText => $"Pickup item {Equipment.ItemName}";

        public void OnValidate()
        {
            if (_equipment != null && _equipment.GetComponent<IEquipment>() == null)
            {
                _equipment = null;
                Debug.Log("Equipment must have an IEquipment component attached");
            }
        }

        public bool CanInteract(GameObject source)
        {
            if (source.GetComponent<PlayerLoadout>() is PlayerLoadout loadout)
            {
                return loadout.CurrentLoadout.CanEquip(Equipment);
            }

            return false;
        }

        public void OnInteract(GameObject source)
        {
            if (source.GetComponent<PlayerLoadout>() is PlayerLoadout loadout)
            {
                if (!loadout.CurrentLoadout.CanEquip(Equipment))
                {
                    return;
                }

                loadout.RequestPickupItemServerRpc(GetComponent<NetworkObject>());
            }
        }

        public void PickupObjectFromLoadout(NetworkBehaviourReference playerLoadout)
        {
            if (grabbed == true)
            {
                return;
            }
            else if (!playerLoadout.TryGet(out PlayerLoadout loadout))
            {
                return;
            }
            else if (!loadout.CurrentLoadout.CanEquip(Equipment))
            {
                return;
            }
            else
            {
                // Set item as grabbed and destroy this item when possible.
                grabbed = true;
                ClientRpcParams clientRpcParams = new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new ulong[] {loadout.OwnerClientId}
                    }
                };

                // Just send the grab event to the owner.
                loadout.EquipItemClientRpc(Equipment.EquipmentId, loadout.CurrentSelected, clientRpcParams);
                GetComponent<NetworkObject>().Despawn();
            }
        }

        public void SetFocusState(GameObject source, bool state)
        {
            // Do nothing... for now
        }
    }
}