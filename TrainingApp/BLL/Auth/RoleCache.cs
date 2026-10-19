using System;
using System.Collections.Concurrent;
using TrainingApp.DAL.Repositories;
using TrainingApp.Domain;

namespace TrainingApp.BLL.Auth
{
    // Cuenta del usuario autenticado en cada request (spec 013): Global.asax arma con ella el
    // principal antes de la autorización por <location>. Se guarda unos minutos para no
    // consultar la BD en cada request; un borrado desde la gestión de usuarios la invalida
    // (Invalidate) y en otro proceso se ve al vencer (TimeToLive). Lleva el Id de la cuenta
    // para que SessionHelper detecte una Session de una cuenta borrada y vuelta a crear con el
    // mismo Code1 (fijación de sesión, 2026-10-19).
    public static class RoleCache
    {
        private static readonly TimeSpan TimeToLive = TimeSpan.FromMinutes(2);

        private static readonly ConcurrentDictionary<string, Entry> _entries =
            new ConcurrentDictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);

        // null si el Code1 no tiene cuenta local (no se guarda: puede crearse en el próximo login).
        public static Account GetAccount(string code1)
        {
            if (string.IsNullOrEmpty(code1)) return null;

            if (_entries.TryGetValue(code1, out var entry) && entry.ExpiresAt > DateTime.UtcNow)
                return entry.Account;

            var user = new UserRepository().GetByCode1(code1);
            if (user == null)
            {
                _entries.TryRemove(code1, out _);
                return null;
            }

            var account = new Account(user.Id, user.Role);
            _entries[code1] = new Entry(account, DateTime.UtcNow.Add(TimeToLive));
            return account;
        }

        public static void Invalidate(string code1)
        {
            if (!string.IsNullOrEmpty(code1))
                _entries.TryRemove(code1, out _);
        }

        public sealed class Account
        {
            public Account(int userId, UserRole role)
            {
                UserId = userId;
                Role   = role;
            }

            public int      UserId { get; }
            public UserRole Role   { get; }
        }

        private sealed class Entry
        {
            public Entry(Account account, DateTime expiresAt)
            {
                Account   = account;
                ExpiresAt = expiresAt;
            }

            public Account  Account   { get; }
            public DateTime ExpiresAt { get; }
        }
    }
}
