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
using nickmaltbie.Treachery.Player.Action;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace nickmaltbie.Treachery.Equipment
{
    public class PlayerLoadout : NetworkBehaviour
    {
        public AbstractActionBehaviour<PunchAttackAction> DefaultPrimaryAction => GetComponent<PunchActionBehaviour>();
        public AbstractActionBehaviour<BlockActorAction> DefaultSecondaryAction => GetComponent<BlockActionBehaviour>();

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

                // Configuration default item states based on held items.
                DefaultPrimaryAction.Action.SetActive(!CurrentLoadout.HasMain);
                DefaultSecondaryAction.Action.SetActive(!CurrentLoadout.HasOffhand);
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
