using Fragsurf.Utility;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Fragsurf.UI
{
    public class ModalWindow : MonoBehaviour
    {

        [SerializeField]
        private GameObject _dragHeader;
        [SerializeField]
        private bool _draggable = true;
        [SerializeField]
        private TMP_Text _titleText;
        [SerializeField]
        private Button _closeButton;

        private string _modalName;
        private Canvas _canvas;
        private RectTransform _rt;

        private void Start()
        {
            var modal = GetComponentInParent<UGuiModal>();
            if (!modal)
            {
                return;
            }

            _modalName = modal.Name;

            if (_dragHeader)
            {
                if(!_dragHeader.TryGetComponent(out ModalDragHandler md))
                {
                    md = _dragHeader.AddComponent<ModalDragHandler>();
                }
                md.OnDrag.AddListener(OnDrag);
                md.OnPointerUp.AddListener(OnPointerUp);
            }

            _titleText.text = modal.Name;

            if (!_closeButton)
            {
                return;
            }

            _closeButton.onClick.AddListener(() =>
            {
                modal.Close();
            });

            _rt = GetComponent<RectTransform>();
            _canvas = GetComponentInParent<Canvas>();

            LoadPosition();
        }

        private void SavePosition()
        {
            if (_draggable && !string.IsNullOrEmpty(_modalName))
            {
                var pos = _rt.anchoredPosition;
                PlayerPrefs.SetString("Window." + _modalName, $"{pos.x}x{pos.y}");
            }
        }

        private void LoadPosition()
        {
            if (_draggable && !string.IsNullOrEmpty(_modalName))
            {
                var pref = PlayerPrefs.GetString($"Window." + _modalName);
                if (string.IsNullOrEmpty(pref))
                {
                    return;
                }
                var split = pref.Split('x');
                if(split == null || split.Length != 2)
                {
                    return;
                }
                if(float.TryParse(split[0], out float x)
                    && float.TryParse(split[1], out float y))
                {
                    _rt.anchoredPosition = new Vector2(x, y);
                    _rt.ClampToParent(_rt.parent as RectTransform);
                }
            }
        }

        private void OnDestroy()
        {
            SavePosition();
        }

        private void OnPointerUp(PointerEventData ed)
        {
            if (!_draggable)
            {
                return;
            }
            _rt.ClampToParent(_rt.parent as RectTransform);
        }

        private void OnDrag(PointerEventData eventData)
        {
            if (!_draggable)
            {
                return;
            }
            _rt.anchoredPosition += eventData.delta / _canvas.scaleFactor;
        }

        public class ModalDragHandler : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
        {
            public UnityEvent<PointerEventData> OnPointerDown = new UnityEvent<PointerEventData>();
            public UnityEvent<PointerEventData> OnPointerUp = new UnityEvent<PointerEventData>();
            public UnityEvent<PointerEventData> OnDrag = new UnityEvent<PointerEventData>();

            void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
            {
                OnPointerDown?.Invoke(eventData);
            }

            void IPointerUpHandler.OnPointerUp(PointerEventData eventData)
            {
                OnPointerUp?.Invoke(eventData);
            }

            void IDragHandler.OnDrag(PointerEventData eventData)
            {
                OnDrag?.Invoke(eventData);
            }
        }

    }
}

