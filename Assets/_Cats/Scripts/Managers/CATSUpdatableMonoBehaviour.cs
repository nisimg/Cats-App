using UnityEngine;

namespace _cats.Scripts.Core._cats.Scripts.Core
{
    public abstract class CATSUpdatableMonoBehaviour : MonoBehaviour, ICATSUpdatable
    {
        protected bool autoRegister = true;
        protected bool useRegularUpdate = true;
        protected bool useLateUpdate = false;
        protected bool useFixedUpdate = false;
        protected int intervalUpdateFrames = 0;

        protected virtual void Start()
        {
            if (autoRegister)
            {
                RegisterUpdates();
            }
        }

        protected virtual void RegisterUpdates()
        {
            if (useRegularUpdate)
                CATSUpdateManager.Instance.RegisterUpdatable(this);
            
            if (useLateUpdate)
                CATSUpdateManager.Instance.RegisterLateUpdatable(this);
            
            if (useFixedUpdate)
                CATSUpdateManager.Instance.RegisterFixedUpdatable(this);
            
            if (intervalUpdateFrames > 0)
                CATSUpdateManager.Instance.RegisterIntervalUpdatable(this, intervalUpdateFrames);
        }

        protected virtual void OnDestroy()
        {
            CATSUpdateManager.Instance.UnregisterFromAll(this);
        }

        protected virtual void OnDisable()
        {
            CATSUpdateManager.Instance.UnregisterFromAll(this);
        }

        protected virtual void OnEnable()
        {
            if (autoRegister)
            {
                RegisterUpdates();
            }
        }

        public abstract void OnUpdate(float deltaTime);
        public virtual void OnLateUpdate(float deltaTime) { }
        public virtual void OnFixedUpdate(float deltaTime) { }
        public virtual void OnIntervalUpdate(float deltaTime) { }
    }
}