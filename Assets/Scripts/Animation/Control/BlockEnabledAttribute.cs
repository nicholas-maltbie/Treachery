
using System;
using UnityEngine;

namespace nickmaltbie.Treachery.Animation.Control
{
    public class BlockEnabledAttribute : Attribute
    {
        public static bool GetBlockState(Type state)
        {
            return Attribute.GetCustomAttribute(state, typeof(BlockEnabledAttribute)) is BlockEnabledAttribute;
        }

        public static void UpdateBlockState(Type state, BlockAnimator blockAnimator)
        {
            blockAnimator.enableBlock = GetBlockState(state);
        }

        public static void UpdateBlockState(Type state, Component player)
        {
            if (player.GetComponentInChildren<BlockAnimator>() is BlockAnimator anim)
            {
                UpdateBlockState(state, anim);
            }
        }
    }
}
