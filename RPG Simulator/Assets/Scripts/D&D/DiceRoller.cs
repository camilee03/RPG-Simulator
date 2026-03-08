using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.TextCore.Text;
using UnityEngine.UI;

public class DiceRoller : MonoBehaviour
{
    [Header("Dice Roll References")]
    [SerializeField] TMP_Text diceNum;
    [SerializeField] GameObject dice;
    [SerializeField] new ParticleSystem particleSystem;
    [SerializeField] PlayerInputManager playerInputManager;

    int finalNumber;
    int index = 1;

    public bool isVisible { get; private set; }

    [Header("Oracle Roll References")]
    [SerializeField] TMP_Text oraclePrompt;
    [SerializeField] TMP_InputField oracleInput;
    [SerializeField] Button oracleButton;
    [SerializeField] GameObject oracleUI;
    [SerializeField] VivoxTextChat vivoxTextChat;
    [SerializeField] UnityEngine.TextAsset secretTable;

    Dictionary<int, int> oracleResult;

    public bool isOracleVisible;
    string currentQuestion;
    int roll1;

    private void Start()
    {
        oracleResult = ReadFromTextFile();
    }

    public Dictionary<int, int> ReadFromTextFile()
    {
        Dictionary<int, int> result = new();

        string[] lines = secretTable.text.Split('\n', System.StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < 20; i++)
        {
            string[] parts = lines[i].Split(':', 2);
            if (parts.Length != 2) Debug.LogError($"Invalid line found on line {i + 1}:\n{lines[i]}");

            if (int.TryParse(parts[0].Trim(), out int d20Number))
            {
                if (d20Number != i+1) Debug.LogError($"Invalid d20 number found on line {i + 1}:\n{d20Number}");
            }
            else Debug.LogError($"Invalid d20 number found on line {i + 1}:\n{d20Number}");

            if (!int.TryParse(parts[1].Trim(), out int secret))  
            {
                Debug.LogError($"Invalid secret value found on line {i + 1}");
            }

            result[d20Number] = secret;
        }

        return result;
    }


    private void Update()
    {
        if (isVisible && Mouse.current.leftButton.wasPressedThisFrame && !EventSystem.current.IsPointerOverGameObject())
        {
            playerInputManager.OnEscape();
        }
    }

    #region Oracle Roll
    public void OracleRoll()
    {
        oracleUI.SetActive(true);
        isOracleVisible = true;

        oracleButton.onClick.AddListener(SubmitOracleQuestion);
        oraclePrompt.text = "Input your oracle question.";
    }

    public void SubmitOracleQuestion()
    {
        currentQuestion = oracleInput.text;

        oracleInput.text = "";
        oracleButton.onClick.RemoveAllListeners();
        oracleButton.onClick.AddListener(SubmitOracleRoll1);
        oraclePrompt.text = "Roll a d20 and enter the result.";
    }

    public void SubmitOracleRoll1()
    {
        if (int.TryParse(oracleInput.text, out int inputInt)
            && inputInt >= 1 && inputInt <= 20) roll1 = inputInt;
        else
        {
            oraclePrompt.text = "Invalid input! Please enter a number between 1 and 20.";
            return;
        }

        oracleInput.text = "";
        oracleButton.onClick.RemoveAllListeners();
        oracleButton.onClick.AddListener(SubmitOracleRoll2);
        oraclePrompt.text = "Now enter your d4 roll result.";
    }

    public void SubmitOracleRoll2()
    {
        int d4Result;
        if (int.TryParse(oracleInput.text, out int inputInt)
            && inputInt >= 1 && inputInt <= 4) d4Result = inputInt;
        else
        {
            oraclePrompt.text = "Invalid input! Please enter a number between 1 and 4.";
            return;
        }

        playerInputManager.OnEscape();

        // Send oracle question and roll results to the DM (host) through Vivox 
        int d20Result = CalculateOracleResult(roll1);
        string oracleMessage = $"/o~ {currentQuestion} -- D20 Roll: {roll1} -- D20 Table Result: {d20Result} -- D4 Roll: {d4Result}";
        vivoxTextChat.SendTextMessage(oracleMessage);
    }

    private int CalculateOracleResult(int roll)
    {
        int result = oracleResult[roll];
        int key = 6969;

        result = result / key;

        int check_result = result * key;
        if (check_result != oracleResult[roll]) Debug.LogError($"Oracle result calculation failed integrity check! {check_result} != {oracleResult[roll]}");

        return result;
    }

    public void ExitOracle()
    {
        oracleUI.SetActive(false);
        isOracleVisible = false;
        oracleInput.text = "";
        oracleButton.onClick.RemoveAllListeners();
    }

    #endregion

    #region Dice Roll

    public void RollDice()
    {
        dice.SetActive(true);
        isVisible = true;
        ChooseNumber(1);
    }

    public void Exit()
    {
        dice.SetActive(false);
        isVisible = false;
    }


    private async void ChooseNumber(float currentX)
    {
        await UpdateY(currentX);

        // Update number here
        finalNumber = Random.Range(1, 21);
        diceNum.text = finalNumber.ToString();

        if (currentX <= 10) ChooseNumber(currentX + index);
        else
        {
            particleSystem.Play();
        }
    }

    async Task UpdateY(float currentX)
    {
        float y = Mathf.Log(currentX, 10);

        await Task.Run(() =>
        {
            Thread.Sleep((int)(y*500));
        });

    }

    #endregion
}
