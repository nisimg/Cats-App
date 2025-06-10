using System;
using System.Collections.Generic;
using UnityEngine;

namespace _cats.Scripts.Core._cats.Scripts.Core
{
    public class CATSUpdateManager : MonoBehaviour
    {
        private List<ICATSUpdatable> updatableObjects = new();
        private List<ICATSUpdatable> lateUpdatableObjects = new();
        private List<ICATSUpdatable> fixedUpdatableObjects = new();
        
        private Dictionary<int, List<ICATSUpdatable>> intervalUpdates = new();
        private Dictionary<int, int> intervalCounters = new();
        
        private List<ICATSUpdatable> toRemove = new();
        private List<ICATSUpdatable> toAdd = new();
        
        private bool isUpdating = false;
        private static CATSUpdateManager instance;
        
        public static CATSUpdateManager Instance
        {
            get
            {
                if (instance == null)
                {
                    CreateUpdateManager();
                }
                return instance;
            }
        }

        private static void CreateUpdateManager()
        {
            var go = new GameObject("CATSUpdateManager");
            instance = go.AddComponent<CATSUpdateManager>();
            DontDestroyOnLoad(go);
        }

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            isUpdating = true;
            
            // Regular updates
            for (int i = updatableObjects.Count - 1; i >= 0; i--)
            {
                if (i < updatableObjects.Count && updatableObjects[i] != null)
                {
                    try
                    {
                        updatableObjects[i].OnUpdate(Time.deltaTime);
                    }
                    catch (Exception e)
                    {
                        CATSDebug.LogException($"Error in Update for {updatableObjects[i]}: {e}");
                    }
                }
            }
            
            HandleIntervalUpdates();
            
            isUpdating = false;
            
            ProcessPendingChanges();
        }

        private void LateUpdate()
        {
            isUpdating = true;
            
            for (int i = lateUpdatableObjects.Count - 1; i >= 0; i--)
            {
                if (i < lateUpdatableObjects.Count && lateUpdatableObjects[i] != null)
                {
                    try
                    {
                        lateUpdatableObjects[i].OnLateUpdate(Time.deltaTime);
                    }
                    catch (Exception e)
                    {
                        CATSDebug.LogException($"Error in LateUpdate for {lateUpdatableObjects[i]}: {e}");
                    }
                }
            }
            
            isUpdating = false;
        }

        private void FixedUpdate()
        {
            isUpdating = true;
            
            for (int i = fixedUpdatableObjects.Count - 1; i >= 0; i--)
            {
                if (i < fixedUpdatableObjects.Count && fixedUpdatableObjects[i] != null)
                {
                    try
                    {
                        fixedUpdatableObjects[i].OnFixedUpdate(Time.fixedDeltaTime);
                    }
                    catch (Exception e)
                    {
                        CATSDebug.LogException($"Error in FixedUpdate for {fixedUpdatableObjects[i]}: {e}");
                    }
                }
            }
            
            isUpdating = false;
        }

        private void HandleIntervalUpdates()
        {
            foreach (var kvp in intervalUpdates)
            {
                int interval = kvp.Key;
                var objects = kvp.Value;
                
                if (!intervalCounters.ContainsKey(interval))
                {
                    intervalCounters[interval] = 0;
                }
                
                intervalCounters[interval]++;
                
                if (intervalCounters[interval] >= interval)
                {
                    intervalCounters[interval] = 0;
                    
                    for (int i = objects.Count - 1; i >= 0; i--)
                    {
                        if (i < objects.Count && objects[i] != null)
                        {
                            try
                            {
                                objects[i].OnIntervalUpdate(Time.deltaTime * interval);
                            }
                            catch (Exception e)
                            {
                                CATSDebug.LogException($"Error in IntervalUpdate for {objects[i]}: {e}");
                            }
                        }
                    }
                }
            }
        }

        private void ProcessPendingChanges()
        {
            foreach (var obj in toAdd)
            {
                if (!updatableObjects.Contains(obj))
                {
                    updatableObjects.Add(obj);
                }
            }
            toAdd.Clear();
            
            foreach (var obj in toRemove)
            {
                RemoveFromAllLists(obj);
            }
            toRemove.Clear();
        }

        private void RemoveFromAllLists(ICATSUpdatable obj)
        {
            updatableObjects.Remove(obj);
            lateUpdatableObjects.Remove(obj);
            fixedUpdatableObjects.Remove(obj);
            
            foreach (var list in intervalUpdates.Values)
            {
                list.Remove(obj);
            }
        }

       
        public void RegisterUpdatable(ICATSUpdatable updatable)
        {
            if (updatable == null) return;
            
            if (isUpdating)
            {
                if (!toAdd.Contains(updatable))
                    toAdd.Add(updatable);
            }
            else
            {
                if (!updatableObjects.Contains(updatable))
                    updatableObjects.Add(updatable);
            }
        }

        public void RegisterLateUpdatable(ICATSUpdatable updatable)
        {
            if (updatable == null) return;
            
            if (!lateUpdatableObjects.Contains(updatable))
                lateUpdatableObjects.Add(updatable);
        }

        public void RegisterFixedUpdatable(ICATSUpdatable updatable)
        {
            if (updatable == null) return;
            
            if (!fixedUpdatableObjects.Contains(updatable))
                fixedUpdatableObjects.Add(updatable);
        }

        public void RegisterIntervalUpdatable(ICATSUpdatable updatable, int frameInterval)
        {
            if (updatable == null || frameInterval <= 0) return;
            
            if (!intervalUpdates.ContainsKey(frameInterval))
            {
                intervalUpdates[frameInterval] = new List<ICATSUpdatable>();
            }
            
            if (!intervalUpdates[frameInterval].Contains(updatable))
                intervalUpdates[frameInterval].Add(updatable);
        }

        // Unregistration methods
        public void UnregisterUpdatable(ICATSUpdatable updatable)
        {
            if (updatable == null) return;
            
            if (isUpdating)
            {
                if (!toRemove.Contains(updatable))
                    toRemove.Add(updatable);
            }
            else
            {
                RemoveFromAllLists(updatable);
            }
        }

        public void UnregisterLateUpdatable(ICATSUpdatable updatable)
        {
            if (updatable == null) return;
            lateUpdatableObjects.Remove(updatable);
        }

        public void UnregisterFixedUpdatable(ICATSUpdatable updatable)
        {
            if (updatable == null) return;
            fixedUpdatableObjects.Remove(updatable);
        }

        public void UnregisterIntervalUpdatable(ICATSUpdatable updatable, int frameInterval)
        {
            if (updatable == null || !intervalUpdates.ContainsKey(frameInterval)) return;
            intervalUpdates[frameInterval].Remove(updatable);
        }

        public void UnregisterFromAll(ICATSUpdatable updatable)
        {
            UnregisterUpdatable(updatable);
        }

        // Utility methods
        public void PauseUpdates()
        {
            enabled = false;
        }

        public void ResumeUpdates()
        {
            enabled = true;
        }

        public int GetActiveUpdatableCount()
        {
            return updatableObjects.Count;
        }

        public int GetActiveLateUpdatableCount()
        {
            return lateUpdatableObjects.Count;
        }

        public int GetActiveFixedUpdatableCount()
        {
            return fixedUpdatableObjects.Count;
        }

        public void ClearAllUpdatables()
        {
            updatableObjects.Clear();
            lateUpdatableObjects.Clear();
            fixedUpdatableObjects.Clear();
            intervalUpdates.Clear();
            intervalCounters.Clear();
            toAdd.Clear();
            toRemove.Clear();
        }

        private void OnDestroy()
        {
            ClearAllUpdatables();
        }
    }
}