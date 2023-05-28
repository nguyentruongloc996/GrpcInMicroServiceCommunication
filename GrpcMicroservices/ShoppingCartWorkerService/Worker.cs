using Grpc.Core;
using Grpc.Net.Client;
using IdentityModel.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProductGrpc.Protos;
using ShoppingCartGrpc.Protos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace ShoppingCartWorkerService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration config;
        private const string USERNAME_CONFIG_KEY = "WorkerService:UserName";
        private const string SHOPPINGCART_URL_CONFIG_KEY = "WorkerService:ShoppingCartServerUrl";
        private const string PRODUCT_URL_CONFIG_KEY = "WorkerService:ProductServerUrl";

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

                // Get Token from IS4
                // Create Shopping cart if not exists.
                // Retrive products
                // Add sc items into SC with client stream

                using var scChannel = GrpcChannel.ForAddress
                    (config.GetValue<string>(SHOPPINGCART_URL_CONFIG_KEY));
                var scClient = new ShoppingCartProtoService.ShoppingCartProtoServiceClient(scChannel);

                //0 Get Token form IS4
                var token = await GetTokenFromIS4();

                //1 Create Shopping cart if not exists.
                var scModel = await GetOrCreateShoppingCartAsync(scClient, token);

                // open sc client stream
                using var scClientStream = scClient.AddItemIntoShopingCart();

                //2 Retrive products 
                using var productChannel = GrpcChannel.ForAddress(config.GetValue<string>(PRODUCT_URL_CONFIG_KEY));
                var productClient = new ProductProtoService.ProductProtoServiceClient(productChannel);

                _logger.LogInformation("GetAllProducts started...");
                using var responseProduct = productClient.GetAllProduct(new GetAllProductRequest());
                await foreach (var product in responseProduct.ResponseStream.ReadAllAsync())
                {
                    _logger.LogInformation($"GetAllProducts Stream Response: {product}");

                    //3 Add product into Shopping Cart
                    var addNewScItem = new AddItemIntoShopingCartRequest
                    {
                        Username = config.GetValue<string>(USERNAME_CONFIG_KEY),
                        DiscountCode = "CODE_100",
                        NewCartItem = new ShoppingCartItemModel
                        {
                            ProductId = product.ProductId,
                            Productname = product.Name,
                            Price = product.Price,
                            Color = "Black",
                            Quantity = 1
                        }
                    };

                    await scClientStream.RequestStream.WriteAsync(addNewScItem);
                    _logger.LogInformation($"ShoppingCart Client Stream Added New Item: {addNewScItem}");
                }
                await scClientStream.RequestStream.CompleteAsync();

                var addItemIntoShoppingCartResponse = await scClientStream;
                _logger.LogInformation($"Add Item into Shopping Cart Response: {addItemIntoShoppingCartResponse}");

                await Task.Delay(config.GetValue<int>("WorkerService:TaskInterval"), stoppingToken);
            }
        }

        private async Task<string> GetTokenFromIS4()
        {
            //discover endponts from metadata
            var client = new HttpClient();
            var disco = await client.GetDiscoveryDocumentAsync(config.GetValue<string>("WorkerService:IdentityServerUrl"));
            if (disco.IsError)
            {
                Console.WriteLine(disco.Error);
                return string.Empty;
            }

            // request token.
            var tokenResponse = await client.RequestClientCredentialsTokenAsync
                (new ClientCredentialsTokenRequest
                {
                    Address = disco.TokenEndpoint,

                    ClientId = "ShoppingCartClient",
                    ClientSecret = "secret",
                    Scope = "ShoppingCartAPI"
                });

            if (tokenResponse.IsError)
            {
                Console.WriteLine(tokenResponse.Error);
                return string.Empty;
            }

            return tokenResponse.AccessToken;
        }

        private async Task<ShoppingCartModel> GetOrCreateShoppingCartAsync(ShoppingCartProtoService.ShoppingCartProtoServiceClient scClient, string token)
        {
            ShoppingCartModel shoppingCartModel = null;
            var header = new Metadata();
            header.Add("Authorization", $"Bearer {token}");

            // try to get sc
            try
            {
                _logger.LogInformation("GetShoppingCartAsync started...");

                var shoppingCartRequest = new GetShoppingCartRequest
                {
                    Username = config.GetValue<string>(USERNAME_CONFIG_KEY)
                };
                shoppingCartModel = await scClient.GetShoppingCartAsync(shoppingCartRequest, header);
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
                        Username = config.GetValue<string>(USERNAME_CONFIG_KEY)
                    };
                    shoppingCartModel = await scClient.CreateShoppingCartAsync(newModel, header);
                    _logger.LogInformation($"CreateShoppingCartAsync Response: {shoppingCartModel}");
                }
                else
                {
                    throw exception;
                }
            }

            return shoppingCartModel;
        }

        static async Task GetAllProductAsync(ProductProtoService. ProductProtoServiceClient client)
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
