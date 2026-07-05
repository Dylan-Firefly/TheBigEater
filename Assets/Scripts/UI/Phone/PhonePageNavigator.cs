using UnityEngine;

public class PhonePageNavigator : MonoBehaviour
{
    [Header("Root Pages")]
    [SerializeField] private GameObject phonePanel;
    [SerializeField] private GameObject appGridRoot;
    [SerializeField] private GameObject postAppRoot;

    [Header("Post App Pages")]
    [SerializeField] private GameObject profilePage;
    [SerializeField] private GameObject commonPage;
    [SerializeField] private GameObject showingResult;
    [SerializeField] private GameObject commentResultPanel;

    [Header("Final Result")]
    [SerializeField] private GameObject goodResult;
    [SerializeField] private GameObject badResult;

    public void ShowClosed()
    {
        SetActive(phonePanel, false);
        SetActive(appGridRoot, true);
        SetActive(postAppRoot, false);
        HidePostPages();
    }

    public void ShowAppGrid()
    {
        SetActive(phonePanel, true);
        SetActive(appGridRoot, true);
        SetActive(postAppRoot, false);
        HidePostPages();
    }

    public void ShowProfile()
    {
        SetActive(phonePanel, true);
        SetActive(appGridRoot, false);
        SetActive(postAppRoot, true);
        SetActive(profilePage, true);
        SetActive(commonPage, false);
        SetActive(showingResult, false);
        SetActive(commentResultPanel, false);
        SetActive(goodResult, false);
        SetActive(badResult, false);
    }

    public void ShowPostDetail(bool showResultPrompt)
    {
        SetActive(phonePanel, true);
        SetActive(appGridRoot, false);
        SetActive(postAppRoot, true);
        SetActive(profilePage, false);
        SetActive(commonPage, true);
        SetActive(showingResult, showResultPrompt);
        SetActive(commentResultPanel, false);
        SetActive(goodResult, false);
        SetActive(badResult, false);
    }

    public void ShowPkPage()
    {
        SetActive(phonePanel, true);
        SetActive(appGridRoot, false);
        SetActive(postAppRoot, true);
        SetActive(profilePage, false);
        SetActive(commonPage, true);
        SetActive(showingResult, false);
        SetActive(commentResultPanel, true);
        SetActive(goodResult, false);
        SetActive(badResult, false);
    }

    public void ShowFinalResult(bool good, bool keepPostPageBehindFinalResult)
    {
        if (!keepPostPageBehindFinalResult)
        {
            SetActive(commonPage, false);
            SetActive(commentResultPanel, false);
        }

        SetActive(showingResult, false);
        SetActive(goodResult, good);
        SetActive(badResult, !good);
    }

    public void SetPhonePanelVisible(bool visible)
    {
        SetActive(phonePanel, visible);
    }

    public void SetShowingResultVisible(bool visible)
    {
        SetActive(showingResult, visible);
    }

    public void HidePostPages()
    {
        SetActive(profilePage, false);
        SetActive(commonPage, false);
        SetActive(showingResult, false);
        SetActive(commentResultPanel, false);
        SetActive(goodResult, false);
        SetActive(badResult, false);
    }

    public void ValidateReferences(Object owner)
    {
        WarnMissing(owner, phonePanel, nameof(phonePanel));
        WarnMissing(owner, appGridRoot, nameof(appGridRoot));
        WarnMissing(owner, postAppRoot, nameof(postAppRoot));
        WarnMissing(owner, profilePage, nameof(profilePage));
        WarnMissing(owner, commonPage, nameof(commonPage));
        WarnMissing(owner, showingResult, nameof(showingResult));
        WarnMissing(owner, commentResultPanel, nameof(commentResultPanel));
        WarnMissing(owner, goodResult, nameof(goodResult));
        WarnMissing(owner, badResult, nameof(badResult));
    }

    private static void WarnMissing(Object owner, Object target, string fieldName)
    {
        if (target == null)
        {
            Debug.LogWarning($"[{nameof(PhonePageNavigator)}] Missing reference: {fieldName}.", owner);
        }
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null && target.activeSelf != active)
        {
            target.SetActive(active);
        }
    }
}
