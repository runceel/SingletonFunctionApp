using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace SingletonFunctionApp.SingletonSample;
public class SingletonActivities(ILogger<SingletonActivities> logger)
{
    [Function(nameof(GetDataAsync))]
    public async Task<InputData[]?> GetDataAsync([ActivityTrigger] string? dummyInput,
        [TableInput(Consts.TableName)] TableClient tableClient,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("{functionName} was triggered.", nameof(GetDataAsync));
        var result = tableClient.QueryAsync<InputData>(x => x.PartitionKey == Consts.InputDataPartitionKey,
            maxPerPage: 3,
            cancellationToken: cancellationToken);
        var page = await result.AsPages().FirstOrDefaultAsync(cancellationToken);
        return page == null ? null : page.Values.ToArray();
    }

    [Function(nameof(ExecuteProcessAsync))]
    public async Task ExecuteProcessAsync([ActivityTrigger] InputData targetData,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("ExecuteProcessAsync was triggered with {targetData}.", JsonSerializer.Serialize(targetData));
        await Task.Delay(1000, cancellationToken);
        logger.LogInformation("ExecuteProcessAsync was finished with {targetData}.", JsonSerializer.Serialize(targetData));
    }

    [Function(nameof(DeleteDataAsync))]
    public async Task<bool> DeleteDataAsync([ActivityTrigger] InputData targetData,
        [TableInput(Consts.TableName)] TableClient tableClient,
        CancellationToken cancellationToken)
    {
        var response = await tableClient.DeleteEntityAsync(
            targetData.PartitionKey, 
            targetData.RowKey, 
            cancellationToken: cancellationToken);
        return !response.IsError;
    }
}
