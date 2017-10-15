using System.IO;
using Xunit.Runners;
using System.Threading;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;

namespace TestsViaFunction
{
    public static class ExecuteTests
    {
        [FunctionName("ExecuteTests")]
        public static void Run([QueueTrigger("queue-with-tests", Connection = "storage_Conn")]TestTypeInfo myQueueItem, [Table(tableName: "TestResults", Connection = "storage_Conn")] ICollector<TestResult> outputTable, TraceWriter log)
        {
            ManualResetEvent executionManualResetEvent = new ManualResetEvent(false);
            var resultProxy = new ResultProxy(outputTable, myQueueItem);
            var fileExists = File.Exists(myQueueItem.DllPath);
            log.Info(fileExists.ToString());
            log.Info(myQueueItem.DllPath);
            var runner = AssemblyRunner.WithAppDomain(myQueueItem.DllPath);
            log.Info("Runner created");
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
