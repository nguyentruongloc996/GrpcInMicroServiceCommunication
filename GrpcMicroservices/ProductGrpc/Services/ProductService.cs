using AutoMapper;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProductGrpc.Data;
using ProductGrpc.Models;
using ProductGrpc.Protos;
using System.Threading.Tasks;
using static ProductGrpc.Protos.ProductProtoService;

namespace ProductGrpc.Services
{
    // override grpc service gen from protofile to use.
    public class ProductService : ProductProtoServiceBase
    {
        private readonly ProductsContext productsContext;
        private readonly IMapper mapper;
        private readonly ILogger<ProductService> logger;

        public ProductService(ProductsContext productsContext, IMapper mapper, ILogger<ProductService> logger)
        {
            this.productsContext = productsContext;
            this.mapper = mapper;
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
            if (product == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, 
                    $"Product with ID={request.ProductId} is not found"));
            }

            var productModel = mapper.Map<ProductModel>(product);

            return productModel;
        }

        public override async Task GetAllProduct(GetAllProductRequest request, IServerStreamWriter<ProductModel> responseStream, ServerCallContext context)
        {
            var productList = await productsContext.Product.ToListAsync();
            if(productList == null)
            {
                return;
            }

            foreach (var product in productList)
            {
                var productModel = mapper.Map<ProductModel>(product);

                await responseStream.WriteAsync(productModel);
            }
        }

        public override async Task<ProductModel> AddProduct(AddProductRequest request, ServerCallContext context)
        {
            var productModel = request.Product;
            var product = mapper.Map<Models.Product>(productModel);

            // add the info into the database to get the ID auto generated.
            productsContext.Product.Add(product);
            await productsContext.SaveChangesAsync();

            // Map back the product with the ID into the model to return
            productModel = mapper.Map<ProductModel>(product);
            return productModel;
        }

        public override async Task<ProductModel> UpdateProduct(UpdateProductRequest request, ServerCallContext context)
        {
            var product = mapper.Map<Product>(request.Product);

            bool isExist = await productsContext.Product.AnyAsync(p => p.ProductId == product.ProductId);
            if (!isExist)
            {
                throw new RpcException(new Status(StatusCode.NotFound,
                    $"Product with ID={product.ProductId} is not found"));
            }

            productsContext.Entry(product).State = EntityState.Modified;
            try
            {
                await productsContext.SaveChangesAsync();
            }
            catch (System.Exception)
            {

                throw;
            }

            var productModel = mapper.Map<ProductModel>(product);
            return productModel;
        }

        public override async Task<DeleteProductResponse> DeleteProduct(DeleteProductRequest request, ServerCallContext context)
        {
            var product = await productsContext.Product.FindAsync(request.ProductId);
            if (product == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound,
                    $"Product with ID={product.ProductId} is not found"));
            }

            productsContext.Product.Remove(product);
            var deleteCount = await productsContext.SaveChangesAsync();

            var response = new DeleteProductResponse
            {
                Success = deleteCount > 0
            };
            return response;
        }

        public override async Task<InsertBulkProductResponse> InsertBulkProduct(IAsyncStreamReader<ProductModel> requestStream, ServerCallContext context)
        {
            while (await requestStream.MoveNext())
            {
                var product = mapper.Map<Product>(requestStream.Current);
                productsContext.Product.Add(product);
            }

            var insertCount = await productsContext.SaveChangesAsync();
            var response = new InsertBulkProductResponse
            {
                Success = insertCount > 0,
                InsertCount = insertCount
            };

            return response;
        }
    }
}
