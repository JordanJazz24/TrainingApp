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
    @Role         TINYINT = 1
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.Users (Code1, Name, PasswordHash, IsFirstLogin, CreatedAt, Role)
    VALUES (@Code1, @Name, @PasswordHash, @IsFirstLogin, @CreatedAt, @Role);
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
