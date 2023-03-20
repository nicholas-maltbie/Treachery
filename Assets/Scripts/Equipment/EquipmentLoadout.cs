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
using System.Collections.Generic;
using nickmaltbie.Treachery.Action;
using nickmaltbie.Treachery.Action.PlayerActions;
using nickmaltbie.Treachery.Interactive.Stamina;
using Unity.Netcode;
using UnityEngine;

namespace nickmaltbie.Treachery.Equipment
{
    public struct NetworkEquipmentLoadout : INetworkSerializable, IEquatable<NetworkEquipmentLoadout>
    {
        public int mainItemId;
        public int offhandItemId;

        public bool Equals(NetworkEquipmentLoadout other)
        {
            return other.mainItemId == mainItemId && other.offhandItemId == offhandItemId;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref mainItemId);
            serializer.SerializeValue(ref offhandItemId);
        }

        public NetworkEquipmentLoadout(int mainItemId = IEquipment.EmptyEquipmentId, int offhandItemId = IEquipment.EmptyEquipmentId)
        {
            this.mainItemId = mainItemId;
            this.offhandItemId = offhandItemId;
        }
    }

    public class EquipmentLoadout
    {
        public bool activeState = false;
        public GameObject Main { get; private set; }
        public GameObject Offhand { get; private set; }

        public IEquipment MainItem => Main?.GetComponent<IEquipment>();
        public IEquipment OffhandItem => Offhand?.GetComponent<IEquipment>();

        public int MainItemId => MainItem?.EquipmentId ?? IEquipment.EmptyEquipmentId;
        public int OffhandItemId => OffhandItem?.EquipmentId ?? IEquipment.EmptyEquipmentId;

        public static implicit operator NetworkEquipmentLoadout(EquipmentLoadout loadout)
        {
            return new NetworkEquipmentLoadout()
            {
                mainItemId = loadout.MainItemId,
                offhandItemId = loadout.OffhandItemId,
            };
        }

        public bool HasMain => Main != null;
        public bool HasOffhand => Offhand != null;
        public bool HasOffhandAction => OffhandItem?.ItemAction != null;
        public bool CanEquipOffhand => !HasOffhand && (!HasMain || MainItem.Weight == EquipmentWeight.OneHanded);

        private Transform parent;
        private IActionActor<PlayerAction> actor;
        private IStaminaMeter stamina;
        private bool isOwner;

        public EquipmentLoadout(Transform parent, IActionActor<PlayerAction> actor, IStaminaMeter stamina, bool isOwner)
        {
            this.parent = parent;
            this.actor = actor;
            this.stamina = stamina;
            this.isOwner = isOwner;
        }

        public IEquipment GetItem(ItemType itemType)
        {
            switch (itemType)
            {
                case ItemType.Main:
                    return MainItem;
                case ItemType.Offhand:
                    return OffhandItem;
            }

            return null;
        }

        public IEnumerable<ItemType> RequiredToSwap(int equipmentId)
        {
            IEquipment equipment = EquipmentLibrary.Singleton.GetEquipment(equipmentId);

            if (HasSpace(equipment))
            {
                yield break;
            }

            if (equipment.ItemType == ItemType.Main)
            {
                if (HasMain)
                {
                    yield return ItemType.Main;
                }

                if (equipment.Weight == EquipmentWeight.TwoHanded)
                {
                    yield return ItemType.Offhand;
                }
            }
            else if (equipment.ItemType == ItemType.Offhand)
            {
                if (HasOffhand)
                {
                    yield return ItemType.Offhand;
                }

                if (MainItem != null && MainItem.Weight == EquipmentWeight.TwoHanded)
                {
                    yield return ItemType.Main;
                }
            }
        }

        public IEquipment RemoveItem(ItemType itemType)
        {
            switch (itemType)
            {
                case ItemType.Main:
                    IEquipment main = MainItem;
                    UpdateMainItem(IEquipment.EmptyEquipmentId);
                    Main = null;
                    return main;
                case ItemType.Offhand:
                    IEquipment offhand = OffhandItem;
                    UpdateOffhandItem(IEquipment.EmptyEquipmentId);
                    Offhand = null;
                    return offhand;
            }

            return null;
        }

        public bool EquipItem(int equipmentId)
        {
            IEquipment equipment = EquipmentLibrary.Singleton.GetEquipment(equipmentId);
            if (!HasSpace(equipment))
            {
                return false;
            }

            if (equipment.ItemType == ItemType.Main)
            {
                UpdateMainItem(equipmentId);
            }
            else if (equipment.ItemType == ItemType.Offhand)
            {
                UpdateOffhandItem(equipmentId);
            }

            return true;
        }

        public bool HasSpace(IEquipment equipment)
        {
            switch (equipment.ItemType)
            {
                case ItemType.Offhand:
                    return CanEquipOffhand;
                case ItemType.Main:
                    return !HasMain && (!HasOffhand || equipment.Weight == EquipmentWeight.OneHanded);
            }

            return false;
        }

        public void SetActive(bool state)
        {
            activeState = state;
            Main?.SetActive(state);
            Offhand?.SetActive(state);

            MainItem?.ItemAction?.SetActive(state);
            OffhandItem?.ItemAction?.SetActive(state);
        }

        public void UpdateItemPositions(EquipmentManager manager)
        {
            if (Main != null)
            {
                Main.transform.SetParent(manager.GetMainHand);
                Main.transform.localPosition = Vector3.forward * 0.1f + Vector3.up * 0.1f;
                Main.transform.localRotation = Quaternion.identity;
            }

            if (Offhand != null)
            {
                Offhand.transform.SetParent(manager.GetOffHand);
                Offhand.transform.localPosition = Vector3.forward * 0.1f + Vector3.up * 0.1f;
                Offhand.transform.localRotation = Quaternion.identity;
            }
        }

        public void UpdateFromNetworkState(NetworkEquipmentLoadout loadout)
        {
            UpdateMainItem(loadout.mainItemId);
            UpdateOffhandItem(loadout.offhandItemId);
        }

        public void UpdateOffhandItem(int equipmentId)
        {
            if (Offhand != null && OffhandItem.EquipmentId != equipmentId)
            {
                GameObject.Destroy(Offhand);
                Offhand = null;
                MainItem?.SecondaryItemAction?.SetActive(!HasOffhandAction);
            }

            if (Offhand == null && equipmentId != IEquipment.EmptyEquipmentId && EquipmentLibrary.Singleton.HasEquipment(equipmentId))
            {
                GameObject prefab = EquipmentLibrary.Singleton.GetEquipment(equipmentId).HeldPrefab;
                Offhand = GameObject.Instantiate(prefab, parent);
                Offhand.SetActive(activeState);

                if (isOwner)
                {
                    OffhandItem?.SetupItemAction(actor, stamina);
                    OffhandItem?.ItemAction?.SetActive(activeState);
                    OffhandItem?.SecondaryItemAction?.SetActive(false);
                    MainItem?.SecondaryItemAction?.SetActive(!HasOffhandAction);
                }
            }
        }

        public void UpdateMainItem(int equipmentId)
        {
            if (Main != null && MainItem.EquipmentId != equipmentId)
            {
                GameObject.Destroy(Main);
                Main = null;
            }

            if (Main == null && equipmentId != IEquipment.EmptyEquipmentId && EquipmentLibrary.Singleton.HasEquipment(equipmentId))
            {
                GameObject prefab = EquipmentLibrary.Singleton.GetEquipment(equipmentId).HeldPrefab;
                Main = GameObject.Instantiate(prefab, parent);
                Main.SetActive(activeState);

                if (isOwner)
                {
                    MainItem?.SetupItemAction(actor, stamina);
                    MainItem?.ItemAction?.SetActive(activeState);
                    MainItem?.SecondaryItemAction?.SetActive(!HasOffhandAction);
                }
            }
        }
    }
}
