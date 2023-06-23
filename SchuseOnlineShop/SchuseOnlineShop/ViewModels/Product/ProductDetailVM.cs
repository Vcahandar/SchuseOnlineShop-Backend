﻿using SchuseOnlineShop.Models;

namespace SchuseOnlineShop.ViewModels.Product
{
    public class ProductDetailVM
    {
        public int Id { get; set; }
        public string ProductName { get; set; }
        public decimal Price { get; set; }
        public string SKU { get; set; }
        public int Rating { get; set; }
        public string Description { get; set; }
        public string BrandName { get; set; }
        public ICollection<ProductComment> ProductComments { get; set; }
        public ICollection<CategorySubCategory> CategorySubCategories { get; set; }
        public ICollection<ProductColor> ProductColors { get; set; }
        public ICollection<ProductImage> ProductImages { get; set; }
        public ICollection<ProductVideo> ProductVideos { get; set; }
        public ICollection<ProductSize> ProductSizes { get; set; }
        public Dictionary<string, string> SectionBgs { get; set; }
        public ProductCommentVM ProductCommentVM { get; set; }
    }
}