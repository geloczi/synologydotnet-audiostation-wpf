using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using System.Windows;

namespace SynAudio.Controls
{
	public class AsyncImage : Image
	{
		private BitmapFrame _bitmapFrame;

		public static DependencyProperty ImageSourceLoadedAsyncProperty = DependencyProperty.Register(
				nameof(ImageSourceLoadedAsync),
				typeof(bool),
				typeof(AsyncImage));
		public bool ImageSourceLoadedAsync
		{
			get => (bool)GetValue(ImageSourceLoadedAsyncProperty);
			private set => SetValue(ImageSourceLoadedAsyncProperty, value);
		}

		public AsyncImage() : base()
		{
			//ImageSource
			//BitmapImage
			Loaded += AsyncImage_Loaded;
		}

		protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnPropertyChanged(e);
			if (e.Property.Name == nameof(Source) && Source is BitmapFrame bf)
			{
				_bitmapFrame = bf;
				// System.Windows.Media.Imaging.BitmapFrameDecode
				_bitmapFrame.DownloadCompleted += _bitmapFrame_DownloadCompleted;
				_bitmapFrame.Changed += _bitmapFrame_Changed;
				_bitmapFrame.DecodeFailed += _bitmapFrame_DecodeFailed;
			}
		}

		private void _bitmapFrame_DecodeFailed(object sender, ExceptionEventArgs e)
		{
		}

		private void _bitmapFrame_Changed(object sender, EventArgs e)
		{
		}

		private void _bitmapFrame_DownloadCompleted(object sender, EventArgs e)
		{
		}

		private void AsyncImage_SourceUpdated(object sender, System.Windows.Data.DataTransferEventArgs e)
		{
			if (!(Source is null))
			{
			}
		}

		private void AsyncImage_Loaded(object sender, RoutedEventArgs e)
		{
			Loaded -= AsyncImage_Loaded;
			if (!(Source is null))
			{
			}
		}
	}
}