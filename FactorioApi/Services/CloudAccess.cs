using DigitalOcean.API;
using DigitalOcean.API.Models.Requests;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace FactorioApi.Services
{
    public interface ICloudAccess
    {
        void Setup();
        void Teardown();
        bool IsServerRunning();
        bool StartServer();
        bool StopServer();
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
            if(IsServerRunning())
            {
                // Destroy any existing server instances before 
                // this application starts to manage them
                Teardown();
            }

            // Copy the files as if server has never been started before
            Setup(configuration);
        }

        public bool StartServer()
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

                    // Start the script
                    var result = Bash(
                        "cd "
                        + configuration["TerraformAwsSettings:ScriptDetails:Directory"]
                        + "; "
                        + configuration["TerraformAwsSettings:ScriptDetails:StartServerCmd"]
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
                    if(!IsServerRunning())
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

        string GetServerIpAddress()
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

    // TODO : Complete this
    public class DigitalOceanAccess : ICloudAccess
    {
        private readonly IConfiguration Configuration;
        private readonly IConfiguration DOConfiguration;
        private DigitalOceanClient DOClient;

        public DigitalOceanAccess(IConfiguration Configuration)
        {
            this.Configuration = Configuration;
            this.DOConfiguration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(Configuration["DigitalOceanSettings:SettingsFile"].ToString(), optional: false, reloadOnChange: true)
                .Build();

            this.DOClient = new DigitalOceanClient(DOConfiguration["ApiToken"].ToString());
        }

        public bool IsServerRunning()
        {
            throw new NotImplementedException();
        }

        public void Setup()
        {
            throw new NotImplementedException();
        }

        public bool StartServer()
        {
            var droplet = new Droplet
            {
                Name = DOConfiguration["Droplet:Name"],
                RegionSlug = DOConfiguration["Droplet:Region"],
                SizeSlug = DOConfiguration["Droplet:Size"],
                ImageIdOrSlug = DOConfiguration["Droplet:Image"],
                Backups = false,
                Ipv6 = false,
                PrivateNetworking = false,

            };

            throw new NotImplementedException();
        }

        public bool StopServer()
        {
            throw new NotImplementedException();
        }

        public void Teardown()
        {
            throw new NotImplementedException();
        }
    }
}
