using Grpc.Core;
using Grpc.Net.Client;
using ProductGrpc.Protos;
using System;
using System.Threading.Tasks;
using static ProductGrpc.Protos.ProductProtoService;

namespace ProductGrpcClient
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Waiting for service to run...");

            using var channel = GrpcChannel.ForAddress("https://localhost:5001");
            var client = new ProductProtoService.ProductProtoServiceClient(channel);
            
            //await GetProductAsync(1, client);
            //await GetAllProductAsync(client);
            await GetAllProductAsyncWithCSharp9(client);

            Console.ReadKey();
        }

        static async Task GetProductAsync(int productId, ProductProtoServiceClient client)
        {
            // GetProductAsync
            Console.WriteLine("GetProductAsync started...");
            var response = await client.GetProductAsync(
                new GetProductRequest
                {
                    ProductId = 1
                }
            );

            Console.WriteLine("GetProductAsync Response: " + response.ToString());
        }

        static async Task GetAllProductAsync(ProductProtoServiceClient client)
        {
            // GetProductAsync
            Console.WriteLine("GetAllProduct started...");
            using (var response = client.GetAllProduct (new GetAllProductRequest()))
            {
                var stream = response.ResponseStream;
                while (await stream.MoveNext(new System.Threading.CancellationToken()))
                {
                    var currentProductModel = stream.Current;
                    Console.WriteLine("Product: " + currentProductModel.ToString());
                }
            }
        }

        static async Task GetAllProductAsyncWithCSharp9(ProductProtoServiceClient client)
        {
            // GetProductAsync
            Console.WriteLine("GetAllProduct started...");

            using var response = client.GetAllProduct(new GetAllProductRequest());
            await foreach (var product in response.ResponseStream.ReadAllAsync())
            {
                Console.WriteLine("Product: " + product);
            }
        }
    }
}
