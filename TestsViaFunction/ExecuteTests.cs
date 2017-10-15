using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using System.Threading;
using Xunit.Runners;

namespace TestsViaFunction
{
    public static class ExecuteTests
    {
        [FunctionName("ExecuteTests")]
        public static void Run([QueueTrigger("myqueue-items", Connection = "")]TestTypeInfo myQueueItem, ICollector<TestResult> outputTable, TraceWriter log)
        {
            ManualResetEvent executionManualResetEvent = new ManualResetEvent(false);
            var resultProxy = new ResultProxy(outputTable, myQueueItem);
            var runner = AssemblyRunner.WithAppDomain(myQueueItem.DllPath);
            runner.OnTestFailed += resultProxy.OnTestFailed;
            runner.OnTestPassed += resultProxy.OnTestPassed;
            runner.OnTestSkipped += resultProxy.OnTestSkipped;
            runner.OnExecutionComplete = info => { executionManualResetEvent.Set(); };
            runner.OnErrorMessage = info => { executionManualResetEvent.Set(); };
            runner.Start(myQueueItem.TypeName);

            executionManualResetEvent.WaitOne();
            executionManualResetEvent.Dispose();
            while (runner.Status != AssemblyRunnerStatus.Idle) // because of https://github.com/xunit/xunit/issues/1347
            {
                Thread.Sleep(10);
            }
            runner.Dispose();
        }
    }

}