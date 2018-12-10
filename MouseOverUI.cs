using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Verify if mouse cursor or any finger touch is over any UI element
/// No need to be attached to GameObject
/// </summary>
/// <see cref="https://github.com/stefanak-michal/Unity-Stuff"/>
/// <author>Michal Stefanak</author>
public class MouseOverUI : MonoBehaviour
{
    static PointerEventData pointerEventData = new PointerEventData(EventSystem);
    static List<RaycastResult> result = new List<RaycastResult>();

    public static bool Verify
    {
        get
        {
            if (Check(Input.mousePosition))
                return true;

            for (int i = 0; i < Input.touchCount; i++)
            {
                if (Check(Input.GetTouch(i).position))
                    return true;
            }

            return false;
        }
    }

    static bool Check(Vector3 position)
    {
        pointerEventData.position = position;
        foreach (var r in Raycasters)
        {
            result.Clear();
            r.Raycast(pointerEventData, result);
            if (result.Count > 0)
                return true;
        }

        return false;
    }

    static GraphicRaycaster[] rr;
    static GraphicRaycaster[] Raycasters
    {
        get
        {
            if (rr == null)
                rr = Resources.FindObjectsOfTypeAll<GraphicRaycaster>();

            return rr;
        }
    }

    static EventSystem es;
    static EventSystem EventSystem
    {
        get
        {
            if (es == null)
                es = GameObject.FindObjectOfType<EventSystem>();

            return es;
        }
    }
}
