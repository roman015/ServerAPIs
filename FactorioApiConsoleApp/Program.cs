using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Net;
using System.Reflection;
using McMaster.Extensions.CommandLineUtils;

namespace FactorioApiConsoleApp
{
    class Program
    {
        [Option(Description = "Values Allowed are 'check', 'start' or 'stop'")]
        [AllowedValues(
            "check",
            "start",
            "stop",
            IgnoreCase = true
            )]
        [Required]
        string Action { get; set; }

        [Option(Description = "6 Digit Otp")]
        [Range(100000, 999999)]
        string Otp { get; set; }

        [Option(Description = "User's Email")]
        string Email { get; set; }

        bool isEmailExtractedFromFile = false;
        string token = "";
        string response = "";

        readonly string emailFile = "email.txt";
        readonly string tokenFile = "token.txt";
        readonly string dateTimeFormat = @"yyyy - MM - dd HH:mm:ss";
        readonly string loginUrl = @"http://roman015.com/Authenticate/Login";
        readonly string startUrl = @"http://roman015.com/Factorio/Start";
        readonly string stopUrl = @"http://roman015.com/Factorio/Stop";
        readonly string checkUrl = @"http://roman015.com/Factorio/Check";

        public static int Main(string[] args) => CommandLineApplication.Execute<Program>(args);

        public void OnExecute()
        {
            // Get email from file if needed
            if (string.IsNullOrWhiteSpace(Email))
            {
                if (!GetEmail())
                {
                    Console.WriteLine("Enter email :");
                    Email = Console.ReadLine();
                }
                else
                {
                    isEmailExtractedFromFile = true;
                }
            }

            // Validate email
            if (string.IsNullOrWhiteSpace(Email))
            {
                Console.Error.WriteLine("Email Invalid, either enter email or use " + emailFile + " to store email");
                Environment.Exit(-1);
            }

            // Get token, either from otp or by existing stored value
            if (string.IsNullOrWhiteSpace(Otp))
            {
                if (!GetToken())
                {
                    Console.Error.WriteLine("Otp not included and token invalid - Otp is required");
                    Environment.Exit(-1);
                }
            }
            else if (!LoginAndGetToken())
            {
                Console.Error.WriteLine("Could not get token");
                Environment.Exit(-1);
            }

            // Offer to store email if possible
            if (!isEmailExtractedFromFile && !string.IsNullOrWhiteSpace(Email))
            {
                Console.WriteLine("Would you like to store email for later use? (y/n)");
                switch (Console.ReadKey().Key)
                {
                    case ConsoleKey.Y:
                        // Store email
                        File.WriteAllText(emailFile, Email);
                        break;
                    case ConsoleKey.N:
                        // Do nothing;
                        break;
                    default:
                        Console.Error.WriteLine("Invalid response, taking no action for now");
                        break;
                }
                Console.WriteLine();
            }

            // Perform selected action
            switch (Action.ToLower())
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

        bool GetEmail()
        {
            if (File.Exists(emailFile))
            {
                Email = File.ReadAllText(emailFile).Trim();
            }

            return !String.IsNullOrWhiteSpace(Email);
        }

        bool GetToken()
        {
            try
            {
                if (File.Exists(tokenFile))
                {
                    string[] tokenLines = File.ReadAllLines(tokenFile);
                    bool isDateTimeValid = DateTime.TryParseExact(
                                               tokenLines[0],
                                               dateTimeFormat,
                                               System.Globalization.CultureInfo.InvariantCulture,
                                               System.Globalization.DateTimeStyles.None,
                                               out DateTime expiry);

                    if (isDateTimeValid)
                    {
                        if (expiry > DateTime.Now)
                        {
                            token = tokenLines[1];
                        }
                        else
                        {
                            Console.Error.WriteLine("Token expired, cannot use existing token");
                            File.Delete(tokenFile);
                        }
                    }
                    else
                    {                       
                        Console.Error.WriteLine("Could not get stored token from file");
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

        bool LoginAndGetToken()
        {
            try
            {
                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(loginUrl);
                webRequest.Method = "POST";
                webRequest.UserAgent = "FactorioApiConsoleApp";
                webRequest.ContentType = "application/json";
                StreamWriter sw = new StreamWriter(webRequest.GetRequestStream());


                sw.WriteLine("{"
                    + "\"Email\":" + "\"" + Email + "\","
                    + "\"Otp\":" + "\"" + Otp + "\""
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
                    tokenWriter.WriteLine(DateTime.Now.AddMinutes(29).ToString(dateTimeFormat));
                    tokenWriter.WriteLine(token);
                    tokenWriter.Close();

                    return true;

                }
                else
                {
                    Console.Error.WriteLine("Failed : Got response " + webResponse.StatusCode);
                    return false;
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message);
                return false;
            }
        }

        string AuthorizedGet(string url)
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
