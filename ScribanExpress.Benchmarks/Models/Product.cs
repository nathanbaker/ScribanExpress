﻿using System;
using System.Collections.Generic;
using System.Text;

namespace ScribanExpress.Benchmarks.Models
{
    public class Product
    {
        public Product(string name, float price, string description)
        {
            Name = name;
            Price = price;
            Description = description;
        }

        public string Name { get; set; }
        public float Price { get; set; }
        public string Description { get; set; }

    }
}
