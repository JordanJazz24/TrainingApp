-- ============================================================
-- 09_MigrateSFCSplitStep.sql
-- Alinea StepType y pesos para el paso SFC Split independiente.
-- Seguro para re-ejecución.
-- ============================================================
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF EXISTS (
    SELECT 1
    FROM dbo.TrainingSessions s
    INNER JOIN dbo.TrainingGuides g ON g.Id = s.TrainingGuideId
    INNER JOIN dbo.Areas a ON a.Id = g.AreaId
    WHERE s.Status = 0 AND a.SerializationType = 1
)
BEGIN
    ROLLBACK TRANSACTION;
    THROW 50001, 'No se puede migrar SFC Split mientras existan sesiones NonSerialized activas.', 1;
END;

DECLARE @GuideId INT;
DECLARE GuideCursor CURSOR LOCAL FAST_FORWARD FOR
SELECT g.Id
FROM dbo.TrainingGuides g
INNER JOIN dbo.Areas a ON a.Id = g.AreaId
WHERE a.SerializationType = 1;

OPEN GuideCursor;
FETCH NEXT FROM GuideCursor INTO @GuideId;

WHILE @@FETCH_STATUS = 0
BEGIN
    -- Forma antigua: 5=LogNC y 6=Complete, sin SFC Split.
    IF NOT EXISTS (SELECT 1 FROM dbo.TrainingSteps WHERE TrainingGuideId = @GuideId AND StepType = 7)
    BEGIN
        UPDATE dbo.TrainingSteps SET StepType = 7
        WHERE TrainingGuideId = @GuideId AND StepType = 6;

        UPDATE dbo.TrainingSteps SET StepType = 6
        WHERE TrainingGuideId = @GuideId AND StepType = 5;

        DECLARE @LogNCOrder INT = (
            SELECT [Order] FROM dbo.TrainingSteps
            WHERE TrainingGuideId = @GuideId AND StepType = 6
        );

        -- Dos fases evitan colisiones con UQ_TrainingSteps_GuideOrder.
        UPDATE dbo.TrainingSteps
        SET [Order] = [Order] + 100
        WHERE TrainingGuideId = @GuideId AND [Order] >= @LogNCOrder;

        UPDATE dbo.TrainingSteps
        SET [Order] = [Order] - 99
        WHERE TrainingGuideId = @GuideId AND [Order] >= @LogNCOrder + 100;

        INSERT INTO dbo.TrainingSteps (TrainingGuideId, StepType, [Order], Weight)
        VALUES (@GuideId, 5, @LogNCOrder, 0.05);
    END;

    -- Normaliza el reparto definitivo: SFC Split 5%, LogNC 10%.
    UPDATE dbo.TrainingSteps SET Weight = 0.05
    WHERE TrainingGuideId = @GuideId AND StepType = 5;

    UPDATE dbo.TrainingSteps SET Weight = 0.10
    WHERE TrainingGuideId = @GuideId AND StepType = 6;

    FETCH NEXT FROM GuideCursor INTO @GuideId;
END;

CLOSE GuideCursor;
DEALLOCATE GuideCursor;

-- Las guías Serialized omiten SFC Split, pero usan los nuevos ordinales.
UPDATE ts SET StepType = 7
FROM dbo.TrainingSteps ts
INNER JOIN dbo.TrainingGuides g ON g.Id = ts.TrainingGuideId
INNER JOIN dbo.Areas a ON a.Id = g.AreaId
WHERE a.SerializationType = 0 AND ts.StepType = 6
  AND NOT EXISTS (
      SELECT 1 FROM dbo.TrainingSteps x
      WHERE x.TrainingGuideId = ts.TrainingGuideId AND x.StepType = 7
  );

UPDATE ts SET StepType = 6
FROM dbo.TrainingSteps ts
INNER JOIN dbo.TrainingGuides g ON g.Id = ts.TrainingGuideId
INNER JOIN dbo.Areas a ON a.Id = g.AreaId
WHERE a.SerializationType = 0 AND ts.StepType = 5;

COMMIT TRANSACTION;
