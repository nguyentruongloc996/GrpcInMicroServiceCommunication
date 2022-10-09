using ProductGrpc.Models;
using System.Collections.Generic;
using System.Linq;

namespace ProductGrpc.Data
{
    public class ProductContextSeed
    {
        // check if the database have any data then
        // add some if not.
        public static void SeedAsync(ProductsContext productsContext)
        {
            if(!productsContext.Product.Any())
            {
                var products = new List<Product> {
                    new Product
                    {
                        ProductId = 1,
                        Name = "Mi107",
                        Description = "New Xiaomi phone",
                        Price = 699,
                        Status = ProductStatus.INSTOCK,
                        CreateTime = System.DateTime.UtcNow
                    },
                    new Product
                    {
                        ProductId = 2,
                        Name = "Sam21",
                        Description = "New Samsung phone",
                        Price = 400,
                        Status = ProductStatus.INSTOCK,
                        CreateTime = System.DateTime.UtcNow
                    },
                    new Product 
                    {
                        ProductId = 3,
                        Name = "Iphone13",
                        Description = "New Apple phone",
                        Price = 1000,
                        Status = ProductStatus.INSTOCK,
                        CreateTime = System.DateTime.UtcNow
                    }
                };

                productsContext.Product.AddRange(products);
                productsContext.SaveChanges();
            }
        }
    }
}
