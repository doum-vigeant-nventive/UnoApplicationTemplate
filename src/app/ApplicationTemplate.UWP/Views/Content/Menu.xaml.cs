using System.Linq;
using ApplicationTemplate.Presentation;
using Uno.Toolkit.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace ApplicationTemplate.Views.Content
{
	public sealed partial class Menu : AttachableUserControl
	{
		public Menu()
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
			var totalHeight = bottomPadding + PresentationConstants.MenuHeight;

			CloseTranslateAnimation.To = totalHeight;
			MenuTranslateTransform.Y = totalHeight;
		}
	}
}
