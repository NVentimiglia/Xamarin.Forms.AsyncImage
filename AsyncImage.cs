using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Ventimiglia.Helpers;
using Xamarin.Forms;

namespace Ventimiglia.Controls
{
    /// <summary>
    /// An image control that supports asyncranous loading. Requires the ImageDownloader Helper
    /// </summary>
    public class AsyncImage : ContentView
    {
        public Grid Root = new Grid
        {
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill
        };

        public ActivityIndicator Indicator = new ActivityIndicator
        {
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill,
            IsRunning = true
        };

        public Image Image = new Image
        {
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill,
            IsVisible = false
        };

        public static readonly BindableProperty RetryProperty = BindableProperty.Create<AsyncImage, int>(p => p.Retry, 3);
        public static readonly BindableProperty OneTimeProperty = BindableProperty.Create<AsyncImage, bool>(p => p.OneTime, true);
        public static readonly BindableProperty SourceProperty = BindableProperty.Create<AsyncImage, string>(p => p.Source, string.Empty, propertyChanged: OnSourcePropertyChanged);

        public int Retry
        {
            get { return (int)GetValue(RetryProperty); }
            set { SetValue(RetryProperty, Retry); }
        }

        public bool OneTime
        {
            get { return (bool)GetValue(OneTimeProperty); }
            set { SetValue(OneTimeProperty, value); }
        }

        public string Source
        {
            get { return (string)GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }

        public Color SpinnerColor
        {
            get { return Indicator.Color; }
            set { Indicator.Color = value; }
        }



        public AsyncImage()
        {
            Cache = new FileCache(TimeSpan.FromDays(5));
            Client = new HttpClient();

            if (Utility == null)
                Utility = DependencyService.Get<IImageUtility>();

            Root.Children.Add(Image);
            Root.Children.Add(Indicator);
            Content = Root;
        }

        protected FileCache Cache;
        protected HttpClient Client;
        static protected IImageUtility Utility;
        protected int Attempts = 0;
        protected bool DidComplete;
        protected bool IsLoading;

        /// <summary>
        /// Called when [items source property changed].
        /// </summary>
        /// <param name="bindable">The bindable.</param>
        /// <param name="value">The value.</param>
        /// <param name="newValue">The new value.</param>
        private static void OnSourcePropertyChanged(BindableObject bindable, string value, string newValue)
        {
            ((AsyncImage)(bindable)).DoLoad();
        }

        async void DoLoad()
        {
            if (IsLoading)
                return;

            Image.IsVisible = false;
            Indicator.IsVisible = true;

            if (string.IsNullOrEmpty(Source))
                return;

            Attempts++;

            if (!Source.Contains("http"))
            {
                Complete(ImageSource.FromFile(Source));
                return;
            }
            try
            {
                IsLoading = true;

                if (!Cache.Exists(Source))
                {
                    var full = await Client.GetByteArrayAsync(Source);

                    var smal = Resize(full);

                    Cache.Write(Source, smal);
                }

                Complete(ImageSource.FromStream(() => { return new MemoryStream(Cache.Read(Source)); }));

                IsLoading = false;
            }
            catch (Exception ex)
            {
                Console.WriteLine("AsyncImage: " + ex.Message);
                IsLoading = false;
                if (Retry > Attempts)
                {
                    DoRetry();
                }
            }
        }

        async void DoRetry()
        {
            await Task.Delay(500);
            DoLoad();
        }

        byte[] Resize(byte[] bits)
        {
            return Utility.ResizeImage(bits, (int)Width, (int)Height);
        }

        void Complete(ImageSource source)
        {
            DidComplete = true;
            Image.Source = source;
            Image.IsVisible = true;
            Indicator.IsVisible = false;
        }
    }
}


