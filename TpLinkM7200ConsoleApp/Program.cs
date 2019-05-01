using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json.Linq;

namespace TpLinkM7200ConsoleApp
{
    class Program
    {
        static string RouterUrl = @"http://tplinkmifi.net";
        static string RouterPassword = "";

        static string RouterToken = "";

        static readonly string ApiEndpoint = @"/cgi-bin/web_cgi";
        static readonly string AuthApiEndpoint = @"/cgi-bin/auth_cgi";

        static int Main(string[] args)
        {
            var ConsoleApp = new CommandLineApplication
            {
                Name = "TpLinkM7200ConsoleApp",
                Description = "To Control a TP Link M7200 from a desktop Application"
            };

            ConsoleApp.HelpOption(inherited: true);            

            var urlOption = ConsoleApp
                .Option<string>("-url <URL>", "Router URL address", CommandOptionType.SingleValue);
            

            var passwordOption = ConsoleApp
                .Option<string>("-p|--Password <PASSWORD>", "Router Password", CommandOptionType.SingleValue)
                .IsRequired();            

            ConsoleApp.Command("Restart", restartCommand =>
            {                             
                restartCommand.Description = "Restart the router";
                restartCommand.OnExecute(() =>
                {
                    RouterUrl = urlOption.HasValue() ? urlOption.Value() : RouterUrl;
                    RouterPassword = passwordOption.Value();

                    RouterToken = GetRouterToken(RouterUrl, ApiEndpoint, RouterPassword);

                    RestartRouter(RouterToken);
                });
            });

            ConsoleApp.OnExecute(() =>
            {
                Console.WriteLine("Specify a subcommand");
                ConsoleApp.ShowHelp();
                return 1;
            });

            return ConsoleApp.Execute(args);
        }

        private static void RestartRouter(string token)
        {
            string RestartRouterMessage = "{\"module\":\"reboot\",\"action\":0}";

            Console.WriteLine(
                MakeAuthorizedPostApiCall(RouterUrl + ApiEndpoint, RestartRouterMessage, token)
                );
        }

        private static string GetRouterToken(string routerUrl, string apiEndpoint, string routerPassword)
        {
            string GetNonceMessage = "{\"module\":\"authenticator\",\"action\":0}";
            string GetTokenMessage = "{\"module\":\"authenticator\",\"action\":1,\"digest\":\"MD5VALUE\"}";
            string response = "", nonce = "", md5hash = "", token = "";
            JObject NonceJson, TokenJson;

            response = MakePostApiCall(routerUrl + AuthApiEndpoint, GetNonceMessage);

            NonceJson = JObject.Parse(response);
            // Sample : { "authedIP": "0.0.0.0", "nonce": "ugxNhmytRCNZywSq", "result": 1 }
            nonce = (string)NonceJson["nonce"];
            if (string.IsNullOrWhiteSpace(nonce))
            {
                throw new Exception("Unable to get nonce from server");
            }

            md5hash = CalculateMD5Hash(routerPassword + ":" + nonce);
            GetTokenMessage = GetTokenMessage.Replace("MD5VALUE", md5hash);

            response = MakePostApiCall(routerUrl + AuthApiEndpoint, GetTokenMessage);

            TokenJson = JObject.Parse(response);
            // Sample : { "token": "XYZ", "authedIP": "1.1.1.1", "factoryDefault": "1", "result": 0 }
            token = (string)TokenJson["token"];
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new Exception("Unable to get token from server");
            }

            return token;
        }

        private static string MakePostApiCall(string url, string jsonData)
        {
            var request = (HttpWebRequest)WebRequest.Create(url);
            var data = Encoding.ASCII.GetBytes(jsonData);

            request.Method = "POST";
            request.Accept = "application/json";
            request.ContentLength = data.Length;

            var stream = request.GetRequestStream();
            stream.Write(data, 0, data.Length);

            var response = (HttpWebResponse)request.GetResponse();
            var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();

            return responseString;
        }

        private static string MakeAuthorizedPostApiCall(string url, string jsonData, string token)
        {
            var request = (HttpWebRequest)WebRequest.Create(url);

            JObject jObject = JObject.Parse(jsonData);
            jObject.Add("token", token);

            var data = Encoding.ASCII.GetBytes(jObject.ToString());

            request.Method = "POST";
            request.ContentLength = data.Length;

            var stream = request.GetRequestStream();
            stream.Write(data, 0, data.Length);

            var response = (HttpWebResponse)request.GetResponse();
            var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();

            return responseString;
        }

        private static string CalculateMD5Hash(string input)
        {
            MD5 md5Hash = MD5.Create();

            // Convert the input string to a byte array and compute the hash.
            byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));

            // Create a new Stringbuilder to collect the bytes
            // and create a string.
            StringBuilder sBuilder = new StringBuilder();

            // Loop through each byte of the hashed data 
            // and format each one as a hexadecimal string.
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            // Return the hexadecimal string.
            return sBuilder.ToString();
        }
    }
}
