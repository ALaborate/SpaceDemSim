using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FlightController : CrossPlatformBehaviour
{
    public float rotationSensitivity = 1 / 7f;
    public float maxRotationSpeed = 45f;

    public GameObject missilePrefab;
    public float fireDelay = 1f;
    public float maxMissileSpawnRadius = 1.2f;

    public RotationProvider provider;
    public event System.Action<ITarget> onFire;
    public event System.Action<ITarget> onSeeTarget;
    public event System.Action<ITarget> onStopSeeTarget;

    [Header("UI")]
    public Image visorImg;
    public Color idleVisorColor;
    public Color hittingVisorColor;
    public Text missileCount;
    public string formatText = "Missile count: {0}\nExperience gain: {1}";

    private int designationChannel;

    // Start is called before the first frame update
    void Start()
    {
        Translator.instance.SubscribeToTranslation(new Translator.Query(this));
        designationChannel = missilePrefab.GetComponent<Missile>().designationChannel;
        UpdateMissileText();
    }

    private void UpdateMissileText()
    {
        missileCount.text = string.Format(formatText, PlayerData.instance.runtime.currentShip.currentMissileCount, PlayerData.instance.runtime.experienceGain);
    }

    Coroutine fireRoutine;
    private IEnumerator FireRoutine(ITarget target)
    {
        float startTime = Time.time;
        var missile = Instantiate(missilePrefab).GetComponent<Missile>();
        var randAngle = Random.value * Mathf.PI * 2;
        missile.transform.position = transform.TransformPoint(new Vector2(maxMissileSpawnRadius * Mathf.Sin(randAngle), maxMissileSpawnRadius * Mathf.Cos(randAngle)));
        missile.target = target;
        while (Time.time < startTime + fireDelay)
        {

            if ((target.GetSumDesignation() & missile.designationChannel) == 0 || prevTarget != target)
            {
                Destroy(missile.gameObject);
                fireRoutine = null;
                yield break;
            }
            yield return null;
        }
        missile.go = true;
        PlayerData.instance.runtime.currentShip.FireMissile();
        UpdateMissileText();
        Handheld.Vibrate();
        onFire?.Invoke(target);
        missile.onKill += () => fireRoutine = null;
        if (target is DestroyableBehaviour db)
        {
            db.onKill += () =>
            {
                UpdateMissileText();
            };
        }

    }


    private float nextTimeToUpdateFps = 0f;
    private const float fpsPeriod = 0.1f;
    private float fps;

    ITarget prevTarget;
    void Update()
    {
        if (Time.time > nextTimeToUpdateFps)
        {
            nextTimeToUpdateFps = Time.time + fpsPeriod;
            const float fpsFilter = 0.8f;
            fps = fpsFilter * fps + (1 - fpsFilter) * (1f / Time.deltaTime);
        }
        provider.UpdateFeedback(new RotationProvider.Feedback(transform.rotation, new Vector3(rotationSensitivity, rotationSensitivity, rotationSensitivity)));

        transform.Rotate(provider.GetAngles() * rotationSensitivity * Time.deltaTime, Space.Self);

        if ((PlayerData.instance.runtime?.currentShip.currentMissileCount ?? 0) > 0)
        {
            void ResetDesignation()
            {
                if (prevTarget != null)
                {
                    onStopSeeTarget?.Invoke(prevTarget);
                    prevTarget.StopDesignation(designationChannel);
                }
            }
            if (Physics.Raycast(transform.position, transform.forward, out var raycastHit))
            {
                var t = raycastHit.collider.gameObject.GetComponent<ITarget>();
                if (t == null)
                {
                    t = raycastHit.collider.gameObject.transform.parent?.GetComponent<ITarget>();
                }
                if (t != null)
                {
                    if (t != prevTarget)
                    {
                        ResetDesignation();
                        onSeeTarget?.Invoke(t);
                        t.Designate(designationChannel);
                    }
                    prevTarget = t;
                    if (fireRoutine == null && PlayerData.instance.runtime.currentShip.currentMissileCount > 0)
                        fireRoutine = StartCoroutine(FireRoutine(t));
                }
                else
                {
                    ResetDesignation();
                    prevTarget = null;
                }
            }
            else
            {
                ResetDesignation();
                prevTarget = null;
            }
        }

        if (fireRoutine != null)
        {
            visorImg.color = hittingVisorColor;
        }
        else
        {
            visorImg.color = idleVisorColor;
        }
    }

    void OnGUI()
    {
        //Output the rotation rate, attitude and the enabled state of the gyroscope as a Label
        // GUI.Label(new Rect(100, 360, 200, 40), "Gyro rotation rate " + gyroscope.rotationRate);
        // GUI.Label(new Rect(100, 380, 200, 40), "Gyro attitude " + gyroscope.attitude);
        // GUI.Label(new Rect(100, 400, 200, 40), "Gyro enabled : " + gyroscope.enabled);
        // GUI.Label(new Rect(100, 400, 200, 40), "Acceleration : " + $"{Input.acceleration.x:F5}; {Input.acceleration.y:F5}; {Input.acceleration.z:F5}");
        GUI.Label(new Rect(100, 500, 200, 40), "Fps : " + (int)fps);
    }

    void OnDisable()
    {
        provider.Dispose();
    }


}

public class CrossPlatformBehaviour : MonoBehaviour
{
    protected bool isAndroid
    {
        get
        {
#if (UNITY_ANDROID)
            return true;
#else
            return false;
#endif
        }
    }
    protected bool isWindows
    {
        get
        {
#if (UNITY_STANDALONE_WIN)
            return true;
#elif UNITY_EDITOR_WIN
            return true;
#else
            return false;
#endif
        }
    }
    protected bool isEditor
    {
        get
        {
#if UNITY_EDITOR_WIN
            return true;
#else
            return false;
#endif
        }
    }
}

public abstract class RotationProvider : CrossPlatformBehaviour, System.IDisposable
{
    public string displayName;
    public Sprite displayImage;
    public string tutorialString;
    public PlayerData.TutorialProgress tutorialValue;
    public GameObject activateOnUsage;

    public abstract Vector2 GetAngles(); //each component should be clamped to -1...1
    protected Feedback feedback;

    public virtual void UpdateFeedback(Feedback feedback)
    {
        this.feedback = feedback;
        if (activateOnUsage != null && !activateOnUsage.activeInHierarchy)
        {
            activateOnUsage.SetActive(true);
        }
    }

    float lastUpdateTime = 0f;
    protected bool readyToUpdate
    {
        get
        {
            if (Time.time <= lastUpdateTime || feedback.creationTime != Time.time) { return false; }
            lastUpdateTime = Time.time;
            return true;
        }
    }

    protected void Start()
    {
        if (activateOnUsage != null) { activateOnUsage.SetActive(false); }
        Translator.instance.SubscribeToTranslation(new Translator.Query(this));
    }

    public struct Feedback
    {
        //do not change properties externally due to creation time mechanism
        public Quaternion currentRotation { get; private set; }
        public Vector3 maxRotSpeed { get; private set; } //current rotation max angular speed
        public float creationTime { get; private set; }

        public Feedback(Quaternion currentRotation, Vector3 maxRotSpeed)
        {
            this.currentRotation = currentRotation;
            this.maxRotSpeed = maxRotSpeed;
            creationTime = Time.time;
        }
    }

    public virtual void Dispose()
    {
        if (activateOnUsage != null) activateOnUsage.SetActive(false);
    }
}
