﻿using Azure;
using Azure.Data.Tables;

namespace SingletonFunctionApp;
public class InputData : ITableEntity
{
    public string Text { get; set; } = "";
    public string PartitionKey { get; set; } = "";
    public string RowKey { get; set; } = "";
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
}
