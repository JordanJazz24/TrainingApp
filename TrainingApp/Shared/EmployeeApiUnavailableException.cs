using System;

namespace TrainingApp.Shared
{
    /// <summary>
    /// La API de empleados no respondió y no hay una lista descargada (AUD-75, B-5). Es la única
    /// falla que el login y el alta de usuarios muestran como "servicio no disponible"; cualquier
    /// otra excepción (ej. la BD) sigue hasta Application_Error.
    /// </summary>
    public class EmployeeApiUnavailableException : Exception
    {
        public EmployeeApiUnavailableException(string message, Exception inner) : base(message, inner) { }
    }
}
