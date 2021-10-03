using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TutorialCore;
using UnityEngine;
using UnityEngine.UI;
using TParams = TutorialCore.TutorialActuator.TriggeringParams;

public class MenuController : MonoBehaviour
{
    [Header("Submenus")]
    public SubmenuController mainButtons;
    public SubmenuController playSubmenu;
    public SubmenuController settingsSubmenu;
    public SubmenuController instructionsSubmenu;
    public SubmenuController creditsSubmenu;
    public SubmenuController tutorialSubmenu;
    [Space]
    public List<GameObject> whatToHide;
    [Header("Buttons")]
    public Button btnBack;
    public Button btnPlay;
    public Button btnSettings;
    public Button btnInstructions;
    public Button btnCredits;
    [Header("Tutorial")]
    public TParams goToSettings;
    public TParams goToLore;


    public State state { get; private set; } = State.none;
    SubmenuController currentController;
    // Start is called before the first frame update
    void Start()
    {
        MineTutorialTrigger.TranslateAllParams(this);
        Translator.instance.onNewLanguageSelected += s => MineTutorialTrigger.TranslateAllParams(this);
        AssignCallbacks();
        ShowNewState(State.tutorial);
    }

    // Update is called once per frame
    void Update()
    {
        ;
    }

    private void BackButtonClick()
    {
        if (currentController != null && currentController.CanHandleBackClick())
        {
            return;
        }
        if (state <= State.main)
        {
            Application.Quit();
        }
        else if (state == State.tutorial)
        {
            ShowNewState(State.main);
        }
        else
        {
            ShowNewState(State.tutorial);
        }
    }
    private BaseTutorialActuator.TriggeringState goToSubmenuState = null;
    private void ShowNewState(State newState)
    {
        if (currentController != null)
        {
            currentController.gameObject.SetActive(false);
        }
        switch (newState)
        {
            case State.main: currentController = mainButtons; break;
            case State.play: currentController = playSubmenu; break;
            case State.settings: currentController = settingsSubmenu; break;
            case State.instructions: currentController = instructionsSubmenu; break;
            // case State.shop: currentController = shopSubmenu; break;
            case State.credits: currentController = creditsSubmenu; break;
            case State.tutorial: currentController = tutorialSubmenu; break;
            case State.none:
            default:
                currentController = null;
                break;
        }

        if (currentController != null)
        {
            currentController.gameObject.SetActive(true);
            currentController.OnBecomeVisible();
            if ((newState == State.settings || newState == State.instructions) && goToSubmenuState != null)
            {
                if (!goToSubmenuState.abort)
                {
                    goToSubmenuState.abort = true;
                    goToSubmenuState.onAbort += () => { goToSubmenuState = null; };
                }
            }
            else if (newState == State.main)
            {
                if (!PlayerData.instance.tutorialProgress.HasFlag(PlayerData.TutorialProgress.TweakTheSettings))
                {
                    // btnBack.interactable = false;
                    btnPlay.interactable = false;
                    btnInstructions.interactable = false;
                    btnCredits.interactable = false;
                    goToSubmenuState = new BaseTutorialActuator.TriggeringState();
                    TutorialActuator.instance.Trigger(this, goToSettings, goToSubmenuState);
                }
                else if (!PlayerData.instance.tutorialProgress.HasFlag(PlayerData.TutorialProgress.ReadTheLore))
                {
                    // btnBack.interactable = false;
                    btnPlay.interactable = false;
                    btnInstructions.interactable = true;
                    btnCredits.interactable = false;
                    goToSubmenuState = new BaseTutorialActuator.TriggeringState();
                    TutorialActuator.instance.Trigger(this, goToLore, goToSubmenuState);
                }
                else
                {
                    // btnBack.interactable = true;
                    btnPlay.interactable = true;
                    btnInstructions.interactable = true;
                    btnCredits.interactable = true;
                }
            }
        }
        state = newState;
    }
    private void AssignCallbacks()
    {
        btnBack.onClick.AddListener(BackButtonClick);
        btnPlay.onClick.AddListener(() => ShowNewState(State.play));
        btnSettings.onClick.AddListener(() => ShowNewState(State.settings));
        btnInstructions.onClick.AddListener(() => ShowNewState(State.instructions));
        btnCredits.onClick.AddListener(() => ShowNewState(State.credits));

        var submenus = from f in GetType().GetFields() where f.FieldType.Name == nameof(SubmenuController) select f.GetValue(this) as SubmenuController;
        foreach (var item in submenus)
        {
            item.needHideMenu += onReceiveHideMenu;
            item.needRestoreMenu += onReceiveShowMenu;
        }
    }
    public enum State
    {
        none = 0,
        main = 10,
        play = 20,
        settings = 30,
        instructions = 40,
        tutorial = 50, //
        credits = 60,
    }

    private void onReceiveHideMenu(SubmenuController sender)
    {
        foreach (var item in whatToHide)
        {
            item.SetActive(false);
        }
    }
    private void onReceiveShowMenu(SubmenuController sender)
    {
        foreach (var item in whatToHide)
        {
            item.SetActive(true);
        }
        currentController.OnBecomeVisible();
    }
}

public abstract class SubmenuController : MonoBehaviour
{
    public Button btnBack;
    public virtual bool CanHandleBackClick() { return false; }
    public virtual void OnBecomeVisible() { }
    public event System.Action<SubmenuController> needHideMenu;
    public event System.Action<SubmenuController> needRestoreMenu;

    protected void HideMenu() { needHideMenu?.Invoke(this); }
    protected void ShowMenu() { needRestoreMenu?.Invoke(this); }
}
