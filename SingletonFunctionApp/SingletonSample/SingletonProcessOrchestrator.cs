using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;

namespace SingletonFunctionApp.SingletonSample;
public class SingletonProcessOrchestrator
{
    public static readonly string InstanceId = nameof(SingletonProcessOrchestrator);

    private static readonly TaskRetryOptions DefaultRetryOptions = new(new RetryPolicy(
        10,
        TimeSpan.FromSeconds(1),
        1,
        TimeSpan.FromSeconds(10)));

    private const int MaxRetryCountForOrchestrator = 3;

    [Function(nameof(SingletonProcessOrchestrator))]
    public async Task RunAsync([OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var logger = context.CreateReplaySafeLogger<SingletonProcessOrchestrator>();
        var retryCount = context.GetInput<int>();
        logger.LogInformation("SingletonProcessOrchestrator was triggered with {retryCount}.", retryCount);
        if (context.InstanceId != InstanceId)
        {
            throw new InvalidOperationException($"The instance ID '{context.InstanceId}' is invalid.");
        }

        var targetDataList = await context.CallActivityAsync<InputData[]?>(
            nameof(SingletonActivities.GetDataAsync),
            new TaskOptions(DefaultRetryOptions));
        if (targetDataList == null || targetDataList.Length == 0)
        {
            logger.LogInformation("No data to process.");
            if (retryCount > MaxRetryCountForOrchestrator)
            {
                logger.LogInformation("SingletonProcessOrchestrator was finished.");
                return;
            }

            retryCount++;
        }
        else
        {
            retryCount = 0;
            foreach (var targetData in targetDataList)
            {
                await context.CallActivityAsync(
                    nameof(SingletonActivities.ExecuteProcessAsync),
                    targetData,
                    new TaskOptions(DefaultRetryOptions));
                var isSuccess = await context.CallActivityAsync<bool>(
                    nameof(SingletonActivities.DeleteDataAsync),
                    targetData,
                    new TaskOptions(DefaultRetryOptions));
                if (!isSuccess)
                {
                    logger.LogWarning("Something went wrong while deleting the data. {targetData}.", targetData);
                }
            }
        }

        await context.CreateTimer(TimeSpan.FromSeconds(3), CancellationToken.None);
        context.ContinueAsNew(retryCount);
    }
}
