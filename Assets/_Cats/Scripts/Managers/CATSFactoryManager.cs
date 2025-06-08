using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace _cats.Scripts.Core
{
    public class CATSFactoryManager
    {
        public void CreateAsync<T>(string name, Vector3 pos, Action<T> onCreated) where T : Object
        {
            var original = Resources.Load<T>(name);
            CreateAsync(original, pos, onCreated);
        } 
        
        public void CreateAsync<T>(T origin, Vector3 pos, Action<T> onCreated) where T : Object
        {
            var clone = Object.Instantiate(origin, pos, Quaternion.identity);
            onCreated?.Invoke(clone);
        }

        public void MultiCreateAsync<T>(T origin, Vector3 pos, int amount, Action<List<T>> onCreated) where T : Object
        {
            List<T> createdObjects = new List<T>();
            
            for (var i = 0; i < amount; i++)
            {
                CreateAsync(origin, pos, OnCreated);
            }

            void OnCreated(T createdObject)
            {
                createdObjects.Add(createdObject);

                if (createdObjects.Count == amount)
                {
                    onCreated?.Invoke(createdObjects);
                }
            }
        }
    }
}




namespace _cats.Scripts.Core
{
    public class CATSPoolManager
    {
        private Dictionary<PoolNames, DDDPool> Pools = new();
        
        private Transform rootPools;

        public CATSPoolManager()
        {
            rootPools = new GameObject("PoolsHolder").transform;
            Object.DontDestroyOnLoad(rootPools);
        }
        
        public void InitPool(PoolNames poolName, int amount)
        {
            
        }
        
        public void InitPool(string resourceName, int amount, int maxAmount = 100)
        {
            var original = Resources.Load<CATSPoolable>(resourceName);
            InitPool(original, amount,  maxAmount);
        }
        
        public void InitPool(CATSPoolable original, int amount, int maxAmount)
        {
            CATSManager.Instance.FactoryManager.MultiCreateAsync(original, Vector3.zero, amount, 
                delegate(List<CATSPoolable> list)
                {
                    foreach (var poolable in list)
                    {
                        poolable.name = original.name;
                        poolable.transform.parent = rootPools;
                        poolable.gameObject.SetActive(false);
                    }
                    
                    var pool = new DDDPool
                    {
                        AllPoolables = new Queue<CATSPoolable>(list),
                        UsedPoolables = new Queue<CATSPoolable>(),
                        AvailablePoolables = new Queue<CATSPoolable>(list),
                        MaxPoolables = maxAmount
                    };

                    Pools.Add(original.poolName, pool);
                });
        }

        public CATSPoolable GetPoolable(PoolNames poolName)
        {
            if (Pools.TryGetValue(poolName, out DDDPool pool))
            {
                if (pool.AvailablePoolables.TryDequeue(out CATSPoolable poolable))
                {

                    poolable.OnTakenFromPool();
                    
                    pool.UsedPoolables.Enqueue(poolable);
                    poolable.gameObject.SetActive(true);
                    return poolable;
                }
                

                return null;
            }

            return null;
        }
        
        
        public void ReturnPoolable(CATSPoolable poolable)
        {
            if (Pools.TryGetValue(poolable.poolName, out DDDPool pool))
            {
                pool.AvailablePoolables.Enqueue(poolable);
                poolable.OnReturnedToPool();
                poolable.gameObject.SetActive(false);
            }
        }


        public void DestroyPool(PoolNames name)
        {
            if (Pools.TryGetValue(name, out DDDPool pool))
            {
                foreach (var poolable in pool.AllPoolables)
                {
                    poolable.PreDestroy();
                    ReturnPoolable(poolable);
                }
                
                foreach (var poolable in pool.AllPoolables)
                {
                    Object.Destroy(poolable);
                }

                pool.AllPoolables.Clear();
                pool.AvailablePoolables.Clear();
                pool.UsedPoolables.Clear();
                
                Pools.Remove(name);
            }
        }
    }
}