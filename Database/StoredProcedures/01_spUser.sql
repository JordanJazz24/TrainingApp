-- ============================================================
-- 01_spUser.sql — SPs de Users (login, alta en el primer
-- acceso, cambio de contraseña y, desde la spec 013, rol,
-- bloqueo, último acceso y gestión por un administrador).
-- Requiere: 00_Schema.sql · Re-ejecutable: sí (CREATE OR ALTER).
-- ============================================================

-- spUserGetByCode1 — usuario local por su código de empleado, con su área
-- (spec 021; NULL si no tiene).
CREATE OR ALTER PROCEDURE dbo.spUserGetByCode1
    @Code1 NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT u.Id, u.Code1, u.Name, u.PasswordHash, u.IsFirstLogin, u.CreatedAt,
           u.Role, u.LastLoginAt, u.FailedLoginCount, u.LockedUntil,
           u.AreaId, p.Name AS ProductName, a.Name AS AreaName
    FROM   dbo.Users u
    LEFT   JOIN dbo.Areas    a ON a.Id = u.AreaId
    LEFT   JOIN dbo.Products p ON p.Id = a.ProductId
    WHERE  u.Code1 = @Code1;
END;
GO

-- spUserInsert — alta en el primer acceso o por un administrador (con su
-- rol: 1 = Operator, 2 = Administrator). Devuelve el Id creado.
CREATE OR ALTER PROCEDURE dbo.spUserInsert
    @Code1        NVARCHAR(50),
    @Name         NVARCHAR(200),
    @PasswordHash NVARCHAR(500),
    @IsFirstLogin BIT,
    @CreatedAt    DATETIME2,
    @Role         TINYINT   = 1,
    @LastLoginAt  DATETIME2 = NULL
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.Users (Code1, Name, PasswordHash, IsFirstLogin, CreatedAt, Role, LastLoginAt)
    VALUES (@Code1, @Name, @PasswordHash, @IsFirstLogin, @CreatedAt, @Role, @LastLoginAt);
    SELECT SCOPE_IDENTITY();
END;
GO

-- spUserUpdatePassword — guarda el hash nuevo y marca IsFirstLogin = 0.
CREATE OR ALTER PROCEDURE dbo.spUserUpdatePassword
    @UserId       INT,
    @PasswordHash NVARCHAR(500)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Users
    SET    PasswordHash = @PasswordHash,
           IsFirstLogin = 0
    WHERE  Id = @UserId;
END;
GO

-- spUserLoginFailed — un login fallido con la cuenta existente (spec 013).
-- Suma el fallo en un solo UPDATE (dos fallos simultáneos no se pisan). Al
-- llegar a @LockThreshold bloquea hasta @LockedUntil y el contador vuelve a 0.
-- Si otro request ya la bloqueó, no suma (un bloqueo no cuenta fallos). La
-- regla (umbral y duración) la decide la BLL. Devuelve el LockedUntil vigente:
-- si es posterior a @Now, la cuenta está bloqueada.
CREATE OR ALTER PROCEDURE dbo.spUserLoginFailed
    @UserId        INT,
    @LockThreshold INT,
    @LockedUntil   DATETIME2,
    @Now           DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Users
    SET    FailedLoginCount = CASE WHEN FailedLoginCount + 1 >= @LockThreshold
                                   THEN 0 ELSE FailedLoginCount + 1 END,
           LockedUntil      = CASE WHEN FailedLoginCount + 1 >= @LockThreshold
                                   THEN @LockedUntil ELSE LockedUntil END
    WHERE  Id = @UserId
      AND  (LockedUntil IS NULL OR LockedUntil <= @Now);

    SELECT LockedUntil FROM dbo.Users WHERE Id = @UserId;
END;
GO

-- spUserLoginSucceeded — login correcto (spec 013): reinicia el contador,
-- quita un bloqueo vencido y guarda el último acceso (UTC). Si otro request
-- bloqueó la cuenta mientras se revisaba la contraseña, no entra: devuelve 0
-- (filas actualizadas) y el bloqueo sigue.
CREATE OR ALTER PROCEDURE dbo.spUserLoginSucceeded
    @UserId      INT,
    @LastLoginAt DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Users
    SET    FailedLoginCount = 0,
           LockedUntil      = NULL,
           LastLoginAt      = @LastLoginAt
    WHERE  Id = @UserId
      AND  (LockedUntil IS NULL OR LockedUntil <= @LastLoginAt);

    SELECT @@ROWCOUNT;
END;
GO

-- ── Gestión de usuarios (spec 013): solo la usa UserAdminService, que
-- verifica antes que quien llama es administrador. ───────────────────────

-- spUserGetById — usuario por Id (el admin autenticado y el usuario sobre
-- el que actúa), con su área (spec 021; NULL si no tiene).
CREATE OR ALTER PROCEDURE dbo.spUserGetById
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT u.Id, u.Code1, u.Name, u.PasswordHash, u.IsFirstLogin, u.CreatedAt,
           u.Role, u.LastLoginAt, u.FailedLoginCount, u.LockedUntil,
           u.AreaId, p.Name AS ProductName, a.Name AS AreaName
    FROM   dbo.Users u
    LEFT   JOIN dbo.Areas    a ON a.Id = u.AreaId
    LEFT   JOIN dbo.Products p ON p.Id = a.ProductId
    WHERE  u.Id = @UserId;
END;
GO

-- spUserSearch — una página de la lista, ordenada por nombre. @Text vacío
-- = todos; si no, el Code1 o el nombre lo contienen (los comodines de LIKE
-- que escriba el admin se toman literales). Dos resultados: el total y las
-- filas de la página, cada una con su cantidad de sesiones de entrenamiento
-- y su área (spec 021; NULL si no tiene).
CREATE OR ALTER PROCEDURE dbo.spUserSearch
    @Text     NVARCHAR(200),
    @Offset   INT,
    @PageSize INT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Pattern NVARCHAR(4000) = NULL;
    IF LTRIM(RTRIM(ISNULL(@Text, N''))) <> N''
        SET @Pattern = N'%' + REPLACE(REPLACE(REPLACE(LTRIM(RTRIM(@Text)),
                           N'[', N'[[]'), N'%', N'[%]'), N'_', N'[_]') + N'%';

    SELECT COUNT(*) AS Total
    FROM   dbo.Users
    WHERE  @Pattern IS NULL OR Code1 LIKE @Pattern OR Name LIKE @Pattern;

    SELECT u.Id, u.Code1, u.Name, u.PasswordHash, u.IsFirstLogin, u.CreatedAt,
           u.Role, u.LastLoginAt, u.FailedLoginCount, u.LockedUntil,
           u.AreaId, p.Name AS ProductName, a.Name AS AreaName,
           (SELECT COUNT(*) FROM dbo.TrainingSessions s WHERE s.UserId = u.Id) AS SessionCount
    FROM   dbo.Users u
    LEFT   JOIN dbo.Areas    a ON a.Id = u.AreaId
    LEFT   JOIN dbo.Products p ON p.Id = a.ProductId
    WHERE  @Pattern IS NULL OR u.Code1 LIKE @Pattern OR u.Name LIKE @Pattern
    ORDER  BY u.Name, u.Code1
    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
END;
GO

-- spUserResetPassword — vuelve a la contraseña por defecto (@PasswordHash)
-- con el cambio obligatorio y desbloquea (P-2). No toca el historial.
CREATE OR ALTER PROCEDURE dbo.spUserResetPassword
    @UserId       INT,
    @PasswordHash NVARCHAR(500)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Users
    SET    PasswordHash     = @PasswordHash,
           IsFirstLogin     = 1,
           FailedLoginCount = 0,
           LockedUntil      = NULL
    WHERE  Id = @UserId;
END;
GO

-- spUserChooseArea — el operador elige su propia área (spec 021, P-4). Solo
-- si la cuenta es Operator y aún no tiene área: dos pestañas no pisan una
-- elección ya hecha. Que el área sea elegible lo verifica la BLL. Devuelve
-- las filas afectadas (0 = ya tenía área, no es Operator o no existe).
CREATE OR ALTER PROCEDURE dbo.spUserChooseArea
    @UserId INT,
    @AreaId INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Users
    SET    AreaId = @AreaId
    WHERE  Id = @UserId
      AND  AreaId IS NULL
      AND  Role = 1;
    SELECT @@ROWCOUNT;
END;
GO

-- spUserSetArea — un admin cambia o quita (@AreaId NULL) el área de un
-- Operator (spec 021, P-5). Que quien llama sea admin y que el área sea
-- elegible lo verifica la BLL. Devuelve las filas afectadas (0 = la cuenta
-- no es Operator o no existe).
CREATE OR ALTER PROCEDURE dbo.spUserSetArea
    @UserId INT,
    @AreaId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Users
    SET    AreaId = @AreaId
    WHERE  Id = @UserId
      AND  Role = 1;
    SELECT @@ROWCOUNT;
END;
GO

-- spUserUnlock — desbloqueo por un admin antes de que venza.
CREATE OR ALTER PROCEDURE dbo.spUserUnlock
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Users
    SET    FailedLoginCount = 0,
           LockedUntil      = NULL
    WHERE  Id = @UserId;
END;
GO

-- spUserDelete — borra la cuenta y todo su historial de entrenamiento
-- (intentos y sesiones), en una transacción y sin recuperación (spec 013,
-- enmienda 2026-10-13). Bajo un lock de aplicación, para que dos admins que
-- se borran entre sí no dejen el sistema sin administradores:
--   · @CallerId (el admin que borra) tiene que seguir siendo admin y no puede
--     borrarse a sí mismo (THROW 50011).
CREATE OR ALTER PROCEDURE dbo.spUserDelete
    @CallerId INT,
    @UserId   INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;

    EXEC sp_getapplock @Resource = N'TrainingApp.UserRoles', @LockMode = N'Exclusive',
                       @LockOwner = N'Transaction';

    IF @CallerId = @UserId
       OR NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Id = @CallerId AND Role = 2)
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50011, N'spUserDelete: quien borra no es administrador o se borra a sí mismo.', 1;
    END;

    DELETE a
    FROM   dbo.TrainingSessionStepAttempts a
    JOIN   dbo.TrainingSessions s ON s.Id = a.SessionId
    WHERE  s.UserId = @UserId;

    DELETE FROM dbo.TrainingSessions WHERE UserId = @UserId;
    DELETE FROM dbo.Users WHERE Id = @UserId;

    COMMIT TRANSACTION;
END;
GO
