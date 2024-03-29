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

using System;
using System.Linq;

namespace nickmaltbie.Treachery.Action.PlayerActions
{
    /// <summary>
    /// Attribute to show performing an action
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class PerformingActionAttribute : Attribute
    {
    }

    public abstract class AbstractBlockActionAttribute : Attribute
    {
        protected abstract bool ActionBlocked(PlayerAction action);

        public static bool CheckBlocked(Type type, PlayerAction action)
        {
            Attribute[] blocks = Attribute.GetCustomAttributes(type, typeof(AbstractBlockActionAttribute));
            return blocks.Any(block => (block as AbstractBlockActionAttribute).ActionBlocked(action));
        }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class BlockAllActionAttribute : AbstractBlockActionAttribute
    {
        protected override bool ActionBlocked(PlayerAction action)
        {
            return true;
        }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class AllowActionAttribute : AbstractBlockActionAttribute
    {
        protected PlayerAction[] actions;

        public AllowActionAttribute(params PlayerAction[] actions)
        {
            this.actions = actions;
        }

        protected override bool ActionBlocked(PlayerAction action)
        {
            return !actions.Contains(action);
        }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class BlockActionAttribute : AbstractBlockActionAttribute
    {
        protected PlayerAction[] actions;

        public BlockActionAttribute(params PlayerAction[] actions)
        {
            this.actions = actions;
        }

        protected override bool ActionBlocked(PlayerAction action)
        {
            return actions.Contains(action);
        }
    }
}
