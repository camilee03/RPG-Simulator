using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class DiceRoller : MonoBehaviour
{
    [SerializeField] TMP_Text diceNum;
    [SerializeField] GameObject dice;
    [SerializeField] new ParticleSystem particleSystem;
    [SerializeField] PlayerInputManager playerInputManager;

    int finalNumber;
    int index = 1;

    public bool isVisible { get; private set; }


    private void Update()
    {
        if (isVisible && Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
        {
            playerInputManager.OnEscape();
        }
    }

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
        else particleSystem.Play();
    }

    async Task UpdateY(float currentX)
    {
        float y = Mathf.Log(currentX, 10);

        await Task.Run(() =>
        {
            Thread.Sleep((int)(y*500));
        });

    }
}
