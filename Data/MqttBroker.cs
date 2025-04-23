using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MQTTnet;
namespace TarkKoduKK.Data
{
    public static class MqttBroker
    {
        public const string Broker = "b2255a4c0cd74f57a5ca3f557f9867a6.s1.eu.hivemq.cloud";
        public const int Port = 8883;
        public const string TopicDrow = "Matrix/drow";
        public const string TopicCommands = "Matrix/commands";  
        public const string Username = "test1";
        public const string Password = "Test12345678";
    }
}
