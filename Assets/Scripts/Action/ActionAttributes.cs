
using System;
using System.Linq;

namespace nickmaltbie.Treachery.Action
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
            var block = Attribute.GetCustomAttribute(type, typeof(AbstractBlockActionAttribute)) as AbstractBlockActionAttribute;
            if (block != null)
            {
                return block.ActionBlocked(action);
            }

            return false;
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