using GoogleSheetsToUnity;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.TestTools;

public class DNDstats : ScriptableObject
{
    public enum SavingThrows { Wisdom, Intelligence, Charisma, Dexterity, Strength, Constitution }
    public static event EventHandler<StatArgs> UpdateUI;
    public class StatArgs : EventArgs
    {
        public Dictionary<string, int> keyValuePairs;
        public string[] keys;
    }


    [HideInInspector]
    public string associatedSheet;// = "1GVXeyWCz0tCjyqE1GWJoayj92rx4a_hu4nQbYmW_PkE";
    [HideInInspector]
    public string associatedWorksheet;// = "Stats";

    public int healthTotal;
    public int healthCurrent;

    public int armorClass;
    public int speed;
    public int initiative;

    public SavingThrows[] proficientST = new SavingThrows[2];

    [Header("Modifiers")]
    public int con;

    public int str;

    public int dex;

    public int wis;

    public int intel;

    public int cha;

    public string[] keys = { "maxHealth", "currentHealth", "initiative", "ac",
            "conST", "strST", "dexST", "wisST", "intST",
            "conBase", "conMod",
            "strBase", "strMod", "altheticsMod",
            "dexBase", "dexMod", "acrobaticsMod", "sleightOfHandMod", "stealthMod",
            "wisBase", "wisMod", "perceptionMod", "insightMod", "survivalMod",
            "intBase", "intMod", "investigationMod", "natureMod", "historyMod",
            "chaBase", "chaMod", "persuasionMod", "deceptionMod", "performanceMod" };


    internal void UpdateStats(GstuSpreadSheet ss)
    {
        Dictionary<string, int> kvp = new();

        /*
        health = int.Parse(ss[name, "Health"].value);
        attack = int.Parse(ss[name, "Attack"].value);
        defence = int.Parse(ss[name, "Defence"].value);
        items.Add(ss[name, "Items"].value.ToString());
        */

        StatArgs stats = new()
        {
            keyValuePairs = kvp,
            keys = keys,
        };

        UpdateUI?.Invoke(this, stats);
    }
}
