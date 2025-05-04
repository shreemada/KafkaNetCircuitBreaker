using Confluent.Kafka;
using Polly;
using Polly.CircuitBreaker;

public class KafkaConsumerService
{
    private readonly IConsumer<string, string> _consumer;
    private readonly IProducer<string, string> _producer;
    private readonly ResiliencePipeline _circuitBreaker;
    private readonly string _targetTopic;

    public KafkaConsumerService(string bootstrapServers,
        string sourceTopic,
        string targetTopic,
        string consumerGroup,
        ResiliencePipeline circuitBreaker)
    {
        _targetTopic = targetTopic;
        _circuitBreaker = circuitBreaker;

        // Configure Consumer
        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = bootstrapServers,
            GroupId = consumerGroup,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };
        _consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
        _consumer.Subscribe(sourceTopic);

        // Configure Producer
        var producerConfig = new ProducerConfig { BootstrapServers = bootstrapServers };
        _producer = new ProducerBuilder<string, string>(producerConfig).Build();
    }
    public async Task RunAsync()
    {
        while (true)
        {
            try
            {
                var consumeResult = _consumer.Consume(TimeSpan.FromSeconds(1));
                if (consumeResult == null) continue;

                await _circuitBreaker.ExecuteAsync(async token =>
                {
                    // Forward message to target topic
                    await _producer.ProduceAsync(_targetTopic, new Message<string, string>
                    {
                        Value = consumeResult.Message.Value
                    }, token);
                    _consumer.Commit(consumeResult); // Commit offset on success
                    Console.WriteLine($"Forwarded: {consumeResult.Message.Value}");
                }, CancellationToken.None);
            }
            catch (BrokenCircuitException)
            {
                Console.WriteLine("Circuit OPEN: Messages temporarily blocked.");
                _consumer.Pause(_consumer.Assignment);
                await Task.Delay(5000);
            }
            catch (ConsumeException ex)
            {
                Console.WriteLine($"Consume Error: {ex.Error.Reason}");
            }
            catch (ProduceException<string, string> ex)
            {
                Console.WriteLine($"Produce Error: {ex.Error.Reason}");
            }
        }
    }
}