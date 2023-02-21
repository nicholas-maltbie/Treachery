
using System;

namespace nickmaltbie.Treachery.Action
{
    /// <summary>
    /// Attribute to show performing an action
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class PerformingActionAttribute : Attribute
    {
    }
}