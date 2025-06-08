using System.Collections.Generic;
using UnityEngine;

namespace _cats.Scripts.Core
{
    public class CATSPoolable : MonoBehaviour
    {
        public PoolNames poolName;

        public virtual void OnReturnedToPool()
        {
            this.gameObject.SetActive(false);
        }
        
        public virtual void OnTakenFromPool()
        {
            this.gameObject.SetActive(true);
        }
        
        public virtual void PreDestroy()
        {
        }
    }
    
    public class DDDPool
    {
        public Queue<CATSPoolable> AllPoolables = new();
        public Queue<CATSPoolable> UsedPoolables = new();
        public Queue<CATSPoolable> AvailablePoolables = new();

        public int MaxPoolables = 100;
    }

    public enum PoolNames
    {
        NA = -1,
        ScoreToast = 0,
        TrianglePool = 1
    }
}