using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimerManager : MonoBehaviour
{
    public delegate void Callback();
    public class TimedEvent
    {
        public float TimeToExecute = 0f;
        public Callback Method = null;
    }

    #region Private Fields

    private List<TimedEvent> _events = new List<TimedEvent>();

    #endregion

    #region Monobehavior Callbacks

    private void Update()
    {
        if (_events.Count == 0) return;

        for (int i = 0; i < _events.Count; i++)
        {
            TimedEvent timedEvent = _events[i];
            if (timedEvent.TimeToExecute <= Time.time)
            {
                timedEvent.Method();
                _events.Remove(timedEvent);
            }
        }
    }

    #endregion

    public void Add(Callback callback, float inSeconds)
    {
        _events.Add(new TimedEvent { TimeToExecute = Time.time + inSeconds, Method = callback });
    }
}
