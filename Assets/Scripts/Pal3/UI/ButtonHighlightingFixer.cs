// ---------------------------------------------------------------------------------------------
//  Copyright (c) 2021-2023, Jiaqi Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ---------------------------------------------------------------------------------------------

namespace Pal3.UI
{
    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.UI;

    // Taken from https://forum.unity.com/threads/button-keyboard-and-mouse-highlighting.294147/
    [RequireComponent(typeof(Selectable))]
    public class ButtonHighlightingFixer : MonoBehaviour, IPointerEnterHandler, IDeselectHandler
    {
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!EventSystem.current.alreadySelecting)
            {
                EventSystem.current.SetSelectedGameObject(gameObject);
            }
        }

        public void OnDeselect(BaseEventData eventData)
        {
            GetComponent<Selectable>().OnPointerExit(null);
        }
    }
}