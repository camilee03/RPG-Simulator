using System.Collections;
using TMPro;
using UnityEngine;
using System.Threading.Tasks;
using System.Threading;

public class DiceRoller : MonoBehaviour
{
    [SerializeField] TMP_Text diceNum;
    [SerializeField] GameObject dice;
    [SerializeField] new ParticleSystem particleSystem;

    int finalNumber;
    int index = 1;

    public bool isVisible { get; private set; }

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
            Thread.Sleep((int)(y*750));
        });

    }
}
