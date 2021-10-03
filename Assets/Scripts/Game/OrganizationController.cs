using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TutorialCore;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = UnityEngine.Random;
using Test = SnitchOfNetwork.Test;

public class OrganizationController : MonoBehaviour
{
    public FlightController playerController;
    public ReadyToWarpButton controlButton;
    public float warpTime = 3f;
    public float minOrientationTime = 10f;

    [Header("Spawning")]
    public GameObject minePrefab;
    public float minMineDistance = 10;
    public float maxMineDistance = 30;
    public float maxMineVelocity = 1;
    public float maxMineAngularVelocity;
    [Space]
    public float frustrumMarginLeft;
    public float frustrumMarginRigth, frustrumMarginTop, frustrumMarginBottom;
    [Header("Experience gaining")]
    public int maxExpReward = 5;
    public int stableExpPerMine = 1;
    public float maxRewardedTime = 5;
    public AnimationCurve rewardWrtTime;
    public TutorialActuator.TriggeringParams rewardParams;
    public TutorialActuator.TriggeringParams escParams;
    public string bonusDisplayFormat = "Speed bounus: {0} exerience points!";
    public string escNotificationDisplayFormat = "If you end your journey before utilizing all missiles, your experience will not be saved: the command tend not to trust thouse who flee the field with no apparent reason";

    private State _state;
    private const State positionStates = State.operationBase | State.workpoint;
    private State state
    {
        get => _state;
        set
        {
            _state = value;
            controlButton.SetNewState(value);
        }
    }
    public bool locationIsClear { get; private set; }
    private TapProvider tapProvider;
    private SnitchOfNetwork snitch;

    // Start is called before the first frame update

    private List<DateTime> utcCrossTimes = new List<DateTime>();
    private List<DateTime> utcUncrossTimes = new List<DateTime>();
    void Start()
    {
        tapProvider = (from p in PlayerData.instance.allProviders where p is TapProvider select p as TapProvider).FirstOrDefault();
        snitch = new SnitchOfNetwork();
        Translator.instance.SubscribeToTranslation(new Translator.Query(this));
        controlButton.btn.onClick.AddListener(() =>
        {
            if (PlayerData.instance.runtime.currentShip.currentMissileCount > 0)
            {
                StartCoroutine(InitNewPoint(InitWorkpoint));
            }
            else
            {
                EndSession();
            }
        });
        playerController.onSeeTarget += t => utcCrossTimes.Add(DateTime.UtcNow);
        playerController.onStopSeeTarget += t => utcUncrossTimes.Add(DateTime.UtcNow);
        playerController.onFire += target =>
        {
            lastFireTime = DateTime.UtcNow;
            state = state | (PlayerData.instance.runtime.currentShip.currentMissileCount <= 0 ? State.outOfAmmo : State.none);
            float actionSeconds = Convert.ToSingle((lastFireTime - (firstActionAfterWarpTime ?? lastInitPointTime)).TotalSeconds);
            int expGainBonus = Convert.ToInt32(maxExpReward * rewardWrtTime.Evaluate(actionSeconds / maxRewardedTime));
            rewardParams.textToSolidShow = string.Format(bonusDisplayFormat, expGainBonus);
            if (expGainBonus > 0)
                TutorialActuator.TriggerAll(this, rewardParams, new BaseTutorialActuator.TriggeringState());
            PlayerData.instance.runtime.experienceGain += stableExpPerMine + expGainBonus;

            if (target is DestroyableBehaviour db)
            {
                state = state | State.leadingMissile;
                db.onKill += () =>
                {
                    state = state & (~State.leadingMissile);
                    locationIsClear = true;

                    var t = new Test(distanceToTarget, targetViewportPosition.x, targetViewportPosition.y,
                    targetVelocityProjection.x, targetVelocityProjection.y, lastInitPointTime);

                    t.creationDuration = TimeSpan.FromSeconds(WarpEffect.lastDuration);
                    t.userActionStartTimeUTC = firstActionAfterWarpTime ?? lastInitPointTime;
                    t.crossTimes.AddRange(utcCrossTimes);
                    t.uncrossTimes.AddRange(utcUncrossTimes);
                    t.succesTimeUTC = lastFireTime;
                    snitch.session.tests.Add(t);
                };
            }

        };
        InitCluster();
        StartCoroutine(InitBase());
    }

    // Update is called once per frame
    protected bool escapeNotified = false;
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {

            if (escapeNotified)
            {
                WarpEffect.isActive = false;
                EndSession(applyExpGain: false);
            }
            else
            {
                escapeNotified = true;
                escParams.text = string.Format(escNotificationDisplayFormat, PlayerData.instance.runtime.experienceGain);
                TutorialActuator.instance.Trigger(this, escParams, new BaseTutorialActuator.TriggeringState());
            }
        }
    }

    DateTime? firstActionAfterWarpTime;
    private DateTime lastInitPointTime;
    private DateTime lastFireTime;
    void LateUpdate() //sic! dont call GetAngles in Update in order not to interfere in feedback cycle
    {
        if (WarpEffect.isActive)
        {
            firstActionAfterWarpTime = null;
        }
        else
        {
            if (!firstActionAfterWarpTime.HasValue)
            {
                if (playerController.provider.GetAngles().SqrMagnitude() > .1f)
                {
                    firstActionAfterWarpTime = DateTime.UtcNow;
                }
            }
        }
    }

    private IEnumerator InitNewPoint(System.Func<IEnumerator> initializationMethod)
    {
        // RenderSettings.skybox = someMaterial;
        var startTime = Time.time;
        var randWarp = Random.value * warpTime;
        foreach (var item in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            Destroy(item);
        }
        WarpEffect.isActive = true;
        state = state & (PlayerData.instance.runtime.currentShip.currentMissileCount > 0 ? ~State.outOfAmmo : ~State.none) | State.warping;
        StartCoroutine(initializationMethod());
        while (Time.time < startTime + randWarp)
        {
            yield return null;
        }
        state = state & (~State.warping);
        WarpEffect.isActive = false;
        lastInitPointTime = DateTime.UtcNow;
        utcCrossTimes.Clear();
        utcUncrossTimes.Clear();
        yield break;
    }
    float distanceToTarget;
    Vector2 targetVelocityProjection, targetViewportPosition;
    private IEnumerator InitWorkpoint()
    {
        locationIsClear = false;
        var initTime = Time.time;
        state = state & (~positionStates) | State.workpoint;
        var rot = Random.rotationUniform;

        state = state | State.orienting;
        while ((state & State.warping) > 0)
        {
            playerController.transform.rotation = rot; //sic! we force rotation in order for flightcontroller to update feedback
            yield return null;
        }
        tapProvider?.ResetTarget();

        var minePos = new Vector3(Random.Range(frustrumMarginLeft, 1 - frustrumMarginRigth), Random.Range(frustrumMarginBottom, 1 - frustrumMarginTop));
        targetViewportPosition = minePos;
        minePos = Camera.main.ViewportPointToRay(minePos).direction.normalized;
        distanceToTarget = minMineDistance + (maxMineDistance - minMineDistance) * (1 - Mathf.Pow(Random.value, 2));
        var worldPos = Camera.main.transform.position + minePos * distanceToTarget;

        var mineGo = Instantiate(minePrefab, worldPos, Random.rotation);
        var mineRb = mineGo.GetComponent<Rigidbody>();
        mineRb.velocity = Random.insideUnitSphere * maxMineVelocity;
        mineRb.angularVelocity = Random.insideUnitSphere * maxMineAngularVelocity;
        targetVelocityProjection = Camera.main.WorldToViewportPoint(worldPos + mineRb.velocity) - (Vector3)targetViewportPosition;

        while (Time.time < initTime + minOrientationTime)
        {
            yield return null;
        }
        state = state & (~State.orienting);
        yield break;
    }



    private IEnumerator InitBase()
    {
        locationIsClear = true;
        state = state & (~positionStates) | State.operationBase;
        // place base, ships, etc
        yield break;
    }

    private void InitCluster()
    {
        playerController.provider = PlayerData.instance.runtime.rotationProvider;
        SceneManager.LoadSceneAsync(PlayerData.instance.runtime.currentCluster.skyboxSceneName, LoadSceneMode.Additive);
        SceneManager.sceneLoaded += SceneLoadСallback;
        //initialize skybox, lightning, etc
    }
    private void EndSession(bool applyExpGain = true)
    {
        var tf = PlayerData.instance.runtime.rotationProvider.tutorialValue;
        if (!PlayerData.instance.tutorialProgress.HasFlag(tf))
        {
            PlayerData.instance.tutorialProgress = PlayerData.instance.tutorialProgress | tf;
        }

        TutorialActuator.instance.AbordAndClearAllOnce();
        SceneManager.UnloadSceneAsync(playerController.gameObject.scene);
        snitch?.SendAndDispose();
        snitch = null;
        PlayerData.instance.EndSession(applyExpGain);
    }

    private void SceneLoadСallback(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == PlayerData.instance.runtime.currentCluster.skyboxSceneName)
        {
            SceneManager.SetActiveScene(scene);
            SceneManager.sceneLoaded -= SceneLoadСallback;
        }
    }

    [System.Flags]
    public enum State
    {
        none, operationBase = 1, workpoint = 2,
        outOfAmmo = 32, orienting = 64, leadingMissile = 128, warping = 256,
    }
}
