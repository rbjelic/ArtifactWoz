using System;
using System.Collections.Generic;

namespace Glove.Model
{
    public class Root
    {
        public List<Product> products { get; set; }
        public List<Attributes> attributes { get; set; }
    }


    public class Product
    {
        public string id { get; set; }
        public string name { get; set; }

        public string description { get; set; }
        public decimal price { get; set; }
        public bool available { get; set; }
    }

    public class Attributes { } // empty

    public class Params
    {
        public decimal discounted_price { get; set; }
    }

    public class ProductId
    {
        public string id { get; set; }
    }

    public class Promotion
    {
        public string promotion_id { get; set; }
        public List<ProductId> product_ids { get; set; }
        public bool enabled { get; set; }
        public string type { get; set; }
        public List<Params> parameters { get; set; }
    }


    public class Transaction
    {
        public string transaction_id { get; set; }
    }

    public class TransactionStatus
    {
        public string transaction_id { get; set; }
        public string status { get; set; }
        public DateTime last_updated_at { get; set; }
        public List<string> details { get; set; }
    }

    public class ShopId
    {
        public string shopid { get; set; }
    }
}