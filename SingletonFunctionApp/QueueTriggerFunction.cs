using System;
using System.Text.Json;
using Azure.Storage.Queues.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using SingletonFunctionApp.SingletonSample;

namespace SingletonFunctionApp;

public class QueueTriggerFunction(TimeProvider timeProvider)
{
    [Function(nameof(QueueTriggerFunction))]
    [TableOutput(Consts.TableName)]
    public async Task<InputData?> Run(
        [QueueTrigger("myqueue-items", Connection = "")] QueueMessage message,
        [DurableClient] DurableTaskClient durableTaskClient,
        CancellationToken cancellationToken)
    {
        var body = message.Body.ToString();
        if (string.IsNullOrWhiteSpace(body)) return null;

        var metadata = await durableTaskClient.GetInstanceAsync(SingletonProcessOrchestrator.InstanceId, cancellationToken);
        if (metadata == null || !metadata.IsRunning)
        {
            await durableTaskClient.ScheduleNewOrchestrationInstanceAsync(
                nameof(SingletonProcessOrchestrator), 
                0,
                new StartOrchestrationOptions(SingletonProcessOrchestrator.InstanceId),
                cancellationToken);
        }

        return new()
        {
            PartitionKey = Consts.InputDataPartitionKey,
            RowKey = Guid.NewGuid().ToString(),
            Timestamp = timeProvider.GetUtcNow(),
            Text = body,
        };
    }
}
