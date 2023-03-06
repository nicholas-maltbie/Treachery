
using UnityEngine;

namespace nickmaltbie.Treachery.Interactive
{
    public class BasicInteractive : MonoBehaviour, IInteractive
    {
        [SerializeField]
        public Sprite sprite;

        public Sprite InteractiveIcon => sprite;
        public string ObjectName => gameObject.name;
        public string InteractionText => string.Empty;
        public bool CanInteract(GameObject source) => true;

        public void OnInteract(GameObject source)
        {
            Debug.Log($"{source} interacted with object {ObjectName}");
        }

        public void SetFocusState(GameObject source, bool state)
        {
            Debug.Log($"{ObjectName} set focus state:{state} from source:{source}");
        }
    }
}