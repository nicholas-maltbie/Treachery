
using UnityEngine;
using UnityEngine.UI;

namespace nickmaltbie.Treachery.UI
{
    public class SegmentedBar : MonoBehaviour
    {
        public Image negative;
        public Image positive;

        public void SetPercent(float percent)
        {
            positive.fillAmount = percent;
            negative.fillAmount = 1 - percent;
        }

        public void SetColor(Color color)
        {
            positive.color = color;
            negative.color = new Color(color.r / 10, color.g / 10, color.b / 10, color.a);
        }
    }
}
