namespace SBaier.DI
{
    public interface Pool<TItem>
    {
		TItem Request();
		void Return(TItem item);
	}

	public interface Pool<TItem, in TArg>
	{
		TItem Request(TArg arg);
		void Return(TItem item);
	}

	public interface Pool<TItem, in TArg1, in TArg2>
	{
		TItem Request(TArg1 arg1, TArg2 arg2);
		void Return(TItem item);
	}

	public interface Pool<TItem, in TArg1, in TArg2, in TArg3>
	{
		TItem Request(TArg1 arg1, TArg2 arg2, TArg3 arg3);
		void Return(TItem item);
	}
}
