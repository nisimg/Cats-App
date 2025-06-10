using System;
using System.Collections;
using _cats.Scripts.Core;
using _cats.Scripts.Core._cats.Scripts.Core;
using UnityEngine;

public class CATSMonoBehaviour : MonoBehaviour, ICATSUpdatable
{
    CATSManager _manager => CATSManager.Instance;

    protected void AddListener(CATSEventNames eventName, Action<object> onGameStart) =>
        _manager.EventsManager.AddListener(eventName, onGameStart);

    protected void RemoveListener(CATSEventNames eventName, Action<object> onGameStart) =>
        _manager.EventsManager.RemoveListener(eventName, onGameStart);

    protected void InvokeEvent(CATSEventNames eventName, object obj) =>
        _manager.EventsManager.InvokeEvent(eventName, obj);

    public Coroutine WaitForSeconds(float time, Action onComplete)
    {
        return StartCoroutine(WaitForSecondsCoroutine(time, onComplete));
    }

    private IEnumerator WaitForSecondsCoroutine(float time, Action onComplete)
    {
        yield return new WaitForSeconds(time);
        onComplete?.Invoke();
    }

    public Coroutine WaitForFrame(Action onComplete)
    {
        return StartCoroutine(WaitForFrameCoroutine(onComplete));
    }

    private IEnumerator WaitForFrameCoroutine(Action onComplete)
    {
        yield return null;
        onComplete?.Invoke();
    }

    public void Start()
    {
        _manager.UpdateManager.RegisterUpdatable(this);
    }

    public virtual void OnUpdate(float deltaTime)
    {
    }

    public virtual void OnLateUpdate(float deltaTime)
    {
    }

    public virtual void OnFixedUpdate(float deltaTime)
    {
    }

    public virtual void OnIntervalUpdate(float deltaTime)
    {
    }

    public void OnDestroy()
    {
        _manager.UpdateManager.UnregisterUpdatable(this);
    }
}