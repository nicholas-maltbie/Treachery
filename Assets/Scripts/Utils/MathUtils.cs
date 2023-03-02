using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace nickmaltbie.Treachery.Utils
{
    public static class MathUtils
    {
        public static float SmoothValue(float x)
        {
            if (x <= 0)
            {
                return 0;
            }
            else if (x >= 1)
            {
                return 1.0f;
            }
            else
            {
                return Mathf.Sin(Mathf.PI * (x - 0.5f) / 2) + 0.5f;
            }
        }
    }
}
