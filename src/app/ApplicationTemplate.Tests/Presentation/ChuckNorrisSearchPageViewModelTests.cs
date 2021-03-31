﻿using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using DynamicData;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Hosting;
using Moq;
using Xunit;

namespace ApplicationTemplate.Tests
{
	public class ChuckNorrisSearchPageViewModelTests : NavigationTestsBase
	{
		[Fact]
		public async Task It_Should_EmptyCriterion_NoResults()
		{
			// Arrange
			var viewModel = new ChuckNorrisSearchPageViewModel();
			viewModel.SearchTerm = string.Empty;

			// Act
			var quotes = await viewModel.Quotes.Load(DefaultCancellationToken);

			// Assert
			using (new AssertionScope())
			{
				quotes.Should().NotBeNull();
				quotes.Should().BeEmpty();
			}
		}

		[Fact]
		public async Task It_Should_NotEmptyCriterion_Results()
		{
			// Arrange
			var viewModel = new ChuckNorrisSearchPageViewModel();

			// Act
			viewModel.SearchTerm = "dog";
			var quotes = await viewModel.Quotes.Load(DefaultCancellationToken);

			// Assert
			using (new AssertionScope())
			{
				quotes.Should()
					.NotBeNull()
					.And
					.NotBeEmpty();

				quotes.All(q => q.Quote.Value.ToUpperInvariant().Contains("DOG"))
					.Should().BeTrue("All quotes found searching for search term 'dog' should contain 'dog' ");
			}
		}

		[Theory]
		[InlineData("")]
		[InlineData("a")]
		[InlineData("aa")]
		public async Task DisplayNothingIfSearchIsNotValid(string searchTerm)
		{
			// Arrange
			var viewModel = new ChuckNorrisSearchPageViewModel();

			// Act
			viewModel.SearchTerm = searchTerm;
			var quotes = await viewModel.Quotes.Load(DefaultCancellationToken);

			// Assert
			quotes.Length
				.Should().Be(0);
		}

		[Fact]
		public async Task UpdateSearchTermProperty()
		{
			// Arrange
			var anything = "Anything";
			var searchPageViewModelMock = new Mock<ChuckNorrisSearchPageViewModel>();

			// Act
			searchPageViewModelMock.Object.SearchTerm = anything;

			// Assert
			searchPageViewModelMock.Object.SearchTerm
				.Should().Be(anything);
		}

		private void CheckForSearchMethodWhenQuotesAreLoading_SpecialConfiguration(IHostBuilder host)
		{
			host.ConfigureServices(services =>
			{
				// This will replace the actual implementation of IApplicationSettingsService with a mocked version.
				ReplaceWithMock<IChuckNorrisService>(services, mock =>
				{
					mock
						.Setup(m => m.Search(It.IsAny<CancellationToken>(), It.IsAny<string>()))
						.ReturnsAsync(new ChuckNorrisQuote[]
						{
							new ChuckNorrisQuote(new ChuckNorrisData.Builder().WithId("0"), false),
							new ChuckNorrisQuote(new ChuckNorrisData.Builder().WithId("1203"), false)
						});

					mock
						.Setup(m => m.GetFavorites(It.IsAny<CancellationToken>()))
						.ReturnsAsync(new SourceList<ChuckNorrisQuote>().AsObservableList());
				});
			});
		}

		/// <summary>
		/// Verify that the Search method from ChuckNorrisService is:
		/// - Exactly called once if the search term is valid.
		/// - Never called if not.
		/// </summary>
		/// <param name="expectedCallCount">Number of times the Search method must be called.</param>
		/// <param name="searchTerm">The term used for the search.</param>
		/// <param name="chuckNorrisServiceMock">The <see cref="IChuckNorrisService"/> used for the search (mocked).</param>
		/// <returns><see cref="Task"/>.</returns>
		[Theory]
		[InlineAutoData("test")]
		[InlineAutoData("")]
		public async Task CheckForSearchMethodWhenQuotesAreLoading(string searchTerm)
		{
			// Arrange
			var chuckNorrisServiceMock = new Mock<IChuckNorrisService>();
			ConfigurationSetup(CheckForSearchMethodWhenQuotesAreLoading_SpecialConfiguration);

			var searchPageViewModel = new ChuckNorrisSearchPageViewModel();

			// Act
			searchPageViewModel.SearchTerm = searchTerm;
			var quotes = await searchPageViewModel.Quotes.Load(DefaultCancellationToken);

			// Assert
			if (string.IsNullOrEmpty(searchTerm))
			{
				// Less than 3 chars will result in an empty array even if Search returns something
				quotes.Length
					.Should().Be(0);
			}
			else
			{
				quotes.Length
					.Should().Be(2);
			}
		}

		private void CheckForMultipleSearchMethodCall_SpecialConfiguration(IHostBuilder host)
		{
			host.ConfigureServices(services =>
			{
				// This will replace the actual implementation of IApplicationSettingsService with a mocked version.
				ReplaceWithMock<IChuckNorrisService>(services, mock =>
				{
					mock
						.Setup(m => m.Search(It.IsAny<CancellationToken>(), "dog"))
						.ReturnsAsync(new ChuckNorrisQuote[]
						{
							new ChuckNorrisQuote(new ChuckNorrisData.Builder().WithId("1"), false),
							new ChuckNorrisQuote(new ChuckNorrisData.Builder().WithId("3"), false),
							new ChuckNorrisQuote(new ChuckNorrisData.Builder().WithId("1204"), false)
						});

					mock
						.Setup(m => m.Search(It.IsAny<CancellationToken>(), "cat"))
						.ReturnsAsync(new ChuckNorrisQuote[]
						{
							new ChuckNorrisQuote(new ChuckNorrisData.Builder().WithId("1"), false)
						});

					mock
						.Setup(s => s.GetFavorites(It.IsAny<CancellationToken>()))
						.ReturnsAsync(new SourceList<ChuckNorrisQuote>().AsObservableList());
				});
			});
		}

		[Fact]
		public async Task CheckForMultipleSearchMethodCall()
		{
			// Arrange
			var chuckNorrisServiceMock = new Mock<IChuckNorrisService>();
			ConfigurationSetup(CheckForMultipleSearchMethodCall_SpecialConfiguration);

			var viewModel = new ChuckNorrisSearchPageViewModel();

			// Act
			viewModel.SearchTerm = "dog";
			var firstQuotes = await viewModel.Quotes.Load(DefaultCancellationToken);

			viewModel.SearchTerm = "cat";
			var secondQuotes = await viewModel.Quotes.Load(DefaultCancellationToken);

			// Assert
			using (new AssertionScope())
			{
				firstQuotes.Length
					.Should().Be(3);

				secondQuotes.Length
					.Should().Be(1);
			}
		}
	}
}
