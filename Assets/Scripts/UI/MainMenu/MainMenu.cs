using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    Button newGameBtn;
    Button contineBtn;
    Button quitBtn;


    private void Awake()
    {
        newGameBtn = transform.GetChild(0).GetComponent<Button>();
        contineBtn = transform.GetChild(1).GetComponent<Button>();
        quitBtn = transform.GetChild(2).GetComponent<Button>();


        newGameBtn.onClick.AddListener(startPlay);
        contineBtn.onClick.AddListener(load);
        quitBtn.onClick.AddListener(exit);
    }
    public void startPlay()
    {
        SceneManager.LoadScene("zhibo");
    }

    public void load()
    {

    }

    public void exit()
    {
        Application.Quit();//只在打包导出程序的时候才会调用直接关闭游戏
    }
}
