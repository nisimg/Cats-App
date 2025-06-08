using System;
using System.Collections.Generic;
using _cats.Scripts.Core;
using UnityEngine;

namespace _cats.Scripts.Core
{
    public class CATSPopupManager
    {
        private Dictionary<string, CATSPopup> registeredPopups = new();
        private Queue<PopupQueueData> popupQueue = new();
        private CATSPopup currentPopup;
        private Transform popupCanvas;
        private bool isShowingPopup = false;

        public CATSPopupManager()
        {
            var canvasGO = GameObject.Find("PopupCanvas");
            if (canvasGO == null)
            {
                canvasGO = new GameObject("PopupCanvas");
                var canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 1000;
                canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
                canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            }

            popupCanvas = canvasGO.transform;
            GameObject.DontDestroyOnLoad(canvasGO);
        }

        public void RegisterPopup(string popupName, CATSPopup popup)
        {
            if (!registeredPopups.ContainsKey(popupName))
            {
                registeredPopups.Add(popupName, popup);
                popup.transform.SetParent(popupCanvas);
                popup.gameObject.SetActive(false);
            }
            else
            {
                CATSDebug.LogWarning($"Popup {popupName} already registered!");
            }
        }

        public void ShowPopup(string popupName, object data = null, Action onCloseCallback = null)
        {
            if (registeredPopups.TryGetValue(popupName, out var popup))
            {
                if (isShowingPopup)
                {
                    // Queue the popup if another is showing
                    popupQueue.Enqueue(new PopupQueueData
                    {
                        PopupName = popupName,
                        Data = data,
                        OnCloseCallback = onCloseCallback
                    });
                    return;
                }

                ShowPopupInternal(popup, data, onCloseCallback);
            }
            else
            {
                CATSDebug.LogError($"Popup {popupName} not found!");
            }
        }

        private void ShowPopupInternal(CATSPopup popup, object data, Action onCloseCallback)
        {
            isShowingPopup = true;
            currentPopup = popup;

            popup.gameObject.SetActive(true);
            popup.Show(data, () =>
            {
                onCloseCallback?.Invoke();
                OnPopupClosed();
            });

            CATSManager.Instance.EventsManager.InvokeEvent(CATSEventNames.OnPopupShown, popup);
        }

        public void CloseCurrentPopup()
        {
            if (currentPopup != null && currentPopup.gameObject.activeSelf)
            {
                currentPopup.Close();
            }
        }

        public void CloseAllPopups()
        {
            popupQueue.Clear();
            CloseCurrentPopup();
        }

        private void OnPopupClosed()
        {
            isShowingPopup = false;
            currentPopup = null;

            // Check if there are queued popups
            if (popupQueue.Count > 0)
            {
                var nextPopup = popupQueue.Dequeue();
                ShowPopup(nextPopup.PopupName, nextPopup.Data, nextPopup.OnCloseCallback);
            }
        }

        public bool IsPopupActive(string popupName)
        {
            if (registeredPopups.TryGetValue(popupName, out var popup))
            {
                return popup.gameObject.activeSelf;
            }

            return false;
        }

        public T GetPopup<T>(string popupName) where T : CATSPopup
        {
            if (registeredPopups.TryGetValue(popupName, out var popup))
            {
                return popup as T;
            }

            return null;
        }

        private class PopupQueueData
        {
            public string PopupName;
            public object Data;
            public Action OnCloseCallback;
        }
    }
}