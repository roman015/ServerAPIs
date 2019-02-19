using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
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

        }

        private static void SetupTfvars(IConfiguration configuration)
        {

        }

        public static void SetupSaveFile(IConfiguration configuration)
        {

        }

        public static void SetupServerBinaries(IConfiguration configuration)
        {

        }

        public static void SetupTerraform(IConfiguration configuration)
        {

        }

        public FactorioServiceResult StartGame()
        {
            throw new NotImplementedException();
        }

        public FactorioServiceResult StopGame()
        {
            throw new NotImplementedException();
        }
    }

    public class FactorioServiceResult
    {
        public string ServerStatus { get; set; }
        public string FactorioVersion { get; set; }
        public DateTime LastSaveOn { get; set; }
    }
}
