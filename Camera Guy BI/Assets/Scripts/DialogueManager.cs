﻿using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    bool showingDialogue;
    bool isFading;
    bool isDisplayingText;
    [HideInInspector]
    public bool choiceBoxActive;
    IEnumerator displayDialogueCoroutine;

    [Header("UI")]
    CanvasGroup canvasGroup;
    [Tooltip("Fow quickly the UI fades in")]
    public float uiFadeInSpeed = 0.4f;
    [Space(15)]
    [Tooltip("The object which holds characters' names")]
    public TextMeshProUGUI nameBox;
    [Tooltip("The object which holds characters' dialogue")]
    public TextMeshProUGUI dialogueBox;
    [Tooltip("The object which holds characters' image")]
    public Image bust;
    [Tooltip("The Box which manages player choices")]
    public ChoiceBox choiceBox;

    [Header("Text Display Options")]
    [Tooltip("The length of time to wait between displaying characters")]
    public float delay = 0.05f;

    [Header("File")]
    string dialogueFilePath;
    [Tooltip("The scene to load")]
    public string sceneName;
    [Tooltip("Whether to clear the scene after it has run")]
    public bool clearAfterScene;

    [Header("Characters")]
    [Tooltip("The array that contains all of the characters")]
    public CharacterPortraitContainer[] characters;
    readonly Dictionary<string, CharacterPortraitContainer> characterDictionary = new Dictionary<string, CharacterPortraitContainer>();

    public Sprite defaultCharacterSprite;

    string[] fileLines;
    int currentLine;
    string characterName, characterDialogue, characterExpression;

    AudioSource voicePlayer;

    private void Awake()
    {
        dialogueFilePath = $"{Application.dataPath + Path.DirectorySeparatorChar}Dialogue{Path.DirectorySeparatorChar}"; // Gets the folder in which scenes are stored
        canvasGroup = GetComponent<CanvasGroup>(); // Gets the canvas group to deal with fading opacity
        foreach (var characterPortraits in characters) // Creates the dictionary
        {
            characterDictionary.Add(characterPortraits.name, characterPortraits);
        }
        ClearDialogueBox(); // Clears the dialogue box, just in case

        voicePlayer = GetComponent<AudioSource>();
    }

    /// <summary>
    /// Clears the dialogue box's name, dialogue and image
    /// </summary>
    void ClearDialogueBox()
    {
        bust.sprite = null;
        nameBox.text = string.Empty;
        dialogueBox.text = string.Empty;
    }

    /// <summary>
    /// Loads the given scene into an array of the scene's lines
    /// </summary>
    /// <param name="_sceneName">The name of the scene. Determines the file to grab. Scene dialogues should be named after the scene they take place in</param>
    public void LoadSceneTextFile(string _sceneName)
    {
        sceneName = _sceneName;
        fileLines = File.ReadAllLines($"{dialogueFilePath + _sceneName}.txt");
        currentLine = 0;
        ClearDialogueBox();
    }

    /// <summary>
    /// Loads the next line from fileLines
    /// </summary>
    public void LoadNewLine()
    { 
        // Split the line into its components and store them
        string[] parsedText = fileLines[currentLine].Split('|');
        currentLine++;

        if (parsedText[0] == "[Choice]")
        {
            DisplayChoices(parsedText);
            return;
        }

        characterName = parsedText[0];
        characterExpression = parsedText[1].ToLower();
        characterDialogue = parsedText[2];

        dialogueBox.text = string.Empty;

        if(characterDictionary[characterName].voiceClip != null)
        {
            voicePlayer.PlayOneShot(characterDictionary[characterName].voiceClip);
        }  

        // Sets the name box
        nameBox.text = characterName;
        // Converts the expression string into the associated Sprite variable in the given character
        try
        {
            bust.sprite = (Sprite)characterDictionary[characterName].GetType().GetField(characterExpression).GetValue(characterDictionary[characterName]);
        }
        catch (KeyNotFoundException)
        {
            bust.sprite = defaultCharacterSprite;
        }

        // Declare and then start the coroutine/IEnumerator so it can be stopped later
        displayDialogueCoroutine = DisplayDialogue(characterDialogue);
        StartCoroutine(displayDialogueCoroutine);
    }

    void DisplayChoices(string[] parsedText)
    {
        choiceBoxActive = true;
        choiceBox.canvasGroup.alpha = 1;
        choiceBox.canvasGroup.interactable = choiceBox.canvasGroup.blocksRaycasts = (choiceBox.canvasGroup.alpha == 1);
        int choices = (parsedText.Count() - 1) / 2;
        for (int i = 1; i <= choices; i++)
        {
            choiceBox.choices[i - 1].gameObject.SetActive(true);
            choiceBox.choices[i - 1].text.text = parsedText[(2 * i) - 1];
            choiceBox.choices[i - 1].scene = parsedText[2 * i];
        }
    }


    /// <summary>
    /// Displays a given string letter by letter
    /// </summary>
    /// <param name="text">The text to display</param>
    /// <returns></returns>
    IEnumerator DisplayDialogue(string text)
    {
        isDisplayingText = true; // Marks the system as typing out letters. Used to determine what happens when pressing enter

        for (int i = 0; i < text.Length; i++) // Adds a letter to the textbox then waits the delay time
        {
            dialogueBox.text += text[i];
            yield return new WaitForSeconds(delay);
        }

        isDisplayingText = false; // Marks the system as no longer typing out
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            // Eats input if no matching file exists or if the UI is fading in/out or if the player is making a choice
            if (!File.Exists($"{dialogueFilePath + sceneName}.txt") || isFading || choiceBoxActive) 
                return;

            if (showingDialogue) // If dialogue boxes are visible
            {
                if (currentLine >= fileLines.Length && !isDisplayingText) // If there are no more lines and system is not typing out
                {
                    if (clearAfterScene) // Clears the scene if told to
                    {
                        sceneName = string.Empty;
                    }
                    StartCoroutine(FadeCanvas(uiFadeInSpeed, canvasGroup.alpha, 0)); // Fades out the UI
                    return;
                }
                if (isDisplayingText) // If the system is typing out
                {
                    StopCoroutine(displayDialogueCoroutine); // Stops the typing out
                    dialogueBox.text = characterDialogue; // Fills the textbox with the entirety of the character's line
                    isDisplayingText = false; // Marks the system as no longer typing out
                }
                else // If the system is not typing out
                {
                    LoadNewLine(); // Loads the next line
                }
            }
            else // If dialogue boxes are not visible
            {
                StartCoroutine(FadeCanvas(uiFadeInSpeed, canvasGroup.alpha, 1)); // Fades in the UI
            }
        }
    }

    /// <summary>
    /// Fades the canvas between opacities
    /// </summary>
    /// <param name="lerpTime">How long it takes for the canvas to fully fade</param>
    /// <param name="start">The starting opacity</param>
    /// <param name="end">The ending opacity</param>
    /// <returns></returns>
    IEnumerator FadeCanvas(float lerpTime, float start, float end)
    {
        if (end == 1) // If fading in
        {
            LoadSceneTextFile(sceneName);
        }

        // For keeping track of the fade
        float timeAtStart = Time.time;
        float timeSinceStart;
        float percentageComplete = 0;

        isFading = true; // Marks the UI as fading
        while (percentageComplete < 1) // Keeps looping until the lerp is complete
        {
            timeSinceStart = Time.time - timeAtStart;
            percentageComplete = timeSinceStart / lerpTime;

            float currentValue = Mathf.Lerp(start, end, percentageComplete);

            canvasGroup.alpha = currentValue;
            yield return new WaitForEndOfFrame();
        }
        isFading = false; // Marks the UI as no longer fading

        showingDialogue = !showingDialogue; // Toggles the representation of whether the UI is visible

        if (end == 1 && !isDisplayingText) // If fading in
        {
            LoadNewLine(); // Loads the next line
        }

        canvasGroup.interactable = canvasGroup.blocksRaycasts = (canvasGroup.alpha == 1);
    }

    //IEnumerator FadeChoices
}