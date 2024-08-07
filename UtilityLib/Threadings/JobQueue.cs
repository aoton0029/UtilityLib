﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UtilityLib.Threadings
{
    public class JobQueue(CancellationToken cancellationToken = default)
    {
        private readonly object _lock = new();
        private readonly Queue<Action> _jobs = [];
        private bool _threadRunning;

        /// <summary>
        /// Adds a job to the work queue.
        /// </summary>
        public void Enqueue(Action job)
        {
            lock (_lock)
            {
                _jobs.Enqueue(job);

                if (!_threadRunning)
                {
                    _threadRunning = true;
                    ThreadUtils.StartBackground(Work, name: nameof(JobQueue));
                }
            }
        }

        private void Work()
        {
            while (true)
            {
                Action job;
                lock (_lock)
                {
                    if (_jobs.Count == 0)
                    {
                        _threadRunning = false;
                        return;
                    }

                    job = _jobs.Dequeue();
                }

                if (cancellationToken.IsCancellationRequested) return;
                job();
            }
        }
    }
}
