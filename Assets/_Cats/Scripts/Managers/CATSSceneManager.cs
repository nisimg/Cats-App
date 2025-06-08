using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _cats.Scripts.Core
{
    public class CATSSceneManager
    {
        private string currentSceneName;
        private Stack<string> sceneHistory = new();
        private List<string> additiveScenes = new();
        private bool isLoading = false;
        
        private CATSSceneTransition currentTransition;
        private Dictionary<string, CATSSceneData> sceneDataCache = new();
        
        // Loading screen reference
        private GameObject loadingScreen;
        private UnityEngine.UI.Slider loadingProgressBar;
        private UnityEngine.UI.Text loadingProgressText;
        
        private const string LOADING_SCREEN_PREFAB = "UI/LoadingScreen";
        
        public CATSSceneManager()
        {
            currentSceneName = SceneManager.GetActiveScene().name;
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
            
            InitializeLoadingScreen();
        }

        ~CATSSceneManager()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
        }

        private void InitializeLoadingScreen()
        {
            var prefab = Resources.Load<GameObject>(LOADING_SCREEN_PREFAB);
            if (prefab != null)
            {
                loadingScreen = GameObject.Instantiate(prefab);
                GameObject.DontDestroyOnLoad(loadingScreen);
                
                // Try to find UI components
                var slider = loadingScreen.GetComponentInChildren<UnityEngine.UI.Slider>();
                if (slider != null) loadingProgressBar = slider;
                
                var text = loadingScreen.GetComponentInChildren<UnityEngine.UI.Text>();
                if (text != null) loadingProgressText = text;
                
                loadingScreen.SetActive(false);
            }
        }

        public void LoadScene(string sceneName, SceneTransitionType transitionType = SceneTransitionType.None, object sceneData = null)
        {
            if (isLoading)
            {
                CATSDebug.LogWarning("Scene is already loading!");
                return;
            }

            if (sceneData != null)
            {
                StoreSceneData(sceneName, sceneData);
            }

            switch (transitionType)
            {
                case SceneTransitionType.None:
                    LoadSceneImmediate(sceneName);
                    break;
                case SceneTransitionType.Fade:
                case SceneTransitionType.LoadingScreen:
                    LoadSceneWithTransition(sceneName, transitionType);
                    break;
            }
        }

        private void LoadSceneImmediate(string sceneName)
        {
            isLoading = true;
            sceneHistory.Push(currentSceneName);
            
            CATSManager.Instance.EventsManager.InvokeEvent(CATSEventNames.OnSceneLoadStart, sceneName);
            SceneManager.LoadScene(sceneName);
        }

        private void LoadSceneWithTransition(string sceneName, SceneTransitionType transitionType)
        {
            isLoading = true;
            sceneHistory.Push(currentSceneName);
            
            // Create transition
            var transitionGO = new GameObject("SceneTransition");
            currentTransition = transitionGO.AddComponent<CATSSceneTransition>();
            GameObject.DontDestroyOnLoad(transitionGO);
            
            currentTransition.StartTransition(transitionType, () =>
            {
                CATSManager.Instance.EventsManager.InvokeEvent(CATSEventNames.OnSceneLoadStart, sceneName);
                
                if (transitionType == SceneTransitionType.LoadingScreen && loadingScreen != null)
                {
                    loadingScreen.SetActive(true);
                    LoadSceneAsync(sceneName);
                }
                else
                {
                    SceneManager.LoadScene(sceneName);
                }
            });
        }

        private void LoadSceneAsync(string sceneName)
        {
            var asyncOperation = SceneManager.LoadSceneAsync(sceneName);
            asyncOperation.allowSceneActivation = false;
            
            // Monitor loading progress
            MonitorAsyncLoading(asyncOperation, sceneName);
        }

        private void MonitorAsyncLoading(AsyncOperation operation, string sceneName)
        {
            CATSManager.Instance.TimeManager.SubscribeTimer(0, () =>
            {
                float progress = Mathf.Clamp01(operation.progress / 0.9f);
                
                // Update loading UI
                if (loadingProgressBar != null)
                    loadingProgressBar.value = progress;
                if (loadingProgressText != null)
                    loadingProgressText.text = $"Loading... {(int)(progress * 100)}%";
                
                CATSManager.Instance.EventsManager.InvokeEvent(CATSEventNames.OnSceneLoadProgress, progress);
                
                if (operation.progress >= 0.9f)
                {
                    CATSManager.Instance.TimeManager.UnSubscribeTimer(0, null);
                    operation.allowSceneActivation = true;
                    
                    if (loadingScreen != null)
                        loadingScreen.SetActive(false);
                }
            });
        }

        public void LoadSceneAdditive(string sceneName, Action<Scene> onLoaded = null)
        {
            if (additiveScenes.Contains(sceneName))
            {
                CATSDebug.LogWarning($"Scene {sceneName} is already loaded additively!");
                return;
            }

            var asyncOp = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            asyncOp.completed += (op) =>
            {
                additiveScenes.Add(sceneName);
                var scene = SceneManager.GetSceneByName(sceneName);
                onLoaded?.Invoke(scene);
                CATSManager.Instance.EventsManager.InvokeEvent(CATSEventNames.OnAdditiveSceneLoaded, sceneName);
            };
        }

        public void UnloadSceneAdditive(string sceneName, Action onUnloaded = null)
        {
            if (!additiveScenes.Contains(sceneName))
            {
                CATSDebug.LogWarning($"Scene {sceneName} is not loaded additively!");
                return;
            }

            var asyncOp = SceneManager.UnloadSceneAsync(sceneName);
            asyncOp.completed += (op) =>
            {
                additiveScenes.Remove(sceneName);
                onUnloaded?.Invoke();
                CATSManager.Instance.EventsManager.InvokeEvent(CATSEventNames.OnAdditiveSceneUnloaded, sceneName);
            };
        }

        public void LoadPreviousScene(SceneTransitionType transitionType = SceneTransitionType.None)
        {
            if (sceneHistory.Count > 0)
            {
                string previousScene = sceneHistory.Pop();
                LoadScene(previousScene, transitionType);
            }
            else
            {
                CATSDebug.LogWarning("No previous scene in history!");
            }
        }

        public void ReloadCurrentScene(SceneTransitionType transitionType = SceneTransitionType.None)
        {
            LoadScene(currentSceneName, transitionType);
        }

        public void StoreSceneData(string sceneName, object data)
        {
            var sceneData = new CATSSceneData
            {
                SceneName = sceneName,
                Data = data,
                Timestamp = DateTime.Now
            };
            
            sceneDataCache[sceneName] = sceneData;
        }

        public T GetSceneData<T>(string sceneName = null) where T : class
        {
            string targetScene = sceneName ?? currentSceneName;
            
            if (sceneDataCache.TryGetValue(targetScene, out var sceneData))
            {
                return sceneData.Data as T;
            }
            
            return null;
        }

        public void ClearSceneData(string sceneName = null)
        {
            if (sceneName != null)
            {
                sceneDataCache.Remove(sceneName);
            }
            else
            {
                sceneDataCache.Clear();
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode == LoadSceneMode.Single)
            {
                currentSceneName = scene.name;
                isLoading = false;
                
                // Complete transition
                if (currentTransition != null)
                {
                    currentTransition.CompleteTransition();
                    GameObject.Destroy(currentTransition.gameObject, 1f);
                    currentTransition = null;
                }
                
                CATSManager.Instance.EventsManager.InvokeEvent(CATSEventNames.OnSceneLoaded, scene.name);
            }
        }

        private void OnSceneUnloaded(Scene scene)
        {
            CATSManager.Instance.EventsManager.InvokeEvent(CATSEventNames.OnSceneUnloaded, scene.name);
        }

        // Public properties
        public string CurrentSceneName => currentSceneName;
        public bool IsLoading => isLoading;
        public List<string> AdditiveScenes => new List<string>(additiveScenes);
        public int SceneHistoryCount => sceneHistory.Count;
    }

    public class CATSSceneData
    {
        public string SceneName;
        public object Data;
        public DateTime Timestamp;
    }

    public enum SceneTransitionType
    {
        None,
        Fade,
        LoadingScreen
    }

    // Simple transition handler
    public class CATSSceneTransition : MonoBehaviour
    {
        private UnityEngine.UI.Image fadeImage;
        private float transitionDuration = 0.5f;

        public void StartTransition(SceneTransitionType type, Action onComplete)
        {
            if (type == SceneTransitionType.Fade)
            {
                CreateFadeOverlay();
                FadeIn(onComplete);
            }
            else
            {
                onComplete?.Invoke();
            }
        }

        public void CompleteTransition()
        {
            if (fadeImage != null)
            {
                FadeOut(() => Destroy(fadeImage.gameObject));
            }
        }

        private void CreateFadeOverlay()
        {
            var canvas = new GameObject("FadeCanvas").AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9999;
            
            var imageGO = new GameObject("FadeImage");
            imageGO.transform.SetParent(canvas.transform, false);
            fadeImage = imageGO.AddComponent<UnityEngine.UI.Image>();
            fadeImage.color = new Color(0, 0, 0, 0);
            
            var rect = fadeImage.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
        }

        private void FadeIn(Action onComplete)
        {
            StartCoroutine(FadeCoroutine(0, 1, onComplete));
        }

        private void FadeOut(Action onComplete)
        {
            StartCoroutine(FadeCoroutine(1, 0, onComplete));
        }

        private IEnumerator FadeCoroutine(float start, float end, Action onComplete)
        {
            float elapsed = 0;
            
            while (elapsed < transitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / transitionDuration;
                float alpha = Mathf.Lerp(start, end, t);
                
                if (fadeImage != null)
                    fadeImage.color = new Color(0, 0, 0, alpha);
                
                yield return null;
            }
            
            onComplete?.Invoke();
        }
    }
}