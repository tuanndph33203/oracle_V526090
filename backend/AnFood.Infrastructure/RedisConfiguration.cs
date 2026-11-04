namespace AnFood.API.Configuration
{
    public class RedisConfiguration
    {
        public bool Enabled { get; set; } = true;
        public string ConnectionString { get; set; } = "localhost";
        public string InstanceName { get; set; } = "UtcCoffee";
    }
}
