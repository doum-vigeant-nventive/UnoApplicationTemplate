﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ApplicationTemplate;
using ApplicationTemplate.Business;
using ApplicationTemplate.Client;
using GeneratedSerializers;
using MallardMessageHandlers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Refit;

namespace ApplicationTemplate
{
	/// <summary>
	/// This class is used for API configuration.
	/// - Configures API endpoints.
	/// - Configures HTTP handlers.
	/// </summary>
	public static class ApiConfiguration
	{
		/// <summary>
		/// Adds the API services to the <see cref="IServiceCollection"/>.
		/// </summary>
		/// <param name="services">The <see cref="IServiceCollection"/>.</param>
		/// <param name="configuration">The <see cref="IConfiguration"/>.</param>
		/// <returns>The updated <see cref="IServiceCollection"/>.</returns>
		public static IServiceCollection AddApi(this IServiceCollection services, IConfiguration configuration)
		{
			// For example purpose: the following line loads the DadJokesEndpoint configuration section and make IOptions<DadJokesEndpointOptions> available for DI.
			services.BindOptionsToConfiguration<DadJokesEndpointOptions>(configuration);

			services
				.AddMainHandler()
				.AddNetworkExceptionHandler()
				.AddExceptionHubHandler()
				.AddAuthenticationTokenHandler()
#if (IncludeFirebaseAnalytics)
				.AddFirebaseHandler()
#endif
				.AddResponseContentDeserializer()
				.AddAuthenticationEndpoint(configuration)
				.AddPostEndpoint(configuration)
				.AddUserProfileEndpoint(configuration)
				.AddDadJokesEndpoint(configuration);

			return services;
		}

		private static IServiceCollection AddUserProfileEndpoint(this IServiceCollection services, IConfiguration configuration)
		{
			return services.AddEndpoint<IUserProfileEndpoint>(configuration, "UserProfileEndpoint");
		}

		private static IServiceCollection AddAuthenticationEndpoint(this IServiceCollection services, IConfiguration configuration)
		{
			return services.AddEndpoint<IAuthenticationEndpoint>(configuration, "AuthenticationEndpoint");
		}

		private static IServiceCollection AddPostEndpoint(this IServiceCollection services, IConfiguration configuration)
		{
			return services
				.AddSingleton<IErrorResponseInterpreter<PostErrorResponse>>(s => new ErrorResponseInterpreter<PostErrorResponse>(
					(request, response, deserializedResponse) => deserializedResponse.Error != null,
					(request, response, deserializedResponse) => new PostEndpointException(deserializedResponse)
				))
				.AddTransient<ExceptionInterpreterHandler<PostErrorResponse>>()
				.AddEndpoint<IPostEndpoint>(configuration, "PostEndpoint", b => b
					.AddHttpMessageHandler<ExceptionInterpreterHandler<PostErrorResponse>>()
					.AddHttpMessageHandler<AuthenticationTokenHandler<AuthenticationData>>()
				);
		}

		private static IServiceCollection AddDadJokesEndpoint(this IServiceCollection services, IConfiguration configuration)
		{
			return services.AddEndpoint<IDadJokesEndpoint>(configuration, "DadJokesEndpoint");
		}

		private static IServiceCollection AddEndpoint<TInterface>(
			this IServiceCollection services,
			IConfiguration configuration,
			string name,
			Func<IHttpClientBuilder, IHttpClientBuilder> configure = null
		)
			where TInterface : class
		{
			var options = configuration.GetSection(name).Get<EndpointOptions>();

			if (options.EnableMock)
			{
				services.AddSingleton<TInterface>();
			}
			else
			{
				var httpClientBuilder = services
					.AddRefitHttpClient<TInterface>(settings: serviceProvider => new RefitSettings()
					{
						ContentSerializer = new ObjectSerializerToContentSerializerAdapter(serviceProvider.GetRequiredService<IObjectSerializer>()),
					})
					.ConfigurePrimaryHttpMessageHandler(serviceProvider => serviceProvider.GetRequiredService<HttpMessageHandler>())
					.ConfigureHttpClient((serviceProvider, client) =>
					{
						client.BaseAddress = options.Url;
						AddDefaultHeaders(client, serviceProvider);
					})
					.AddHttpMessageHandler<ExceptionHubHandler>();

				configure?.Invoke(httpClientBuilder);

				httpClientBuilder.AddHttpMessageHandler<NetworkExceptionHandler>();

#if (IncludeFirebaseAnalytics)
//-:cnd:noEmit
#if __ANDROID__
//+:cnd:noEmit
				httpClientBuilder.AddHttpMessageHandler<FirebasePerformanceHandler>();
//-:cnd:noEmit
#endif
//+:cnd:noEmit
#endif
			}

			return services;
		}

		private static IServiceCollection AddMainHandler(this IServiceCollection services)
		{
			return services.AddTransient<HttpMessageHandler>(s =>
//-:cnd:noEmit
#if __IOS__
//+:cnd:noEmit
				new NSUrlSessionHandler()
//-:cnd:noEmit
#elif __ANDROID__
//+:cnd:noEmit
				new Xamarin.Android.Net.AndroidClientHandler()
//-:cnd:noEmit
#else
//+:cnd:noEmit
				new HttpClientHandler()
//-:cnd:noEmit
#endif
//+:cnd:noEmit
			);
		}

		private static IServiceCollection AddResponseContentDeserializer(this IServiceCollection services)
		{
			return services.AddSingleton<IResponseContentDeserializer, ObjectSerializerToResponseContentDeserializer>();
		}

		private static IServiceCollection AddNetworkExceptionHandler(this IServiceCollection services)
		{
			return services
				.AddSingleton<INetworkAvailabilityChecker>(new NetworkAvailabilityChecker(GetIsNetworkAvailable))
				.AddTransient<NetworkExceptionHandler>();
		}

		private static IServiceCollection AddExceptionHubHandler(this IServiceCollection services)
		{
			return services
				.AddSingleton<IExceptionHub>(new ExceptionHub())
				.AddTransient<ExceptionHubHandler>();
		}

		private static IServiceCollection AddAuthenticationTokenHandler(this IServiceCollection services)
		{
			return services
				.AddSingleton<IAuthenticationTokenProvider<AuthenticationData>>(s => s.GetRequiredService<IAuthenticationService>())
				.AddTransient<AuthenticationTokenHandler<AuthenticationData>>();
		}

#if (IncludeFirebaseAnalytics)
		private static IServiceCollection AddFirebaseHandler(this IServiceCollection services)
		{
//-:cnd:noEmit
#if __ANDROID__
//+:cnd:noEmit
			return services.AddTransient<FirebasePerformanceHandler>();
//-:cnd:noEmit
#else
//+:cnd:noEmit
			return services;
//-:cnd:noEmit
#endif
//+:cnd:noEmit
		}
#endif

		private static Task<bool> GetIsNetworkAvailable(CancellationToken ct)
		{
//-:cnd:noEmit
#if WINDOWS_UWP || __ANDROID__ || __IOS__
			// TODO #172362: Not implemented in Uno.
			// return NetworkInformation.GetInternetConnectionProfile()?.GetNetworkConnectivityLevel() == NetworkConnectivityLevel.InternetAccess;
			return Task.FromResult(Xamarin.Essentials.Connectivity.NetworkAccess == Xamarin.Essentials.NetworkAccess.Internet);
#else
			return Task.FromResult(true);
#endif
//+:cnd:noEmit
		}

		private static void AddDefaultHeaders(HttpClient client, IServiceProvider serviceProvider)
		{
			client.DefaultRequestHeaders.Add("Accept-Language", CultureInfo.CurrentCulture.Name);

			// TODO #172779: Looks like our UserAgent is not of a valid format.
			// TODO #183437: Find alternative for UserAgent.
			// client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", serviceProvider.GetRequiredService<IEnvironmentService>().UserAgent);
		}

		/// <summary>
		/// Adds a Refit client to the service collection.
		/// </summary>
		/// <typeparam name="T">Type of the Refit interface</typeparam>
		/// <param name="services">Service collection</param>
		/// <param name="settings">Optional. Settings to configure the instance with</param>
		/// <returns>Updated IHttpClientBuilder</returns>
		private static IHttpClientBuilder AddRefitHttpClient<T>(this IServiceCollection services, Func<IServiceProvider, RefitSettings> settings = null)
			where T : class
		{
			services.AddSingleton(serviceProvider => RequestBuilder.ForType<T>(settings?.Invoke(serviceProvider)));

			return services
				.AddHttpClient(typeof(T).FullName)
				.AddTypedClient((client, serviceProvider) => RestService.For(client, serviceProvider.GetService<IRequestBuilder<T>>()));
		}
	}
}
