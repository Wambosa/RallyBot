using System;
using System.Net;
using System.Text;
using System.Net.Http;

namespace Tai {

    internal static class HttpService {

        public static string GetRawJson(Uri uri, CredentialCache credentials) {

            string result               = "emtpy";
            var request                 = (HttpWebRequest)WebRequest.Create(uri);
            request.Credentials         = credentials;
            HttpWebResponse response    = (HttpWebResponse)request.GetResponse();

            if (response.StatusCode == HttpStatusCode.OK) {

                result = new System.IO.StreamReader(response.GetResponseStream()).ReadToEnd();

            }else{
                result = "no response? status code :" + response.StatusCode;
            }

            return result;
        }

        public static CachedAuthentication GetCachedAuthentication(Uri uri, CredentialCache credentials) {
            //todo: add in check for internet connection
            string body             = "emtpy";
            var request             = (HttpWebRequest)WebRequest.Create(uri);
            request.Credentials     = credentials;
            request.CookieContainer = new CookieContainer();
            var response            = (HttpWebResponse)request.GetResponse();
            body                    = new System.IO.StreamReader(response.GetResponseStream()).ReadToEnd();

            return new CachedAuthentication() {
                rawAuthResponse = body,
                cookies = response.Cookies
            };
        }

	    public static CookieContainer CookieMonster(CookieContainer cookieJar, CookieCollection cookies, Cookie yummy)
	    {
			// Eat the cookies, keep the internal context, cookies are going to be useful i think.
		    return cookieJar;
	    }

        public static string PostJson(string url, string rawJsonObject, CredentialCache credentials, CachedAuthentication cachedAuth) {

            string json_response = string.Empty;

            using (WebClient client = new WebClient()) {//todo: understand 'using' better

                foreach(Cookie cookie in cachedAuth.cookies) {
                    client.Headers.Add(cookie.Name, cookie.Value);}

                client.Headers[HttpRequestHeader.ContentType] = "application/json";
                client.Credentials = credentials;
                byte[] byte_response = client.UploadData(url, Encoding.UTF8.GetBytes(rawJsonObject));

                json_response = Encoding.UTF8.GetString(byte_response);
            }

            return json_response;
        }
    }
}