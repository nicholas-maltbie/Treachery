
namespace nickmaltbie.Treachery.Interactive.Stamina
{
    /// <summary>
    /// An action that costs some amount of stamina.
    /// </summary>
    public interface IStaminaAction
    {
        /// <summary>
        /// THe cost of this action.
        /// </summary>
        float Cost { get; }
    }
}
