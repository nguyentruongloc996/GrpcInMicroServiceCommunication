using AutoMapper;
using ShoppingCartGrpc.Protos;

namespace ShoppingCartGrpc.Mapper
{
    public class ShoppingCartProfile : Profile
    {
        protected ShoppingCartProfile()
        {
            CreateMap<Models.ShoppingCart, ShoppingCartModel>().ReverseMap();
            CreateMap<Models.ShoppingCartItem, ShoppingCartItemModel>().ReverseMap();
        }
    }
}
