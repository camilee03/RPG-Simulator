using System;
using TMPro;
using UnityEngine;

public class DNDUI : MonoBehaviour
{
    [SerializeField] DNDstats stats;


    [Header("Top Stats")]
    [SerializeField] TMP_Text maxHealthText;
    [SerializeField] TMP_Text currentHealthText;
    [SerializeField] TMP_Text initiativeText;
    [SerializeField] TMP_Text acText;

    [Header("Saving Throws")]
    [SerializeField] TMP_Text conST;
    [SerializeField] TMP_Text strST;
    [SerializeField] TMP_Text dexST;
    [SerializeField] TMP_Text wisST;
    [SerializeField] TMP_Text intST;

    [Header("Modifiers -- CON")]
    [SerializeField] TMP_Text conBase;
    [SerializeField] TMP_Text conMod;

    [Header("Modifiers -- STR")]
    [SerializeField] TMP_Text strBase;
    [SerializeField] TMP_Text strMod;
    [SerializeField] TMP_Text altheticsMod;

    [Header("Modifiers -- DEX")]
    [SerializeField] TMP_Text dexBase;
    [SerializeField] TMP_Text dexMod;
    [SerializeField] TMP_Text acrobaticsMod;
    [SerializeField] TMP_Text sleightOfHandMod;
    [SerializeField] TMP_Text stealthMod;
    //[SerializeField] TMP_Text ;

    [Header("Modifiers -- WIS")]
    [SerializeField] TMP_Text wisBase;
    [SerializeField] TMP_Text wisMod;
    [SerializeField] TMP_Text perceptionMod;
    [SerializeField] TMP_Text insightMod;
    [SerializeField] TMP_Text survivalMod;

    [Header("Modifiers -- INT")]
    [SerializeField] TMP_Text intBase;
    [SerializeField] TMP_Text intMod;
    [SerializeField] TMP_Text investigationMod;
    [SerializeField] TMP_Text natureMod;
    [SerializeField] TMP_Text historyMod;
    //[SerializeField] TMP_Text ;

    [Header("Modifiers -- CHA")]
    [SerializeField] TMP_Text chaBase;
    [SerializeField] TMP_Text chaMod;
    [SerializeField] TMP_Text persuasionMod;
    [SerializeField] TMP_Text deceptionMod;
    [SerializeField] TMP_Text performanceMod;


    TMP_Text[] allStats;

    private void OnEnable()
    {
        DNDstats.UpdateUI += UpdateUI;
    }

    private void OnDisable()
    {
        DNDstats.UpdateUI -= UpdateUI;
    }

    private void Start()
    {
        SetStats();
    }

    private void SetStats()
    {
        allStats = new TMP_Text[] { maxHealthText, currentHealthText, initiativeText, acText,
            conST, strST, dexST, wisST, intST, 
            conBase, conMod,
            strBase, strMod, altheticsMod,
            dexBase, dexMod, acrobaticsMod, sleightOfHandMod, stealthMod,
            wisBase, wisMod, perceptionMod, insightMod, survivalMod,
            intBase, intMod, investigationMod, natureMod, historyMod,
            chaBase, chaMod, persuasionMod, deceptionMod, performanceMod
        };
    }

    private void UpdateUI(object sender, DNDstats.StatArgs e)
    {
        for (int i = 0; i < e.keys.Length; i++)
        {
            int value = e.keyValuePairs[e.keys[i]];

            if ((e.keys[i].EndsWith("Mod") || e.keys[i].EndsWith("ST")) && value > 0)
            {
                allStats[i].text = $"+ {value}";
            }
            else
            {
                allStats[i].text = $"+ {value}";
            }
        }
    }
}
