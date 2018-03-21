using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Net;
using System.IO;
using System.Windows.Forms;

namespace CSharp虾米签到 {
	class Program {
		const string LoginHomeUrl = "https://login.xiami.com/member/login";
		const string LoginUrl = "https://login.xiami.com/passport/login";
		const string CheckInUrl = "http://www.xiami.com/task/signin";
		const string UserAgent = "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/60.0.3112.113 Safari/537.36";

		static readonly Uri HostUri = new Uri("http://www.xiami.com");

		static CookieContainer cookies = new CookieContainer();

		static string Post(string url, IReadOnlyDictionary<string, string> headers = null, IReadOnlyDictionary<string, string> data = null) {
			string strData = data != null ? string.Join("&", data.Select(kv => $"{kv.Key}={HttpUtility.UrlEncode(kv.Value, Encoding.UTF8)}")) : "";
			var bytesData = Encoding.UTF8.GetBytes(strData);

			var http = WebRequest.CreateHttp(url);
			http.Method = "POST";
			http.UserAgent = UserAgent;
			http.CookieContainer = cookies;
			http.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
			http.ContentLength = bytesData.Length;

			if (headers != null) {
				foreach (var kv in headers) {
					switch (kv.Key) {
						case "Referer": http.Referer = kv.Value; break;
						default: http.Headers.Add(kv.Key, kv.Value); break;
					}
				}
			}
			var writeStream = http.GetRequestStream();
			writeStream.Write(bytesData, 0, bytesData.Length);
			writeStream.Flush();
			using (var res = http.GetResponse()) {
				if (res.Headers["Set-Cookie"] != null) cookies.SetCookies(HostUri, res.Headers["Set-Cookie"]);
				var setcookie = res.Headers["Set-Cookie"];
				return new StreamReader(res.GetResponseStream(), Encoding.UTF8).ReadToEnd();
			}
		}

		static IReadOnlyDictionary<string, string> GetCookies(CookieContainer cookies) {
			var result = new Dictionary<string, string>();
			var cs = cookies.GetCookies(HostUri);
			foreach (Cookie c in cs) {
				result.Add(c.Name, c.Value);
			}
			return result;
		}

		static bool Login(string email, string password) {
			var http = WebRequest.CreateHttp(LoginHomeUrl);
			http.Method = "GET";
			http.UserAgent = UserAgent;
			using (var res = http.GetResponse() as HttpWebResponse) {
				cookies.SetCookies(HostUri, res.Headers["Set-Cookie"]);
			}
			var cookie = GetCookies(cookies);

			var headers = new Dictionary<string, string> {
				{ "Referer", LoginHomeUrl },
				{ "X-Requested-With", "XMLHttpRequest" }
			};
			var data = new Dictionary<string, string> {
				{ "account", email },
				{ "pw", password },
				{ "submit", "登 录" },
				{ "verifycode", "" },
				{ "done", "http://www.xiami.com" },
				{ "_xiamitoken", cookie["_xiamitoken"] }
			};
			var result = Post(LoginUrl, headers, data);
			Console.WriteLine($"登录：{result}");
			if (!result.Contains("true")) {
				MessageBox.Show("虾米签到登录失败。", "错误");
				return false;
			}
			return true;
		}

		static void CheckIn() {
			var cookie = GetCookies(cookies);

			var headers = new Dictionary<string, string> {
				{ "Referer", "http://www.xiami.com" },
			};
			cookies.Add(HostUri, new Cookie("user", cookie["user"]));
			cookies.Add(HostUri, new Cookie("member_auth", cookie["member_auth"]));
			cookies.Add(HostUri, new Cookie("login_method", cookie["login_method"]));
			var result = Post(CheckInUrl, headers).Trim();
			if (!int.TryParse(result, out _)) {
				MessageBox.Show("虾米签到过程中发生错误，服务器返回结果不是整型。(1)", "错误");
				return;
			}

			cookies.Add(HostUri, new Cookie("t_sign_auth", result));
			result = Post(CheckInUrl, headers).Trim();
			if (int.TryParse(result, out _)) {
				Console.WriteLine($"签到天数：{result}");
			} else {
				MessageBox.Show("虾米签到过程中发生错误，服务器返回结果不是整型。(2)", "错误");
			}
		}

		static void Main(string[] args) {
			if (Login("email", "password"))
				CheckIn();
		}
	}
}
