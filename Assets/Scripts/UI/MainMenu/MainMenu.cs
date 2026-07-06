using UnityEngine;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button saveButton;
    [SerializeField] private Button quitButton;

    private void OnEnable()
    {
        if (newGameButton != null)
        {
            newGameButton.onClick.AddListener(StartNewGame);
        }

        if (saveButton != null)
        {
            saveButton.interactable = false;
        }

        if (quitButton != null)
        {
            quitButton.onClick.AddListener(QuitGame);
        }
    }

    private void OnDisable()
    {
        if (newGameButton != null)
        {
            newGameButton.onClick.RemoveListener(StartNewGame);
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveListener(QuitGame);
        }
    }

    public void StartNewGame()
    {
        AudioManager.PlayMainMenu();
        GameManager.EnsureInstance().StartNewGame();
    }

    public void QuitGame()
    {
        AudioManager.PlayMainMenu();
        GameManager.EnsureInstance().QuitGame();
    }
}
