using System.Collections;
using System.Collections.Generic;
using TutorialCore;
using UnityEngine;
using UnityEngine.UI;
using TParams = TutorialCore.TutorialActuator.TriggeringParams;

public class TutorialSubmenu : SubmenuController
{
    [Header("Tutorial")]
    public TParams onPressTheButton;
    public TParams onSorry;
    public Button dummyButton;
    [Header("Timing")]
    public float hiddenDuration = 10f;
    public float delay = 2f;

    public override void OnBecomeVisible()
    {
        if (!PlayerData.instance.tutorialProgress.HasFlag(PlayerData.TutorialProgress.TweakTheSettings))
        {
            StartCoroutine(TutorialRoutine());
        }
        else
        {
            StartCoroutine(ShowMain());
        }
        base.OnBecomeVisible();
    }
    bool clicked = false;
    List<BaseTutorialActuator.TriggeringState> states = new List<BaseTutorialActuator.TriggeringState>();
    private IEnumerator TutorialRoutine()
    {
        BaseTutorialActuator.TriggeringState getState()
        {
            states.Add(new BaseTutorialActuator.TriggeringState());
            return states[states.Count - 1];
        }

        btnBack.interactable = false;
        dummyButton.gameObject.SetActive(false);
        yield return new WaitForSeconds(delay);
        while (TutorialActuator.instance == null) yield return null;
        TutorialActuator.instance.Trigger(this, onPressTheButton, new BaseTutorialActuator.TriggeringState());
        yield return new WaitForSeconds(hiddenDuration * .5f);
        TutorialActuator.instance.Trigger(this, onPressTheButton, new BaseTutorialActuator.TriggeringState());
        yield return new WaitForSeconds(hiddenDuration * .5f);
        TutorialActuator.instance.Trigger(this, onSorry, getState());
        dummyButton.gameObject.SetActive(true);
        clicked = false;
        dummyButton.onClick.AddListener(() => { clicked = true; }); //is there a minor memory leak?
        float nextTimeToTrigger = Time.time + hiddenDuration * .7f;
        while (!clicked)
        {
            yield return null;
            if (!clicked && Time.time >= nextTimeToTrigger)
            {
                nextTimeToTrigger = Time.time + hiddenDuration * .7f;
                TutorialActuator.instance.Trigger(this, onPressTheButton, getState());
            }
        }
        foreach (var item in states)
        {
            item.abort = true;
        }
        states.Clear();
        btnBack.onClick.Invoke();
        yield break;
    }
    private IEnumerator ShowMain()
    {
        yield return new WaitForEndOfFrame();
        btnBack.onClick.Invoke();
    }
}