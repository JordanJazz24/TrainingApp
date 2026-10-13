-- ============================================================
-- 01_spUser.sql — SPs de Users (login, alta en el primer
-- acceso, cambio de contraseña y, desde la spec 013, rol,
-- bloqueo, último acceso y gestión por un administrador).
-- Requiere: 00_Schema.sql · Re-ejecutable: sí (CREATE OR ALTER).
-- ============================================================

-- spUserGetByCode1 — usuario local por su código de empleado.
CREATE OR ALTER PROCEDURE dbo.spUserGetByCode1
    @Code1 NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Code1, Name, PasswordHash, IsFirstLogin, CreatedAt,
           Role, LastLoginAt, FailedLoginCount, LockedUntil
    FROM   dbo.Users
    WHERE  Code1 = @Code1;
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
