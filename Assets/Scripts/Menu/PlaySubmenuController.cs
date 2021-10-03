using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlaySubmenuController : SubmenuController
{
    public const string playSceneName = "DeminingScene";

    public Button playButton;
    public Dropdown controllerSelectionDd;
    public Text controllerSelectionText;
    public int controllerChoisExpTreshold = 200;
    public string controllerSelectionAvailable = "Select a controller for next journey";
    public string controllerSelectionUndeserved = "Finnish journey on all available controllers, or gain {0} experience to gain ability to choose controller";


    protected void Awake()
    {

    }

    protected void Start()
    {
        playButton.onClick.AddListener(PlayClicked);
        SceneManager.sceneUnloaded += PlaySceneUnloaded;
        SceneManager.sceneLoaded += PlaySceneLoaded;
        Translator.instance.SubscribeToTranslation(new Translator.Query(this));
        Translator.instance.onNewLanguageSelected += s => PopulateControllersDd();

        PopulateControllersDd();
        RandomizeController();
    }

    public override void OnBecomeVisible()
    {
        //show ships etc
        if (PlayerData.instance.tutorialProgress.HasFlag(PlayerData.instance.controllerTutorials)
            || PlayerData.instance.playerExperience >= controllerChoisExpTreshold)
        {
            controllerSelectionText.text = controllerSelectionAvailable;
            controllerSelectionDd.interactable = true;
        }
        else
        {
            controllerSelectionText.text = string.Format(controllerSelectionUndeserved, controllerChoisExpTreshold);
            controllerSelectionDd.interactable = false;
        }
    }

    protected void PlayClicked()
    {
        var provider = PlayerData.instance.allProviders.Find(p => p.displayName == controllerSelectionDd.options[controllerSelectionDd.value].text);
        PlayerData.instance.BeginSession(PlayerData.instance.allClusters[0], PlayerData.instance.allShips[0], provider);
        SceneManager.LoadSceneAsync(playSceneName, LoadSceneMode.Additive);
    }

    protected void PlaySceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == playSceneName)
        {
            HideMenu();
            SceneManager.SetActiveScene(scene);
        }
    }
    protected void PlaySceneUnloaded(Scene unloaded)
    {
        if (unloaded.name == playSceneName)
        {
            //session ended
            RandomizeController();
            ShowMenu();
        }
    }
    private void PopulateControllersDd()
    {
        controllerSelectionDd.ClearOptions();
        List<Dropdown.OptionData> newOptions = new List<Dropdown.OptionData>();
        foreach (var item in PlayerData.instance.allProviders)
        {
            newOptions.Add(new Dropdown.OptionData(item.displayName, item.displayImage));
        }
        controllerSelectionDd.AddOptions(newOptions);
    }
    private void RandomizeController()
    {
        controllerSelectionDd.value = Random.Range(0, controllerSelectionDd.options.Count);
    }

}