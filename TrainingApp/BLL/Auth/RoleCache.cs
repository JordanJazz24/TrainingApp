using System;
using System.Collections.Concurrent;
using TrainingApp.DAL.Repositories;
using TrainingApp.Domain;

namespace TrainingApp.BLL.Auth
{
    // Rol del usuario autenticado en cada request (spec 013): Global.asax arma con él el
    // principal antes de la autorización por <location>. Se guarda unos minutos para no
    // consultar la BD en cada request; un cambio de rol desde la gestión de usuarios lo
    // invalida (Invalidate) y en otro proceso se ve al vencer (TimeToLive).
    public static class RoleCache
    {
        private static readonly TimeSpan TimeToLive = TimeSpan.FromMinutes(2);

        private static readonly ConcurrentDictionary<string, Entry> _entries =
            new ConcurrentDictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);

        // null si el Code1 no tiene cuenta local (no se guarda: puede crearse en el próximo login).
        public static UserRole? GetRole(string code1)
        {
            if (string.IsNullOrEmpty(code1)) return null;

            if (_entries.TryGetValue(code1, out var entry) && entry.ExpiresAt > DateTime.UtcNow)
                return entry.Role;

            var user = new UserRepository().GetByCode1(code1);
            if (user == null)
            {
                _entries.TryRemove(code1, out _);
                return null;
            }

            _entries[code1] = new Entry(user.Role, DateTime.UtcNow.Add(TimeToLive));
            return user.Role;
        }

        public static void Invalidate(string code1)
        {
            if (!string.IsNullOrEmpty(code1))
                _entries.TryRemove(code1, out _);
        }

        private sealed class Entry
        {
            public Entry(UserRole role, DateTime expiresAt)
            {
                Role      = role;
                ExpiresAt = expiresAt;
            }

            public UserRole Role { get; }
            public DateTime ExpiresAt { get; }
        }
    }
}
