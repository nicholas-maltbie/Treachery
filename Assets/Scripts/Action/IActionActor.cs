
namespace nickmaltbie.Treachery.Action
{
    /// <summary>
    /// Actor who can be busy performing some action.
    /// </summary>
    public interface IActionActor
    {
        /// <summary>
        /// Is the actor busy performing some action.
        /// </summary>
        public bool IsBusy();
    }
}