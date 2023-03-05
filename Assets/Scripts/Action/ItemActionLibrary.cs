
using UnityEngine;
using UnityEngine.InputSystem;

namespace nickmaltbie.Treachery.Equipment
{
    [CreateAssetMenu(fileName = "ItemActionLibrary", menuName = "ScriptableObjects/ItemActionLibrary", order = 1)]
    public class ItemActionLibrary : ScriptableObject
    {
        public InputActionReference mainHandAction;
        public InputActionReference offHandAction;

        public InputActionReference GetActionReference(ItemType type)
        {
            switch (type)
            {
                case ItemType.Main:
                    return mainHandAction;
                case ItemType.Offhand:
                    return offHandAction;
            }

            return mainHandAction;
        }
    }
}
