using System;
using System.Collections.Generic;
using System.Text;

namespace Common.Helper
{
    public class MyConfig
    {
        public string BucketName { get; set; }
        public string Path { get; set; }
        public string ServerStoragePath { get; set; }
        public string RabbitMQAddress { get; set; }
        public string RabbitMQQueueName { get; set; }
    }
}
