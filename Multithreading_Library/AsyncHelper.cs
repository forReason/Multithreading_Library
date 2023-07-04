using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Multithreading_Library
{
    /// <summary>
    /// Provides helper methods to run asynchronous tasks synchronously.
    /// </summary>
    public static class AsyncHelper
    {
        /// <summary>
        /// Runs the provided asynchronous task synchronously on a new thread to avoid deadlocks.
        /// </summary>
        /// <param name="task">The asynchronous task to run.</param>
        /// <exception cref="AggregateException">Throws if the asynchronous task encounters an exception.</exception>
        public static void RunSync(Func<Task> task)
        {
            var oldContext = SynchronizationContext.Current;
            var synch = new ExclusiveSynchronizationContext();
            SynchronizationContext.SetSynchronizationContext(synch);
            synch.Post(async _ =>
            {
                try
                {
                    await task();  // Await the provided task
                }
                catch (Exception e)
                {
                    synch.InnerException = e;  // Store exception to be thrown later
                    throw;
                }
                finally
                {
                    synch.EndMessageLoop();
                }
            }, null);
            synch.BeginMessageLoop();  // Start the message loop

            SynchronizationContext.SetSynchronizationContext(oldContext);
        }
        /// <summary>
        /// Runs the provided asynchronous task synchronously on a new thread to avoid deadlocks.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="func">The asynchronous task to run.</param>
        /// <returns>The result of the Task.</returns>
        /// <exception cref="AggregateException">Throws if the asynchronous task encounters an exception.</exception>
        public static TResult RunSync<TResult>(Func<Task<TResult>> func)
        {
            var oldContext = SynchronizationContext.Current;
            var synch = new ExclusiveSynchronizationContext();
            SynchronizationContext.SetSynchronizationContext(synch);
            TResult result = default;

            synch.Post(async _ =>
            {
                try
                {
                    result = await func();  // Await the provided task
                }
                catch (Exception e)
                {
                    synch.InnerException = e;  // Store exception to be thrown later
                    throw;
                }
                finally
                {
                    synch.EndMessageLoop();
                }
            }, null);
            synch.BeginMessageLoop();  // Start the message loop

            SynchronizationContext.SetSynchronizationContext(oldContext);
            return result;
        }


        private class ExclusiveSynchronizationContext : SynchronizationContext
        {
            private bool done;
            public Exception InnerException { get; set; }
            readonly AutoResetEvent workItemsWaiting = new AutoResetEvent(false);
            readonly Queue<Tuple<SendOrPostCallback, object>> items = new Queue<Tuple<SendOrPostCallback, object>>();

            public override void Send(SendOrPostCallback d, object state)
            {
                throw new NotSupportedException("We cannot send to our same thread");
            }

            public override void Post(SendOrPostCallback d, object state)
            {
                lock (items)
                {
                    items.Enqueue(Tuple.Create(d, state));
                }
                workItemsWaiting.Set();
            }

            public void EndMessageLoop()
            {
                Post(_ => done = true, null);  // Signal the end of the message loop
            }

            public void BeginMessageLoop()
            {
                while (!done)
                {
                    Tuple<SendOrPostCallback, object> task = null;
                    lock (items)
                    {
                        if (items.Count > 0)
                        {
                            task = items.Dequeue();  // Dequeue the next task
                        }
                    }
                    if (task != null)
                    {
                        task.Item1(task.Item2);  // Execute the task

                        // If an exception occurred in the task, throw an AggregateException
                        if (InnerException != null)
                        {
                            throw new AggregateException("AsyncHelper.Run method threw an exception.", InnerException);
                        }
                    }
                    else
                    {
                        workItemsWaiting.WaitOne();  // Wait for a new task to be posted
                    }
                }
            }

            public override SynchronizationContext CreateCopy()
            {
                return this;
            }
        }
    }

}
