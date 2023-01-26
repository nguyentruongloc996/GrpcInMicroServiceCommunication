using DiscountGrpc.Data;
using DiscountGrpc.Protos;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;
using static DiscountGrpc.Protos.DiscountProtoService;

namespace DiscountGrpc.Services
{
    public class DiscountService : DiscountProtoService.DiscountProtoServiceBase
    {
        private readonly ILogger<DiscountService> logger;

        public DiscountService(ILogger<DiscountService> logger)
        {
            this.logger = logger;
        }

        public override Task<DiscountModel> GetDiscount(GetDiscountRequest request, ServerCallContext context)
        {
            var discount = DiscountContext.Discount.FirstOrDefault(d => d.Code == request.DiscountCode);

            logger.LogInformation($"Discount is operated with the {discount.Code} code and the amount is : {discount.Amount}");

            return Task.FromResult(new DiscountModel
            {
                DiscountId = discount.DiscountId,
                Code = discount.Code,
                Amount = discount.Amount,
            });
        }
    }
}
