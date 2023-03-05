
using nickmaltbie.Treachery.Action;
using nickmaltbie.Treachery.Action.PlayerActions;
using UnityEngine;

namespace nickmaltbie.Treachery.Equipment
{
    public class BasicItem : AbstractEquipment
    {
        public override void OnPutAway()
        {
            // Do nothing, it's just a basic item.
        }

        public override void OnTakeOut()
        {
            // Do nothing, it's just a basic item.
        }

        public override void UpdateOnOut()
        {
            // Do nothing, it's just a basic item.
        }

        public override void Use(IActionActor<PlayerAction> actor)
        {
            // Do nothing, it's just a basic item.
            Debug.Log($"Used item:{gameObject.name}");
        }
    }
}