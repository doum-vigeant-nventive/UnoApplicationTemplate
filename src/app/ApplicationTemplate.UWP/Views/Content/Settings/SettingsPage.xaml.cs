﻿using ApplicationTemplate.Presentation;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace ApplicationTemplate.Views.Content
{
	public sealed partial class SettingsPage : Page
	{
		public SettingsPage()
		{
			this.InitializeComponent();
			InitializeSafeArea();
		}

		/// <summary>
		/// This method handles the bottom padding for phones like iPhone X.
		/// </summary>
		private void InitializeSafeArea()
		{
			var full = Windows.UI.Xaml.Window.Current.Bounds;
			var bounds = ApplicationView.GetForCurrentView().VisibleBounds;

			var bottomPadding = full.Bottom - bounds.Bottom;
			SafeAreaRow.Height = new GridLength(bottomPadding + PresentationConstants.MenuHeight);
		}

		private void OnThemeButtonClicked(object sender, Windows.UI.Xaml.RoutedEventArgs e)
		{
			// Set theme for window root.
			if (Windows.UI.Xaml.Window.Current.Content is FrameworkElement root)
			{
				switch (root.ActualTheme)
				{
					case ElementTheme.Default:
					case ElementTheme.Light:
						root.RequestedTheme = ElementTheme.Dark;
						//-:cnd:noEmit
#if __ANDROID__ || __IOS__
						//+:cnd:noEmit
						// No need for the dispatcher here
						Windows.UI.ViewManagement.StatusBar.GetForCurrentView().ForegroundColor = Windows.UI.Colors.Black;
						//-:cnd:noEmit
#endif
						//+:cnd:noEmit
						break;
					case ElementTheme.Dark:
						root.RequestedTheme = ElementTheme.Light;
						//-:cnd:noEmit
#if __ANDROID__ || __IOS__
						//+:cnd:noEmit
						// No need for the dispatcher here
						Windows.UI.ViewManagement.StatusBar.GetForCurrentView().ForegroundColor = Windows.UI.Colors.White;
						//-:cnd:noEmit
#endif
						//+:cnd:noEmit
						break;
				}
			}
		}
	}
}
