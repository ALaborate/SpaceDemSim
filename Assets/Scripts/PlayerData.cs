using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;


public class PlayerData : MonoBehaviour
{
    public static PlayerData instance { get; private set; }

    public List<Ship> allShips;
    public List<Cluster> allClusters;
    public List<RotationProvider> allProviders;

    [HideInInspector] public TutorialProgress tutorialProgress;

    public Runtime runtime { get; private set; } = null;

    #region Persistents
    public int playerExperience { get; private set; }
    public IList<string> availableShipNames { get; private set; }
    public IList<string> availableClusterNames { get; private set; }
    #endregion

    public event System.Action<int> onChangeExperience;

    public const string SHIPS_KEY = "availShips";
    public const string CLUSTERS_KEY = "availClusters";
    public const char SEPARATOR = ',';
    public const string EXPERIENCE_KEY = "experience";
    public const string TUTORIAL_STATE_KEY = "tutorialProgress";

    void Awake()
    {
        instance = this;
        LoadPersistents();
    }
    void OnApplicationQuit()
    {
        SavePersistents();
    }

    private void LoadPersistents()
    {
        var ships = PlayerPrefs.GetString(SHIPS_KEY, string.Empty);
        if (!string.IsNullOrEmpty(ships))
        {
            availableShipNames = ships.Split(SEPARATOR).ToList();
        }
        else availableShipNames = new List<string>();

        var clusters = PlayerPrefs.GetString(CLUSTERS_KEY, string.Empty);
        if (!string.IsNullOrEmpty(clusters))
        {
            availableClusterNames = clusters.Split(SEPARATOR).ToList();
        }
        else availableClusterNames = new List<string>();

        playerExperience = PlayerPrefs.GetInt(EXPERIENCE_KEY, default);
        LoadTutorialProgress();

        controllerTutorials = TutorialProgress.None;
        foreach (var item in PlayerData.instance.allProviders)
        {
            controllerTutorials = controllerTutorials | item.tutorialValue;
        }
    }
    private void SavePersistents()
    {
        var s = SEPARATOR.ToString();
        PlayerPrefs.SetString(SHIPS_KEY, string.Join(s, availableShipNames));
        PlayerPrefs.SetString(CLUSTERS_KEY, string.Join(s, availableClusterNames));
        PlayerPrefs.SetInt(EXPERIENCE_KEY, playerExperience);
        SaveTutorialProgress();
    }
    private void LoadTutorialProgress()
    {
        var values = System.Enum.GetValues(typeof(TutorialProgress)).Cast<TutorialProgress>().ToArray();
        var names = System.Enum.GetNames(typeof(TutorialProgress));
        var passedNames = new HashSet<string>(PlayerPrefs.GetString(TUTORIAL_STATE_KEY, string.Empty).Split(SEPARATOR));
        var progress = TutorialProgress.None;
        for (int i = 0; i < names.Length; i++)
        {
            if (passedNames.Contains(names[i]))
            {
                progress = progress | values[i];
            }
        }
        tutorialProgress = progress;
    }
    private void SaveTutorialProgress()
    {
        var values = System.Enum.GetValues(typeof(TutorialProgress)).Cast<TutorialProgress>().ToArray();
        var names = System.Enum.GetNames(typeof(TutorialProgress));
        StringBuilder sb = new StringBuilder(names.Length);
        for (int i = 0; i < names.Length; i++)
        {
            if ((tutorialProgress & values[i]) > 0)
            {
                sb.Append(names[i]);
                sb.Append(SEPARATOR);
            }
        }
        PlayerPrefs.SetString(TUTORIAL_STATE_KEY, sb.ToString());
    }


    public void BeginSession(Cluster destination, Ship ship, RotationProvider provider)
    {
        runtime = new Runtime(destination, ship, provider);
    }
    public void EndSession(bool applyExpGain)
    {
        if (runtime != null)
        {
            SceneManager.UnloadSceneAsync(runtime.currentCluster.skyboxSceneName);
            if (applyExpGain)
                playerExperience += runtime.experienceGain;
            onChangeExperience?.Invoke(playerExperience);
            runtime.Dispose();
        }
        runtime = null;
    }
    public void ClearData()
    {
        var psf = from fi in this.GetType().GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                  where fi.IsLiteral && fi.FieldType.Name == typeof(string).Name
                  select fi;
        foreach (var item in psf)
        {
            var value = item.GetValue(this).ToString();
            PlayerPrefs.DeleteKey(value);
        }
        Awake();
        onChangeExperience?.Invoke(playerExperience);
    }
    public void DisableTutorial()
    {
        var values = System.Enum.GetValues(typeof(TutorialProgress)).Cast<TutorialProgress>().ToArray();
        foreach (var item in values)
        {
            tutorialProgress = tutorialProgress | item;
        }
        SaveTutorialProgress();
    }
    public void RestartTutorial()
    {
        tutorialProgress = TutorialProgress.None;
        PlayerPrefs.DeleteKey(TUTORIAL_STATE_KEY);
    }

    public TutorialProgress controllerTutorials { get; private set; }
    public void RestartControllersTutorial()
    {
        tutorialProgress = tutorialProgress & (~controllerTutorials);
        SaveTutorialProgress();
    }


    public class Runtime : System.IDisposable
    {
        public GameMode gameMode = GameMode.test;
        public Cluster currentCluster;
        public Ship currentShip;
        public RotationProvider rotationProvider;
        public int experienceGain = 0; //valid through session, on session end it converts to experience

        public Runtime(Cluster destination, Ship ship, RotationProvider rotationProvider)
        {
            currentCluster = destination;
            ship.LoadMissiles();
            currentShip = ship;
            this.rotationProvider = rotationProvider;
        }

        public void Dispose()
        {
            ;
        }

        public enum GameMode
        {
            none = 0, test = 1, game = 2
        }
    }

    [System.Flags]
    public enum TutorialProgress
    {
        None = 0,
        SeeMine = 1 << 0, FindMine = 1 << 1,
        WarpFromBase = 1 << 2, WarpFromSpace = 1 << 3, OutOfAmmo = 1 << 4,
        AccelerometerController = 1 << 6, ButtonsController = 1 << 7, TapController = 1 << 8, DragController = 1 << 9,
        TweakTheSettings = 1 << 10, ReadTheLore = 1 << 11,

    }

    [System.Serializable]
    public struct Ship
    {
        public string shipName; // = "MSA Invincible";
        public int missileFullLoad;

        public int currentMissileCount { get; private set; }

        public void LoadMissiles() { currentMissileCount = missileFullLoad; }
        public void FireMissile() { currentMissileCount--; }
    }

    [System.Serializable]
    public class Cluster
    {
        public string clusterName; // = "Lentis-gamma";
        public string skyboxSceneName;
    }

    public static TutorialCore.TutorialActuator.TriggeringState GetTutorialControllingState(TutorialProgress progressPoint, float minSecondsToPass = float.PositiveInfinity)
    {
        var ret = new TutorialCore.TutorialActuator.TriggeringState();
        ret.onSucces += () =>
        {
            PlayerData.instance.tutorialProgress = PlayerData.instance.tutorialProgress | progressPoint;
        };
        if (minSecondsToPass < float.PositiveInfinity)
        {
            var minTimeToShow = Time.time + minSecondsToPass;
            ret.onAbort += () =>
            {
                if (Time.time >= minTimeToShow)
                {
                    PlayerData.instance.tutorialProgress = PlayerData.instance.tutorialProgress | progressPoint;
                }
            };
        }
        return ret;
    }
}