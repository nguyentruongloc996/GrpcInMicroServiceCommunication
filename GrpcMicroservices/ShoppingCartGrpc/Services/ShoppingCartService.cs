using AutoMapper;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ShoppingCartGrpc.Data;
using ShoppingCartGrpc.Models;
using ShoppingCartGrpc.Protos;
using System.Linq;
using System.Threading.Tasks;

namespace ShoppingCartGrpc.Services
{
    [Authorize]
    public class ShoppingCartService : ShoppingCartProtoService.ShoppingCartProtoServiceBase
    {
        private readonly ShoppingCartContext shoppingCartDBbContext;
        private readonly IMapper mapper;
        private readonly ILogger<ShoppingCartService> logger;
        private readonly DiscountService discountService;

        public ShoppingCartService(ShoppingCartContext shoppingCartDBbContext, 
            IMapper mapper, 
            ILogger<ShoppingCartService> logger, 
            DiscountService discountService)
        {
            this.shoppingCartDBbContext = shoppingCartDBbContext;
            this.mapper = mapper;
            this.logger = logger;
            this.discountService = discountService;
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

        [AllowAnonymous]
        public override async Task<AddItemIntoShopingCartResponse> AddItemIntoShopingCart(IAsyncStreamReader<AddItemIntoShopingCartRequest> requestStream, ServerCallContext context)
        {
            // Get sc if exist or not
            while (await requestStream.MoveNext())
            {
                var shoppingCart = await shoppingCartDBbContext.ShoppingCarts.
                    FirstOrDefaultAsync(s => s.UserName == requestStream.Current.Username);
                if (shoppingCart == null)
                {
                    throw new RpcException(new Status(StatusCode.NotFound,
                        "Shopping Cart with Username={request.Username} does not exist"));
                }
                
                // CHeck the item if exist in sc or not

                var newAddedCartItem = mapper.Map<ShoppingCartItem>(requestStream.Current.NewCartItem);
                var cartItem = shoppingCart.Items.FirstOrDefault(i => i.ProductId == newAddedCartItem.ProductId);
                if (cartItem != null)
                {
                    cartItem.Quantity++;
                }
                else
                {
                    // grpc call discount service -- check discount and calculate the imtem last price
                    var discount = await discountService.GetDiscount(requestStream.Current.DiscountCode);
                    newAddedCartItem.Price -= discount.Amount;

                    shoppingCart.Items.Add(newAddedCartItem);
                }

            }

            var insertCount = await shoppingCartDBbContext.SaveChangesAsync();

            var response = new AddItemIntoShopingCartResponse
            {
                Success = insertCount > 0,
                InsertCount = insertCount,
            };
            return response;
        }

        [AllowAnonymous]
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
