using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProductGrpc.Data;
using ProductGrpc.Protos;
using System.Threading.Tasks;
using static ProductGrpc.Protos.ProductProtoService;

namespace ProductGrpc.Services
{
    public class ProductService : ProductProtoServiceBase
    {
        private readonly ProductsContext productsContext;
        private readonly ILogger<ProductService> logger;

        public ProductService(ProductsContext productsContext, ILogger<ProductService> logger)
        {
            this.productsContext = productsContext;
            this.logger = logger;
        }

        // Can be used to test Grpc service from the Grpc command line tool.
        public override Task<Empty> Test(Empty request, ServerCallContext context)
        {
            return base.Test(request, context);
        }

        public override async Task<ProductModel> GetProduct(GetProductRequest request, ServerCallContext context)
        {
            var product = await productsContext.Product.FindAsync(request.ProductId);
            if(product == null)
            {

            }

            var productModel = new ProductModel
            {
                ProductId = product.ProductId,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                Status = ProductStatus.Instock,
                CreatedTime = Timestamp.FromDateTime(product.CreateTime)
            };

            return productModel;
        }

        public override async Task GetAllProduct(GetAllProductRequest request, IServerStreamWriter<ProductModel> responseStream, ServerCallContext context)
        {
            var productList = await productsContext.Product.ToListAsync();
            if(productList == null)
            {

            }

            foreach (var product in productList)
            {
                var productModel = new ProductModel
                {
                    ProductId = product.ProductId,
                    Name = product.Name,
                    Description = product.Description,
                    Price = product.Price,
                    Status = ProductStatus.Instock,
                    CreatedTime = Timestamp.FromDateTime(product.CreateTime)
                };

                await responseStream.WriteAsync(productModel);
            }
        }
    }
}
