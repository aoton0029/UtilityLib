﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UtilityLib.Tasks.AsyncTasks
{
    public sealed class CancellationTokenTaskSource<T> : IDisposable
    {
        /// <summary>
        /// The cancellation token registration, if any. This is <c>null</c> if the registration was not necessary.
        /// </summary>
        private readonly IDisposable? _registration;

        /// <summary>
        /// Creates a task for the specified cancellation token, registering with the token if necessary.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token to observe.</param>
        public CancellationTokenTaskSource(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                Task = System.Threading.Tasks.Task.FromCanceled<T>(cancellationToken);
                return;
            }
            var tcs = new TaskCompletionSource<T>();
            _registration = cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken), useSynchronizationContext: false);
            Task = tcs.Task;
        }

        /// <summary>
        /// Gets the task for the source cancellation token.
        /// </summary>
        public Task<T> Task { get; private set; }

        /// <summary>
        /// Disposes the cancellation token registration, if any. Note that this may cause <see cref="Task"/> to never complete.
        /// </summary>
        public void Dispose()
        {
            _registration?.Dispose();
        }
    }
}
