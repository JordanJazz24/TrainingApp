-- ============================================================
-- 03_spTrainingSession.sql — SPs de TrainingSessions (iniciar,
-- leer y abandonar). Avanzar y cerrar una sesión lo hace
-- spStepAttemptRecord (04), junto con el intento.
-- Requiere: 00_Schema.sql · Re-ejecutable: sí (CREATE OR ALTER).
--
-- Status: 0 = InProgress, 1 = Completed, 2 = Abandoned.
-- ============================================================

-- spTrainingSessionInsert — crea la sesión. Devuelve el Id creado.
-- @ResourceSetupType: el de la guía al empezar (spec 020, D-188).
CREATE OR ALTER PROCEDURE dbo.spTrainingSessionInsert
    @UserId           INT,
    @TrainingGuideId  INT,
    @Mode             TINYINT,
    @Status           TINYINT,
    @CurrentStepOrder INT,
    @StartedAt        DATETIME2,
    @ResourceSetupType TINYINT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.TrainingSessions
        (UserId, TrainingGuideId, Mode, Status, CurrentStepOrder, FinalScore, StartedAt, ResourceSetupType)
    VALUES
        (@UserId, @TrainingGuideId, @Mode, @Status, @CurrentStepOrder, 0, @StartedAt, @ResourceSetupType);
    SELECT SCOPE_IDENTITY();
END;
GO

-- spTrainingSessionGetById
CREATE OR ALTER PROCEDURE dbo.spTrainingSessionGetById
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, UserId, TrainingGuideId, Mode, Status,
           CurrentStepOrder, FinalScore, StartedAt, CompletedAt, ResourceSetupType
    FROM   dbo.TrainingSessions
    WHERE  Id = @Id;
END;
GO

-- spTrainingSessionAbandon — marca la sesión como Abandoned solo si sigue
-- InProgress: una evaluación ya completada no se pisa (AUD-05).
CREATE OR ALTER PROCEDURE dbo.spTrainingSessionAbandon
    @SessionId INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.TrainingSessions
    SET    Status      = 2,
           CompletedAt = SYSUTCDATETIME()
    WHERE  Id     = @SessionId
    AND    Status = 0;
END;
GO

-- spTrainingSessionAbandonActiveByUser — abandona todas las sesiones
-- InProgress del usuario: una sola sesión activa por operador (D-9). Se usa
-- al iniciar una guía y en el logout. Conserva los intentos (AUD-01, D-4).
CREATE OR ALTER PROCEDURE dbo.spTrainingSessionAbandonActiveByUser
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.TrainingSessions
    SET    Status      = 2,
           CompletedAt = SYSUTCDATETIME()
    WHERE  UserId = @UserId
    AND    Status = 0;
END;
GO
