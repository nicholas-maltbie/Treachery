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

using System.Linq;
using nickmaltbie.Treachery.Equipment;
using Unity.Netcode;
using UnityEngine;

namespace nickmaltbie.Treachery.Interactive.Item
{
    public class PickupItem : MonoBehaviour, IInteractive
    {
        public IEquipment Equipment { get; set; }

        public Sprite InteractiveIcon => Equipment.ItemIcon;

        public string ObjectName => Equipment.ItemName;
        public bool grabbed = false;

        public string InteractionText => $"Pickup item {Equipment.ItemName}";

        public bool CanInteract(GameObject source)
        {
            return source.GetComponent<PlayerLoadout>() is PlayerLoadout;
        }

        public void OnInteract(GameObject source)
        {
            if (source.GetComponent<PlayerLoadout>() is PlayerLoadout loadout)
            {
                if (loadout.CurrentLoadout.HasSpace(Equipment))
                {
                    loadout.RequestPickupItemServerRpc(GetComponent<NetworkObject>());
                }
                else
                {
                    loadout.RequestSwapItemServerRpc(GetComponent<NetworkObject>());
                }
            }
        }

        public void PickupObjectFromLoadout(NetworkBehaviourReference playerLoadout, bool swap = false)
        {
            PlayerLoadout loadout = null;
            if (grabbed)
            {
                return;
            }
            else if (!playerLoadout.TryGet(out loadout))
            {
                return;
            }
            else if (!swap && !loadout.CurrentLoadout.HasSpace(Equipment))
            {
                return;
            }

            // Set item as grabbed and destroy this item when possible.
            grabbed = true;
            var clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] { loadout.OwnerClientId }
                }
            };

            // Check if we should swap instead
            if (!loadout.CurrentLoadout.HasSpace(Equipment) && swap)
            {
                // Have the player drop their current items to make space
                // for this new item.
                ItemType[] toSwap = loadout.CurrentLoadout.RequiredToSwap(Equipment.EquipmentId).ToArray();
                loadout.SwapItemClientRpc(toSwap, Equipment.EquipmentId, loadout.CurrentSelected, clientRpcParams);
            }
            else
            {
                // Just send the grab event to the owner.
                loadout.EquipItemClientRpc(Equipment.EquipmentId, loadout.CurrentSelected, clientRpcParams);
            }

            GetComponent<NetworkObject>().Despawn();
        }

        public void SetFocusState(GameObject source, bool state)
        {
            // Do nothing... for now
        }
    }
}
