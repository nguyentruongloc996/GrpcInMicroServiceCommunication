using Grpc.Net.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProductGrpc.Protos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ProductWorkerService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration _configuration;

        public Worker(ILogger<Worker> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

                var serverUrl = _configuration.GetValue<string>("WorkerService:ServerUrl");
                using var channel = GrpcChannel.ForAddress(serverUrl);
                var client = new ProductProtoService.ProductProtoServiceClient(channel);

                Console.WriteLine("GetProductAsync started...");
                var response = await client.GetProductAsync(
                        new GetProductRequest
                        {
                            ProductId = 1
                        }
                    );
                Console.WriteLine(response.ToString());

                var taskIntervalTime = _configuration.GetValue<int>("WorkerService:TaskInterval");
                await Task.Delay(taskIntervalTime, stoppingToken);
            }
        }
    }
}
