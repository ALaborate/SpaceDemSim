

using System.Collections;
using System.Collections.Generic;
using TutorialCore;
using UnityEngine;
using UnityEngine.UI;

public class InstructionsSubmenuController : SubmenuController
{
    public Button backButtonEmulator;
    public TutorialActuator.TriggeringParams giveMinute;
    public float minReadTime = 30f;

    public override void OnBecomeVisible()
    {
        MineTutorialTrigger.TranslateAllParams(this);
        if (!PlayerData.instance.tutorialProgress.HasFlag(PlayerData.TutorialProgress.ReadTheLore))
        {
            backButtonEmulator.gameObject.SetActive(true);
            backButtonEmulator.onClick.AddListener(ForceUnclick);
            btnBack.interactable = false;
            StartCoroutine(DelayRead());
        }
        else
        {
            backButtonEmulator.gameObject.SetActive(false);
            btnBack.interactable = true;
        }
    }
    protected void ForceUnclick()
    {
        backButtonEmulator.gameObject.SetActive(false);
        // backButtonEmulator.onClick.RemoveAllListeners();
        TutorialActuator.instance.Trigger(this, giveMinute, new BaseTutorialActuator.TriggeringState());
    }
    protected IEnumerator DelayRead()
    {
        yield return new WaitForSeconds(minReadTime);
        btnBack.interactable = true;
        backButtonEmulator.gameObject.SetActive(false);
        PlayerData.instance.tutorialProgress = PlayerData.instance.tutorialProgress | PlayerData.TutorialProgress.ReadTheLore;
    }
}