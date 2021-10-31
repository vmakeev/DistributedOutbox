Configuration:

```
services.AddDistributedOutbox(
            options =>
            {
                options.ErrorDelay = TimeSpan.FromSeconds(1);
                options.NoEventsPublishedDelay = TimeSpan.FromSeconds(1);
            })
        .UsePostgresOutboxStorage<MyFavoriteDbContext>(
            options =>
            {
                options.Schema = "public";
                options.Table = "OutboxEvents";
                options.ParallelLimit = 100;
                options.SequentialLimit = 100;
            })
        .WithEventTargets("Event1", "some-kafka-topic")
        .WithEventTargets("AnotherEvent", new[] { "another-kafka-topic", "special-topic" })
        .WithEventTargets("Event1", new[] { "append-existing-event-to-another-topic", "this-is-legal" })
        .WithEventTargets(new[] { "Alarm1", "Alarm2", "Alarm3" }, "alarm-topic")
        .WithEventTargets(new[] { "Broadcast1", "Broadcast2" }, new[] { "topic1", "topic2" })
        .UseKafkaOutboxTarget(
            options =>
            {
                options.MessageEncoding = Encoding.UTF8;
                options.ProducerConfig = new ProducerConfig
                {
                    BootstrapServers = "my.kafka.server",
                    BatchNumMessages = 100,
                    LingerMs = 10
                };
            });
```

Usage (requires transaction when `_dbContext.SaveChanges()` method called):

```
public class Foo
{
    private readonly IOutbox _outbox;

    public Foo(IOutbox outbox)
    {
        _outbox = outbox;
    }

    public async Task Bar(CancellationToken cancellationToken)
    {
        // non-sequential event, parallel processing
        var event1 = new PostgresOutboxEventData(
            eventKey: "event1Key",
            eventType: "event1Type",
            payload: new SomeJsonSerializablePayloadObject()
        );
        
        // sequential event, serial processing per sequenceName
        var event2 = new PostgresOutboxEventData(
            eventKey: "event2Key",
            eventType: "event2Type",
            sequenceName: "event2SequenceName",
            payload: new SomeJsonSerializablePayloadObject()
        );

        // will be stored in memory only, waiting for _dbContext.SaveChanges() call
        await _outbox.AddEventsAsync(new[] { event1, event2 }, cancellationToken);
    }
}
```