using System;
using System.Collections.Generic;

/// <summary>
/// This class stores and lazy-creates objects in a list;
/// each is identified by its index. It's a wrapper around
/// a list, but while it is serializable, the data in it will
/// be discarded upon serialization, so that it must be
/// reloaded rather than restored.
/// </summary>
[Serializable]
public class LazyList<T>
{
	[NonSerialized]
	private List<T>
		lazyStorage;

	/// <summary>
	/// This discards all data in this object, so that
	/// GetOrCreate() must creat new values.
	/// </summary>
	public void Reset ()
	{
		lazyStorage = null;
	}

	/// <summary>
	/// This method extracts an item from the list; if the list has
	/// not yet got an item at the index, this method invokes creator
	/// (passing 'index' to it) and returns that, but stores it for
	/// later also.
	/// </summary>
	public T GetOrCreate (int index, Func<int, T> creator)
	{
		if (index < 0) {
			throw new ArgumentOutOfRangeException ("index");
		}

		if (creator == null) {
			throw new ArgumentNullException ("creator");
		}

		List<T> list = lazyStorage;

		if (list == null) {
			lazyStorage = list = new List<T> (index + 1);
		}

		if (index < list.Count) {
			T item = list [index];
		
			if (item != null) {
				return item;
			}
		}
		
		T created = creator (index);

		if (created == null) {
			throw new InvalidOperationException ("A LazyList creator delegate cannot return null.");
		}

		ExtendAndSet (list, index, created);
		return created;
	}

	/// <summry>
	/// Expands 'list' until it is just large enough to contain at element
	/// at 'index', and places 'item' there. If it must expand the list by
	/// more than 1 element, the additiona elements before 'index' will be set
	/// to 'fillValue'.
	/// </summary>
	private static void ExtendAndSet (List<T> list, int index, T item, T fillValue = default(T))
	{
		list.Capacity = Math.Max (list.Capacity, index + 1);
		
		while (list.Count < index - 1) {
			list.Add (fillValue);
		}
		
		if (index < list.Count) {
			list [index] = item;
		} else {
			list.Add (item);
		}
	}
}