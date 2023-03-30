// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.UI
{
    using TMPro;
    using UnityEngine;
    using UnityEngine.EventSystems;

    public class MainMenuButtonScript : MonoBehaviour, IPointerEnterHandler,
        IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
    {
        private TextMeshProUGUI _text;

        private void Start()
        {
            _text = GetComponentInChildren<TextMeshProUGUI>();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _text.fontStyle = FontStyles.Underline;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!EventSystem.current.alreadySelecting)
            {
                _text.fontStyle = FontStyles.Normal;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _text.color = Color.white;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _text.color = Color.black;
        }

        public void OnSelect(BaseEventData eventData)
        {
            if (_text != null)
            {
                _text.fontStyle = FontStyles.Underline;
            }
        }

        public void OnDeselect(BaseEventData eventData)
        {
            _text.fontStyle = FontStyles.Normal;
        }
    }
}