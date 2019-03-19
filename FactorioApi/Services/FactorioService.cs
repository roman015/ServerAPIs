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
        Object StartGame();
        Object StopGame();
        Object CheckGame();
    }

    public class FactorioService : IFactorioService
    {
        private readonly ICloudAccess CloudAccess;

        public FactorioService(ICloudAccess CloudAccess)
        {
            this.CloudAccess = CloudAccess;
        }
               
        public Object StartGame()
        {
            if(!CloudAccess.IsServerRunning())
            {
                return new
                {
                    status = CloudAccess.StartServer() ? "Running" : "Failed",
                    ip = CloudAccess.GetServerIpAddress()
                }; 
            }
            else
            {
                return new
                {
                    status = "Running",
                    ip = CloudAccess.GetServerIpAddress()
                };
            }
        }

        public Object StopGame()
        {
            if (CloudAccess.IsServerRunning())
            {
                return new
                {
                    status = CloudAccess.StopServer() ? "Stopped" : "Failed"
                }; 
            }
            else
            {
                return new
                {
                    status = "Stopped"
                };
            }
        }

        public Object CheckGame()
        {
            if(CloudAccess.IsServerRunning())
            {
                return new
                {
                    status = "Running",
                    ip = CloudAccess.GetServerIpAddress()
                };
            }
            else
            {
                return new
                {
                    status = "Stopped",
                    ip = ""
                };
            }
        }


    }
}
