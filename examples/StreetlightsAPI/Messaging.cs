using System;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Saunter.Attributes;

namespace StreetlightsAPI
{
    public class LightMeasuredEvent
    {
        /// <summary>
        /// Id of the streetlight.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Light intensity measured in lumens.
        /// </summary>
        public int Lumens { get; set; }

        /// <summary>
        /// Light intensity measured in lumens.
        /// </summary>
        public DateTime SentAt { get; set; }
    }
    
    public interface IStreetlightMessageBus
    {
        void PublishLightMeasuredEvent(Streetlight streetlight, int lumens);
    }


    [AsyncApi]
    public class StreetlightMessageBus : IStreetlightMessageBus
    {
        private const string LightMeasuredTopic = "light/measured";

        private readonly ILogger _logger;

        public StreetlightMessageBus(ILoggerFactory logger)
        {
            _logger = logger.CreateLogger("Streetlight");
        }
        
        [Channel(LightMeasuredTopic)]
        [PublishOperation(typeof(LightMeasuredEvent), Summary = "Inform about environmental lighting conditions for a particular streetlight.")]
        public void PublishLightMeasuredEvent(Streetlight streetlight, int lumens)
        {
            var lightMeasuredEvent = new LightMeasuredEvent
            {
                Id = streetlight.Id,
                Lumens = lumens,
                SentAt = DateTime.Now,
            };
            var payload = JsonConvert.SerializeObject(lightMeasuredEvent);

            // Simulate publishing a message to the channel.
            // In reality this would call some kind of pub/sub client library and publish.
            // e.g. mqttClient.PublishAsync(message);
            // e.g. amqpClient.BasicPublish(LightMeasuredTopic, routingKey, props, payloadBytes);
            _logger.LogInformation("Publishing message {Payload} to {Topic}", payload, LightMeasuredTopic);
        }
    }
}