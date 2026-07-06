using Fungus;
using UnityEngine;
using UnityEngine.InputSystem;

public class Npc1 : MonoBehaviour
{
    public GameObject Dialog;
    public string dialogBlock = "Click Pen";//点击笔跳转的Bloc块对话
    public bool isChat=false ;

    private void Awake()
    {
        Dialog.SetActive(false);
    }

    private void Update()
    {
        if (isChat&&Input .GetKeyDown (KeyCode.F ))
        {
            Dialog.SetActive(true);
            isChat = false;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision .gameObject .CompareTag("Player"))
        {
            isChat = true;
            Debug.Log(1);            
        }
    }

    public void Onclick()
    {
        Dialog.SetActive(false);
        Flowchart flowchart = GameObject.Find("Flowchart").GetComponent<Flowchart>();
        if (flowchart.HasBlock(dialogBlock))
        {//查找我们调用的Block对话块是否存在
         //执行对话
         //flowchart.StopAllBlocks();//停止其他的Block对话调用
            flowchart.ExecuteBlock(dialogBlock);//执行对话
        }
    }
}
