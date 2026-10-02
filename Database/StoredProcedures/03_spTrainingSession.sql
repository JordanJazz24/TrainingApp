-- ============================================================
-- 03_spTrainingSession.sql
-- Stored procedures para TrainingSessions.
-- ============================================================

-- -------------------------------------------------------
-- spTrainingSessionInsert
-- Devuelve el Id de la sesión creada.
-- -------------------------------------------------------
IF OBJECT_ID('dbo.spTrainingSessionInsert', 'P') IS NOT NULL
    DROP PROCEDURE dbo.spTrainingSessionInsert;
GO

CREATE PROCEDURE dbo.spTrainingSessionInsert
    @OperatorId       INT,
    @TrainingGuideId  INT,
    @Mode             TINYINT,
    @Status           TINYINT,
    @CurrentStepOrder INT,
    @StartedAt        DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.TrainingSessions
        (OperatorId, TrainingGuideId, Mode, Status, CurrentStepOrder, FinalScore, StartedAt)
    VALUES
        (@OperatorId, @TrainingGuideId, @Mode, @Status, @CurrentStepOrder, 0, @StartedAt);
    SELECT SCOPE_IDENTITY();
END;
GO

-- -------------------------------------------------------
-- spTrainingSessionGetById
-- -------------------------------------------------------
IF OBJECT_ID('dbo.spTrainingSessionGetById', 'P') IS NOT NULL
    DROP PROCEDURE dbo.spTrainingSessionGetById;
GO

CREATE PROCEDURE dbo.spTrainingSessionGetById
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, OperatorId, TrainingGuideId, Mode, Status,
           CurrentStepOrder, FinalScore, StartedAt, CompletedAt
    FROM   dbo.TrainingSessions
    WHERE  Id = @Id;
END;
GO

-- -------------------------------------------------------
-- spTrainingSessionUpdateStep
-- Avanza CurrentStepOrder al siguiente paso.
-- -------------------------------------------------------
IF OBJECT_ID('dbo.spTrainingSessionUpdateStep', 'P') IS NOT NULL
    DROP PROCEDURE dbo.spTrainingSessionUpdateStep;
GO

CREATE PROCEDURE dbo.spTrainingSessionUpdateStep
    @SessionId        INT,
    @CurrentStepOrder INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.TrainingSessions
    SET    CurrentStepOrder = @CurrentStepOrder
    WHERE  Id = @SessionId;
END;
GO

-- -------------------------------------------------------
-- spTrainingSessionComplete
-- Marca la sesión como completada con el puntaje final.
-- Status 1 = Completed
-- -------------------------------------------------------
IF OBJECT_ID('dbo.spTrainingSessionComplete', 'P') IS NOT NULL
    DROP PROCEDURE dbo.spTrainingSessionComplete;
GO

CREATE PROCEDURE dbo.spTrainingSessionComplete
    @SessionId   INT,
    @FinalScore  DECIMAL(5,2),
    @CompletedAt DATETIME2
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.TrainingSessions
    SET    Status      = 1,
           FinalScore  = @FinalScore,
           CompletedAt = @CompletedAt
    WHERE  Id = @SessionId;
END;
GO

-- -------------------------------------------------------
-- spTrainingSessionAbandon
-- Marca la sesión como abandonada (Status 2 = Abandoned).
-- -------------------------------------------------------
IF OBJECT_ID('dbo.spTrainingSessionAbandon', 'P') IS NOT NULL
    DROP PROCEDURE dbo.spTrainingSessionAbandon;
GO

CREATE PROCEDURE dbo.spTrainingSessionAbandon
    @SessionId INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.TrainingSessions
    SET    Status      = 2,
           CompletedAt = SYSUTCDATETIME()
    WHERE  Id = @SessionId;
END;
GO

-- -------------------------------------------------------
-- spTrainingSessionAbandonActiveByOperator
-- Marca como Abandoned (Status 2) las sesiones InProgress
-- (Status 0) del operador para una guía dada antes de
-- iniciar una nueva. Conserva los intentos (auditoría
-- AUD-01, decisión D-4): el DELETE que lo precedía fallaba
-- por la FK de TrainingSessionStepAttempts.
-- -------------------------------------------------------
IF OBJECT_ID('dbo.spTrainingSessionDeleteActiveByOperator', 'P') IS NOT NULL
    DROP PROCEDURE dbo.spTrainingSessionDeleteActiveByOperator;
GO

IF OBJECT_ID('dbo.spTrainingSessionAbandonActiveByOperator', 'P') IS NOT NULL
    DROP PROCEDURE dbo.spTrainingSessionAbandonActiveByOperator;
GO

CREATE PROCEDURE dbo.spTrainingSessionAbandonActiveByOperator
    @OperatorId      INT,
    @TrainingGuideId INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.TrainingSessions
    SET    Status      = 2,
           CompletedAt = SYSUTCDATETIME()
    WHERE  OperatorId      = @OperatorId
    AND    TrainingGuideId = @TrainingGuideId
    AND    Status          = 0;
END;
GO
