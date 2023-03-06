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

using nickmaltbie.Treachery.Action;
using nickmaltbie.Treachery.Action.PlayerActions;
using nickmaltbie.Treachery.Interactive.Stamina;
using UnityEngine;
using UnityEngine.InputSystem;

namespace nickmaltbie.Treachery.Equipment
{
    public abstract class AbstractEquipment : MonoBehaviour, IEquipment
    {
        private static System.Random random = new System.Random();

        [SerializeField]
        public int equipmentId = IEquipment.EmptyEquipmentId;

        [SerializeField]
        public GameObject itemPrefab;

        [SerializeField]
        public Sprite itemIcon;

        [SerializeField]
        public ItemType itemType = ItemType.Main;

        [SerializeField]
        public EquipmentWeight equipmentWeight;

        [SerializeField]
        public ItemActionLibrary itemActionLibrary;

        public bool InHand { get; private set; }

        public int EquipmentId => equipmentId;

        public GameObject HeldPrefab => itemPrefab;

        public Sprite ItemIcon => itemIcon;

        public ItemType ItemType => itemType;

        public EquipmentWeight Weight => equipmentWeight;

        public bool CanHold => true;

        public ActorConditionalAction<PlayerAction> ItemAction { get; protected set; }

        protected InputActionReference InputAction => itemActionLibrary.GetActionReference(ItemType);

        public string ItemName => gameObject.name;

        public virtual void SetupItemAction(IActionActor<PlayerAction> actor, IStaminaMeter stamina)
        {
            ItemAction = new ItemAction(InputAction, actor, stamina, this, ItemType == ItemType.Main ? PlayerAction.PrimaryItem : PlayerAction.OffhandItem);
            ItemAction.Setup();
        }

        public void OnValidate()
        {
            if (equipmentId == IEquipment.EmptyEquipmentId)
            {
                equipmentId = random.Next();
            }
        }

        public abstract void PerformAction();
    }
}
