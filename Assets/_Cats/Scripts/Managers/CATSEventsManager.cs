using System;
using System.Collections.Generic;

namespace _cats.Scripts.Core
{
    public class CATSEventsManager
    {
        private Dictionary<CATSEventNames, List<Action<object>>> activeListeners = new();

        public void AddListener(CATSEventNames eventName, Action<object> onGameStart)
        {
            if (activeListeners.TryGetValue(eventName, out var listOfEvents))
            {
                listOfEvents.Add(onGameStart);
                return;
            }
            
            activeListeners.Add(eventName, new List<Action<object>>{onGameStart});
        }
        
        public void RemoveListener(CATSEventNames eventName, Action<object> onGameStart)
        {
            if (activeListeners.TryGetValue(eventName, out var listOfEvents))
            {
                listOfEvents.Remove(onGameStart);

                if (listOfEvents.Count <= 0)
                {
                    activeListeners.Remove(eventName);
                }
            }
        }
        
        public void InvokeEvent(CATSEventNames eventName, object obj)
        {
            if (activeListeners.TryGetValue(eventName, out var listOfEvents))
            {
                for (int i = 0; i < listOfEvents.Count; i++)
                {
                    var action = listOfEvents[i];
                    action.Invoke(obj);
                }
            }
        }
    }
    
    public enum CATSEventNames
    {
        OnPause,
        OfflineTimeRefreshed,
        OnUserBalanceChanged,
        OnValueChanged,
        OnCurrencyChanged,
        OnGameInitialized,
        OnGameStart,
        OnGameOver,
        OnFirstMoveDone,
        OnPopupShown,
        OnMusicChanged,
        OnSceneLoadStart,
        OnSceneLoadProgress,
        OnSceneLoaded,
        OnSceneUnloaded,
        OnAdditiveSceneLoaded,
        OnAdditiveSceneUnloaded
    }
}

