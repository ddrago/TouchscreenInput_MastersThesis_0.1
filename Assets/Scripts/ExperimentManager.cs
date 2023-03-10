using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class ExperimentManager : MonoBehaviour
{
    [Header("Experiment variables")]
    public int instructionMultiplicationNumber = 3;
    public float interSelectionPauseDuration = 0.5f;
    private int participantNumber;
    private bool canSelect = true;

    [Header("GameObject Elements")]
    public LogsManager logsManager;
    public Text instructionGiver;
    public MirrorUIManager mirrorUIManager;
    public TouchscreenMenu touchscreenMenu;
    public ControllerManager controllerManager;
    public VoiceCommands voiceManager;
    public BaselineManager baselineManager;

    [Header("Menu Manager")]
    public GameObject VoiceConditionButton;
    public GameObject TouchscreenConditionButton;
    public GameObject ControllerConditionButton;
    public GameObject BaselineConditionButton;
    public GameObject MainMenu;
    public GameObject VoiceMenu;
    public GameObject TouchscreenMenu;
    public GameObject ControllerMenu;
    public GameObject BaselineMenu;
    public Text ParticipantNumberInputField;

    private static List<string> instructions_to_give = new List<string>(new string[] { "Music", "Calls", "Maps", "News", "Weather", "Terrain" });
    private static List<int> index_instructions_to_give = new List<int>(new int[] { 0, 1, 2, 3, 4, 5 });
    private string next_instruction;

    // Mid-study variables
    public int turnNumber = 0;
    private string currentCondition;
    public bool studyCurrentlyOngoing = false;

    private static System.Random rnd = new System.Random();

    public void StartExperiment()
    {
        logsManager.participantNumber = participantNumber;
        logsManager.InitLogging();
        instructionGiver.text = "Loading...";
        logsManager.LogOnCSV("[START EXPERIMENT]", "N/A", "N/A", 404, 404, true);

        // Set the list of instructions for the participants (for all conditions)
        mirrorUIManager.ServerSetInstructions(index_instructions_to_give, instructions_to_give, instructionMultiplicationNumber);
    }

    public void StartCondition(string condition)
    {
        turnNumber = 0;
        currentCondition = condition;
        studyCurrentlyOngoing = true;

        // If the condition is either controller or touchscreen, take the string instructions list and
        // randomize it. Then update the name of the buttons on the screen to follow this list in the
        // same order. 

        switch (condition)
        {
            case "Voice":
                next_instruction = mirrorUIManager.GetVoiceInstructions()[turnNumber];
                break;
            case "Touchscreen":
                touchscreenMenu.OnStartCondition();
                next_instruction = touchscreenMenu.GetInstructionCorrespondingToIndex(getCurrentInstruction());
                break;
            case "Controller":
                controllerManager.OnStartCondition();
                next_instruction = controllerManager.GetInstructionCorrespondingToIndex(getCurrentInstruction());
                break;
            case "Baseline":
                baselineManager.OnStartCondition();
                break;
            default:
                Debug.LogError("Condition not found");
                break;
        }

        //Show the instruction 
        UpdateInstructionGiver(next_instruction);
        DeactivateButtonsForDuration(interSelectionPauseDuration);

        // Log everything
        logsManager.LogOnCSV(string.Format("[START {0} CONDITION]", condition.ToUpper()), "N/A", "N/A", 404, 404, true);
        logsManager.LogInstructions(mirrorUIManager.GetInstructions());
    }

    public void UpdateInstructionGiver(string instruction)
    {
        instructionGiver.text = instruction;
    }

    public void NextInstruction()
    {
        turnNumber += 1;

        if (mirrorUIManager.GetInstructions().Count != mirrorUIManager.GetVoiceInstructions().Count)
        {
            Debug.LogError("ERROR: index-based instructions and name-based ones should have the same amount of elements");
            return;
        }

        if (turnNumber < mirrorUIManager.GetInstructions().Count)
        {
            switch (currentCondition)
            {
                case "Voice":
                    next_instruction = mirrorUIManager.GetVoiceInstructions()[turnNumber];
                    break;
                case "Touchscreen":
                    next_instruction = touchscreenMenu.GetInstructionCorrespondingToIndex(getCurrentInstruction());
                    break;
                case "Controller":
                    next_instruction = controllerManager.GetInstructionCorrespondingToIndex(getCurrentInstruction());
                    break;
            }

            UpdateInstructionGiver(next_instruction);
        }
        else EndCondition();
    }

    public void EndCondition()
    {
        studyCurrentlyOngoing = false;
        if (currentCondition == "Voice")
            voiceManager.StopKeywordRecognizer();

        logsManager.LogOnCSV(string.Format("[END {0} CONDITION]", currentCondition.ToUpper()), "N/A", "N/A", 404, 404, true);

        instructionGiver.text = "Loading...";

        GoBackToMainMenu();
    }

    public void SetConditionButtonsInteractiveStatus(bool status)
    {
        VoiceConditionButton.GetComponent<Button>().interactable = status;
        TouchscreenConditionButton.GetComponent<Button>().interactable = status;
        ControllerConditionButton.GetComponent<Button>().interactable = status;
        BaselineConditionButton.GetComponent<Button>().interactable = status;
    }

    public void GoBackToMainMenu()
    {
        // We want to be able to see the main menu
        MainMenu.SetActive(true);

        // And we want any other condition menu to disappear
        VoiceMenu.SetActive(false);
        TouchscreenMenu.SetActive(false);
        ControllerMenu.SetActive(false);
        BaselineMenu.SetActive(false);
    }

    public void SelectItem(string item, string targetItem, int i)
    {
        if (studyCurrentlyOngoing)
        {
            FindObjectOfType<AudioManager>().Play("BeepPositive");

            bool targetItemWasSelected = i == getCurrentInstruction();

            Debug.Log(string.Format("CanSelect: {5}, item: {0}, target: {1}, index: {2}, targetIndex: {3}, isCorrect: {4}",
                item,
                targetItem,
                i,
                getCurrentInstruction(),
                targetItemWasSelected,
                canSelect.ToString()));

            logsManager.LogOnCSV(
                string.Format("[{0}]", currentCondition.ToUpper()),
                item,
                targetItem,
                i,
                getCurrentInstruction(),
                targetItemWasSelected);

            NextInstruction();
        }
        else
            Debug.Log("Too early!");

        DeactivateButtonsForDuration(interSelectionPauseDuration);
    }

    public void SelectItemVoiceCondition(string item)
    {
        if (studyCurrentlyOngoing)
        {
            FindObjectOfType<AudioManager>().Play("BeepPositive");

            string targetItem = mirrorUIManager.GetVoiceInstructions()[turnNumber];

            bool targetItemWasSelected = (item == targetItem.ToLower());

            Debug.Log(string.Format("item: {0}, target: {1}, index: {2}, targetIndex: {3}, isCorrect: {4}",
                item,
                targetItem,
                404,
                404,
                targetItemWasSelected));

            logsManager.LogOnCSV(
                string.Format("[{0}]", currentCondition.ToUpper()),
                item,
                targetItem,
                404,
                404,
                targetItemWasSelected);
        }
        else Debug.Log("WARNING: Before providing input, please select a condition.");
    }

    private void DeactivateButtonsForDuration(float pauseDuration)
    {
        //Debug.Log(currentCondition);
        //make it impossible to select for 0.5 seconds after selection
        switch (currentCondition)
        {
            case "Touchscreen":
                touchscreenMenu.SetButtonsInteractability(false);
                break;
            case "Controller":
                controllerManager.SetButtonsInteractability(false);
                break;
            default:
                Debug.LogError("Condition not found");
                break;
        }
        Invoke("enableSelection", pauseDuration);
    }

    public void enableSelection()
    {
        switch (currentCondition)
        {
            case "Touchscreen":
                touchscreenMenu.SetButtonsInteractability(true);
                break;
            case "Controller":
                controllerManager.SetButtonsInteractability(true);
                break;
            default:
                Debug.LogError("Condition not found");
                break;
        }
    }

    public int getCurrentInstruction()
    {
        return mirrorUIManager.GetInstructions()[turnNumber];
    }

    public List<string> GetCopyOfInstructionNames()
    {
        return new List<string>(instructions_to_give);
    }

    public void UpdateParticipantNumber()
    {
        if(!int.TryParse(ParticipantNumberInputField.text, out participantNumber))
            Debug.LogError("ERROR: Participant Number given is not an integer!");
    }
}
