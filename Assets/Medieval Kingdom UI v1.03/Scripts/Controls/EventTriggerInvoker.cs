using UnityEngine;
using UnityEngine.Events;

namespace MedievalKingdomUI.Scripts.Controls
{
    public class EventTriggerInvoker : MonoBehaviour
    {
        [SerializeField]
        private UnityEvent eventToTrigger;

        public void Trigger()
        {
            eventToTrigger.Invoke();
        }
    }
}
