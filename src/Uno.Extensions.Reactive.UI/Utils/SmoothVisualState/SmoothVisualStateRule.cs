﻿#pragma warning disable CS1591 // XML Doc, will be moved elsewhere

using System;
using System.Linq;

namespace Uno.Extensions.Reactive.UI;

public class SmoothVisualStateRule
{
	public string? Group { get; set; }

	public string? From { get; set; }

	public string? To { get; set; }

	public TimeSpan? Delay { get; set; }

	public TimeSpan? MinDuration { get; set; }

	public SmoothStateRuleResult Get(VisualStateGroup group, VisualState? current, VisualState target)
	{
		if (Group is not null && group.Name != Group)
		{
			return default;
		}

		if (From is not null && current?.Name != From)
		{
			return default;
		}

		if (To is not null && target.Name != To)
		{
			return default;
		}

		return new() { Delay = Delay, MinDuration = MinDuration };
	}
}
