using nickmaltbie.Treachery.Action;
using nickmaltbie.Treachery.Action.PlayerActions;
using UnityEngine;

namespace nickmaltbie.Treachery.Equipment
{
    public abstract class AbstractEquipment : MonoBehaviour, IEquipment
    {
        [SerializeField]
        public int equipmentId;

        [SerializeField]
        public GameObject itemPrefab;

        [SerializeField]
        public Texture2D itemIcon;

        [SerializeField]
        public ItemType itemType = ItemType.Main;

        [SerializeField]
        public EquipmentWeight equipmentWeight;

        public bool InHand { get; private set; }

        public int EquipmentId => equipmentId;

        public GameObject HeldPrefab => itemPrefab;

        public Texture2D ItemIcon => itemIcon;

        public ItemType ItemType => itemType;

        public EquipmentWeight Weight => equipmentWeight;

        public bool CanHold => true;

        public abstract void OnPutAway();

        public abstract void OnTakeOut();

        public void UpdateEquippedState(bool state)
        {
            InHand = state;
        }

        public abstract void UpdateOnOut();

        public abstract void Use(IActionActor<PlayerAction> actor);
    }
}
