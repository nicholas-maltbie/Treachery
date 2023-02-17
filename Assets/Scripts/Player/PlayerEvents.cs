
using nickmaltbie.StateMachineUnity.Event;

namespace nickmaltbie.Treachery.Player
{
    public class PlayerDeath : IEvent
    {
        public static PlayerDeath Instance = new PlayerDeath();
        private PlayerDeath() { }
    }

    public class PunchEvent : IEvent
    {
        public static PunchEvent Instance = new PunchEvent();
        private PunchEvent() { }
    }

    public class ReviveEvent : IEvent
    {
        public static ReviveEvent Instance = new ReviveEvent();
        private ReviveEvent() { }
    }
}