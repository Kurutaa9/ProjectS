
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace MedievalKingdomUI.Scripts.Controls.Transform
{
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasScaler))]
    public class TransformInRect : MonoBehaviour
    {
        [SerializeField]
        private UnityEngine.Transform[] transformsToAffect;
        
        private RectTransform _rectTransform;
        private CanvasScaler _canvasScaler;
        private Vector3[] _transformsInitialSizes;
        private Vector2 _referenceResolution;
        
        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _canvasScaler = GetComponent<CanvasScaler>();
        }

        private void Start()
        {
            _referenceResolution = _canvasScaler.referenceResolution;
            if (transformsToAffect == null || transformsToAffect.Length == 0) return;
            _transformsInitialSizes = transformsToAffect.Select(t => t.localScale).ToArray();
            Resize();
        }

        private void OnRectTransformDimensionsChange()
        {
            Resize();
        }

        private void Resize()
        {
            if (!enabled || transformsToAffect.Length == 0 || !_rectTransform || _transformsInitialSizes == null) return;
            var sizeMultiplier = Mathf.Max(_rectTransform.rect.size.x / _referenceResolution.x, 1f);
            for (var i = 0; i < transformsToAffect.Length; i++)
            {
                transformsToAffect[i].localScale = _transformsInitialSizes[i] * sizeMultiplier;
            }
        }
    }
}