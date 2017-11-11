using System;
using System.Configuration;
using System.IO;
using System.Reflection;
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
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
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

        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            string projectDir = System.Environment.GetEnvironmentVariable("DLL_Location", EnvironmentVariableTarget.Process);
            int commaIndex = args.Name.IndexOf(',');
            string shortAssemblyName = commaIndex == -1? args.Name : args.Name.Substring(0, commaIndex);
            string fileName = Path.Combine(projectDir, shortAssemblyName + ".dll");
            if (File.Exists(fileName))
            {
                Assembly result = Assembly.LoadFrom(fileName);
                return result;
            }
            else
                return Assembly.GetExecutingAssembly().FullName == args.Name ? Assembly.GetExecutingAssembly() : null;
        }
    }

}
