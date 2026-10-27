using System.Collections.Concurrent;
using System.Management.Automation;
using ProductivityTools.PSCmdlet;
using ProductivityTools.TechBricks.Data;
using ProductivityTools.TechBricks.Data.Auth;

namespace ProductivityTools.TechBricks.Cmdlet
{
    /// <summary>
    /// Base class of TechBricks cmdlets. PowerShell cmdlets cannot be async, so
    /// async work is executed here synchronously while messages produced on
    /// background threads are written to the host from the pipeline thread
    /// (Write* methods may be called only from that thread).
    /// </summary>
    public abstract class TechBricksCmdletBase : PSCmdletPT
    {
        private readonly CancellationTokenSource cancellation = new();
        private readonly ConcurrentQueue<string> messages = new();

        private AuthService? authService;

        protected AuthService AuthService => authService ??= new AuthService(message => messages.Enqueue(message));

        protected TechBricksClient CreateClient() => new TechBricksClient(AuthService);

        protected T Run<T>(Func<CancellationToken, Task<T>> action)
        {
            Task<T> task = Task.Run(() => action(cancellation.Token));
            // WaitOne instead of task.Wait, which would wrap errors in AggregateException.
            while (!((IAsyncResult)task).AsyncWaitHandle.WaitOne(200))
            {
                FlushMessages();
            }
            FlushMessages();
            return task.GetAwaiter().GetResult();
        }

        protected void Run(Func<CancellationToken, Task> action)
        {
            Run<bool>(async token => { await action(token); return true; });
        }

        protected override void ProcessRecord()
        {
            try
            {
                Execute();
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
            {
                // Ctrl+C - PowerShell stops the pipeline on its own.
            }
            catch (Exception ex) when (ex is not PipelineStoppedException)
            {
                FlushMessages();
                ThrowTerminatingError(new ErrorRecord(ex, "TechBricksError", ErrorCategory.NotSpecified, null));
            }
        }

        protected abstract void Execute();

        protected override void StopProcessing()
        {
            cancellation.Cancel();
            base.StopProcessing();
        }

        private void FlushMessages()
        {
            while (messages.TryDequeue(out var message))
            {
                WriteInformation(new HostInformationMessage { Message = message }, new[] { "PSHOST" });
            }
        }
    }
}
