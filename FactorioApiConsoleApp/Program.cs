using System;
using System.IO;
using System.Net;
using System.Reflection;

namespace FactorioApiConsoleApp
{
    class Program
    {
        static string action = "";
        static string otp = "";
        static string email = "";
        static string token = "";
        static string response = "";

        static readonly string emailFile = "email.txt";
        static string tokenFile = "token.txt";

        static readonly string loginUrl = @"http://roman015.com/Authenticate/Login";
        static readonly string startUrl = @"http://roman015.com/Factorio/Start";
        static readonly string stopUrl = @"http://roman015.com/Factorio/Stop";
        static readonly string checkUrl = @"http://roman015.com/Factorio/Check";

        static void Main(string[] args)
        {
            Console.WriteLine(String.Join("|", args));

            // Get Arguments
            switch (args.Length)
            {
                case 3:
                    email = args[2];
                    otp = args[1];
                    action = args[0];
                    break;
                case 2:
                    otp = args[1];
                    action = args[0];
                    break;
                case 1:
                    action = args[0];
                    break;
                default:
                    DisplayHelp();
                    Environment.Exit(-1);
                    break;
            }

            // Get email from file if needed
            if (string.IsNullOrWhiteSpace(email))
            {
                if (!GetEmail())
                {
                    Console.WriteLine("Enter email :");
                    email = Console.ReadLine();
                }
            }

            // Get otp if needed
            if (string.IsNullOrWhiteSpace(otp))
            {
                Console.WriteLine("Enter otp :");
                otp = Console.ReadLine();
            }

            // Validate action
            switch (action.ToLower())
            {
                case "start":
                case "stop":
                case "check":
                    break;
                default:
                    DisplayHelp();
                    Environment.Exit(-1);
                    break;
            }           

            for (int i = 0; i < otp.Length; i++)
            {
                if (!char.IsDigit(otp.ToCharArray()[i]))
                {
                    DisplayHelp();
                    Environment.Exit(-1);
                }
            }

            // Validate email
            if (string.IsNullOrWhiteSpace(email))
            {
                DisplayHelp();
                Environment.Exit(-1);
            }

            // Get token
            if (!GetToken())
            {
                LoginAndGetToken();
            }


            // Perform selected action
            switch (action.ToLower())
            {
                case "check":
                    response = AuthorizedGet(checkUrl);
                    break;
                case "start":
                    response = AuthorizedGet(startUrl);
                    break;
                case "stop":
                    response = AuthorizedGet(stopUrl);
                    break;
            }

            // Display Response
            // TODO : Something more useful someday
            Console.WriteLine(response);
        }

        static void DisplayHelp()
        {
            Console.WriteLine("Syntax: "
                    + System.AppDomain.CurrentDomain.FriendlyName
                    + " "
                    + "<start/stop/check>"
                    + " "
                    + "<OTP>"
                    + " "
                    + "[email]");
            Console.WriteLine("OTP - 6 digit number");
            Console.WriteLine("email - Your registered email. Can also be stored in email.txt to save time");
        }

        static bool GetEmail()
        {
            if (File.Exists(emailFile))
            {
                email = File.ReadAllText(emailFile);
            }

            return !String.IsNullOrWhiteSpace(email);
        }

        static bool GetToken()
        {
            try
            {
                if (File.Exists(tokenFile))
                {
                    string[] tokenLines = File.ReadAllLines(tokenFile);

                    if (DateTime.TryParse(tokenLines[0], out DateTime expiry)
                        && expiry < DateTime.Now)
                    {
                        token = tokenLines[1];
                    }
                    else
                    {
                        File.Delete(tokenFile);
                    }
                }

                Console.WriteLine(
                    "Getting Stored Token..."
                    + (string.IsNullOrWhiteSpace(token) ? "Failed" : "<" + token + ">")
                    );

                return !string.IsNullOrWhiteSpace(token);
            }
            catch (Exception)
            {
                token = "";
                return false;
            }
        }

        static void LoginAndGetToken()
        {
            try
            {
                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(loginUrl);
                webRequest.Method = "POST";
                webRequest.UserAgent = "FactorioApiConsoleApp";
                webRequest.ContentType = "application/json";
                StreamWriter sw = new StreamWriter(webRequest.GetRequestStream());


                sw.WriteLine("{"
                    + "\"Email\":" + "\"" + email + "\","
                    + "\"Otp\":" + "\"" + otp + "\""
                    + "}");
                sw.Flush();

                Console.Write("Getting Token from server...");
                HttpWebResponse webResponse = (HttpWebResponse)webRequest.GetResponse();
                if (webResponse.StatusCode == HttpStatusCode.OK)
                {
                    // Get the token from the response
                    token = new StreamReader(webResponse.GetResponseStream())
                                        .ReadToEnd()
                                        .Replace(Environment.NewLine, "")
                                        .Replace("{", "")
                                        .Replace("}", "")
                                        .Replace("\"", "")
                                        .Replace("token", "")
                                        .Replace(":", "")
                                        .Trim();
                    Console.WriteLine("<" + token + ">");

                    // Store token for later use
                    var tokenWriter = File.CreateText(tokenFile);
                    tokenWriter.WriteLine(DateTime.Now.AddMinutes(29).ToString());
                    tokenWriter.WriteLine(token);

                }
                else
                {
                    Console.WriteLine("Failed : Got response " + webResponse.StatusCode);
                    Environment.Exit(-1);
                }
            }
            catch(Exception e)
            {
                Console.Write(e.Message);
                Environment.Exit(-1);
            }
        }

        static string AuthorizedGet(string url)
        {
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
            webRequest.Headers.Add("Authorization", "Bearer " + token);

            Console.Write("Calling " + url + "...");
            HttpWebResponse webResponse = (HttpWebResponse)webRequest.GetResponse();
            if (webResponse.StatusCode == HttpStatusCode.OK)
            {
                Console.WriteLine("Success");
                return (new StreamReader(webResponse.GetResponseStream())).ReadToEnd();
            }
            else
            {
                Console.WriteLine("Failed : Got response " + webResponse.StatusCode);
                Environment.Exit(-1);
            }

            return "";
        }
    }
}
