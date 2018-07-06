using System;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using Polly;

namespace consoleDemo
{
    class Program
    {
        #region Globle Variables
        static HttpClient client = new HttpClient();
        static Policy _circuitBreakerPolicy;
        static Policy _retryPolicy;
        readonly static int exceptionThreshold = 3;//set value here
        readonly static int durationOfBreak = 10;//set value here
        readonly static int retryTimes = 20;//set value here
        static string baseAddress = "https://localhost:5001/";//Set Uri refer to webapi project
        static string path="api/values";//set value here
        #endregion
        static void Main(string[] args)
        {
            InitClient();
            InitPolicy();
            Call_CircuitBreakerPolicy();// enable this line if test CircuitBreaker policy
            //Call_RetryPolicy();//enable this line if test Retry policy
        }

        static void Call_RetryPolicy()
        {
            try
            {
                Console.WriteLine("[Retry Policy] Calling Webapi:");
                _retryPolicy.Execute(() =>
                {
                    Console.WriteLine(GetResponse(path).Result);
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error occured in application : " + ex.Message);
            }
        }
        static void Call_CircuitBreakerPolicy()
        {
            int count = 1;
            while (true)
            {
                try
                {
                    Console.WriteLine("[Circuit Breaker Policy] This is " + count.ToString() + " time calling Webapi:");
                    _circuitBreakerPolicy.Execute(() =>
                    {
                        Console.WriteLine(GetResponse("api/values").Result);
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error occured in application : " + ex.Message);
                }
                Task.Delay(2000).Wait();
                count++;
            }
        }

        #region Initial HttpClient,Policy.
        static void InitPolicy()
        {
            #region Init CircuitBreaker Policy
            _circuitBreakerPolicy = Policy.Handle<AggregateException>(x =>
            {
                var result = x.InnerException is HttpRequestException;
                return result;
            })
            .CircuitBreaker(exceptionThreshold, TimeSpan.FromSeconds(durationOfBreak));
            #endregion

            #region Init Retry Policy
            _retryPolicy = Policy.Handle<AggregateException>(x =>
            {
                var result = x.InnerException is HttpRequestException;
                return result;
            }).Retry(retryTimes, (ex, count) => { Console.WriteLine("[Retry Policy] Retrying"); });
            #endregion
        }
        static async Task<string> GetResponse(string path)
        {
            string values = null; ;
            HttpResponseMessage response = await client.GetAsync(path);
            if (response.IsSuccessStatusCode)
            {
                values = await response.Content.ReadAsStringAsync();
            }
            return values;
        }
        static void InitClient()
        {
            ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
            client.BaseAddress = new Uri(baseAddress);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }
        #endregion
    }
}
