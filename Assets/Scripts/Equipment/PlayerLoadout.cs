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
using System.Linq;
using nickmaltbie.Treachery.Action;
using nickmaltbie.Treachery.Action.PlayerActions;
using nickmaltbie.Treachery.Interactive.Stamina;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

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
        public bool CahEquipOffhand => !HasOffhand && (!HasMain || MainItem.Weight == EquipmentWeight.OneHanded);

        private EquipmentLibrary library;
        private Transform parent;
        private IActionActor<PlayerAction> actor;
        private IStaminaMeter stamina;
        private bool isOwner;

        public EquipmentLoadout(EquipmentLibrary library, Transform parent, IActionActor<PlayerAction> actor, IStaminaMeter stamina, bool isOwner)
        {
            this.library = library;
            this.parent = parent;
            this.actor = actor;
            this.stamina = stamina;
            this.isOwner = isOwner;
        }

        public bool RemoveItem(ItemType itemType)
        {
            switch (itemType)
            {
                case ItemType.Main:
                    bool hasMain = Main != null;
                    UpdateMainItem(IEquipment.EmptyEquipmentId);
                    return hasMain;
                case ItemType.Offhand:
                    bool hasOffhand = Offhand != null;
                    UpdateOffhandItem(IEquipment.EmptyEquipmentId);
                    return hasOffhand;
            }

            return false;
        }

        public bool EquipItem(int equipmentId)
        {
            IEquipment equipment = library.GetEquipment(equipmentId);
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

            MainItem?.ItemAction.SetActive(state);
            OffhandItem?.ItemAction.SetActive(state);
        }

        public void UpdateItemPositions(EquipmentManager manager)
        {
            if (Main != null)
            {
                Main.transform.SetParent(manager.GetMainHand);
                Main.transform.localPosition = Vector3.zero;
                Main.transform.localRotation = Quaternion.identity;
            }

            if (Offhand != null)
            {
                Offhand.transform.SetParent(manager.GetOffHand);
                Offhand.transform.localPosition = Vector3.zero;
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
            }

            if (Offhand == null && equipmentId != IEquipment.EmptyEquipmentId && library.HasEquipment(equipmentId))
            {
                GameObject prefab = library.GetEquipment(equipmentId).HeldPrefab;
                Offhand = GameObject.Instantiate(prefab, parent);
                Offhand.SetActive(activeState);

                if (isOwner)
                {
                    OffhandItem?.SetupItemAction(actor, stamina);
                    OffhandItem?.ItemAction?.SetActive(activeState);
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

            if (Main == null && equipmentId != IEquipment.EmptyEquipmentId && library.HasEquipment(equipmentId))
            {
                GameObject prefab = library.GetEquipment(equipmentId).HeldPrefab;
                Main = GameObject.Instantiate(prefab, parent);
                Main.SetActive(activeState);

                if (isOwner)
                {
                    MainItem?.SetupItemAction(actor, stamina);
                    MainItem?.ItemAction?.SetActive(activeState);
                }
            }
        }
    }

    public class PlayerLoadout : NetworkBehaviour
    {
        [SerializeField]
        public int MaxLoadouts = 3;

        [SerializeField]
        public EquipmentLibrary library;

        [SerializeField]
        public EquipmentManager manager;

        public InputActionReference incrementLoadoutSelection;
        public InputActionReference decrementLoadoutSelection;
        public InputActionReference loadoutScroll;

        private EquipmentLoadout[] loadouts;
        private NetworkVariable<int> currentLoadout = new NetworkVariable<int>(
            value: 0,
            readPerm: NetworkVariableReadPermission.Everyone,
            writePerm: NetworkVariableWritePermission.Owner);
        private NetworkList<NetworkEquipmentLoadout> networkLoadouts;

        private KeyControl GetDigitKey(int index)
        {
            switch (index)
            {
                case 0: return Keyboard.current.digit0Key;
                case 1: return Keyboard.current.digit1Key;
                case 2: return Keyboard.current.digit2Key;
                case 3: return Keyboard.current.digit3Key;
                case 4: return Keyboard.current.digit4Key;
                case 5: return Keyboard.current.digit5Key;
                case 6: return Keyboard.current.digit6Key;
                case 7: return Keyboard.current.digit7Key;
                case 8: return Keyboard.current.digit8Key;
                case 9: return Keyboard.current.digit9Key;
            }

            return Keyboard.current.digit0Key;
        }

        public void Awake()
        {
            networkLoadouts = new NetworkList<NetworkEquipmentLoadout>(
                Enumerable.Range(0, MaxLoadouts).Select(_ => new NetworkEquipmentLoadout()),
                readPerm: NetworkVariableReadPermission.Everyone,
                writePerm: NetworkVariableWritePermission.Owner);
            currentLoadout.OnValueChanged += OnLoadoutSelected;
            networkLoadouts.OnListChanged += OnLoadoutModified;
        }

        public void Start()
        {
            loadouts = Enumerable.Range(0, MaxLoadouts).Select(_ => new EquipmentLoadout(
                library,
                transform,
                GetComponent<IActionActor<PlayerAction>>(),
                GetComponent<IStaminaMeter>(),
                IsOwner)).ToArray();
            loadouts[CurrentSelected].SetActive(true);

            incrementLoadoutSelection.action.Enable();
            decrementLoadoutSelection.action.Enable();
            loadoutScroll.action.Enable();

            for (int i = 0; i < MaxLoadouts; i++)
            {
                if (Keyboard.current != null)
                {
                    var selectSlot = new InputAction(
                        name: $"Select Slot {i}",
                        type: InputActionType.Button,
                        binding: GetDigitKey(i + 1).path,
                        interactions: "Press"
                    );

                    int selected = i;
                    selectSlot.performed += _ =>
                    {
                        ChangeSelectedLoadout(selected);
                    };

                    selectSlot.Enable();
                }
            }

            incrementLoadoutSelection.action.performed += _ => IncrementLoadout();
            decrementLoadoutSelection.action.performed += _ => DecrementLoadout();
            loadoutScroll.action.performed += (action) =>
            {
                float value = action.ReadValue<float>();
                if (value > 0)
                {
                    IncrementLoadout();
                }
                else
                {
                    DecrementLoadout();
                }
            };

            for (int i = 0; i < loadouts.Length; i++)
            {
                loadouts[i].UpdateFromNetworkState(networkLoadouts[i]);
                loadouts[i].SetActive(i == CurrentSelected);
                loadouts[i].UpdateItemPositions(manager);
            }
        }

        private void IncrementLoadout() => ChangeSelectedLoadout((CurrentSelected + 1) % MaxLoadouts);
        private void DecrementLoadout() => ChangeSelectedLoadout((CurrentSelected - 1 + MaxLoadouts) % MaxLoadouts);

        public EquipmentLoadout CurrentLoadout => loadouts[CurrentSelected];
        public EquipmentLoadout GetLoadout(int idx) => loadouts != null ?
            loadouts[idx] :
            new EquipmentLoadout(library, transform, GetComponent<IActionActor<PlayerAction>>(), GetComponent<IStaminaMeter>(), false);

        public int CurrentSelected => currentLoadout.Value;

        public void ChangeSelectedLoadout(int selected)
        {
            if (!IsOwner)
            {
                return;
            }

            currentLoadout.Value = selected;
        }

        public void RemoveItemFromLoadout(ItemType itemType, int slot)
        {
            if (IsOwner)
            {
                EquipmentLoadout loadout = loadouts[slot];
                loadout.RemoveItem(itemType);
            }
        }

        public void AddItemToLoadout(int equipmentId, int slot)
        {
            if (IsOwner)
            {
                EquipmentLoadout loadout = loadouts[slot];
                loadout.EquipItem(equipmentId);
                loadout.UpdateItemPositions(manager);
            }
        }

        public void SynchronizeLoadouts()
        {
            for (int i = 0; i < MaxLoadouts; i++)
            {
                if (!networkLoadouts[i].Equals(loadouts[i]))
                {
                    networkLoadouts[i] = loadouts[i];
                }
            }
        }

        public void Update()
        {
            if (IsOwner)
            {
                SynchronizeLoadouts();
                for (int i = 0; i < loadouts.Length; i++)
                {
                    ActorConditionalAction<PlayerAction> mainAction = loadouts[i].MainItem?.ItemAction;
                    ActorConditionalAction<PlayerAction> offhandAction = loadouts[i].OffhandItem?.ItemAction;
                    mainAction?.Update();
                    offhandAction?.Update();
                }
            }
        }

        public void OnLoadoutSelected(int previousLoadout, int currentLoadout)
        {
            EquipmentLoadout previous = loadouts[previousLoadout];
            EquipmentLoadout current = loadouts[currentLoadout];

            previous.SetActive(false);
            current.SetActive(true);
            current.UpdateItemPositions(manager);
        }

        public void OnLoadoutModified(NetworkListEvent<NetworkEquipmentLoadout> changeEvent)
        {
            EquipmentLoadout modified = loadouts[changeEvent.Index];
            modified.UpdateFromNetworkState(changeEvent.Value);
            modified.UpdateItemPositions(manager);
        }
    }
}
