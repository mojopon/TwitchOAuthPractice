using System;
using System.Diagnostics;
using System.Net;
using System.Text;

namespace TwitchOAuthPractice
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // Twitch Developer Consoleに表示されるClientIDを入力
            var clientId = "";
            var redirectUrl = "http://localhost:" + SimpleWebServer.Port;

            SimpleWebServer server = new SimpleWebServer();
            server.OnTokenReceived += (str) =>
            {
                Console.WriteLine("トークン取得:" + str);
            };
            server.Start();

            var oauthUrl = string.Format("https://id.twitch.tv/oauth2/authorize?client_id={0}&redirect_uri={1}&response_type=token&scope=chat:read%20chat:edit", clientId, redirectUrl);
            Console.WriteLine("URL:" + oauthUrl);
            ProcessStartInfo pi = new ProcessStartInfo()
            {
                FileName = oauthUrl,
                UseShellExecute = true,
            };
            Process.Start(pi);

            Console.WriteLine("トークン取得まで待機");
            Console.ReadLine();

            server.Close();
        }

        /// <summary>
        /// ブラウザからのリダイレクトでアクセストークンの取得を行うためのサーバーの起動・管理および取得したアクセストークンの通知を行うクラス
        /// </summary>
        private class SimpleWebServer
        {
            public static readonly int Port = 15555;

            private HttpListener _listener = new HttpListener();
            public event Action<string> OnTokenReceived;

            public void Start()
            {
                _listener.Prefixes.Add("http://localhost:" + Port.ToString() + "/");
                _listener.Start();

                WaitForRequest();
            }

            public void Close()
            {
                _listener.Stop();
                _listener.Close();
            }

            private async void WaitForRequest()
            {
                HttpListenerContext context = null;
                try
                {
                    context = await _listener.GetContextAsync();
                }
                catch (Exception ex)
                {
                    // エラー時の処理
                }

                if (context != null)
                {

                    var response = context.Response;
                    response.StatusCode = (int)HttpStatusCode.OK;
                    response.ContentType = "text/html";

                    byte[] buffer = Encoding.UTF8.GetBytes(CreateResponseHtml());
                    response.OutputStream.Write(buffer, 0, buffer.Length);
                    response.OutputStream.Close();

                    GetAccessToken(context);

                    WaitForRequest();
                }
            }

            private string CreateResponseHtml()
            {
                return $@"
                <!DOCTYPE html><html><head></head><body></body>
                  <script>
                    var hash = location.hash;
                    if(hash.length > 0) 
                    {{
                      var params = hash.split('&');     
                      if(params.length > 0 && params[0].match(/#access_token/)) 
                      {{
                        var token = params[0].split('=')[1];
                        window.location.replace(`http://localhost:{Port}/?access_token=` + token);
                      }}
                    }}
                  </script>
                </html>
            ";
            }

            private void GetAccessToken(HttpListenerContext context)
            {
                var request = context.Request;

                if (!string.IsNullOrEmpty(request.QueryString["access_token"]))
                {
                    OnTokenReceived?.Invoke(request.QueryString["access_token"]);
                }
            }
        }
    }


}