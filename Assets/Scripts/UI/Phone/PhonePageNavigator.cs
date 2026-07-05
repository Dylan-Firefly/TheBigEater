using UnityEngine;

public class PhonePageNavigator : MonoBehaviour
{
    [Header("Pages")]
    [SerializeField] private GameObject phonePanel;
    [SerializeField] private GameObject appGridRoot;
    [SerializeField] private GameObject postAppRoot;
    [SerializeField] private GameObject profilePage;
    [SerializeField] private GameObject commonPage;
    [SerializeField] private GameObject showingResult;
    [SerializeField] private GameObject commentResultPanel;
    [SerializeField] private GameObject goodResult;
    [SerializeField] private GameObject badResult;

    [ContextMenu("Auto Bind")]
    public void AutoBind()
    {
        AutoBind(transform);
    }

    public void AutoBind(Transform root)
    {
        Transform searchRoot = root != null ? root : transform;
        phonePanel = phonePanel != null ? phonePanel : PhoneUiLookup.FindGameObject(searchRoot, "PhonePanel");
        appGridRoot = appGridRoot != null ? appGridRoot : PhoneUiLookup.FindGameObject(searchRoot, "AppGridRoot");
        postAppRoot = postAppRoot != null ? postAppRoot : PhoneUiLookup.FindGameObject(searchRoot, "PostAppRoot");
        profilePage = profilePage != null ? profilePage : PhoneUiLookup.FindGameObject(searchRoot, "Profile");
        commonPage = commonPage != null ? commonPage : PhoneUiLookup.FindGameObject(searchRoot, "CommonPage");
        showingResult = showingResult != null ? showingResult : PhoneUiLookup.FindGameObject(searchRoot, "ShowingResult");
        commentResultPanel = commentResultPanel != null ? commentResultPanel : PhoneUiLookup.FindGameObject(searchRoot, "CommentResultPanel");
        goodResult = goodResult != null ? goodResult : PhoneUiLookup.FindGameObject(searchRoot, "GoodResult");
        badResult = badResult != null ? badResult : PhoneUiLookup.FindGameObject(searchRoot, "BadResult");
    }

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

    public void ShowPublishedPost(bool showResultGuide)
    {
        SetActive(phonePanel, true);
        SetActive(appGridRoot, false);
        SetActive(postAppRoot, true);
        SetActive(profilePage, false);
        SetActive(commonPage, true);
        SetActive(showingResult, showResultGuide);
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

    private void HidePostPages()
    {
        SetActive(profilePage, false);
        SetActive(commonPage, false);
        SetActive(showingResult, false);
        SetActive(commentResultPanel, false);
        SetActive(goodResult, false);
        SetActive(badResult, false);
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null && target.activeSelf != active)
        {
            target.SetActive(active);
        }
    }
}
