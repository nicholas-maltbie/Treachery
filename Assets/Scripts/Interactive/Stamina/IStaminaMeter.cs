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
