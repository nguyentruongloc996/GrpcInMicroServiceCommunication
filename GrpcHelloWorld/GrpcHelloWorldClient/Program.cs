using Grpc.Net.Client;
using GrpcHelloWorldClient.Protos;
using System;
using System.Threading.Tasks;

namespace GrpcHelloWorldClient
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // create a channel in port 5001 which is server port.
            using var channel = GrpcChannel.ForAddress("https://localhost:5001");
            // transmiss the channel into the constructor of Server Client.
            var client = new HelloService.HelloServiceClient(channel);
            // Call for SayHello service, and wait for response.
            var reply = await client.SayHelloAsync(
                    new HelloRequest { Name = "Hello SWN Client" }
                );

            Console.WriteLine("Greeting: " + reply.Message);
            Console.ReadKey();
        }
    }
}
