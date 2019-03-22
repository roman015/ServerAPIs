using DigitalOcean.API;
using DigitalOcean.API.Models.Requests;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace FactorioApi.Services
{
    public interface ICloudAccess
    {
        void Setup();
        void Teardown();
        bool IsServerRunning();
        bool StartServer(out string serverVersion);
        bool StopServer();
        string GetServerIpAddress();
    }

    public class TerraFormAwsAccess : ICloudAccess
    {
        private static bool IsFirstTimeSetupDone = false;
        private readonly IConfiguration configuration;
        private readonly static object awsLock = new object();

        public TerraFormAwsAccess(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public bool IsServerRunning()
        {
            // TODO : Actually connect to the machine as well
            string serverIP = GetServerIpAddress();

            return !string.IsNullOrWhiteSpace(serverIP);
        }

        public void Setup()
        {
            if (IsServerRunning())
            {
                // Destroy any existing server instances before 
                // this application starts to manage them
                Teardown();
            }

            // Copy the files as if server has never been started before
            Setup(configuration);
        }

        public bool StartServer(out string serverVersion)
        {
            if (Monitor.TryEnter(awsLock))
            {
                try
                {
                    // Copy the save file
                    File.Copy(
                        sourceFileName:
                            configuration["TerraformAwsSettings:ScriptDetails:Setup:SaveFile"],
                        destFileName:
                            configuration["TerraformAwsSettings:ScriptDetails:Directory"]
                            + configuration["TerraformAwsSettings:ScriptDetails:SaveFile"],
                        overwrite:
                            true);

                    var LastAccessedOn = File.GetLastAccessTime(
                            configuration["TerraformAwsSettings:ScriptDetails:Setup:SaveFile"]
                        );

                    // Get the latest Factorio Server Version Number (if possible) to add to start script
                    string versionFlag = "";
                    serverVersion = GetLatestExperimentalVersionNumber();
                    if (!String.IsNullOrWhiteSpace(serverVersion))
                    {
                        versionFlag = configuration["TerraformAwsSettings:ScriptDetails:Directory"]
                            .Replace("FACTORIO_SERVER_VERSION", serverVersion);
                    }

                    // Start the script
                    var result = Bash(
                        "cd "
                        + configuration["TerraformAwsSettings:ScriptDetails:Directory"]
                        + "; "
                        + configuration["TerraformAwsSettings:ScriptDetails:StartServerCmd"]
                        + " "
                        + versionFlag
                        );

                    Console.WriteLine("StartGame : "
                        + Environment.NewLine
                        + "--------------------"
                        + Environment.NewLine
                        + result
                        + Environment.NewLine
                        + "--------------------");

                    return true;
                }
                finally
                {
                    Monitor.Exit(awsLock);
                }
            }
            else
            {
                serverVersion = "";
                return false;
            }
        }

        public bool StopServer()
        {
            if (Monitor.TryEnter(awsLock))
            {
                try
                {
                    // Check if the server is running
                    if (!IsServerRunning())
                    {
                        return false;
                    }

                    // Get the server ip
                    string ServerIP = GetServerIpAddress();

                    // Create the copy save command
                    string copySaveFileCmd = configuration["TerraformAwsSettings:ScriptDetails:CopySaveFileCmd"]
                        .Replace("SSH_KEY",
                            configuration["TerraformAwsSettings:ScriptDetails:Directory"]
                            + configuration["TerraformAwsSettings:ScriptDetails:SshKey"])
                        .Replace("SERVER_IP", ServerIP)
                        .Replace("LOCAL_SAVE_FILE",
                            configuration["TerraformAwsSettings:ScriptDetails:Setup:SaveFile"]);

                    // Copy the save file from server to storage first
                    var copyResult = Bash(copySaveFileCmd);

                    Console.WriteLine("CopySaveFile : "
                        + Environment.NewLine
                        + "--------------------"
                        + Environment.NewLine
                        + copyResult
                        + Environment.NewLine
                        + "--------------------");

                    // Next start the script to stop the server
                    var result = Bash(
                        "cd "
                        + configuration["TerraformAwsSettings:ScriptDetails:Directory"]
                        + "; "
                        + configuration["TerraformAwsSettings:ScriptDetails:StopServerCmd"]
                        );

                    Console.WriteLine("StopGame : "
                        + Environment.NewLine
                        + "--------------------"
                        + Environment.NewLine
                        + result
                        + Environment.NewLine
                        + "--------------------");

                    return true;
                }
                finally
                {
                    Monitor.Exit(awsLock);
                }
            }
            else
            {
                return false;
            }
        }

        public void Teardown()
        {
            // Something to make sure there is no server running, we don't care about 
            // save files or other stuff, cause we just want to pull the plug here.            
            var result = Bash(
                        "cd "
                        + configuration["TerraformAwsSettings:ScriptDetails:Directory"]
                        + "; "
                        + configuration["TerraformAwsSettings:ScriptDetails:Setup:StopServerCmd"]
                        );

            Console.WriteLine("Teardown : "
                + Environment.NewLine
                + "--------------------"
                + Environment.NewLine
                + result
                + Environment.NewLine
                + "--------------------");
        }

        public string GetServerIpAddress()
        {
            IPAddress result;

            // Get the output variable from terraform, if present
            var cmdResult = Bash(
                "cd "
                + configuration["TerraformAwsSettings:ScriptDetails:Directory"]
                + "; "
                + configuration["TerraformAwsSettings:ScriptDetails:GetServerIpCmd"]
                ).Trim();

            Console.WriteLine("GetServerIpAddress : "
                        + Environment.NewLine
                        + "--------------------"
                        + Environment.NewLine
                        + cmdResult
                        + Environment.NewLine
                        + "--------------------");

            // Check if string is a valid ip address before returning it
            if (IPAddress.TryParse(cmdResult, out result))
            {
                return cmdResult;
            }
            else
            {
                return null;
            }
        }

        public string GetLatestExperimentalVersionNumber()
        {
            string result = "";
            String downloadURL = "";

            try
            {
                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(
                    configuration["TerraformAwsSettings:ScriptDetails:FactorioExperimentalHeadlessUrl"]);
                HttpWebResponse response = (HttpWebResponse)webRequest.GetResponse();

                downloadURL = response.ResponseUri.AbsolutePath;
                Uri uri = new Uri(downloadURL);
                downloadURL = uri.AbsolutePath;
                Console.WriteLine("RECEIVED : " + downloadURL);

                // Process URL Response
                // https://dcdn.factorio.com/releases/factorio_headless_x64_0.17.17.tar.xz                
                var match = Regex.Match(downloadURL, @"(\d+)\.(\d+)\.(\d+)");
                if (match.Success)
                {
                    result = match.Groups[0]
                        .ToString()
                        .Trim();
                    Console.WriteLine("GetLatestExperimentalVersionNumber : Latest Version '" + result + "'");
                }
                else
                {
                    Console.WriteLine("GetLatestExperimentalVersionNumber : Could not get Version number from download url!");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("GetLatestExperimentalVersionNumber : " + e.Message);
            }

            return result;
        }

        void Setup(IConfiguration setupConfiguration)
        {
            if (!IsFirstTimeSetupDone)
            {
                SetupSshKeys(setupConfiguration);
                SetupTfvars(setupConfiguration);
                SetupSaveFile(setupConfiguration);
                SetupServerBinaries(setupConfiguration);
                SetupTerraform(setupConfiguration);
                IsFirstTimeSetupDone = true;
            }
            else
            {
                Console.WriteLine("Someone is calling FactorioService::Setup Again");
            }
        }

        void SetupSshKeys(IConfiguration configuration)
        {
#if USE_SSH_KEY_FROM_CONFIG
            // Copy the private key
            File.Copy(
                sourceFileName:
                    configuration["TerraformAwsSettings:ScriptDetails:Setup:SshKeySource"],
                destFileName:
                    configuration["TerraformAwsSettings:ScriptDetails:Directory"]
                    + configuration["TerraformAwsSettings:ScriptDetails:SshKey"],
                overwrite:
                    true);

            // Copy the public key
            File.Copy(
                sourceFileName:
                    configuration["TerraformAwsSettings:ScriptDetails:Setup:SshKeySource"]
                    + ".pub",
                destFileName:
                    configuration["TerraformAwsSettings:ScriptDetails:Directory"]
                    + configuration["TerraformAwsSettings:ScriptDetails:SshKey"]
                    + ".pub",
                overwrite:
                    true);

            // TODO: Review if chmod is needed
#endif
        }

        void SetupTfvars(IConfiguration configuration)
        {
            // Copy the tfvars file
            File.Copy(
                sourceFileName:
                    configuration["TerraformAwsSettings:ScriptDetails:Setup:TfvarsSource"],
                destFileName:
                    configuration["TerraformAwsSettings:ScriptDetails:Directory"]
                    + configuration["TerraformAwsSettings:ScriptDetails:Tfvars"],
                overwrite:
                    true);
        }

        void SetupSaveFile(IConfiguration configuration)
        {
            // Copy the save file
            File.Copy(
                sourceFileName:
                    configuration["TerraformAwsSettings:ScriptDetails:Setup:SaveFile"],
                destFileName:
                    configuration["TerraformAwsSettings:ScriptDetails:Directory"]
                    + configuration["TerraformAwsSettings:ScriptDetails:SaveFile"],
                overwrite:
                    true);
        }

        void SetupServerBinaries(IConfiguration configuration)
        {
            // TODO : Copy the factorio binary during setup

            // Copy the server settings json file
            File.Copy(
                sourceFileName:
                    configuration["TerraformAwsSettings:ScriptDetails:Setup:SettingsJson"],
                destFileName:
                    configuration["TerraformAwsSettings:ScriptDetails:Directory"]
                    + configuration["TerraformAwsSettings:ScriptDetails:SettingsJson"],
                overwrite:
                    true);
        }

        void SetupTerraform(IConfiguration configuration)
        {
            var result = Bash(
                "cd "
                + configuration["TerraformAwsSettings:ScriptDetails:Directory"]
                + "; "
                + configuration["TerraformAwsSettings:ScriptDetails:Setup:SetupTerraformCmd"]
                );

            Console.WriteLine(result);
        }

        private string Bash(string cmd)
        {
            var escapedArgs = cmd.Replace("\"", "\\\"");

            var process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"-c \"{escapedArgs}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };

            process.Start();
            string result = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            return result;
        }
    }
}
