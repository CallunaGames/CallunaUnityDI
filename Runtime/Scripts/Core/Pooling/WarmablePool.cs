namespace Calluna.DI
{
    public interface WarmablePool<TItem>
    {
        void WarmUp(int count);
    }
}
