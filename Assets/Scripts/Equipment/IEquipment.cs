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
using nickmaltbie.Treachery.Utils;
using UnityEngine;

namespace nickmaltbie.Treachery.Equipment
{
    public enum ItemType
    {
        Main,
        Offhand,
    }

    public enum EquipmentWeight
    {
        OneHanded,
        TwoHanded,
    }

    public interface IEquipment
    {
        public const int EmptyEquipmentId = -1;

        int EquipmentId { get; }
        GameObject HeldPrefab { get; }
        Sprite ItemIcon { get; }
        ItemType ItemType { get; }
        EquipmentWeight Weight { get; }
        string ItemName { get; }
        bool CanDrop { get; }
        ActorConditionalAction<PlayerAction> ItemAction { get; }
        ActorConditionalAction<PlayerAction> SecondaryItemAction { get; }
        bool DisableDefaultPrimary { get; }
        bool DisableDefaultSecondary { get; }
        ColliderConfiguration WorldShape { get; }
        void PerformAction();
        void SetupItemAction(GameObject player, IActionActor<PlayerAction> actor, IStaminaMeter meter);
        void OnRemoveFromInventory(PlayerLoadout player, Vector3 throwDirection);
    }
}
