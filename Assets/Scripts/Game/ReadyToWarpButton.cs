using System.Collections;
using System.Collections.Generic;
using TutorialCore;
using UnityEngine;
using UnityEngine.UI;

public class ReadyToWarpButton : StateControllerBehaviour<OrganizationController.State>
{
    [Header("Tutorial")]
    public TutorialActuator.TriggeringParams onBaseText;
    public float baseTutorialDelay = 30f;
    public TutorialActuator.TriggeringParams onWarpText;
    public TutorialActuator.TriggeringParams onOutOfAmmo;
    public float tutorialAcceptDuration = 5f;
    [Space]
    public TutorialActuator.TriggeringParams onNewController;
    [Header("UI")]
    public string outOfAmmoStr = "Out of ammo. Return to base.";
    public string allClearStr = "All clear, ready to proceed!";
    public string lNlStr = "Locked and loaded, ready to proceed.";
    [HideInInspector]
    public Button btn;

    private Text text;
    void Awake()
    {
        btn = GetComponent<Button>();
        text = btn.GetComponentInChildren<Text>();
    }

    protected void Start()
    {
        Translator.instance.SubscribeToTranslation(new Translator.Query(this));
        TranslateAllParams();
        Translator.instance.onNewLanguageSelected += s => TranslateAllParams();
        SetNewState(state);
    }
    private void TranslateAllParams()
    {
        MineTutorialTrigger.TranslateAllParams(this);
    }

    PlayerData.TutorialProgress startedTutorials = PlayerData.TutorialProgress.None;
    // Update is called once per frame
    void Update()
    {
        if ((state & OrganizationController.State.workpoint) > 0 && ((state & disablingStates) == 0))
        {
            if ((startedTutorials & PlayerData.TutorialProgress.WarpFromSpace) == 0)
            {
                if (!PlayerData.instance.tutorialProgress.HasFlag(PlayerData.TutorialProgress.WarpFromSpace))
                {
                    TutorialActuator.TriggerAll(this, onWarpText, PlayerData.GetTutorialControllingState(PlayerData.TutorialProgress.WarpFromSpace, tutorialAcceptDuration));
                    startedTutorials = startedTutorials | PlayerData.TutorialProgress.WarpFromSpace;
                }
            }
            else if (state.HasFlag(OrganizationController.State.outOfAmmo) && ((startedTutorials & PlayerData.TutorialProgress.OutOfAmmo) == 0))
            {
                if ((PlayerData.instance.tutorialProgress & PlayerData.TutorialProgress.OutOfAmmo) == 0)
                {
                    TutorialActuator.TriggerAll(this, onOutOfAmmo, PlayerData.GetTutorialControllingState(PlayerData.TutorialProgress.OutOfAmmo, tutorialAcceptDuration));
                    startedTutorials = startedTutorials | PlayerData.TutorialProgress.OutOfAmmo;
                }
            }
        }
    }

    private const OrganizationController.State disablingStates = OrganizationController.State.orienting | OrganizationController.State.leadingMissile | OrganizationController.State.warping;
    public override void SetNewState(OrganizationController.State newState)
    {
        base.SetNewState(newState);
        if (newState.HasFlag(OrganizationController.State.outOfAmmo))
        {
            text.text = outOfAmmoStr;
        }
        else if ((newState & OrganizationController.State.workpoint) > 0)
        {
            text.text = allClearStr;
        }
        else
        {
            //base
            text.text = lNlStr;
            var provTutor = PlayerData.instance.runtime.rotationProvider.tutorialValue;
            if (!PlayerData.instance.tutorialProgress.HasFlag(provTutor))
            {
                TutorialActuator.TriggerAll(this,
                new BaseTutorialActuator.TriggeringParams(onNewController) { text = PlayerData.instance.runtime.rotationProvider.tutorialString },
                new BaseTutorialActuator.TriggeringState());
            }
            if ((PlayerData.instance.tutorialProgress & PlayerData.TutorialProgress.WarpFromBase) == 0)
            {
                StartCoroutine(DelayBaseTutorial());
            }
        }

        if ((newState & disablingStates) > 0)
        {
            btn.interactable = false;
        }
        else
        {
            btn.interactable = true;
        }
    }

    IEnumerator DelayBaseTutorial()
    {
        yield return new WaitForSeconds(baseTutorialDelay);
        if (state.HasFlag(OrganizationController.State.operationBase) && (state & disablingStates) == 0)
        {
            TutorialActuator.TriggerAll(this, onBaseText, PlayerData.GetTutorialControllingState(PlayerData.TutorialProgress.WarpFromBase, tutorialAcceptDuration));
        }
    }
}

public class StateControllerBehaviour<T> : MonoBehaviour where T : struct, System.Enum
{
    protected T state;
    public virtual void SetNewState(T newState)
    {
        state = newState;
    }
}
