using System;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Widget;
using Android.OS;
using System.Collections.Generic;
using Xamarin.Media;
using Xamarin.Social.Services;

namespace Xamarin.Social.Sample.Android
{
	[Activity (Label = "Xamarin.Social Sample", MainLauncher = true)]
	public class MainActivity : Activity
	{

		private static readonly FacebookService Facebook = new FacebookService {
			ClientId = "Client ID from https://developers.facebook.com/apps",
			RedirectUrl = new Uri ("Redirect URL from https://developers.facebook.com/apps")
		};

		private static readonly FlickrService Flickr = new FlickrService {
			ConsumerKey = "Key from http://www.flickr.com/services/apps/by/me",
			ConsumerSecret = "Secret from http://www.flickr.com/services/apps/by/me",
			CallbackUrl = new Uri ("Callback URL from http://www.flickr.com/services/apps/by/me")
		};

		private static readonly TwitterService Twitter = new TwitterService {
			ConsumerKey = "Consumer key from https://dev.twitter.com/apps",
			ConsumerSecret = "Consumer secret from https://dev.twitter.com/apps",
			CallbackUrl = new Uri ("Callback URL from https://dev.twitter.com/apps")
		};

		void Share (Service service, Button shareButton)
		{
			Item item = new Item {
				Text = "I'm sharing great things using Xamarin!",
				Links = new List<Uri> {
					new Uri ("http://xamarin.com"),
				},
			};

			Intent intent = service.GetShareUI (this, item, shareResult => {
				shareButton.Text = service.Title + " shared: " + shareResult;
			});

			StartActivity (intent);
		}

		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			SetContentView (Resource.Layout.Main);

			Button flickrButton = FindViewById<Button> (Resource.Id.Flickr);
			flickrButton.Click += (sender, args) =>
			{

				var picker = new MediaPicker (this);

				var intentPicker = picker.GetPickPhotoUI ();

				StartActivityForResult (intentPicker, 1);
			};

			Button facebookButton = FindViewById<Button>(Resource.Id.Facebook);
			facebookButton.Click += (sender, args) => Share (Facebook, facebookButton);

			Button twitterButton = FindViewById<Button> (Resource.Id.Twitter);
			twitterButton.Click += (sender, args) => Share (Twitter, twitterButton);
		}

		protected async override void OnActivityResult (int requestCode, Result resultCode, Intent data)
		{
			if (requestCode != 1)
				return;

			if (resultCode == Result.Canceled)
				return;

			var file = await data.GetMediaFileExtraAsync (this);

			using (var stream = file.GetStream ()) {
				var item = new Item ("I'm sharing great things using Xamarin!") {
					Images = new[] { new ImageData (file.Path) }
				};

				Intent intent = Flickr.GetShareUI (this, item, shareResult => {
					FindViewById<Button> (Resource.Id.Flickr).Text = "Flickr shared: " + shareResult;
				});

				StartActivity (intent);
			}
		}
	}
}