using UnityEngine;
using UnityEngine.UI;

namespace MedievalKingdomUI.Scripts.Controls
{
    [RequireComponent(typeof(Toggle))]
    public class HighlightToggle : MonoBehaviour
    {
        [SerializeField]
        private Color onColor;
        [SerializeField]
        private Color offColor;
        [SerializeField]
        private Graphic graphic;
        
        private void Start()
        {
            var toggle = GetComponent<Toggle>();
            graphic.color = toggle.isOn ? onColor : offColor;
            toggle.onValueChanged.AddListener(value => { graphic.color = value ? onColor : offColor; });
        }
    }
}
