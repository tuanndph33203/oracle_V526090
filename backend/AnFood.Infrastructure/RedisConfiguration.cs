namespace AnFood.API.Configuration
{
    public class RedisConfiguration
    {
        public bool Enabled { get; set; } = true;
        public string ConnectionString { get; set; } = "10.8.101.221";
        public string InstanceName { get; set; } = "AnPos";
    }
}
