using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using System.IO;
using System.Reflection;


namespace TestsViaFunction
{
    public static class RetrieveTypes
    {
        [FunctionName("RetrieveTypes")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequestMessage req, [Queue("queue-with-tests", Connection = "storage_Conn")] ICollector<TestTypeInfo> outputQueueItem, TraceWriter log)
        {
            string fileName = GetHeader(req, "fileName");
            Guid executiondIdentifier = Guid.NewGuid();
            var dllLocation = ConfigurationManager.AppSettings["dllLocation"];
            var testAssemblyPath = Path.Combine(dllLocation, fileName);
            var loadedAssembly = Assembly.LoadFile(testAssemblyPath);
            var testTypes = GetLoadableTypes(loadedAssembly).Where(type => type.IsClass && type.Name.Contains("Test")).Select(e => new TestTypeInfo() { DllPath = testAssemblyPath, TypeName = e.FullName, Guid = executiondIdentifier }).ToList();

            if (!testTypes.Any())
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "Assembly has no test types");
            }
            testTypes.ForEach(e => outputQueueItem.Add(e));
            return req.CreateResponse(HttpStatusCode.OK, testTypes);
        }

        public static string GetHeader(HttpRequestMessage request, string key)
        {
            IEnumerable<string> keys = null;
            if (!request.Headers.TryGetValues(key, out keys))
                return null;

            return keys.First();
        }

        public static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                return e.Types.Where(t => t != null);
            }
        }
    }
}