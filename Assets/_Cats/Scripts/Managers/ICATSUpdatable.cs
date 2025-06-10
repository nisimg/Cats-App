namespace _cats.Scripts.Core._cats.Scripts.Core
{
    public interface ICATSUpdatable
    {
        void OnUpdate(float deltaTime);
        void OnLateUpdate(float deltaTime) { }
        void OnFixedUpdate(float deltaTime) { }
        void OnIntervalUpdate(float deltaTime) { }
    }
}