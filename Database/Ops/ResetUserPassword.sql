-- ============================================================
-- ResetUserPassword.sql — vuelve a la contraseña por defecto la
-- cuenta de un usuario (versión de prueba, T1 de la Fase 8.5).
--
-- Temporal: lo reemplaza el reset por un administrador (spec 013).
-- Uso: poner el Code1 en @Code1 y ejecutar. El usuario entra con
-- Constants.DEFAULT_PASSWORD y la app lo manda a ChangePassword.
-- También la desbloquea (spec 013). No borra sus sesiones ni sus
-- intentos. Desde la spec 013 también lo hace un admin en Gestión de
-- usuarios; el script queda para cuando no hay un admin disponible.
--
-- @DefaultPasswordHash es PasswordHelper.Hash(DEFAULT_PASSWORD)
-- precalculado: T-SQL no reproduce el PBKDF2 de la app. Versionarlo
-- no agrega riesgo porque la contraseña ya está en el repo (D-18).
-- Si cambia DEFAULT_PASSWORD, regenerarlo (con la app compilada):
--   $a = [Reflection.Assembly]::LoadFile("<ruta>\TrainingApp\bin\TrainingApp.dll")
--   $pw = $a.GetType("TrainingApp.Shared.Constants").GetField("DEFAULT_PASSWORD").GetValue($null)
--   $a.GetType("TrainingApp.Shared.PasswordHelper").GetMethod("Hash").Invoke($null, @($pw))
--
-- Requiere: 00_Schema.sql · Re-ejecutable: sí.
-- ============================================================
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Code1               NVARCHAR(50)  = N'';   -- ← Code1 del usuario
DECLARE @DefaultPasswordHash NVARCHAR(500) =
    N'P00QpwQOxnbsrPnMeHFhl/Pi66+x1detOPfR8iSJfUijhAwXCAL/exNhCEj7QaxO';

IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Code1 = @Code1)
BEGIN
    RAISERROR(N'No existe un usuario con Code1 "%s". Si nunca entró, no hace falta el reset: entra con la contraseña por defecto.', 16, 1, @Code1);
    RETURN;
END;

UPDATE dbo.Users
SET    PasswordHash     = @DefaultPasswordHash,
       IsFirstLogin     = 1,
       FailedLoginCount = 0,
       LockedUntil      = NULL
WHERE  Code1 = @Code1;

PRINT N'Contraseña de ' + @Code1 + N' restablecida a la por defecto; deberá cambiarla al entrar.';
