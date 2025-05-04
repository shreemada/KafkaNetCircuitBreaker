using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Polly;
using System.Net.Sockets;

var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

var kafkaConfig = config.GetSection("Kafka");
var circuitBreakerConfig = config.GetSection("CircuitBreaker");

var circuitBreaker = new ResiliencePipelineBuilder()
    .AddCircuitBreaker(new Polly.CircuitBreaker.CircuitBreakerStrategyOptions
    {
        FailureRatio = 0.9,
        SamplingDuration = TimeSpan.FromSeconds(10),
        MinimumThroughput = 3,
        BreakDuration = TimeSpan.FromSeconds(circuitBreakerConfig.GetValue<int>("BreakDurationSeconds")),
        ShouldHandle = args => ValueTask.FromResult(args.Outcome.Exception is KafkaException or SocketException),
        OnOpened = args =>
        {
            Console.WriteLine($"Circuit OPEN: {args.Outcome.Exception!.Message}. Retry in {args.BreakDuration.TotalSeconds}s.");
            return default;
        },
        OnClosed = _ =>
        {
            Console.WriteLine("Circuit CLOSED.");
            return default;
        },
        OnHalfOpened = _ =>
        {
            Console.WriteLine("Circuit HALF-OPEN.");
            return default;
        }
    })
    .Build();

var consumerService = new KafkaConsumerService(
    kafkaConfig.GetValue<string>("BootstrapServers"),
    kafkaConfig.GetValue<string>("SourceTopic"),
    kafkaConfig.GetValue<string>("TargetTopic"),
    kafkaConfig.GetValue<string>("ConsumerGroup"),
    circuitBreaker
);

await consumerService.RunAsync();