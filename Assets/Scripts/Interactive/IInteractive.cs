
using UnityEngine;

namespace nickmaltbie.Treachery.Interactive
{
    public interface IInteractive
    {
        Sprite InteractiveIcon { get; }
        string ObjectName { get; }
        string InteractionText { get; }
        bool CanInteract(GameObject source);
        void OnInteract(GameObject source);
        void SetFocusState(GameObject source, bool state);
    }
}