﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace UtilityLib.Workers.WorkerQueue
{
    public interface IWorkItemPusher
    {
        ValueTask PushAsync(WorkItem workItem, CancellationToken stoppingToken);
        ValueTask PushAsync(IEnumerable<WorkItem> workItems, CancellationToken stoppingToken);
    }

    public interface IWorkItemQueue : IWorkItemPusher
    {
        int Count { get; }

        ValueTask<WorkItem> PopAsync(CancellationToken stoppingToken);
    }

    public class WorkItemQueue : IWorkItemQueue
    {
        private readonly Channel<WorkItem> channel;

        public int Count => channel.Reader.Count;

        public WorkItemQueue(Channel<WorkItem> channel) => this.channel = channel;

        public ValueTask PushAsync(WorkItem workItem, CancellationToken stoppingToken) => channel.Writer.WriteAsync(workItem, stoppingToken);
        public async ValueTask PushAsync(IEnumerable<WorkItem> workItems, CancellationToken stoppingToken)
        {
            foreach (var workItem in workItems)
            {
                await PushAsync(workItem, stoppingToken);
            }
        }

        public ValueTask<WorkItem> PopAsync(CancellationToken stoppingToken) => channel.Reader.ReadAsync(stoppingToken);
    }
}
