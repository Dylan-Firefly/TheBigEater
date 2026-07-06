using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class OpeningChapterController : MonoBehaviour
{
    [Header("Opening Live")]
    [SerializeField] private GameObject openingLiveRoot;
    [SerializeField] private int clicksToContinue = 10;
    [SerializeField] private float secondsToContinue = 10f;
    [SerializeField] private bool startTimerOnAwake = true;

    [Header("CG / Dialogue")]
    [SerializeField] private VideoSequencePlayer cg1Player;
    [SerializeField] private VideoSequencePlayer cg2Player;
    [SerializeField] private FungusDialogueBridge transformDialogue;

    [Header("Choice / Failure UI")]
    [SerializeField] private GameObject choicePanel;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private float gameOverReturnDelay = 5f;

    private int clickCount;
    private bool advancedFromLive;
    private Coroutine liveTimerRoutine;
    private Coroutine gameOverRoutine;

    private void Awake()
    {
        SetActive(choicePanel, false);
        SetActive(gameOverPanel, false);
        SetActive(openingLiveRoot, true);
    }

    private void OnEnable()
    {
        if (cg1Player != null)
        {
            cg1Player.Completed += ShowChoice;
        }

        if (cg2Player != null)
        {
            cg2Player.Completed += FinishOpeningChapter;
        }

        if (transformDialogue != null)
        {
            transformDialogue.Completed += PlayCg2;
        }
    }

    private void Start()
    {
        GameManager.EnsureInstance().SetState(GameFlowState.OpeningLive);

        if (startTimerOnAwake && secondsToContinue > 0f)
        {
            liveTimerRoutine = StartCoroutine(OpeningLiveTimerRoutine());
        }
    }

    private void OnDisable()
    {
        if (cg1Player != null)
        {
            cg1Player.Completed -= ShowChoice;
        }

        if (cg2Player != null)
        {
            cg2Player.Completed -= FinishOpeningChapter;
        }

        if (transformDialogue != null)
        {
            transformDialogue.Completed -= PlayCg2;
        }
    }

    public void RegisterOpeningClick()
    {
        if (advancedFromLive)
        {
            return;
        }

        clickCount++;
        if (clickCount >= clicksToContinue)
        {
            AdvanceFromOpeningLive();
        }
    }

    public void ChooseKeepStatus()
    {
        AudioManager.PlayGenericButton();
        GameManager.EnsureInstance().ChooseOpeningBranch(OpeningChoice.KeepStatus);
        SetActive(choicePanel, false);
        SetActive(gameOverPanel, true);

        if (gameOverRoutine != null)
        {
            StopCoroutine(gameOverRoutine);
        }

        gameOverRoutine = StartCoroutine(GameOverRoutine());
    }

    public void ChooseTransformHome()
    {
        AudioManager.PlayGenericButton();
        GameManager.EnsureInstance().ChooseOpeningBranch(OpeningChoice.TransformHome);
        SetActive(choicePanel, false);
        if (transformDialogue != null)
        {
            transformDialogue.Play();
        }
        else
        {
            PlayCg2();
        }
    }

    private IEnumerator OpeningLiveTimerRoutine()
    {
        yield return new WaitForSeconds(secondsToContinue);
        AdvanceFromOpeningLive();
    }

    private void AdvanceFromOpeningLive()
    {
        if (advancedFromLive)
        {
            return;
        }

        advancedFromLive = true;
        if (liveTimerRoutine != null)
        {
            StopCoroutine(liveTimerRoutine);
            liveTimerRoutine = null;
        }

        SetActive(openingLiveRoot, false);
        GameManager.EnsureInstance().OnOpeningCg1Started();
        if (cg1Player != null)
        {
            cg1Player.Play();
        }
        else
        {
            ShowChoice();
        }
    }

    private void ShowChoice()
    {
        GameManager.EnsureInstance().OnOpeningChoiceShown();
        SetActive(choicePanel, true);
    }

    private void PlayCg2()
    {
        GameManager.EnsureInstance().OnTransformDialogueFinished();
        if (cg2Player != null)
        {
            cg2Player.Play();
        }
        else
        {
            FinishOpeningChapter();
        }
    }

    private void FinishOpeningChapter()
    {
        GameManager.EnsureInstance().OnOpeningChapterFinished();
    }

    private IEnumerator GameOverRoutine()
    {
        yield return new WaitForSeconds(gameOverReturnDelay);
        GameManager.EnsureInstance().ReturnToMainMenu();
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null)
        {
            target.SetActive(active);
        }
    }
}
