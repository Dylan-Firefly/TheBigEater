using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonColor : MonoBehaviour
{
    public Image textImage;
    public Color normalColor = Color.white;//常规颜色
    public Color selectColor = Color.yellow;//按下/鼠标放在按键上/方向键导航选中
    //private Selectable sel;
    private Button btn;

    void Start()
    {
        //sel = GetComponent<Selectable>();//Selectable 是 Unity UI 里所有可交互控件的父类基类
        //Button、Toggle、Slider、InputField 这些组件全都继承自 Selectable。
        //Button 本质就是一种特殊的 Selectable。
        //可以通过Selectable获得按钮的状态，从而改变文字颜色

        btn = GetComponent<Button>();
        textImage.color = normalColor;
    }

    void Update()
    {
        bool isHover = IsPointerOverUIObject();
        bool isPress = isHover && Input.GetMouseButton(0);
        bool isSelect = EventSystem.current.currentSelectedGameObject == gameObject;

        bool isActive = isHover || isPress || isSelect;
        textImage.color = isActive ? selectColor : normalColor;
    }
    // 判断鼠标是否悬浮在当前按钮上
    bool IsPointerOverUIObject()
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = Input.mousePosition;
        var list = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, list);
        foreach (var res in list)
        {
            if (res.gameObject == gameObject)
                return true;
        }
        return false;
    }
}
