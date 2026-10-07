-- ============================================================
-- 01_spOperator.sql — SPs de Operators (login, alta en el primer
-- acceso y cambio de contraseña).
-- Requiere: 00_Schema.sql · Re-ejecutable: sí (CREATE OR ALTER).
-- ============================================================

-- spOperatorGetByCode1 — operador local por su código de empleado.
CREATE OR ALTER PROCEDURE dbo.spOperatorGetByCode1
    @Code1 NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Code1, Name, PasswordHash, IsFirstLogin, CreatedAt
    FROM   dbo.Operators
    WHERE  Code1 = @Code1;
END;
GO

-- spOperatorInsert — alta en el primer acceso. Devuelve el Id creado.
CREATE OR ALTER PROCEDURE dbo.spOperatorInsert
    @Code1        NVARCHAR(50),
    @Name         NVARCHAR(200),
    @PasswordHash NVARCHAR(500),
    @IsFirstLogin BIT,
    @CreatedAt    DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.Operators (Code1, Name, PasswordHash, IsFirstLogin, CreatedAt)
    VALUES (@Code1, @Name, @PasswordHash, @IsFirstLogin, @CreatedAt);
    SELECT SCOPE_IDENTITY();
END;
GO

-- spOperatorUpdatePassword — guarda el hash nuevo y marca IsFirstLogin = 0.
CREATE OR ALTER PROCEDURE dbo.spOperatorUpdatePassword
    @OperatorId   INT,
    @PasswordHash NVARCHAR(500)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Operators
    SET    PasswordHash = @PasswordHash,
           IsFirstLogin = 0
    WHERE  Id = @OperatorId;
END;
GO
