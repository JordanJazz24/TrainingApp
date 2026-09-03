-- ============================================================
-- 04_spStepAttempt.sql
-- Stored procedures para TrainingSessionStepAttempts.
-- ============================================================

-- -------------------------------------------------------
-- spStepAttemptInsert
-- -------------------------------------------------------
IF OBJECT_ID('dbo.spStepAttemptInsert', 'P') IS NOT NULL
    DROP PROCEDURE dbo.spStepAttemptInsert;
GO

CREATE PROCEDURE dbo.spStepAttemptInsert
    @SessionId     INT,
    @StepId        INT,
    @AttemptNumber INT,
    @IsCorrect     BIT,
    @Timestamp     DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.TrainingSessionStepAttempts
        (SessionId, StepId, AttemptNumber, IsCorrect, Timestamp)
    VALUES
        (@SessionId, @StepId, @AttemptNumber, @IsCorrect, @Timestamp);
END;
GO

-- -------------------------------------------------------
-- spStepAttemptGetBySession
-- Todos los intentos de una sesión, ordenados cronológicamente.
-- -------------------------------------------------------
IF OBJECT_ID('dbo.spStepAttemptGetBySession', 'P') IS NOT NULL
    DROP PROCEDURE dbo.spStepAttemptGetBySession;
GO

CREATE PROCEDURE dbo.spStepAttemptGetBySession
    @SessionId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, SessionId, StepId, AttemptNumber, IsCorrect, Timestamp
    FROM   dbo.TrainingSessionStepAttempts
    WHERE  SessionId = @SessionId
    ORDER  BY Timestamp;
END;
GO

-- -------------------------------------------------------
-- spStepAttemptGetBySessionAndStep
-- Todos los intentos de un paso específico en una sesión.
-- Usado para calcular AttemptNumber del próximo intento.
-- -------------------------------------------------------
IF OBJECT_ID('dbo.spStepAttemptGetBySessionAndStep', 'P') IS NOT NULL
    DROP PROCEDURE dbo.spStepAttemptGetBySessionAndStep;
GO

CREATE PROCEDURE dbo.spStepAttemptGetBySessionAndStep
    @SessionId INT,
    @StepId    INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, SessionId, StepId, AttemptNumber, IsCorrect, Timestamp
    FROM   dbo.TrainingSessionStepAttempts
    WHERE  SessionId = @SessionId
    AND    StepId    = @StepId
    ORDER  BY AttemptNumber;
END;
GO
