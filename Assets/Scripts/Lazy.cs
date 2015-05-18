using System;
using System.Threading;

/// <summary>
/// This static class provides utilities for convenient lazy-initialization.
/// </summary>
public static class Lazy
{
	/// <summary>
	/// This method initializes 'store', if it is null, by constructing
	/// an instance using the default constructor. Thie method is mostly
	/// threadsafe, but can create more than one instance if multiple
	/// threads call it; only one will be returned to all threads
	/// and all extra instances will be abandoned.
	/// 
	/// This method returns whatever is in store, or if it was null,
	/// whatever object wins the race to initialize it.
	/// 
	/// This is pretty much like LazyInitializer.EnsureInitialized
	/// from .NET 4 reimplemented with a shorter name.
	/// </summary>
	public static T Init<T> (ref T store)
		where T : class, new()
	{
		return Init (ref store, () => new T ());
	}

	/// <summary>
	/// This initializes the field indicated if it is null, by
	/// calling the delegate given to obtain the value. This
	/// method is threadsafe, though it may call 'creator' more
	/// than once in the event of a race; only one resulting object
	/// survives, however, and that's what will be returned.
	/// 
	/// This method returns whatever is in store, or if it was null,
	/// whatever object wins the race to initialize it.
	/// 
	/// This is pretty much like LazyInitializer.EnsureInitialized
	/// from .NET 4 reimplemented with a shorter name.
	/// </summary>
	public static T Init<T> (ref T store, Func<T> creator)
		where T : class
	{
		if (creator == null)
			throw new ArgumentNullException ("creator");

		T loaded = store;
		
		if (loaded != null) {
			// we're not on .NET anymore, so lets be safe: this ensures
			// all initializing writes made to 'loaded' are visible
			// to this thread before we try to use it. This is probably
			// not needed in practice, but by the spec it is required.
			Thread.MemoryBarrier ();
			return loaded;
		}
		
		T created = creator ();
		// this compare-exchange also does a memory barrier for us, so
		// we need not do an extra one.
		loaded = Interlocked.CompareExchange (ref store, created, null);
		return loaded ?? created;
	}
}