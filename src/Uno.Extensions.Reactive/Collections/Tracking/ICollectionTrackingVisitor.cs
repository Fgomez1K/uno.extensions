using System;
using System.Collections;
using System.Linq;

namespace nVentive.Umbrella.Collections.Tracking;

/// <summary>
/// A callback that has to invoked **before** the corresponding event is being dequeue from the <see cref="CollectionChangesQueue"/>.
/// </summary>
/// <remarks>Be aware that this callback is invoked each time the <see cref="CollectionChangesQueue.DequeueChanges"/> is invoked.</remarks>
/// <remarks>Usually this is a good way to execute some action on the UI thread before an item is beeing added / changed / removed from a LisView.</remarks>
internal delegate void BeforeCallback();

/// <summary>
/// A callback that has to invoked **after** the corresponding event has been dequeued from the <see cref="CollectionChangesQueue"/>.
/// </summary>
/// <remarks>Be aware that this callback is invoked each time the <see cref="CollectionChangesQueue.DequeueChanges"/> is invoked.</remarks>
/// <remarks>Usually this is a good way to execute some action on the UI thread after an item has been added / changed / removed from a LisView.</remarks>
internal delegate void AfterCallback();

/// <summary>
/// A composite callback which contains callbacks for multiple phases amd potentially some other changes
/// </summary>
internal interface ICompositeCallback
{
	/// <summary>
	/// Invokes all the callbacks at once
	/// </summary>
	void Invoke(CallbackPhase phases, bool silently);
}

[Flags]
internal enum CallbackPhase
{
	/// <summary>
	/// Before raising the change event
	/// </summary>
	Before = 1,

	/// <summary>
	/// At (almost) the same time as the event (a.k.a. 'instead of')
	/// </summary>
	Main = 2,

	/// <summary>
	/// After raising the change event
	/// </summary>
	After = 4,

	/// <summary>
	/// All phases at once
	/// </summary>
	All = Before | Main | After,
}

/// <summary>
/// A visitor which can be used when tracking changes between 2 collections.
/// </summary>
internal interface ICollectionTrackingVisitor
{
	/// <summary>
	/// Invoked when an item is added in the target collection.
	/// </summary>
	/// <param name="item">The added item</param>
	/// <param name="callbacks">Callbacks collection on which some callbacks can be added in order to get them included as part of the result collection of changes.</param>
	void AddItem(object item, ICollectionTrackingCallbacks callbacks);

	/// <summary>
	/// Invoked when an Equals item appears in both previous and target collections.
	/// </summary>
	/// <remarks>No collection changed event is created for this item.</remarks>
	/// <param name="original">The instance of the item in the previous collection.</param>
	/// <param name="updated">The instance of the item in the target collection.</param>
	/// <param name="callbacks">
	/// Callbacks collection on which some callbacks can be added in order to get them included as part of the result collection of changes.
	/// <remarks>For the 'Same', as no event is raise, there is no difference between before and after callbacks.</remarks>
	/// </param>
	void SameItem(object original, object updated, ICollectionTrackingCallbacks callbacks);

	/// <summary>
	/// Invoked when a new version of an item is present in the target collection.
	/// </summary>
	/// <param name="original">The previous version</param>
	/// <param name="updated">The updated version</param>
	/// <param name="callbacks">Callbacks collection on which some callbacks can be added in order to get them included as part of the result collection of changes.</param>
	/// <returns>A boolean which indicates if a 'Replace' event should be added in the result collection of changes, or if it's manage by the application.</returns>
	bool ReplaceItem(object original, object updated, ICollectionTrackingCallbacks callbacks);

	/// <summary>
	/// Invoked when an item is removed in the target collection.
	/// </summary>
	/// <param name="item">The removed item</param>
	/// <param name="callbacks">Callbacks collection on which some callbacks can be added in order to get them included as part of the result collection of changes.</param>
	void RemoveItem(object item, ICollectionTrackingCallbacks callbacks);

	/// <summary>
	/// Invoked when a reset event is raised instead of properly tracking the changes between the collections.
	/// </summary>
	/// <param name="oldItems">A list of all the items removed from the target collection</param>
	/// <param name="newItems">A list of all the new items present in the target collection</param>
	/// <param name="callbacks">Callbacks collection on which some callbacks can be added in order to get them included as part of the result collection of changes.</param>
	void Reset(IList oldItems, IList newItems, ICollectionTrackingCallbacks callbacks);
}
