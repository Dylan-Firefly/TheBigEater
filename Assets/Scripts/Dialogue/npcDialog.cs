using Fungus;
using UnityEngine;

public class npcDialog : MonoBehaviour
{
    public string pen = "Click Pen";//点击笔跳转的Bloc块对话
    private bool canChat = false;//是否允许对话

    private void OnTriggerStay2D(Collider2D collision)
    {
        if(collision .gameObject .CompareTag("Player"))
        {
            canChat = true;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision .gameObject .CompareTag("Player"))
        {
            canChat = false;
        }
    }

    private void Update()
    {
        if(Input .GetKeyDown (KeyCode.F )&&canChat)
        {
            Flowchart flowchart = GameObject.Find("mainMapFlowchart").GetComponent<Flowchart>();
            //在project里面不能找到预制体fungus自带的脚本
            //需要在Hierarchy里面查找实例化的gamobject再查找调用它本身的脚本

            if (flowchart.HasBlock(pen))
            {
                //flowchart.StopAllBlocks();//停止其他的Block对话调用
                flowchart.ExecuteBlock(pen);//执行对话
            }

            canChat = false;
        }
    }

}
