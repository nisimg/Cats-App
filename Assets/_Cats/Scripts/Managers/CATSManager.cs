﻿using System;
using _cats.Core;
using _cats.Scripts.Core._cats.Scripts.Core;
using UnityEngine;

namespace _cats.Scripts.Core
{
    [Serializable]
    public class CATSManager
    {
        public Action onInitAction;
        public static CATSManager Instance { get; set; }
        public CATSEventsManager EventsManager { get; set; }
        public CATSFactoryManager FactoryManager { get; set; }
        public CATSTimeManager TimeManager { get; set; }
        public CATSPoolManager PoolManager { get; set; }
        public CATSPopupManager PopupManager { get; set; }
        public CATSAudioManager AudioManager { get; set; }
        public CATSSceneManager SceneManager { get; set; }
        public CATSUpdateManager UpdateManager { get; set; }

        public CATSCurrencyManager currencyManager;

        public CATSManager(Action<bool> onInitAction)
        {
            if (Instance != null)
            {
                return;
            }

            Instance = this;
            try
            {
                InitManagers(onInitAction);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                onInitAction?.Invoke(false);
            }
        }

        private void InitManagers(Action<bool> onInitAction)
        {
            EventsManager = new CATSEventsManager();
            Debug.Log("EventsManager");
            
            FactoryManager = new CATSFactoryManager();
            Debug.Log("FactoryManager");
            
            TimeManager = new CATSTimeManager();
            Debug.Log("TimeManager");
            
            PoolManager = new CATSPoolManager();
            Debug.Log("PoolManager");
            
            PopupManager = new CATSPopupManager();
            Debug.Log("PopupManager");
            
            AudioManager = new CATSAudioManager();
            Debug.Log("AudioManager");
            
            SceneManager = new CATSSceneManager();
            Debug.Log("SceneManager");
            
            UpdateManager = CATSUpdateManager.Instance;
            Debug.Log("UpdateManager");

            currencyManager = new CATSCurrencyManager();
            onInitAction?.Invoke(true);
            
            
        }
    }
}