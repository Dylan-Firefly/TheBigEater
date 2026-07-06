using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class UIButtonClickSound : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private AudioManager.UiSound sound = AudioManager.UiSound.GenericButton;
    [SerializeField] private Button button;
    [SerializeField] private bool playOnlyWhenInteractable = true;

    private void Awake()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (playOnlyWhenInteractable && button != null && !button.interactable)
        {
            return;
        }

        Play();
    }

    public void Play()
    {
        AudioManager.PlayUi(sound);
    }
}
