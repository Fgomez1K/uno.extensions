﻿using System;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Uno.Extensions.Reactive.Generator;

internal record BindableListFromFeedOfListProperty(IPropertySymbol Property, ITypeSymbol CollectionType, ITypeSymbol ItemType) : IMappedMember
{
	/// <inheritdoc />
	public string Name => Property.Name;

	/// <inheritdoc />
	public string? GetBackingField()
		=> null;

	/// <inheritdoc />
	public string GetDeclaration()
		=> $"{Property.GetAccessibilityAsCSharpCodeString()} {NS.Reactive}.IListFeed<{ItemType}> {Property.Name} {{ get; }}"; // Note: This should be a State

	/// <inheritdoc />
	public string GetInitialization()
		=> @$"
			var {Property.GetCamelCaseName()}Source = {N.Ctor.Model}.{Property.Name} ?? throw new NullReferenceException(""The list feed property '{Property.Name}' is null. Public feeds properties must be initialized in the constructor."");
			var {Property.GetCamelCaseName()}SourceListFeed = {N.ListFeed.Extensions.ToListFeed}<{CollectionType}, {ItemType}>({Property.GetCamelCaseName()}Source);
			{Property.Name} = new {NS.Bindings}.BindableListFeed<{ItemType}>(nameof({Property.Name}), {Property.GetCamelCaseName()}SourceListFeed, {N.Ctor.Ctx});";
}
