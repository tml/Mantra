using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reactive.Linq;

namespace Mantra
{
	public static class SliceCollections
	{
		public static Slice<T> Slice<T>(this List<T> collection)
		{
			return new Slice<T>(collection, 0, collection.Count);
		}

		public static Slice<T> Slice<T>(this List<T> collection, int left)
		{
			return new Slice<T>(collection, left, collection.Count);
		}

		public static Slice<T> Slice<T>(this List<T> collection, int left, int right)
		{
			return new Slice<T>(collection, left, right);
		}
	}

	public struct Slice<T> : IList<T>
	{
		private List<T> backing;
		private int left;
		private int count;

		public Slice(List<T> backing, int left, int count)
		{
			this.backing = backing;
			this.left = left;
			this.count = count;
		}

		public Slice<T> ReSlice(int left)
		{
			return new Slice<T>(this.backing, this.left + left, count - left);
		}

		public Slice<T> ReSlice(int left, int count)
		{
			if (count + left >= count) throw new IndexOutOfRangeException();
			return new Slice<T>(this.backing, this.left + left, count);
		}

		public T this[int index]
		{
			get
			{
				if (index >= count) throw new IndexOutOfRangeException();
				return backing[left + index];
			}
			set
			{
				if (index >= count) throw new IndexOutOfRangeException();
				backing[left + index] = value;
			}
		}

		public int Count
		{
			get { return count; }
		}

		public bool Empty
		{
			get
			{
				return count == 0;
			}
		}

		public Slice<T> GetRange(int left, int count)
		{
			return new Slice<T>(backing, this.left + left, count);
		}

		public Slice<T> With(T term)
		{
			if (backing.Count == left + count)
			{
				backing.Add(term);
				count += 1;
				return this;
			}
			List<T> list = backing.GetRange(left, count);
			list.Add(term);
			return new Slice<T>(list, 0, list.Count);
		}

		public Slice<T> WithRange(IEnumerable<T> enumerable)
		{
			if (backing.Count <= left + count)
			{
				backing.AddRange(enumerable);
                count += enumerable.Count();
				return this;
			}
			List<T> list = backing.GetRange(left, count);
			list.AddRange(enumerable);
			return new Slice<T>(list, 0, list.Count);
		}

		private class Enumerator : IEnumerator<T>
		{
			private List<T> backing;
			private int left;
			private int count;
			private int current = -1;

			public Enumerator(List<T> backing, int left, int count)
			{
				this.backing = backing;
				this.left = left;
				this.count = count;
			}

			public T Current
			{
				get { return backing[current + left]; }
			}

			public void Dispose()
			{
			}

			object System.Collections.IEnumerator.Current
			{
				get { return Current; }
			}

			public bool MoveNext()
			{
				current += 1;
				return current < count;
			}

			public void Reset()
			{
				current = -1;
			}
		}

		private IEnumerable<T> Enumerate()
		{
			for (int i = left; i < left + count; ++i)
			{
				yield return backing[i];
			}
		}

		public IEnumerator<T> GetEnumerator()
		{
			return new Enumerator(backing, left, count);
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public void Add(T item)
		{
			throw new NotImplementedException();
		}

		public void Clear()
		{
			throw new NotImplementedException();
		}

		public bool Contains(T item)
		{
			throw new NotImplementedException();
		}

		public void CopyTo(T[] array, int arrayIndex)
		{
			for (int i = 0; i < count; ++i)
			{
				array[arrayIndex + i] = backing[left + i];
			}
		}

		public bool IsReadOnly
		{
			get { return true; }
		}

		public bool Remove(T item)
		{
			throw new NotImplementedException();
		}

		public int IndexOf(T item)
		{
			throw new NotImplementedException();
		}

		public void Insert(int index, T item)
		{
			throw new NotImplementedException();
		}

		public void RemoveAt(int index)
		{
			throw new NotImplementedException();
		}
	}
}
