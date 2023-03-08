
using System;
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
        public bool CahEquipOffhand => !HasOffhand && (!HasMain || MainItem.Weight == EquipmentWeight.OneHanded);

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

        public IEquipment RemoveItem(ItemType itemType)
        {
            switch (itemType)
            {
                case ItemType.Main:
                    IEquipment main = MainItem;
                    UpdateMainItem(IEquipment.EmptyEquipmentId);
                    return main;
                case ItemType.Offhand:
                    IEquipment offhand = OffhandItem;
                    UpdateOffhandItem(IEquipment.EmptyEquipmentId);
                    return offhand;
            }

            return null;
        }

        public bool EquipItem(int equipmentId)
        {
            IEquipment equipment = EquipmentLibrary.Singleton.GetEquipment(equipmentId);
            if (!CanEquip(equipment))
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

        public bool CanEquip(IEquipment equipment)
        {
            switch (equipment.ItemType)
            {
                case ItemType.Offhand:
                    return CahEquipOffhand;
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
