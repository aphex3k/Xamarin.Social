//
//  Copyright 2012, Xamarin Inc.
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//
//        http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Text;
using System.Threading;
using Xamarin.Auth;
using System.Diagnostics;
using System.Json;

namespace Xamarin.Social.Services
{
	public class FacebookService : OAuth2Service
	{
		public FacebookService ()
			: base ("Facebook", "Facebook")
		{
			CreateAccountLink = new Uri ("https://www.facebook.com");

			MaxTextLength = int.MaxValue;
			MaxImages = 1;
			MaxLinks = 1;

			AuthorizeUrl = new Uri ("https://m.facebook.com/dialog/oauth/");
			RedirectUrl = new Uri ("http://www.facebook.com/connect/login_success.html");

			Scope = "publish_stream";
		}

		protected override Task<string> GetUsernameAsync (IDictionary<string, string> accountProperties)
		{
			var request = CreateRequest (
				"GET",
				new Uri ("https://graph.facebook.com/me"),
				new Account ("", accountProperties));

			return request.GetResponseAsync ().ContinueWith (reqTask => {
				var json = reqTask.Result.GetResponseText ();
				var username = GetValueFromJson (json, "name");
				if (string.IsNullOrEmpty (username)) {
					throw new Exception ("Could not read username from the /me API call");
				}
				else {
					return username;
				}
			});
		}
		
		static string GetValueFromJson (string json, string key)
		{
			var p = json.IndexOf ("\"" + key + "\"");
			if (p < 0) return "";
			var c = json.IndexOf (":", p);
			if (c < 0) return "";
			var q = json.IndexOf ("\"", c);
			if (q < 0) return "";
			var b = q + 1;
			var e = b;
			for (; e < json.Length && json[e] != '\"'; e++) {
			}
			var r = json.Substring (b, e - b);
			return r;
		}

		public async override Task ShareItemAsync (Item item, Account account, CancellationToken cancellationToken)
		{
			string placeId = null;

			if (item.Location.Latitude != 0 && item.Location.Longitude != 0)
			{
				Request placeRequest = CreateRequest("GET", new Uri ("https://graph.facebook.com/search"), account);

				placeRequest.Parameters ["type"] = "place";
				placeRequest.Parameters ["center"] = item.Location.Latitude.ToString() + "," + item.Location.Longitude.ToString();
				placeRequest.Parameters ["distance"] = "100";

                var reqTask = await placeRequest.GetResponseAsync(cancellationToken).ConfigureAwait(false);

                var obj = JsonObject.Parse(reqTask.GetResponseText());

				if (obj.ContainsKey("data") && obj["data"].Count > 0 && obj["data"][0].ContainsKey("id"))
					placeId = obj ["data"] [0] ["id"];

                await ShareItemWithLocationAsync(item, account, placeId, cancellationToken).ConfigureAwait(false);

                return;
			} 
			else
			{
                try
                {
                    await ShareItemWithLocationAsync(item, account, null, cancellationToken).ConfigureAwait(false);
                }
                catch(Exception e)
                {
                    #if DEBUG
                    throw(e);
                    #endif
                }
                return;
			}
		}

		public Task ShareItemWithLocationAsync (Item item, Account account, string placeId, CancellationToken cancellationToken)
		{

			Request req;

			if (item.Images.Count > 0) {
				req = CreateRequest ("POST", new Uri ("https://graph.facebook.com/me/photos"), account);
				item.Images.First ().AddToRequest (req, "source");

				var message = new StringBuilder ();
				message.Append (item.Text);
				foreach (var l in item.Links) {
					message.AppendLine ();
					message.Append (l.AbsoluteUri);
				}
				req.AddMultipartData ("message", message.ToString ());
				if (placeId != null) {

					req.AddMultipartData("place", placeId);
				}
			}
			else {
				req = CreateRequest ("POST", new Uri ("https://graph.facebook.com/me/feed"), account);
				req.Parameters["message"] = item.Text;
				if (item.Links.Count > 0) {
					req.Parameters["link"] = item.Links.First ().AbsoluteUri;
				}
				if (placeId != null) {

					req.Parameters.Add("place", placeId);
				}
			}

			return req.GetResponseAsync (cancellationToken).ContinueWith (reqTask => {
				var content = reqTask.Result.GetResponseText ();
				if (!content.Contains ("\"id\"")) {
					throw new SocialException ("Facebook returned an unrecognized response.");
				}
            });
		}
	}
}

