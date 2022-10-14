﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using ApplicationTemplate.Client;
using Nventive.View.Converters;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.ViewManagement;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

namespace ApplicationTemplate.Views.Content.Diagnostics;

public sealed partial class HttpDebuggerView : UserControl
{
	public HttpDebuggerView()
	{
		this.InitializeComponent();

		var view = App.Window;
		TrySetDetailsScrollViewerMaxHeight(view);
		view.SizeChanged += OnVisibleBoundsChanged;
	}

	private void OnVisibleBoundsChanged(object sender, WindowSizeChangedEventArgs args)
	{
		TrySetDetailsScrollViewerMaxHeight((Window)sender);
	}

	private void TrySetDetailsScrollViewerMaxHeight(Window window)
	{
		var height = window.Bounds.Height;
		if (height > 0)
		{
			DetailsScrollViewer.MaxHeight = height * 0.60;
		}
	}
}

public class HttpTraceStatusCustomValueConverter : ConverterBase
{
	public object FallbackValue { get; set; }

	public object ValueWhenPending { get; set; }

	public object ValueWhenReceived { get; set; }

	public object ValueWhenFailed { get; set; }

	public object ValueWhenCancelled { get; set; }

	protected override object Convert(object value, Type targetType, object parameter)
	{
		return value switch
		{
			HttpTraceStatus.Pending => ValueWhenPending,
			HttpTraceStatus.Received => ValueWhenReceived,
			HttpTraceStatus.Failed => ValueWhenFailed,
			HttpTraceStatus.Cancelled => ValueWhenCancelled,
			_ => FallbackValue,
		};
	}
}