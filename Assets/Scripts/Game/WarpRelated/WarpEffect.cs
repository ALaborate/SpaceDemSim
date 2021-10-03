using System.Collections;
using System.Collections.Generic;
using TutorialCore;
using UnityEngine;

public class WarpEffect : MonoBehaviour
{
    public Material warpMaterial;
    public float fadeDuration = 1f;

    private RenderTexture bufer = null;
    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (bufer == null)
        {
            bufer = RenderTexture.GetTemporary(src.descriptor);
        }
        UpdateMaterial();
        Graphics.Blit(src, bufer, warpMaterial);
        Graphics.Blit(bufer, dest);
    }
    void OnDisable()
    {
        RenderTexture.ReleaseTemporary(bufer);
        bufer = null;
        warpMaterial.SetFloat(idCurrentTime, 0);
        warpMaterial.SetFloat(idTimeOfEnd, 0);
    }
    private void UpdateMaterial()
    {
        warpMaterial.SetFloat(idFadeToScreenDuration, fadeDuration);
        warpMaterial.SetFloat(idCurrentTime, Time.time);
        warpMaterial.SetFloat(idTimeOfEnd, _timeOfEnd);
        warpMaterial.SetTexture("_RenderingResult", bufer);
    }

    private static float _timeOfEnd = 0f;
    private static float _lastDuration; //in seconds
    public static bool isActive
    {
        get { return _timeOfEnd < 0; }
        set
        {
            if (!value)
            {
                if (_timeOfEnd < 0)
                {
                    _timeOfEnd = Time.time;
                    _lastDuration = Time.time - _lastDuration;
                }

                TutorialActuator.instance.ReleaseHold();
            }
            else
            {
                _timeOfEnd = -1;
                _lastDuration = Time.time;

                TutorialActuator.instance.AbordAndClearAllOnce();
                TutorialActuator.instance.PutOnHold();
            }
        }
    }
    public static float lastDuration { get { return isActive ? 0f : _lastDuration; } }
    public static float timeOfEnd { get { return _timeOfEnd; } }


    public const int defaultId = int.MaxValue;
    private int _idFadeToScreenDuration = defaultId;
    public int idFadeToScreenDuration
    {
        get
        {
            if (_idFadeToScreenDuration == defaultId)
            {
                _idFadeToScreenDuration = Shader.PropertyToID("_FadeToScreenDuration");
            }
            return _idFadeToScreenDuration;
        }
    }
    private int _idTimeOfEnd = defaultId;
    public int idTimeOfEnd
    {
        get
        {
            if (_idTimeOfEnd == defaultId)
            {
                _idTimeOfEnd = Shader.PropertyToID("_TimeOfEnd");
            }
            return _idTimeOfEnd;
        }
    }
    private int _idCurrentTime = defaultId;
    public int idCurrentTime
    {
        get
        {
            if (_idCurrentTime == defaultId)
            {
                _idCurrentTime = Shader.PropertyToID("_CurrentTime");
            }
            return _idCurrentTime;
        }
    }
}
