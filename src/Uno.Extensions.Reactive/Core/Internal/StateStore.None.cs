﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Uno.Extensions.Reactive.Core;

internal class NoneStateStore : IStateStore
{
	/// <inheritdoc />
	bool IStateStore.HasSubscription<TSource>(TSource source)
		=> false;

	/// <inheritdoc />
	public FeedSubscription<T> GetOrCreateSubscription<TSource, T>(TSource source)
		where TSource : class, ISignal<Message<T>>
		=> throw new InvalidOperationException("Cannot create a subscription on SourceContext.None. " + SourceContext.NoneContextErrorDesc);

	/// <inheritdoc />
	public TState GetOrCreateState<TSource, TState>(TSource source, Func<SourceContext, TSource, TState> factory)
		where TSource : class
		where TState : IStateImpl, IAsyncDisposable
		=> throw new InvalidOperationException("Cannot create a state on SourceContext.None. " + SourceContext.NoneContextErrorDesc);

	/// <inheritdoc />
	public TState CreateState<T, TState>(Option<T> initialValue, Func<SourceContext, Option<T>, TState> factory)
		where TState : IStateImpl, IAsyncDisposable
		=> throw new InvalidOperationException("Cannot create a state on SourceContext.None. " + SourceContext.NoneContextErrorDesc);

	/// <inheritdoc />
	public async ValueTask DisposeAsync()
	{
	}
}
