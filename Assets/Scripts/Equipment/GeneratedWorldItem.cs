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

using nickmaltbie.Treachery.Interactive.Item;
using Unity.Netcode;
using UnityEngine;

namespace nickmaltbie.Treachery.Equipment
{
    [RequireComponent(typeof(PickupItem))]
    public class GeneratedWorldItem : NetworkBehaviour
    {
        [SerializeField]
        public EquipmentLibrary library;

        public void SetEquipment(int equipmentId)
        {
            this.equipmentId.Value = equipmentId;
        }

        private NetworkVariable<int> equipmentId = new NetworkVariable<int>(
            value: IEquipment.EmptyEquipmentId,
            readPerm: NetworkVariableReadPermission.Everyone,
            writePerm: NetworkVariableWritePermission.Server
        );

        private GameObject spawned;

        public void Start()
        {
            equipmentId.OnValueChanged += UpdateEquipment;
            UpdateEquipment(0, equipmentId.Value);
        }

        public void UpdateEquipment(int previous, int current)
        {
            if (spawned != null)
            {
                GameObject.Destroy(spawned);
                spawned = null;
            }

            if (current != IEquipment.EmptyEquipmentId)
            {
                IEquipment equip = library.GetEquipment(current);
                spawned = GameObject.Instantiate(equip.HeldPrefab, transform);
                spawned.transform.localPosition = Vector3.zero;
                spawned.transform.localRotation = Quaternion.identity;
                GetComponent<PickupItem>().Equipment = equip;
                equip.WorldShape.AttachCollider(gameObject);
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
        }
    }
}
