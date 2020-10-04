using System;
using Microsoft.Extensions.Configuration;

namespace Hrimsoft.SqlRunner
{
    public class DatabaseConfiguration
    {
        public string Name           { get; set; }
        public string ConnectionString { get; set; }
        public string Password         { get; set; }
    }
}