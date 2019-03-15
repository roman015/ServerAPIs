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
        string StartGame();
        string StopGame();

        string CheckGame();
    }

    public class FactorioService : IFactorioService
    {
        private readonly ICloudAccess CloudAccess;

        public FactorioService(ICloudAccess CloudAccess)
        {
            this.CloudAccess = CloudAccess;
        }
               
        public string StartGame()
        {
            if(!CloudAccess.IsServerRunning())
            {
                return CloudAccess.StartServer() ? "Success" : "Failed To Start Server";
            }
            else
            {
                return "Server is already running";
            }
        }

        public string StopGame()
        {
            if (CloudAccess.IsServerRunning())
            {
                return CloudAccess.StopServer() ? "Success" : "Failed To Stop Server";
            }
            else
            {
                return "Server is already stopped";
            }
        }

        public string CheckGame()
        {
            return CloudAccess.IsServerRunning() ? "Running" : "Stopped";
        }


    }
}
