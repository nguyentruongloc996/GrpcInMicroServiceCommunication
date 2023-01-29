using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ShoppingCartGrpc.Protos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace ShoppingCartWorkerService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration config;

        public Worker(ILogger<Worker> logger, IConfiguration configuration)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.config = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {

                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

                // Create Shopping cart if not exists.
                // Retrive products
                // Add sc items into SC with client stream

                using var scChannel = GrpcChannel.ForAddress
                    (config.GetValue<string>("WorkerService:ShoppingCartServerUrl"));
                var scClient = new ShoppingCartProtoService.ShoppingCartProtoServiceClient(scChannel);
                var scModel = await GetOrCreateShoppingCartAsync(scClient);
                

                await Task.Delay(config.GetValue<int>("WorkerService:TaskInterval"), stoppingToken);
            }
        }

        private async Task<ShoppingCartModel> GetOrCreateShoppingCartAsync(ShoppingCartProtoService.ShoppingCartProtoServiceClient scClient)
        {
            ShoppingCartModel shoppingCartModel = null;

            // try to get sc
            try
            {
                _logger.LogInformation("GetShoppingCartAsync started...");
                var shoppingCartRequest = new GetShoppingCartRequest
                {
                    Username = config.GetValue<string>("WorkerService:UserName")
                };
                shoppingCartModel = await scClient.GetShoppingCartAsync(shoppingCartRequest);
                _logger.LogInformation($"GetShoppingCartAsync Response: {shoppingCartModel}");
            }
            catch (RpcException exception)
            {
                // create shoping cart
                if(exception.StatusCode == StatusCode.NotFound)
                {
                    _logger.LogInformation("CreateShoppingCartAsync started...");
                    var newModel = new ShoppingCartModel
                    {
                        Username = config.GetValue<string>("WorkerService:UserName")
                    };
                    shoppingCartModel = await scClient.CreateShoppingCartAsync(newModel);
                    _logger.LogInformation($"CreateShoppingCartAsync Response: {shoppingCartModel}");
                }
                else
                {
                    throw exception;
                }
            }

            return shoppingCartModel;
        }
    }
}
