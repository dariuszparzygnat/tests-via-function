using Microsoft.WindowsAzure.Storage.Table;

namespace TestsViaFunction
{
    public class TestResult : TableEntity
    {
        public string Status { get; set; }
        public double ExecutionTime { get; set; }
        public string SkippedReason { get; set; }
        public string ExceptionMessage { get; set; }
        public string ExceptionStackTrace { get; set; }
        public string ExceptionType { get; set; }
    }
}
