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

using nickmaltbie.StateMachineUnity.Event;

namespace nickmaltbie.Treachery.Player
{
    public class PlayerDeathEvent : IEvent
    {
        public static PlayerDeathEvent Instance = new PlayerDeathEvent();
        private PlayerDeathEvent() { }
    }

    public class PunchEvent : IEvent
    {
        public static PunchEvent Instance = new PunchEvent();
        private PunchEvent() { }
    }

    public class PlayerReviveEvent : IEvent
    {
        public static PlayerReviveEvent Instance = new PlayerReviveEvent();
        private PlayerReviveEvent() { }
    }

    public class OnHitEvent : IEvent
    {
        public static OnHitEvent Instance = new OnHitEvent();
        private OnHitEvent() { }
    }

    public class DodgeStart : IEvent
    {
        public static DodgeStart Instance = new DodgeStart();
        private DodgeStart() { }
    }

    public class DodgeStop : IEvent
    {
        public static DodgeStop Instance = new DodgeStop();
        private DodgeStop() { }
    }
}
