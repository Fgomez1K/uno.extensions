﻿using System;
using System.Collections.Generic;
using System.Linq;
using nVentive.Umbrella.Collections;
using Umbrella.Presentation.Feeds.Collections._BindableCollection.Facets;
using ISchedulerInfo = Windows.System.DispatcherQueue;

namespace Umbrella.Presentation.Feeds.Collections._BindableCollection.Data
{
	/// <summary>
	/// A holder of a data layer for a given scheduler context
	/// </summary>
	internal sealed class DataLayerHolder : ILayerHolder, IBindableCollectionViewSource, IDisposable
	{
		private readonly DataLayerHolder? _parent;
		private readonly ISchedulerInfo? _context;
		private readonly IBindableCollectionDataLayerStrategy _layerStrategy;
		private readonly IEnumerable<object> _facets;

		private DataLayerChangesBuffer? _currentChangesBuffer;
		private DataLayerChangesBuffer? _nextChangesBuffer;

		public ICollectionView View { get; }

		public DifferentialObservableCollection Items { get; }

		public IObservableCollection? CurrentSource => _currentChangesBuffer?.Collection;

		public IBindableCollectionViewSource? Parent => _parent;

		//public IScheduler Scheduler => _parent != null
		//	? _parent.Scheduler
		//	: (_context.IsOnDispatcher
		//		? _context.GetDispatcher()
		//		: System.Reactive.Concurrency.Scheduler.Immediate);

		/// <summary>
		/// Creates a holder for the root layer of data
		/// </summary>
		public static DataLayerHolder Create(
			IBindableCollectionDataLayerStrategy layerStrategy,
			IObservableCollection items, 
			ISchedulerInfo? context)
		{
			var holder = new DataLayerHolder(null, layerStrategy, context);
			var initContext = layerStrategy.CreateUpdateContext(VisitorType.InitializeCollection, TrackingMode.Reset);
			var initializer = holder.Init(items, initContext);

			// We need to Flush buffer even for a Init: even if we don't have a CollectChange to raise, we may have other (applicative) callbacks.
			holder.Schedule(initializer.Complete);

			return holder;
		}

		/// <summary>
		/// Creates a holder for a sub data layer of this
		/// </summary>
		public (DataLayerHolder holder, DataLayerInitializer initializer) CreateSubLayer(IObservableCollection subItems, IUpdateContext changes)
		{
			var holder = new DataLayerHolder(this, _layerStrategy.CreateSubLayer(), _context);
			var initializer = holder.Init(subItems, changes);

			// Initializer contains a snapshot of items when the collection is added. It's used by parent to raise add for those items on its FlatView.
			return (holder, initializer);
		}

		private DataLayerHolder(DataLayerHolder? parent, IBindableCollectionDataLayerStrategy layerStrategy, ISchedulerInfo? context)
		{
			_context = context;
			_parent = parent;
			_layerStrategy = layerStrategy;

			(Items, View, _facets) = _layerStrategy.CreateView(this);
		}

		private DataLayerInitializer Init(IObservableCollection source, IUpdateContext context)
		{
			// Prepare the buffer responsible to manage the collection changes from both: collection updates and collection changes events
			// Note: The tracker built here is for FUTURE CollectionChanged events. We MUST NOT use the 'initContext' for that.
			var changesContext = _layerStrategy.CreateUpdateContext(VisitorType.CollectionChanged, TrackingMode.Auto);
			var changesTracker = _layerStrategy.GetTracker(this, changesContext);
			var changesBuffer = new DataLayerChangesBuffer(this, source, changesTracker);

			// Then start the buffer in order to analyze the current items of the 'source'
			// Here we use the context which cause this holder to be instanciated (i.e. the 'initContext')
			var initTracker = _layerStrategy.GetTracker(this, context);
			var initializer = changesBuffer.Initialize(initTracker);

			_currentChangesBuffer = changesBuffer;

			return initializer;
		}

		/// <summary>
		/// Updates the root data layer
		/// </summary>
		public void Update(IObservableCollection source, TrackingMode mode)
		{
			if (_currentChangesBuffer is null)
			{
				throw new InvalidOperationException("Invalid state. You must invoke the Init first.");
			}

			if (object.ReferenceEquals(source, _currentChangesBuffer.Collection))
			{
				return;
			}

			var context = _layerStrategy.CreateUpdateContext(VisitorType.UpdateCollection, mode);
			var tracker = _layerStrategy.GetTracker(this, context);
			var initializer = PrepareUpdate(source, tracker);

			// Schedule to flush the buffers to apply the update
			Schedule(initializer.Complete);
		}

		/// <summary>
		/// Updates a child data layer
		/// </summary>
		internal DataLayerInitializer PrepareUpdate(IObservableCollection source, IUpdateContext context)
		{
			var tracker = _layerStrategy.GetTracker(this, context); // re-use the context of the parent layer
			var initializer = PrepareUpdate(source, tracker);

			return initializer;
		}

		private DataLayerInitializer PrepareUpdate(IObservableCollection source, ILayerTracker tracker)
		{
			var from = _nextChangesBuffer ?? _currentChangesBuffer ?? throw new InvalidOperationException("Invalid state. You must invoke the init first.");
			var to = _nextChangesBuffer = from.UpdateTo(source);

			// Detects changes between 'from.Collection' and 'source'
			var initializer = to.Initialize(tracker);

			// Adds the callbacks needed to properly apply the update
			var previous = default(DataLayerChangesBuffer);
			initializer.Prepend(WillApplyUpdate);
			initializer.Append(DidApplyUpdate);

			return initializer;

			void WillApplyUpdate()
			{
				previous = _currentChangesBuffer;
				if (previous != from)
				{
					throw new InvalidOperationException("The previous update was not applied yet!");
				}

				CurrentSourceChanging?.Invoke(this, new CurrentSourceUpdateEventArgs(previous.Collection, to.Collection));

				_currentChangesBuffer = to;
			}

			void DidApplyUpdate()
			{
				if (previous != from)
				{
					throw new InvalidOperationException("Wooups ... all callbacks was not applied properly, and that is not recoverable!");
				}

				CurrentSourceChanged?.Invoke(this, new CurrentSourceUpdateEventArgs(previous.Collection, to.Collection));
			}
		}

		internal IObservableCollectionSnapshot PrepareRemove()
		{
			if (_currentChangesBuffer is null)
			{
				throw new InvalidOperationException("Invalid state. You must invoke the init first.");
			}

			return _currentChangesBuffer.Stop();
		}

		#region IBindableCollectionViewSource
		public event EventHandler<CurrentSourceUpdateEventArgs>? CurrentSourceChanging;
		public event EventHandler<CurrentSourceUpdateEventArgs>? CurrentSourceChanged;

		public TFacet GetFacet<TFacet>()
		{
			var facet = _facets.OfType<TFacet>().FirstOrDefault();
			if (facet == null)
			{
				throw new InvalidOperationException($"Facet '{typeof(TFacet).Name}' is not available on this holder ({GetType().Name}).");
			}
			return facet;
		}
		#endregion

		public void Schedule(Action action)
		{
			if (_parent != null)
			{
				_parent.Schedule(action);
			}
			else if (_context is not null)
			{
				_context.TryEnqueue(() => action());
			}
			else
			{
				action();
			}
		}

		public void Dispose()
		{
			(View as IDisposable)?.Dispose();
			_currentChangesBuffer?.Dispose();
		}
	}
}
