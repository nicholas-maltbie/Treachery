
namespace nickmaltbie.Treachery.Action
{
    /// <summary>
    /// Actor who can be busy performing some action.
    /// </summary>
    public interface IActionActor<TAction>
    {
        /// <summary>
        /// Can the player perform a specific action.
        /// </summary>
        /// <param name="action">Type of action the player wants to perform.</param>
        /// <returns>True if they can, false otherwise.</returns>
        bool CanPerform(TAction action);
    }
}