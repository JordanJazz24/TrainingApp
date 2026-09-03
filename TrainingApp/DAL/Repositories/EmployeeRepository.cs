using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.Caching;
using Newtonsoft.Json;
using TrainingApp.Domain;
using TrainingApp.Shared;

namespace TrainingApp.DAL.Repositories
{
    // Fetches employee list from the external company API.
    // Hot path: served from MemoryCache (TTL = Constants.EmployeeCacheMinutes).
    // Resilience: if the API is unreachable after the TTL expires, the last
    // successfully fetched list is served indefinitely until the API recovers.
    // Only fails hard on the very first call if the API was never reachable.
    public class EmployeeRepository
    {
        private const string CacheKey = "EmployeeList";
        private static readonly HttpClient _http = new HttpClient();

        // Survives cache expiry — holds the last successful fetch for the process lifetime.
        private static volatile IList<Employee> _lastKnownGood;

        public Employee GetByCode1(string code1)
        {
            return GetAll().FirstOrDefault(e =>
                string.Equals(e.Code1, code1, StringComparison.OrdinalIgnoreCase));
        }

        private IList<Employee> GetAll()
        {
            var cache = MemoryCache.Default;
            if (cache[CacheKey] is IList<Employee> cached)
                return cached;

            try
            {
                var employees = FetchFromApi();
                _lastKnownGood = employees;
                cache.Set(CacheKey, employees, new CacheItemPolicy
                {
                    AbsoluteExpiration = DateTimeOffset.UtcNow.AddMinutes(Constants.EmployeeCacheMinutes)
                });
                return employees;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceWarning(
                    "[EmployeeRepository] API unavailable — serving stale employee list. Error: " + ex.Message);

                if (_lastKnownGood != null)
                    return _lastKnownGood;

                throw new InvalidOperationException(
                    "El servicio de validación de empleados no está disponible y no existe una lista en caché.", ex);
            }
        }

        private static IList<Employee> FetchFromApi()
        {
            // ConfigureAwait(false) prevents capturing the ASP.NET synchronization context,
            // avoiding the classic Web Forms deadlock when blocking on async via GetResult().
            string json = _http.GetStringAsync(Constants.EmployeeApiUrl)
                              .ConfigureAwait(false)
                              .GetAwaiter()
                              .GetResult();
            var dtos = JsonConvert.DeserializeObject<List<EmployeeDto>>(json);
            return dtos.Select(d => new Employee
            {
                Code1     = d.Code1,
                Nombre    = d.Nombre,
                Apellidos = d.Apellidos
            }).ToList();
        }

        private class EmployeeDto
        {
            [JsonProperty("Code1")]
            public string Code1 { get; set; }
            [JsonProperty("Name")]
            public string Nombre { get; set; }
            [JsonProperty("LastName")]
            public string Apellidos { get; set; }
        }
    }
}
