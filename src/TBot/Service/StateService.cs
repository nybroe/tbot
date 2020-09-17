using Microsoft.AspNetCore.Hosting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TBot.Models;

namespace TBot.Service
{
    public class StateService : IStateService
    {
        private readonly IHostingEnvironment _hostingEnv;
        private readonly IStorageService _storageService;
        public StateService(IStorageService storageService, IHostingEnvironment hostingEnv)
        {
            _storageService = storageService;
            _hostingEnv = hostingEnv;
        }

        public async Task<StateModel> GetAsync()
        {
            var jsonEnvName = _hostingEnv.IsDevelopment() ? "test.json" : "prod.json";

            var blobState = await _storageService.DownloadAsync("state", jsonEnvName);
            if (blobState != null)
            {
                return JsonConvert.DeserializeObject<StateModel>(blobState);
            }

            return null;
        }

        public async Task<List<OrderModel>> GetTradesAsync()
        {
            var jsonEnvName = _hostingEnv.IsDevelopment() ? "test.json" : "prod.json";

            var blobState = await _storageService.DownloadAsync("trades", jsonEnvName);
            if (blobState != null)
            {
                return JsonConvert.DeserializeObject<List<OrderModel>>(blobState);
            }

            return null;
        }
    }
}
