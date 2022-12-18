using AutoMapper;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ShoppingCartGrpc.Data;
using ShoppingCartGrpc.Models;
using ShoppingCartGrpc.Protos;
using System.Linq;
using System.Threading.Tasks;

namespace ShoppingCartGrpc.Services
{
    public class ShoppingCartService : ShoppingCartProtoService.ShoppingCartProtoServiceBase
    {
        private readonly ShoppingCartContext shoppingCartDBbContext;
        private readonly IMapper mapper;
        private readonly ILogger<ShoppingCartService> logger;

        public ShoppingCartService(ShoppingCartContext shoppingCartDBbContext, IMapper mapper, ILogger<ShoppingCartService> logger)
        {
            this.shoppingCartDBbContext = shoppingCartDBbContext;
            this.mapper = mapper;
            this.logger = logger;
        }

        public override async Task<ShoppingCartModel> GetShoppingCart(GetShoppingCartRequest request, ServerCallContext context)
        {
            var shoppingCart = await shoppingCartDBbContext.ShoppingCarts.
                FirstOrDefaultAsync(s => s.UserName == request.Username);

            if (shoppingCart == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, 
                    "Shopping Cart with Username={request.Username} does not exist"));
            }

            var shoppingCartModel = mapper.Map<ShoppingCartModel>(shoppingCart);
            return shoppingCartModel;
        }

        public override async Task<ShoppingCartModel> CreateShoppingCart(ShoppingCartModel request, ServerCallContext context)
        {
            var shoppingCart = mapper.Map<ShoppingCart>(request);

            var isExist = await shoppingCartDBbContext.ShoppingCarts
                .AnyAsync(s => s.UserName == shoppingCart.UserName);
            if (isExist)
            {
                logger.LogError($"Invalid UserName for ShoppingCart creation. UserName: {shoppingCart.UserName}");
                throw new RpcException(new Status(StatusCode.AlreadyExists, $"Shopping cart with username {shoppingCart.UserName} is already exist"));
            }

            shoppingCartDBbContext.ShoppingCarts.Add(shoppingCart);
            await shoppingCartDBbContext.SaveChangesAsync();
            logger.LogInformation($"Shopping Cart is successfully created: {shoppingCart.Id}_{shoppingCart.UserName}");

            var shoppingCartGrpcModel = mapper.Map<ShoppingCartModel>(shoppingCart);
            return shoppingCartGrpcModel;
        }

        public override Task<AddItemIntoShopingCartResponse> AddItemIntoShopingCart(IAsyncStreamReader<AddItemIntoShopingCartRequest> requestStream, ServerCallContext context)
        {
            return base.AddItemIntoShopingCart(requestStream, context);
        }

        public override async Task<RemoveItemIntoShoppingCartResponse> RemoveItemIntoShoppingCart(RemoveItemIntoShoppingCartRequest request, ServerCallContext context)
        {
            var shoppingCart = await shoppingCartDBbContext.ShoppingCarts
                .FirstOrDefaultAsync(s => s.UserName == request.Username);
            if (shoppingCart == null)
            {
                logger.LogError($"Cannot find shoping cart with user name {request.Username}");
                throw new RpcException(new Status(StatusCode.NotFound, $"Shopping cart with username {request.Username} is not found"));
            }

            var removeCartItem = shoppingCart.Items
                .FirstOrDefault(i => i.ProductId == request.RemoveCartItem.ProductId);
            if (removeCartItem == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, $"Cart item with productId = {request.RemoveCartItem.ProductId} does not exist in this user name = {request.Username} shopping cart"));
            }

            shoppingCart.Items.Remove(removeCartItem);
            var removeCount = await shoppingCartDBbContext.SaveChangesAsync();

            var response = new RemoveItemIntoShoppingCartResponse
            {
                Success = removeCount > 0
            };

            return response;
        }
    }
}
