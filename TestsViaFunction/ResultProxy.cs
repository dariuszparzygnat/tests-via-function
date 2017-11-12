using Microsoft.Azure.WebJobs;
using Xunit.Runners;

namespace TestsViaFunction
{
    public class ResultProxy
    {
        private readonly ICollector<TestResult> _collector;
        private readonly string _partitionKey;

        public ResultProxy(ICollector<TestResult> storageTable, TestTypeInfo testTypeInfo)
        {
            _collector = storageTable;
            _partitionKey = testTypeInfo.Guid.ToString();
        }

        public void OnTestSkipped(TestSkippedInfo testSkippedInfo)
        {
            var testResult = new TestResult()
            {
                PartitionKey = _partitionKey,
                RowKey = testSkippedInfo.TestDisplayName,
                SkippedReason = testSkippedInfo.SkipReason,
                Status = "Skipped"
            };
            _collector.Add(testResult);
        }

        public void OnTestPassed(TestPassedInfo testPassedInfo)
        {
            var testResult = new TestResult()
            {
                PartitionKey = _partitionKey,
                RowKey = testPassedInfo.TestDisplayName,
                ExecutionTime = (double)testPassedInfo.ExecutionTime,
                Status = "Passed"
            };
            _collector.Add(testResult);
        }

        public void OnTestFailed(TestFailedInfo testFailedInfo)
        {
            var testResult = new TestResult()
            {
                PartitionKey = _partitionKey,
                RowKey = testFailedInfo.TestDisplayName,
                ExecutionTime = (double)testFailedInfo.ExecutionTime,
                Status = "Failed",
                ExceptionType = testFailedInfo.ExceptionType,
                ExceptionMessage = testFailedInfo.ExceptionMessage,
                ExceptionStackTrace = testFailedInfo.ExceptionStackTrace
            };
            _collector.Add(testResult);
        }
    }
}
