using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Uno;

namespace nVentive.Umbrella.Collections.Tracking;

partial class CollectionTracker
{
	private class SnapshotEnumerator<TCollection>
		where TCollection : IList
	{
		private readonly TCollection _snapshot;
		private readonly IndexOf<TCollection> _indexOf;
		private readonly IgnoredIndexCollection _ignored = new();

		public SnapshotEnumerator(
			TCollection snapshot,
			IndexOf<TCollection> indexOf)
		{
			_snapshot = snapshot;
			_indexOf = indexOf;
		}

		public int CurrentIndex { get; private set; } = -1;

		public object? Current { get; private set; }

		/// <summary>
		/// Count of items that have been ignored by the enumerator
		/// </summary>
		public int Ignored { get; private set; }

		public bool MoveNext()
		{
			var index = CurrentIndex;
			while (_ignored.TryDequeue(++index))
			{
				Ignored++;
			}

			if (index >= _snapshot.Count)
			{
				CurrentIndex = -1;
				Current = default(object);

				return false;
			}
			else
			{
				CurrentIndex = index;
				Current = _snapshot[index];

				return true;
			}
		}

		public void Ignore(int index)
		{
			if (index <= CurrentIndex)
			{
				throw new InvalidOperationException("You cannot ignore an index which was already enumerated.");
			}
			_ignored.Add(index);
		}

		public (int index, int ignoredItemsAfter) NextIndexOf(object item)
		{
			using var ignored = _ignored.GetEnumerator();
			var index = CurrentIndex;
			do
			{
				index = _indexOf(item, _snapshot, index + 1);

				// Item is missing, break
				if (index < 0)
				{
					return (-1, -1);
				}

				// Move the ignored enumerator forward to catch up 'index'
				while (ignored.MoveNext() && ignored.Current < index)
				{
				}

				// If the 'index' is ignored, retry
			} while (index == ignored.Current);

			return (index, ignored.Remaining);
		}

		private sealed class IgnoredIndexCollection
		{
			private IgnoredIndexCollectionNode _head = IgnoredIndexCollectionNode.Tail;

			public bool TryDequeue(int value)
			{
				if (_head.Value == value)
				{
					_head = _head.Next!;
					return true;
				}
				else
				{
					return false;
				}
			}

			public void Add(int value)
			{
				if (_head.Value >= value)
				{
					// Insert on top of the collection

					if (_head.Value == value)
					{
						return;
					}

					_head = new IgnoredIndexCollectionNode
					{
						Next = _head,
						Value = value
					};
				}
				else
				{
					var parent = _head;
					while (parent.Next.Value <= value)
					{
						parent = parent.Next;
					}

					// Insert between current and current.Next

					if (parent.Value == value)
					{
						return;
					}

					var node = new IgnoredIndexCollectionNode
					{
						Next = parent.Next ?? IgnoredIndexCollectionNode.Tail,
						Value = value
					};
					parent.Next = node;
				}
			}

			public Enumerator GetEnumerator() => new Enumerator(_head);

			public class IgnoredIndexCollectionNode
			{
				public static IgnoredIndexCollectionNode Tail { get; } = new()
				{
					Value = int.MaxValue
				};

				public IgnoredIndexCollectionNode Next = Tail; // So Tail.Next is actually 'null'

				public int Value;
			}

			public class Enumerator : IEnumerator<int>
			{
				private readonly IgnoredIndexCollectionNode _head;
				private IgnoredIndexCollectionNode? _current;

				public Enumerator(IgnoredIndexCollectionNode head)
					=> _head = head;

				object? IEnumerator.Current => _current?.Value;
				public int Current => _current?.Value ?? IgnoredIndexCollectionNode.Tail.Value;

				public int Remaining
				{
					get
					{
						var count = 0;
						var node = _current;
						while (node is not null && node != IgnoredIndexCollectionNode.Tail)
						{
							count++;
							node = node.Next;
						}

						return count;
					}
				}

				public bool MoveNext()
				{
					if (_current is null)
					{
						_current = _head;
						return true;
					}
					else if (_current == IgnoredIndexCollectionNode.Tail)
					{
						return false;
					}
					else
					{
						_current = _current.Next;
						return _current != IgnoredIndexCollectionNode.Tail;
					}
				}

				public void Reset() => throw new NotSupportedException();

				public void Dispose() { }
			}
		}
	}
}
