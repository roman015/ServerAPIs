using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FactorioApi.Services
{
    public interface IFactorioService
    {
        FactorioServiceResult StartGame();
        FactorioServiceResult StopGame();
    }

    public class FactorioService : IFactorioService
    {
        private static bool IsFirstTimeSetupDone = false;
        private readonly IConfiguration configuration;
        private static object awsLock = new object();

        public FactorioService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        // Needs to be called once only
        public static void Setup(IConfiguration setupConfiguration)
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

        private static void SetupSshKeys(IConfiguration configuration)
        {
            // Copy the private key
            File.Copy(
                sourceFileName:
                    configuration["ScriptDetails:Setup:SshKeySource"],
                destFileName:
                    configuration["ScriptDetails:Directory"]
                    + configuration["ScriptDetails:SshKey"],
                overwrite:
                    true);

            // Copy the public key
            File.Copy(
                sourceFileName:
                    configuration["ScriptDetails:Setup:SshKeySource"]
                    + ".pub",
                destFileName:
                    configuration["ScriptDetails:Directory"]
                    + configuration["ScriptDetails:SshKey"]
                    + ".pub",
                overwrite:
                    true);

            // TODO: Review if chmod is needed
        }

        private static void SetupTfvars(IConfiguration configuration)
        {
            // Copy the tfvars file
            File.Copy(
                sourceFileName:
                    configuration["ScriptDetails:Setup:TfvarsSource"],
                destFileName:
                    configuration["ScriptDetails:Directory"]
                    + configuration["ScriptDetails:Tfvars"],
                overwrite:
                    true);
        }

        public static void SetupSaveFile(IConfiguration configuration)
        {
            // Copy the save file
            File.Copy(
                sourceFileName:
                    configuration["ScriptDetails:Setup:SaveFile"],
                destFileName:
                    configuration["ScriptDetails:Directory"]
                    + configuration["ScriptDetails:SaveFile"],
                overwrite:
                    true);
        }

        public static void SetupServerBinaries(IConfiguration configuration)
        {
            // TODO : Copy the factorio binary during setup

            // Copy the server settings json file
            File.Copy(
                sourceFileName:
                    configuration["ScriptDetails:Setup:SettingsJson"],
                destFileName:
                    configuration["ScriptDetails:Directory"]
                    + configuration["ScriptDetails:SettingsJson"],
                overwrite:
                    true);
        }

        public static void SetupTerraform(IConfiguration configuration)
        {
            var result = Bash(
                "cd "
                + configuration["ScriptDetails:Directory"]
                + "; "
                + configuration["ScriptDetails:Setup:SetupTerraformCmd"]
                );

            Console.WriteLine(result);
        }

        public FactorioServiceResult StartGame()
        {
            if (Monitor.TryEnter(awsLock))
            {
                try
                {
                    // Copy the save file
                    File.Copy(
                        sourceFileName:
                            configuration["ScriptDetails:Setup:SaveFile"],
                        destFileName:
                            configuration["ScriptDetails:Directory"]
                            + configuration["ScriptDetails:SaveFile"],
                        overwrite:
                            true);

                    var LastAccessedOn = File.GetLastAccessTime(
                            configuration["ScriptDetails:Setup:SaveFile"]
                        );

                    // Start the script
                    var result = Bash(
                        "cd "
                        + configuration["ScriptDetails:Directory"]
                        + "; "
                        + configuration["ScriptDetails:Setup:StartServerCmd"]
                        );

                    Console.WriteLine(result);

                    return new FactorioServiceResult()
                    {
                        ServerStatus = "Started",
                        FactorioVersion = "UNKNOWN",
                        LastSaveOn = LastAccessedOn
                    };
                }
                finally
                {
                    Monitor.Exit(awsLock);
                }
            }
            else
            {
                return new FactorioServiceResult()
                {
                    ServerStatus = "ERROR : Another Request in progress",
                };
            }
        }

        public FactorioServiceResult StopGame()
        {
            if (Monitor.TryEnter(awsLock))
            {
                try
                {
                    // Start the script to stop the server
                    var result = Bash(
                        "cd "
                        + configuration["ScriptDetails:Directory"]
                        + "; "
                        + configuration["ScriptDetails:Setup:StopServerCmd"]
                        );

                    Console.WriteLine(result);

                    // Copy the save file back to storage
                    File.Copy(
                        sourceFileName:
                            configuration["ScriptDetails:Directory"]
                            + configuration["ScriptDetails:SaveFile"],
                        destFileName:
                            configuration["ScriptDetails:Setup:SaveFile"],                            
                        overwrite:
                            true);

                    var LastAccessedOn = File.GetLastAccessTime(
                            configuration["ScriptDetails:Setup:SaveFile"]
                        );

                    return new FactorioServiceResult()
                    {
                        ServerStatus = "Stopped",
                        FactorioVersion = "UNKNOWN",
                        LastSaveOn = LastAccessedOn
                    };
                }
                finally
                {
                    Monitor.Exit(awsLock);
                }
            }
            else
            {
                return new FactorioServiceResult()
                {
                    ServerStatus = "ERROR : Another Request in progress",
                };
            }
        }

        private static string Bash(string cmd)
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

    public class FactorioServiceResult
    {
        public string ServerStatus { get; set; }
        public string FactorioVersion { get; set; }
        public DateTime LastSaveOn { get; set; }
    }
}
