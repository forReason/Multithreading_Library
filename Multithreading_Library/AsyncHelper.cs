// ReSharper disable AsyncVoidLambda
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
            var synchronisationContext = new ExclusiveSynchronizationContext();
            SynchronizationContext.SetSynchronizationContext(synchronisationContext);
            synchronisationContext.Post(async _ =>
            {
                try
                {
                    await task();  // Await the provided task
                }
                catch (Exception? e)
                {
                    synchronisationContext.InnerException = e;  // Store exception to be thrown later
                    throw;
                }
                finally
                {
                    synchronisationContext.EndMessageLoop();
                }
            }, null);
            synchronisationContext.BeginMessageLoop();  // Start the message loop

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
            var synchronisationContext = new ExclusiveSynchronizationContext();
            SynchronizationContext.SetSynchronizationContext(synchronisationContext);
            TResult result = default!;

            synchronisationContext.Post(async _ =>
            {
                try
                {
                    result = await func();  // Await the provided task
                }
                catch (Exception? e)
                {
                    synchronisationContext.InnerException = e;  // Store exception to be thrown later
                    throw;
                }
                finally
                {
                    synchronisationContext.EndMessageLoop();
                }
            }, null);
            synchronisationContext.BeginMessageLoop();  // Start the message loop

            SynchronizationContext.SetSynchronizationContext(oldContext);
            return result!;
        }


        /// <summary>
        /// Provides an exclusive synchronization context that executes posted callbacks in a single-threaded, serial fashion.
        /// This synchronization context does not support sending callbacks to the same thread it runs on and throws a <see cref="NotSupportedException"/> if attempted.
        /// </summary>
        private class ExclusiveSynchronizationContext : SynchronizationContext
        {
            private bool _done;
            /// <summary>
            /// Gets or sets any inner exception that might have occurred during the execution of a callback.
            /// </summary>
            public Exception? InnerException { get; set; }
            readonly AutoResetEvent _workItemsWaiting = new AutoResetEvent(false);
            readonly Queue<Tuple<SendOrPostCallback, object>> _items = new Queue<Tuple<SendOrPostCallback, object>>();

            /// <summary>
            /// Schedules a callback for execution. Does not support direct execution on the same thread.
            /// </summary>
            /// <param name="d">The delegate to execute.</param>
            /// <param name="state">The state to pass to the delegate.</param>
            /// <exception cref="NotSupportedException">Thrown when an attempt is made to send a callback to the same thread.</exception>
            public override void Send(SendOrPostCallback d, object? state)
            {
                throw new NotSupportedException("We cannot send to our same thread");
            }

            /// <summary>
            /// Posts a callback for execution. The callback is queued and executed serially in the message loop.
            /// </summary>
            /// <param name="d">The delegate to execute.</param>
            /// <param name="state">The state to pass to the delegate.</param>
            public override void Post(SendOrPostCallback d, object? state)
            {
                lock (_items)
                {
                    _items.Enqueue(Tuple.Create(d, state)!);
                }
                _workItemsWaiting.Set();
            }

            /// <summary>
            /// Signals the end of the message loop, allowing the <see cref="BeginMessageLoop"/> method to exit.
            /// </summary>
            public void EndMessageLoop()
            {
                Post(_ => _done = true, null);  // Signal the end of the message loop
            }

            /// <summary>
            /// Starts the message loop, executing queued callbacks serially until <see cref="EndMessageLoop"/> is called.
            /// </summary>
            public void BeginMessageLoop()
            {
                while (!_done)
                {
                    Tuple<SendOrPostCallback, object> task = null!;
                    lock (_items)
                    {
                        if (_items.Count > 0)
                        {
                            task = _items.Dequeue();  // Dequeue the next task
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
                        _workItemsWaiting.WaitOne();  // Wait for a new task to be posted
                    }
                }
            }

            /// <summary>
            /// Creates a copy of the current synchronization context.
            /// </summary>
            /// <returns>A reference to this instance, as this context does not support creating actual copies.</returns>
            public override SynchronizationContext CreateCopy()
            {
                return this;
            }
        }
    }
}
