

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TutorialCore;
using UnityEngine;
using UnityEngine.UI;


public class SettingsSubmenuController : SubmenuController
{
    public Button eraseProggress;
    public Button disableTutorial;
    public Button restartTutorial;
    public Button restartControllersTutorial;
    public Dropdown langSelection;
    public TutorialActuator.TriggeringParams selectLanguage;

    protected void Start()
    {
        eraseProggress?.onClick.AddListener(EraseClicked);
        disableTutorial?.onClick.AddListener(DisableTutorialClicked);
        restartTutorial?.onClick.AddListener(RestartTutorialClicked);
        restartControllersTutorial?.onClick.AddListener(PlayerData.instance.RestartControllersTutorial);
        PopulateLanguages();
        langSelection.onValueChanged.AddListener(newValue =>
        {
            var name = langSelection.options[newValue].text;
            Translator.instance.ChangeLanguage(name);
        });
        langSelection.value = 0;
    }

    public override void OnBecomeVisible()
    {
        MineTutorialTrigger.TranslateAllParams(this);
        if (!PlayerData.instance.tutorialProgress.HasFlag(PlayerData.TutorialProgress.TweakTheSettings))
        {
            var state = new BaseTutorialActuator.TriggeringState();
            TutorialActuator.instance.Trigger(this, selectLanguage, state);
            btnBack.interactable = false;
            Translator.instance.onNewLanguageSelected += s =>
            {
                PlayerData.instance.tutorialProgress = PlayerData.instance.tutorialProgress | PlayerData.TutorialProgress.TweakTheSettings;
                btnBack.interactable = true;
                state.abort = true;
            };
        }
    }
    protected void EraseClicked()
    {
        PlayerData.instance.ClearData();
    }
    protected void DisableTutorialClicked()
    {
        PlayerData.instance.DisableTutorial();
    }
    protected void RestartTutorialClicked()
    {
        PlayerData.instance.RestartTutorial();
    }
    protected void PopulateLanguages()
    {
        langSelection.ClearOptions();
        var newOptions = new List<Dropdown.OptionData>();
        newOptions.AddRange(from od in Translator.instance.GetLanguageNames() where od != Translator.defaultLangName select new Dropdown.OptionData(od));
        langSelection.AddOptions(newOptions);
    }
}