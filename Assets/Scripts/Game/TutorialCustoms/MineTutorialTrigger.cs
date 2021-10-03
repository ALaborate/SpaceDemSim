using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TutorialCore;
using UnityEngine;

public class MineTutorialTrigger : TutorialAppearanceTrigger
{
    [Header("Override params")]
    public string findMineText;
    public string seeMine1stTimeText;



    protected new void Start()
    {
        Translator.instance.TranslateObject(new Translator.Query(this));
        triggeringParams = TranslatedParams(triggeringParams);

        if ((PlayerData.instance.tutorialProgress & PlayerData.TutorialProgress.SeeMine) > 0)
        {
            if ((PlayerData.instance.tutorialProgress & PlayerData.TutorialProgress.FindMine) > 0)
            {
                ; // do nothing: player passed both stages
            }
            else
            {
                // do find mine
                triggeringParams.noSplit = true; //dont show split text
                triggeringParams.text = findMineText;
                highlight = false;
                state = PlayerData.GetTutorialControllingState(PlayerData.TutorialProgress.FindMine, triggeringParams.timeToShow * .5f);
                base.Start();
            }
        }
        else
        {
            // do see mine
            triggeringParams.noSplit = false; //show original text as well
            triggeringParams.textToSolidShow = seeMine1stTimeText;
            state = PlayerData.GetTutorialControllingState(PlayerData.TutorialProgress.SeeMine, triggeringParams.timeToShow * .67f);
            highlight = true;
            base.Start();
        }
    }
    public static TutorialActuator.TriggeringParams TranslatedParams(TutorialActuator.TriggeringParams original)
    {
        var ret = new TutorialActuator.TriggeringParams(original);
        ret.splitLabeling = Translator.instance.TranslateString(original.splitLabeling);
        ret.textToSolidShow = Translator.instance.TranslateString(original.textToSolidShow);
        ret.textToSplitShow = Translator.instance.TranslateString(original.textToSplitShow);
        return ret;
    }
    public static void TranslateAllParams(object parent)
    {
        var fields = from f in parent.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public)
                     where f.FieldType == typeof(TutorialActuator.TriggeringParams)
                     select f;
        foreach (var item in fields)
        {
            if (item.GetValue(parent) is TutorialActuator.TriggeringParams value)
            {
                item.SetValue(parent, TranslatedParams(value));
            }
        }
    }
}
