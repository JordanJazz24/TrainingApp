using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TrainingApp.Domain;
using TrainingApp.Shared;

namespace TrainingApp.DAL.Repositories
{
    // Lista de empleados de la API de la empresa, en memoria del proceso. Se descarga al
    // arrancar la aplicación (WarmUp) y, cuando vence (Constants.EmployeeCacheMinutes), se
    // refresca en segundo plano: un login nunca espera a la API salvo que todavía no exista
    // ninguna lista. Si la API falla al refrescar, se sigue sirviendo la última lista y se
    // reintenta a los Constants.EmployeeApiRetryMinutes. Solo falla si nunca se pudo descargar.
    public class EmployeeRepository
    {
        private static readonly HttpClient _http = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(Constants.EmployeeApiTimeoutSeconds)
        };

        // Última lista descargada; null hasta la primera descarga exitosa.
        private static volatile EmployeeList _current;

        // La primera descarga es una sola aunque lleguen varios logins a la vez.
        private static readonly object _firstDownloadLock = new object();

        // Primeras descargas fallidas. Quien esperaba el lock mientras una fallaba no la repite
        // (con la API caída, los logins en cola fallan juntos en vez de 10 s cada uno).
        private static volatile int _firstDownloadFailures;

        // 1 mientras hay un refresco en segundo plano en curso.
        private static int _refreshing;

        public Employee GetByCode1(string code1)
        {
            return GetAll().FirstOrDefault(e =>
                string.Equals(e.Code1, code1, StringComparison.OrdinalIgnoreCase));
        }

        // Precarga al arrancar la aplicación, en segundo plano: el primer login ya encuentra la
        // lista. Si falla, el primer login vuelve a intentar la descarga.
        public void WarmUp()
        {
            Task.Run(() =>
            {
                try
                {
                    GetAll();
                }
                catch (Exception ex)
                {
                    Trace.TraceWarning("[EmployeeRepository] Falló la precarga de empleados: " + ex.Message);
                }
            });
        }

        private static IList<Employee> GetAll()
        {
            var current = _current;
            if (current != null)
            {
                if (DateTime.UtcNow >= current.RefreshAfterUtc)
                    RefreshInBackground(current);
                return current.Employees;
            }

            int failuresBefore = _firstDownloadFailures;
            lock (_firstDownloadLock)
            {
                if (_current != null)
                    return _current.Employees;
                if (_firstDownloadFailures != failuresBefore)
                    throw Unavailable(null);

                try
                {
                    return Download().Employees;
                }
                catch (Exception ex)
                {
                    _firstDownloadFailures++;
                    Trace.TraceWarning("[EmployeeRepository] API no disponible y sin lista descargada. Error: " + ex.Message);
                    throw Unavailable(ex);
                }
            }
        }

        private static InvalidOperationException Unavailable(Exception inner)
        {
            return new InvalidOperationException(
                "El servicio de validación de empleados no está disponible y no existe una lista en caché.", inner);
        }

        // Un solo refresco a la vez. Mientras corre, los logins siguen con la lista vencida.
        private static void RefreshInBackground(EmployeeList stale)
        {
            if (Interlocked.CompareExchange(ref _refreshing, 1, 0) != 0)
                return;

            Task.Run(() =>
            {
                try
                {
                    Download();
                }
                catch (Exception ex)
                {
                    Trace.TraceWarning("[EmployeeRepository] API no disponible, se sigue sirviendo la última lista. Error: " + ex.Message);
                    // Misma lista con el próximo reintento; si otra escritura ya publicó una
                    // lista más nueva, no se pisa.
                    var retry = new EmployeeList(stale.Employees,
                        DateTime.UtcNow.AddMinutes(Constants.EmployeeApiRetryMinutes));
#pragma warning disable 420 // Interlocked respeta la semántica volatile del campo
                    Interlocked.CompareExchange(ref _current, retry, stale);
#pragma warning restore 420
                }
                finally
                {
                    Interlocked.Exchange(ref _refreshing, 0);
                }
            });
        }

        private static EmployeeList Download()
        {
            var list = new EmployeeList(FetchFromApi(),
                DateTime.UtcNow.AddMinutes(Constants.EmployeeCacheMinutes));
            _current = list;
            return list;
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

        // Lista y momento a partir del cual hay que refrescarla. Inmutable: se reemplaza entera.
        private sealed class EmployeeList
        {
            public EmployeeList(IList<Employee> employees, DateTime refreshAfterUtc)
            {
                Employees       = employees;
                RefreshAfterUtc = refreshAfterUtc;
            }

            public IList<Employee> Employees { get; }
            public DateTime RefreshAfterUtc { get; }
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
