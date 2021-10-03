using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShowExperience : MonoBehaviour
{
    public string experienceTemplateText = "Experience: {0}";
    Text text;
    void Awake()
    {
        text = GetComponent<Text>();
    }

    // Update is called once per frame
    void Start()
    {
        UpdExp(PlayerData.instance.playerExperience);
        PlayerData.instance.onChangeExperience += UpdExp;
        Translator.instance.SubscribeToTranslation(new Translator.Query(this));
        Translator.instance.onNewLanguageSelected += nl => UpdExp(prevExp);
    }

    private int prevExp;
    private void UpdExp(int newExp)
    {
        prevExp = newExp;
        // newExp = PlayerData.instance.playerExperience; //just in case
        text.text = string.Format(experienceTemplateText, newExp);
    }
}
