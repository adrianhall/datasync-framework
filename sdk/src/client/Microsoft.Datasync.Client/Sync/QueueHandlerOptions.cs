﻿namespace Microsoft.Datasync.Client.Sync;

internal class QueueHandlerOptions
{
    public static QueueHandlerOptions DefaultOptions { get; } = new QueueHandlerOptions();

    public bool AddChangesToQueue { get; set; } = true;
}
