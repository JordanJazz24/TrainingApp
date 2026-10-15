-- ============================================================
-- 04_spStepAttempt.sql — SPs de TrainingSessionStepAttempts.
-- Requiere: 00_Schema.sql · Re-ejecutable: sí (CREATE OR ALTER).
-- ============================================================

-- spStepAttemptRecord — registra un intento y, en la misma transacción,
-- avanza la sesión al paso siguiente (@NextStepOrder) o la cierra con su
-- puntaje final (@FinalScore; Status 1 = Completed). Sin ninguno de los dos,
-- solo registra el intento (incorrecto). Las decisiones (avanzar, cerrar,
-- puntaje) las toma la BLL; el SP solo las guarda juntas (AUD-06).
-- @Reason y @Action: registro de actividad (spec 013), en la misma
-- transacción; null solo para quien todavía no los manda.
CREATE OR ALTER PROCEDURE dbo.spStepAttemptRecord
    @SessionId     INT,
    @StepId        INT,
    @StepOrder     INT,
    @AttemptNumber INT,
    @IsCorrect     BIT,
    @Timestamp     DATETIME2,
    @NextStepOrder INT          = NULL,
    @FinalScore    DECIMAL(5,2) = NULL,
    @Reason        TINYINT      = NULL,
    @Action        NVARCHAR(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @NextStepOrder IS NOT NULL AND @FinalScore IS NOT NULL
        THROW 50001, N'spStepAttemptRecord: avanzar y cerrar la sesión son excluyentes.', 1;

    BEGIN TRANSACTION;

    -- La BLL decidió con la sesión en curso, en este paso y con este número de intento.
    -- UPDLOCK serializa dos envíos simultáneos de la misma sesión (por ejemplo, desde dos
    -- navegadores): el segundo encuentra la sesión ya cambiada y falla sin guardar nada.
    IF NOT EXISTS (SELECT 1 FROM dbo.TrainingSessions WITH (UPDLOCK, HOLDLOCK)
                   WHERE  Id = @SessionId AND Status = 0 AND CurrentStepOrder = @StepOrder)
       OR (SELECT COUNT(*) FROM dbo.TrainingSessionStepAttempts
           WHERE  SessionId = @SessionId AND StepId = @StepId) <> @AttemptNumber - 1
        THROW 50002, N'spStepAttemptRecord: la sesión cambió mientras se registraba el intento.', 1;

    INSERT INTO dbo.TrainingSessionStepAttempts
        (SessionId, StepId, AttemptNumber, IsCorrect, Timestamp, Reason, Action)
    VALUES
        (@SessionId, @StepId, @AttemptNumber, @IsCorrect, @Timestamp, @Reason, @Action);

    IF @FinalScore IS NOT NULL
        UPDATE dbo.TrainingSessions
        SET    Status      = 1,
               FinalScore  = @FinalScore,
               CompletedAt = @Timestamp
        WHERE  Id = @SessionId;
    ELSE IF @NextStepOrder IS NOT NULL
        UPDATE dbo.TrainingSessions
        SET    CurrentStepOrder = @NextStepOrder
        WHERE  Id = @SessionId;

    COMMIT TRANSACTION;
END;
GO

-- spStepAttemptGetBySession — todos los intentos de una sesión, en orden
-- cronológico. La BLL calcula con ellos el número de intento y el puntaje.
CREATE OR ALTER PROCEDURE dbo.spStepAttemptGetBySession
    @SessionId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, SessionId, StepId, AttemptNumber, IsCorrect, Timestamp, Reason, Action
    FROM   dbo.TrainingSessionStepAttempts
    WHERE  SessionId = @SessionId
    ORDER  BY Timestamp;
END;
GO
