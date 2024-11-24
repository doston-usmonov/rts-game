namespace RTS.Core.Pooling
{
    public interface IPoolable
    {
        void OnSpawn();
        void OnDespawn();
        void ReturnToPool();
    }
}
