﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Chinook.DataLoader;
using Chinook.DynamicMvvm;
using Chinook.DynamicMvvm.Implementations;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Uno.Extensions;

namespace ApplicationTemplate;

/// <summary>
/// This class is used for view model configuration.
/// - Configures the dynamic properties.
/// - Configures the dynamic commands.
/// - Configures the data loaders.
/// </summary>
public static class ViewModelConfiguration
{
	/// <summary>
	/// Adds the mvvm services to the <see cref="IServiceCollection"/>.
	/// </summary>
	/// <param name="services">Service collection.</param>
	/// <returns><see cref="IServiceCollection"/>.</returns>
	public static IServiceCollection AddMvvm(this IServiceCollection services)
	{
		return services
			.AddDynamicProperties()
			.AddDynamicCommands()
			.AddDataLoaders()
			.AddValidatorsFromAssemblyContaining(typeof(ViewModelConfiguration), ServiceLifetime.Singleton);
	}

	private static IServiceCollection AddDynamicCommands(this IServiceCollection services)
	{
		return services
			.AddSingleton<IDynamicCommandBuilderFactory>(s =>
				new DynamicCommandBuilderFactory(c => c
					.CatchErrors(s.GetRequiredService<IDynamicCommandErrorHandler>())
					.WithLogs(s.GetRequiredService<ILogger<IDynamicCommand>>())
					.WithStrategy(new RaiseCanExecuteOnDispatcherCommandStrategy(c.ViewModel))
					.DisableWhileExecuting()
					.OnBackgroundThread()
					.CancelPrevious()
				)
			);
	}

	private static IServiceCollection AddDynamicProperties(this IServiceCollection services)
	{
		return services.AddSingleton<IDynamicPropertyFactory, ValueChangedOnBackgroundTaskDynamicPropertyFactory>();
	}

	private static IServiceCollection AddDataLoaders(this IServiceCollection services)
	{
		return services.AddSingleton<IDataLoaderBuilderFactory>(s =>
		{
			return new DataLoaderBuilderFactory(b => b
				.OnBackgroundThread()
				.WithEmptySelector(GetIsEmpty)
				.WithAnalytics(
					onSuccess: async (ct, request, value) => { /* Some analytics */ },
					onError: async (ct, request, error) => { typeof(ViewModelConfiguration).Log().LogError(error, "Error while loading {Request}.", request); }
				)
			);

			bool GetIsEmpty(IDataLoaderState state)
			{
				return state.Data == null || (state.Data is IEnumerable enumerable && !enumerable.Cast<object>().Any());
			}
		});
	}
}
