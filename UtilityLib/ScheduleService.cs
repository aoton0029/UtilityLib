﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UtilityLib
{
    public class ScheduleService
    {
        private readonly object _lockObject = new object();
        private readonly Dictionary<string, TimerBasedTask> _taskDict = new Dictionary<string, TimerBasedTask>();

        public ScheduleService()
        {

        }

        public void StartTask(string name, Action action, int dueTime, int period)
        {
            lock (_lockObject)
            {
                if (_taskDict.ContainsKey(name)) return;
                var timer = new System.Threading.Timer(TaskCallback, name, Timeout.Infinite, Timeout.Infinite);
                _taskDict.Add(name, new TimerBasedTask { Name = name, Action = action, Timer = timer, DueTime = dueTime, Period = period, Stopped = false });
                timer.Change(dueTime, period);
            }
        }
        public void StopTask(string name)
        {
            lock (_lockObject)
            {
                if (_taskDict.ContainsKey(name))
                {
                    var task = _taskDict[name];
                    task.Stopped = true;
                    task.Timer.Dispose();
                    _taskDict.Remove(name);
                }
            }
        }

        private void TaskCallback(object obj)
        {
            var taskName = (string)obj;
            TimerBasedTask task;

            if (_taskDict.TryGetValue(taskName, out task))
            {
                try
                {
                    if (!task.Stopped)
                    {
                        task.Timer.Change(Timeout.Infinite, Timeout.Infinite);
                        task.Action();
                    }
                }
                catch (ObjectDisposedException) { }
                catch (Exception ex)
                {
                    //if (_logger != null)
                    //{
                    //    _logger.Error(string.Format("Task has exception, name: {0}, due: {1}, period: {2}", task.Name, task.DueTime, task.Period), ex);
                    //}
                }
                finally
                {
                    try
                    {
                        if (!task.Stopped)
                        {
                            task.Timer.Change(task.Period, task.Period);
                        }
                    }
                    catch (ObjectDisposedException) { }
                    catch (Exception ex)
                    {
                        //if (_logger != null)
                        //{
                        //    _logger.Error(string.Format("Timer change has exception, name: {0}, due: {1}, period: {2}", task.Name, task.DueTime, task.Period), ex);
                        //}
                    }
                }
            }
        }

        class TimerBasedTask
        {
            public string Name;
            public Action Action;
            public System.Threading.Timer Timer;
            public int DueTime;
            public int Period;
            public bool Stopped;
        }


    }
}
