
namespace nickmaltbie.Treachery.Interactive.Stamina
{
    /// <summary>
    /// Measure the stamina level of some actor in the game.
    /// </summary>
    public interface IStaminaMeter
    {
        /// <summary>
        /// Remaining stamina for the actor.
        /// </summary>
        float RemainingStamina { get; }

        /// <summary>
        /// Maximum stamina for the actor.
        /// </summary>
        float MaximumStamina { get; }

        /// <summary>
        /// Percentage of stamina remaining/max as a value between 0 and 1.
        /// </summary>
        float PercentRemainingStamina { get; }

        /// <summary>
        /// Restore stamina to the actor.
        /// </summary>
        /// <param name="amount">Amount of stamina to restore.</param>
        void RestoreStamina(float amount);

        /// <summary>
        /// Exhaust some stamina from the actor.
        /// </summary>
        /// <param name="amount">Amount of stamina to exhaust.</param>
        void ExhaustStamina(float amount);

        /// <summary>
        /// Spend some stamina if the player has enough.
        /// </summary>
        /// <param name="amount">Amount of stamina to spend.</param>
        /// <returns>True if the stamina can be spent, false otherwise.</returns>
        bool SpendStamina(float amount);

        /// <summary>
        /// Spend some stamina if the player has enough.
        /// </summary>
        /// <param name="action">Spend stamina equivalent to the cost of the action.</param>
        /// <returns>True if the stamina can be spent, false otherwise.</returns>
        bool SpendStamina(IStaminaAction action);

        /// <summary>
        /// Does the actor have at a given amount of stamina.
        /// </summary>
        /// <param name="amount">Amount of stamina.</param>
        /// <returns>True if the actor has the amount, false otherwise.</returns>
        bool HasEnoughStamina(float amount);

        /// <summary>
        /// Does the actor have at a given amount of stamina.
        /// </summary>
        /// <param name="action">Action with a required stamina cost.</param>
        /// <returns>True if the actor has the amount, false otherwise.</returns>
        bool HasEnoughStamina(IStaminaAction action);
    }
}