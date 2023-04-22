using UnityEngine;
 
namespace nickmaltbie.Treachery.Enemy.Subs
{
    public class SubscriberReader : MonoBehaviour
    {
        public TextAsset jsonFile;
    
        public Subscribers ParseSubs()
        {
            Subscribers subs = JsonUtility.FromJson<Subscribers>(jsonFile.text);
            return subs;
        }
    }
}