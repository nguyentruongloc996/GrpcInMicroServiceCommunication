using Google.Protobuf.WellKnownTypes;
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
            
            await GetProductAsync(client, 1);
            
            await GetAllProductAsyncWithCSharp9(client);
            await AddProductAsync(client);
            await GetAllProductAsyncWithCSharp9(client);

            await UpdateProductAsync(client);
            await GetAllProductAsync(client);

            await DeleteProductAsync(client, 3);
            await GetAllProductAsync(client);

            await InsertBulkProduct(client);
            await GetAllProductAsyncWithCSharp9(client);

            Console.ReadKey();
        }

        private static async Task InsertBulkProduct(ProductProtoServiceClient client)
        {
            Console.WriteLine("InsertBulkProduct stared...");

            using var clientBulk = client.InsertBulkProduct();

            for (int i = 0; i < 3; i++)
            {
                var productModel = new ProductModel
                {
                    Name = $"Product{i}",
                    Description = " Bulk inserted product",
                    Price = 399,
                    Status = ProductStatus.Instock,
                    CreatedTime = Timestamp.FromDateTime(DateTime.UtcNow)
                };

                await clientBulk.RequestStream.WriteAsync(productModel);
            }

            await clientBulk.RequestStream.CompleteAsync();

            var responseBulk = await clientBulk;
            Console.WriteLine($"Status: {responseBulk.Success}. Insert Count: {responseBulk.InsertCount}");
        }

        private static async Task DeleteProductAsync(ProductProtoServiceClient client, int productId)
        {
            Console.WriteLine("DeleteProductAsync stared...");
            var deleteProductResponse = await client.DeleteProductAsync(new DeleteProductRequest
            {
                ProductId = productId
            });

            Console.WriteLine("DeleteProductAsync Response: " + deleteProductResponse.Success.ToString());
        }

        private static async Task UpdateProductAsync(ProductProtoServiceClient client)
        {
            Console.WriteLine("UpdateProductAsync started...");
            var updateProductResponse = await client.UpdateProductAsync(new UpdateProductRequest
            {
                Product = new ProductModel
                {
                    ProductId = 1,
                    Name = "Red",
                    Description = " New Red Phone Mi10T",
                    Price = 699,
                    Status = ProductStatus.Instock,
                    CreatedTime = Timestamp.FromDateTime(DateTime.UtcNow)
                }
            });
        }

        private static async Task AddProductAsync(ProductProtoServiceClient client)
        {
            Console.WriteLine("AddProductAsync started...");
            var addProductResponse = await client.AddProductAsync(
                    new AddProductRequest
                    {
                        Product = new ProductModel
                        {
                            Name = "Red",
                            Description = " New Red Phone Mi10T",
                            Price = 699,
                            Status = ProductStatus.Instock,
                            CreatedTime = Timestamp.FromDateTime(DateTime.UtcNow)
                        }
                    }
                ) ;

            Console.WriteLine("AddProduct Response: " + addProductResponse.ToString());
        }

        static async Task GetProductAsync(ProductProtoServiceClient client, int productId)
        {
            // GetProductAsync
            Console.WriteLine("GetProductAsync started...");
            var response = await client.GetProductAsync(
                new GetProductRequest
                {
                    ProductId = productId
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
