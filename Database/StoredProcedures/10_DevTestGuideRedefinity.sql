-- ============================================================
-- 10_DevTestGuideRedefinity.sql
-- DATOS DE PRUEBA — NO USAR EN PRODUCCIÓN
--
-- Carga pasos a la guía de ejemplo de Redefinity (sin pasos hasta ahora) para
-- poder recorrer AssemblyControl en el navegador: ninguna guía real usa Assembly
-- todavía (spec 006, AC-9). Redefinity es un área de ejemplo de 05_SeedData.sql
-- (Serialized, RequiresAssembly=1, RequiresDataCollection=1) — no representa
-- una decisión de negocio.
--
-- Prerequisitos: 00_Schema.sql, 05_SeedData.sql
-- Seguro para re-ejecución: inserta pasos solo si la guía no tiene ninguno.
--
-- Flujo de pasos y pesos (sin SFC Split: el área es Serialized):
--   1. ResourceSetup      0.20
--   2. LineClearance      0.05  — apertura
--   3. Search             0.05
--   4. Assembly           0.15
--   5. DataCollection     0.30
--   6. LineClearance      0.05  — cierre
--   7. LogNC              0.10
--   8. Complete           0.10
--
-- Suma de pesos: 0.20+0.05+0.05+0.15+0.30+0.05+0.10+0.10 = 1.00
-- ============================================================

DECLARE @AreaId  INT = (SELECT Id FROM dbo.Areas WHERE Name = 'Redefinity');
DECLARE @GuideId INT = (
    SELECT Id FROM dbo.TrainingGuides
    WHERE AreaId = @AreaId AND Title = 'Guía Redefinity — Flujo Completo'
);

IF @GuideId IS NULL
    THROW 50001, 'No existe la guía de Redefinity: ejecutar 05_SeedData.sql primero.', 1;

IF NOT EXISTS (SELECT 1 FROM dbo.TrainingSteps WHERE TrainingGuideId = @GuideId)
BEGIN
    -- Apertura
    INSERT INTO dbo.TrainingSteps (TrainingGuideId, StepType, [Order], Weight) VALUES (@GuideId, 0, 1, 0.20); -- ResourceSetup
    INSERT INTO dbo.TrainingSteps (TrainingGuideId, StepType, [Order], Weight) VALUES (@GuideId, 1, 2, 0.05); -- LineClearance (apertura)
    INSERT INTO dbo.TrainingSteps (TrainingGuideId, StepType, [Order], Weight) VALUES (@GuideId, 2, 3, 0.05); -- Search / Start Order
    INSERT INTO dbo.TrainingSteps (TrainingGuideId, StepType, [Order], Weight) VALUES (@GuideId, 3, 4, 0.15); -- Assembly
    INSERT INTO dbo.TrainingSteps (TrainingGuideId, StepType, [Order], Weight) VALUES (@GuideId, 4, 5, 0.30); -- DataCollection
    -- Cierre
    INSERT INTO dbo.TrainingSteps (TrainingGuideId, StepType, [Order], Weight) VALUES (@GuideId, 1, 6, 0.05); -- LineClearance (cierre)
    INSERT INTO dbo.TrainingSteps (TrainingGuideId, StepType, [Order], Weight) VALUES (@GuideId, 6, 7, 0.10); -- LogNC
    INSERT INTO dbo.TrainingSteps (TrainingGuideId, StepType, [Order], Weight) VALUES (@GuideId, 7, 8, 0.10); -- Complete
END;

-- Publicada para que aparezca en StartSession.
UPDATE dbo.TrainingGuides SET IsPublished = 1 WHERE Id = @GuideId AND IsPublished = 0;
