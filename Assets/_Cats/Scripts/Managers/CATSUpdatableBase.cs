namespace _cats.Scripts.Core._cats.Scripts.Core
{
    public abstract class CATSUpdatableBase : ICATSUpdatable
    {
        protected bool isRegistered = false;

        public virtual void OnUpdate(float deltaTime) { }
        public virtual void OnLateUpdate(float deltaTime) { }
        public virtual void OnFixedUpdate(float deltaTime) { }
        public virtual void OnIntervalUpdate(float deltaTime) { }

        protected virtual void RegisterToUpdateManager()
        {
            if (!isRegistered)
            {
                CATSUpdateManager.Instance.RegisterUpdatable(this);
                isRegistered = true;
            }
        }

        protected virtual void UnregisterFromUpdateManager()
        {
            if (isRegistered)
            {
                CATSUpdateManager.Instance.UnregisterFromAll(this);
                isRegistered = false;
            }
        }

        protected virtual void RegisterLateUpdate()
        {
            CATSUpdateManager.Instance.RegisterLateUpdatable(this);
        }

        protected virtual void RegisterFixedUpdate()
        {
            CATSUpdateManager.Instance.RegisterFixedUpdatable(this);
        }

        protected virtual void RegisterIntervalUpdate(int frameInterval)
        {
            CATSUpdateManager.Instance.RegisterIntervalUpdatable(this, frameInterval);
        }

        ~CATSUpdatableBase()
        {
            UnregisterFromUpdateManager();
        }
    }
}