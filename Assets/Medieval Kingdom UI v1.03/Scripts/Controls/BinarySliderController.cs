using UnityEngine;
using UnityEngine.UI;

namespace MedievalKingdomUI.Scripts.Controls
{
    public class BinarySliderController : MonoBehaviour
    {
        [SerializeField]
        private Text valueTitle;
        [SerializeField]
        private Slider slider;

        [SerializeField]
        private string[] values;
        [SerializeField]
        private Color[] titleColors;

        public void Turn(bool value)
        {
            var decimalValue = value ? 1 : 0;
            slider.value = slider.maxValue * decimalValue;
            valueTitle.text = values[decimalValue];
            valueTitle.color = titleColors[decimalValue];
        }

        private void Start()
        {
            if (values.Length != 2 
                || titleColors.Length != 2
                || valueTitle == null
                || slider == null)
            {
                gameObject.SetActive(false);
            }
        }
    }
}