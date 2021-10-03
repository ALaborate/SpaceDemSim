using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TutorialCore
{
    public class TutorialAppearanceTrigger : MonoBehaviour
    {
        public TutorialActuator.TriggeringParams triggeringParams;
        public bool highlight = true;

        protected TutorialActuator.TriggeringState state = new BaseTutorialActuator.TriggeringState();
        protected float startTime;

        // Start is called before the first frame update
        protected void Start()
        {
            if (highlight)
                state.worldHighlightPos = transform.position;
            startTime = Time.time;
            TutorialActuator.TriggerAll(this, triggeringParams, state);
        }

        protected void Update()
        {
            if (state != null && state.worldHighlightPos.HasValue)
                state.worldHighlightPos = transform.position;
        }

        protected void OnDisable()
        {
            if (state != null && state.worldHighlightPos.HasValue)
                state.abort = true;
        }
    }
}