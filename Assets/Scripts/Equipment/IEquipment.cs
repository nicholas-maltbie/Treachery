
using System;
using nickmaltbie.Treachery.Action;
using nickmaltbie.Treachery.Action.PlayerActions;
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
        Texture2D ItemIcon { get; }
        ItemType ItemType { get; }
        EquipmentWeight Weight { get; }
        bool CanHold { get; }
        void Use(IActionActor<PlayerAction> actor);
        void UpdateOnOut();
        void OnTakeOut();
        void OnPutAway();
        void UpdateEquippedState(bool state);
    }
}