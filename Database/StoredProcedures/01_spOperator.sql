-- ============================================================
-- 01_spOperator.sql
-- Stored procedures para la tabla Operators.
-- ============================================================

-- -------------------------------------------------------
-- spOperatorGetByCode1
-- -------------------------------------------------------
IF OBJECT_ID('dbo.spOperatorGetByCode1', 'P') IS NOT NULL
    DROP PROCEDURE dbo.spOperatorGetByCode1;
GO

CREATE PROCEDURE dbo.spOperatorGetByCode1
    @Code1 NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Code1, Name, PasswordHash, IsFirstLogin, CreatedAt
    FROM   dbo.Operators
    WHERE  Code1 = @Code1;
END;
GO

-- -------------------------------------------------------
-- spOperatorInsert
-- Devuelve el Id del registro creado.
-- -------------------------------------------------------
IF OBJECT_ID('dbo.spOperatorInsert', 'P') IS NOT NULL
    DROP PROCEDURE dbo.spOperatorInsert;
GO

CREATE PROCEDURE dbo.spOperatorInsert
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

-- -------------------------------------------------------
-- spOperatorUpdatePassword
-- También marca IsFirstLogin = 0.
-- -------------------------------------------------------
IF OBJECT_ID('dbo.spOperatorUpdatePassword', 'P') IS NOT NULL
    DROP PROCEDURE dbo.spOperatorUpdatePassword;
GO

CREATE PROCEDURE dbo.spOperatorUpdatePassword
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
