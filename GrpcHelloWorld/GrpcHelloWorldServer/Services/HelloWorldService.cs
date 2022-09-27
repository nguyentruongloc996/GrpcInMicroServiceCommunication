using Grpc.Core;
using GrpcHelloWorldServer.Protos;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GrpcHelloWorldServer.Services
{
    public class HelloWorldService : HelloService.HelloServiceBase
    {
        // Inherited from the service class generate from the protofile
        // for customize it
        
        private readonly ILogger<HelloWorldService> logger;

        public HelloWorldService(ILogger<HelloWorldService> logger)
        {
            this.logger = logger;
        }

        public override Task<HelloResponse> SayHello(HelloRequest request, ServerCallContext context)
        {
            string resultMessage = $"Hello {request.Name}";

            var response = new HelloResponse
            {
                Message = resultMessage
            };

            return Task.FromResult(response);
        }
    }
}
