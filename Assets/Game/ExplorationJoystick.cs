using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace VeinVanguard
{
    public class ExplorationJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public RectTransform knob;
        public Action<Vector2> changed;
        public Vector2 Value { get; private set; }
        [Range(0, .5f)] public float deadzone = .12f;
        int? pointer;
        public void OnPointerDown(PointerEventData data)
        {
            if(pointer.HasValue)return;
            pointer=data.pointerId;OnDrag(data);
        }
        public void OnDrag(PointerEventData data)
        {
            if(pointer!=data.pointerId)return;
            var rect=(RectTransform)transform;
            if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect,data.position,data.pressEventCamera,out var point))return;
            float radius=Mathf.Max(1,Mathf.Min(rect.rect.width,rect.rect.height)*.5f-knob.rect.width*.5f);
            var displacement=Vector2.ClampMagnitude((point-rect.rect.center)/radius,1);
            knob.anchoredPosition=displacement*radius;
            float length=displacement.magnitude;
            Value=length<=deadzone?Vector2.zero:displacement.normalized*((length-deadzone)/(1-deadzone));
            changed?.Invoke(Value);
        }
        public void OnPointerUp(PointerEventData data){if(pointer==data.pointerId)ResetInput();}
        public void ResetInput(){pointer=null;Value=Vector2.zero;if(knob)knob.anchoredPosition=Vector2.zero;changed?.Invoke(Value);}
        void OnDisable()=>ResetInput();
    }
}
