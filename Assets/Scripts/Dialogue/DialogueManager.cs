using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DialogueManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject dialoguePanel;
    public TMP_Text nameText;
    public TMP_Text dialogueText;

    [Header("Typing Settings")]
    public float typingSpeed = 0.02f;

    [Header("Alpha Mask Settings")]
    private const string HTML_ALPHA = "<color=#00000000>";

    private Queue<string> sentences;
    private string currentSentence = "";
    private Coroutine typingCoroutine = null;
    private bool isTyping = false;
    public bool isOpen = false;

    void Awake()
    {
        sentences = new Queue<string>();
    }

    void Start()
    {
        if (sentences == null)
            sentences = new Queue<string>();

        if (!isOpen && dialoguePanel != null)
            dialoguePanel.SetActive(false);
    }

    void Update()
    {
        if (isOpen && (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0)))
        {
            HandleInput();
        }
    }

    public void StartDialogue(string speakerName, string[] dialogueSentences)
    {
        if (sentences == null)
            sentences = new Queue<string>();

        isOpen = true;
        dialoguePanel.SetActive(true);
        nameText.text = speakerName;

        sentences.Clear();
        foreach (string sentence in dialogueSentences)
            sentences.Enqueue(sentence);

        DisplayNextSentence();
    }

    public void DisplayNextSentence()
    {
        if (sentences.Count == 0)
        {
            EndDialogue();
            return;
        }

        currentSentence = sentences.Dequeue();
        
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeSentenceAlpha(currentSentence));
    }

    private IEnumerator TypeSentenceAlpha(string sentence)
    {
        isTyping = true;
        
        for (int i = 0; i <= sentence.Length; i++)
        {
            string displayedText = sentence.Substring(0, i) + HTML_ALPHA + sentence.Substring(i) + "</color>";
            dialogueText.text = displayedText;
            yield return new WaitForSeconds(typingSpeed);
        }

        dialogueText.text = sentence;
        isTyping = false;
        typingCoroutine = null;
    }

    private void HandleInput()
    {
        if (isTyping)
        {
            // Skip typing effect and show full sentence
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            dialogueText.text = currentSentence;
            isTyping = false;
            typingCoroutine = null;
        }
        else
        {
            // Move to next sentence
            DisplayNextSentence();
        }
    }

    private void EndDialogue()
    {
        isOpen = false;
        isTyping = false;
        dialogueText.text = "";
        dialoguePanel.SetActive(false);
    }
}

