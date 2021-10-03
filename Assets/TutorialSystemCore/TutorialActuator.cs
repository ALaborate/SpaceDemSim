using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace TutorialCore
{

    public class TutorialActuator : BaseTutorialActuator
    {

    }
    public struct DisplayingParams
    {
        public bool holdStarting;
        public bool holdCurrent;
        public bool discardCurrent;

        public bool clearQueueOnce;

        public DisplayingParams(DisplayingParams other)
        {
            this.holdStarting = other.holdStarting;
            this.holdCurrent = other.holdCurrent;
            this.discardCurrent = other.discardCurrent;
            this.clearQueueOnce = other.clearQueueOnce;
        }

        public static DisplayingParams Default
        {
            get
            {
                return new DisplayingParams();
            }
        }
        public static DisplayingParams AbordAll
        {
            get
            {
                return new DisplayingParams()
                {
                    discardCurrent = true,
                    clearQueueOnce = true,
                };
            }
        }
    }

    public class BaseTutorialActuator : MonoBehaviour
    {
        public static List<BaseTutorialActuator> actuators = new List<BaseTutorialActuator>();
        public static BaseTutorialActuator instance { get { return actuators == null || actuators.Count == 0 ? null : actuators[0]; } }

        public DisplayingParams displayingParams { get; set; }
        ///<summary> Delegates in this list are executed at the end of update. Return true to unsubscribe. Usecase: set <see cref="displayingParams"/> to default after clearing queue.</summary>
        [HideInInspector]
        public List<System.Func<bool>> onQueueUpdated { get; } = new List<Func<bool>>();


        public RectTransform highliter;
        public Text textComponent;
        [Space]
        public float textUpdatePeriod = 0.25f;

        protected void Start()
        {
            actuators.Add(this);
            displayingParams = DisplayingParams.Default;
            queue = new Queue<Tuple<TriggeringParams, TriggeringState>>();
            highliter.gameObject.SetActive(false);
            textComponent.gameObject.SetActive(false);
        }
        private Queue<Tuple<TriggeringParams, TriggeringState>> queue;
        protected void LateUpdate()
        {
            if (displayingParams.clearQueueOnce)
            {
                queue.Clear();
            }
            if (routine == null && !displayingParams.holdStarting && queue.Count > 0)
            {
                var par = queue.Dequeue();
                routine = StartCoroutine(HighlightRoutine(par.Item1, par.Item2));
            }
            for (int i = 0; i < onQueueUpdated.Count; i++)
            {
                if (onQueueUpdated[i]())
                {
                    onQueueUpdated.RemoveAt(i--);
                }
            }
        }


        private Coroutine routine;
        private IEnumerator HighlightRoutine(TriggeringParams parameters, TriggeringState triggeringState)
        {
            var worldHighlightPos = triggeringState.worldHighlightPos;
            var words = from w in parameters.textToSplitShow.Split(' ') where !string.IsNullOrEmpty(w) select w;
            var textToShow = new List<string>(12);
            if (!parameters.noSplit)
            {
                textToShow.AddRange(words);
            }
            textToShow.Add(parameters.textToSolidShow);

            textComponent.text = string.Empty;
            textComponent.gameObject.SetActive(true);
            var startTime = Time.time;
            var nextTimeToUpdText = startTime + textUpdatePeriod;
            for (int i = 0; i < textToShow.Count; i++)
            {
                worldHighlightPos = triggeringState.worldHighlightPos;
                UpdateHighlighterPos(worldHighlightPos);
                highliter.gameObject.SetActive(worldHighlightPos.HasValue ? (i % 2) == 0 : false);


                if (Time.time >= nextTimeToUpdText)
                {
                    nextTimeToUpdText = Time.time + textUpdatePeriod;
                    textComponent.text = textToShow[i];
                }
                else
                {
                    i--;
                }

                if (ShouldBrake()) { yield break; }

                yield return null;
            }
            highliter.gameObject.SetActive(worldHighlightPos.HasValue);

            while (Time.time <= startTime + parameters.timeToShow)
            {
                worldHighlightPos = triggeringState.worldHighlightPos;
                UpdateHighlighterPos(worldHighlightPos);
                if (ShouldBrake()) { yield break; }
                yield return null;
            }
            ClearRoutine(triggeringState);
            yield break;

            void UpdateHighlighterPos(Vector3? whp)
            {
                if (!whp.HasValue) return;
                var pos = Camera.main.WorldToScreenPoint(whp.Value);
                highliter.anchorMin = highliter.anchorMax = Vector2.zero;
                highliter.anchoredPosition = pos;
            }
            bool ShouldBrake()
            {
                if (triggeringState.abort || displayingParams.holdCurrent || displayingParams.discardCurrent)
                {
                    if (!displayingParams.discardCurrent && !triggeringState.abort)
                    {
                        var lst = new List<Tuple<TriggeringParams, TriggeringState>>(queue);
                        lst.Insert(0, new Tuple<TriggeringParams, TriggeringState>(parameters, triggeringState));
                        queue = new Queue<Tuple<TriggeringParams, TriggeringState>>(lst);
                    }

                    ClearRoutine(triggeringState, aborted: true);
                    return true;
                }
                return false;
            }
        }
        private void ClearRoutine(TriggeringState state, bool aborted = false)
        {
            highliter.gameObject.SetActive(false);
            textComponent.gameObject.SetActive(false);
            if (aborted)
            {
                state.InvokeAbort();
            }
            else
            {
                state.InvokeSucces();
            }
            routine = null;
        }

        public void Trigger(Component sender, TriggeringParams parameters, TriggeringState triggeringState)
        {
            if (Filter(ref sender, ref parameters, ref triggeringState))
            {
                queue.Enqueue(new Tuple<TriggeringParams, TriggeringState>(parameters, triggeringState));
            }
        }

        public static void TriggerAll(Component sender, TriggeringParams parameters, TriggeringState triggeringState)
        {
            foreach (var item in actuators)
            {
                item.Trigger(sender, parameters, triggeringState);
            }
        }
        public void AbordAndClearAllOnce()
        {
            var dPar = new DisplayingParams(displayingParams);
            dPar.clearQueueOnce = true;
            dPar.discardCurrent = true;
            TutorialActuator.instance.displayingParams = dPar;
            onQueueUpdated.Add(() =>
            {
                var dParInner = new DisplayingParams(displayingParams);
                dParInner.clearQueueOnce = false;
                dParInner.discardCurrent = false;
                TutorialActuator.instance.displayingParams = dParInner;
                return true; //used to unsubscribe
            });
        }
        public void PutOnHold()
        {
            var dPar = new DisplayingParams(displayingParams);
            dPar.holdStarting = true;
            dPar.holdCurrent = true;
            TutorialActuator.instance.displayingParams = dPar;
        }
        public void ReleaseHold()
        {
            var dPar = new DisplayingParams(displayingParams);
            dPar.holdStarting = false;
            dPar.holdCurrent = false;
            TutorialActuator.instance.displayingParams = dPar;
        }




        protected virtual bool Filter(ref Component sender, ref TriggeringParams parameters, ref TriggeringState triggeringState)
        {
            return true;
        }
        public enum Buttons
        {
            None = 0, OK,
        }
        [System.Serializable]
        public struct TriggeringParams
        {
            public Buttons buttons;
            public float timeToShow;
            public float radius;
            // public string textToShow;
            public string splitLabeling;
            public string textToSplitShow;
            public string textToSolidShow;
            public bool noSplit;

            public TriggeringParams(TriggeringParams other)
            {
                this.buttons = other.buttons;
                this.timeToShow = other.timeToShow;
                this.radius = other.radius;
                this.textToSplitShow = other.textToSplitShow;
                this.textToSolidShow = other.textToSolidShow;
                this.splitLabeling = other.splitLabeling;
                this.noSplit = other.noSplit;
                // textToShow = labeling = ""; //todo
            }
            public string text
            {
                get
                {
                    return noSplit ? textToSolidShow : textToSplitShow;
                }
                set
                {
                    if (noSplit)
                    {
                        textToSolidShow = value;
                    }
                    else
                    {
                        textToSplitShow = splitLabeling + " " + value;
                    }
                }
            }
        }

        public class TriggeringState
        {
            public Vector3? worldHighlightPos;
            public bool abort = false;
            public event System.Action onSucces;
            public event System.Action onAbort;

            public void InvokeAbort() => onAbort?.Invoke();
            public void InvokeSucces() => onSucces?.Invoke();
        }

    }
}

