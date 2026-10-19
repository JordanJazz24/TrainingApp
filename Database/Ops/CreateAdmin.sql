-- ============================================================
-- CreateAdmin.sql — primer administrador (spec 013). Los siguientes
-- se crean desde el módulo administrativo (Gestión de usuarios).
--
-- Uso: poner el Code1 y el nombre completo del empleado y ejecutar.
--   · Sin cuenta: la crea como Administrator con la contraseña por
--     defecto (Constants.DEFAULT_PASSWORD) y el cambio obligatorio.
--     Entra por el login único (/Pages/Auth/Login.aspx, spec 016) y
--     cambia la contraseña antes de usar el módulo (AC-6).
--   · Con cuenta: solo le da el rol (conserva su contraseña). Los
--     roles son excluyentes: ya no entrena; su entrenamiento en curso
--     se abandona (D-9). La app ve el rol nuevo en hasta 2 minutos
--     (RoleCache) o al reciclar el App Pool.
-- El Code1 tiene que estar en la API de empleados para poder entrar.
--
-- @DefaultPasswordHash es PasswordHelper.Hash(DEFAULT_PASSWORD), el
-- mismo de ResetUserPassword.sql: si cambia DEFAULT_PASSWORD, se
-- regenera con el comando de ese script.
--
-- Requiere: 00_Schema.sql con las columnas de la spec 013.
-- Re-ejecutable: sí.
-- ============================================================
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Code1               NVARCHAR(50)  = N'';   -- ← Code1 del empleado
DECLARE @Name                NVARCHAR(200) = N'';   -- ← nombre completo (solo si no tiene cuenta)
DECLARE @DefaultPasswordHash NVARCHAR(500) =
    N'P00QpwQOxnbsrPnMeHFhl/Pi66+x1detOPfR8iSJfUijhAwXCAL/exNhCEj7QaxO';
DECLARE @Administrator       TINYINT       = 2;

IF LTRIM(RTRIM(@Code1)) = N''
BEGIN
    RAISERROR(N'Falta el Code1 del empleado.', 16, 1);
    RETURN;
END;

BEGIN TRANSACTION;

IF EXISTS (SELECT 1 FROM dbo.Users WITH (UPDLOCK, HOLDLOCK) WHERE Code1 = @Code1)
BEGIN
    UPDATE dbo.Users SET Role = @Administrator WHERE Code1 = @Code1;
    -- Status: 0 = InProgress, 2 = Abandoned.
    UPDATE s SET s.Status = 2
    FROM   dbo.TrainingSessions s
    JOIN   dbo.Users u ON u.Id = s.UserId
    WHERE  u.Code1 = @Code1 AND s.Status = 0;
    PRINT N'Cuenta existente: ' + @Code1 + N' ahora es Administrator.';
END
ELSE
BEGIN
    IF LTRIM(RTRIM(@Name)) = N''
    BEGIN
        ROLLBACK TRANSACTION;
        RAISERROR(N'El Code1 "%s" no tiene cuenta: falta el nombre completo para crearla.', 16, 1, @Code1);
        RETURN;
    END;

    INSERT INTO dbo.Users (Code1, Name, PasswordHash, IsFirstLogin, CreatedAt, Role)
    VALUES (@Code1, @Name, @DefaultPasswordHash, 1, SYSUTCDATETIME(), @Administrator);
    PRINT N'Cuenta creada: ' + @Code1 + N' como Administrator, con la contraseña por defecto.';
END;

COMMIT TRANSACTION;
