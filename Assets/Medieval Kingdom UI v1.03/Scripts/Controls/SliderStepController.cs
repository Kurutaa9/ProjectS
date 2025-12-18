using UnityEngine;
using UnityEngine.UI;

namespace MedievalKingdomUI.Scripts.Controls
{
    public class SliderStepController : MonoBehaviour
    {
        [SerializeField]
        private Text textComponent;
        [SerializeField]
        private Slider sliderComponent;
        [SerializeField]
        private string[] values;

        public void UpdatePercentValue(float value)
        {
            var intValue = (int)value;
            textComponent.text = values[intValue];
        }

        private void Start()
        {
            if (textComponent == null
                || sliderComponent == null
                || values.Length == 0)
            {
                gameObject.SetActive(false);
            }

            sliderComponent.maxValue = values.Length - 1;
            sliderComponent.wholeNumbers = true;
        }
    }
}