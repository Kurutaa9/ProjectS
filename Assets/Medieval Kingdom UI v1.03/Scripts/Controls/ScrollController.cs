using System;
using UnityEngine;
using UnityEngine.UI;

namespace MedievalKingdomUI.Scripts.Controls
{
    [RequireComponent(typeof(Scrollbar))]
    public class ScrollController : MonoBehaviour
    {
        [SerializeField]
        private float step = 0.1f;
        
        private Scrollbar _scrollbar;

        public void Increase()
        {
            if (Math.Abs(_scrollbar.value - 1f) < 0.05) return;
            _scrollbar.value += step;
        }
        
        public void Decrease()
        {
            if (Math.Abs(_scrollbar.value) < 0.05) return;
            _scrollbar.value -= step;
        }

        private void Start()
        {
            _scrollbar = GetComponent<Scrollbar>();
        }
    }
}
