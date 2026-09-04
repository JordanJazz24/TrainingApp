-- ============================================================
-- 07_RCMoldeoGuide.sql
-- Guía de entrenamiento real para R&C / Moldeo.
--
-- Prerequisitos: 00_Schema.sql, 06_RCMoldeoCatalog.sql
-- Seguro para re-ejecución: INSERT solo si no existe.
--
-- Flujo de pasos y pesos:
--   1. ResourceSetup      0.20  — configuración previa al inicio
--   2. LineClearance      0.04  — LC estándar de apertura (parte del 10% LC total)
--   3. Search             0.15  — navegación al Start Order; hereda el peso de
--                                  Assembly (no aplica a Moldeo: RequiresAssembly=false)
--   4. LineClearance      0.03  — LC Moldeo: 3 preguntas booleanas dentro del
--                                  Start wizard (DCGTM_LC_MOLDEO/A, hardcodeadas
--                                  en StepValidator por área según Fase 3)
--   5. DataCollection     0.30  — 15 parámetros del catálogo Moldeo
--   6. LineClearance      0.03  — LC de cierre
--   7. LogNC              0.15  — selección de NC code del catálogo Moldeo
--   8. Complete           0.10  — cierre del proceso
--
-- Suma de pesos: 0.20+0.04+0.15+0.03+0.30+0.03+0.15+0.10 = 1.00
-- ============================================================

DECLARE @RCId   INT = (SELECT Id FROM dbo.Products WHERE Name = 'R&C');
DECLARE @AreaId INT = (SELECT Id FROM dbo.Areas   WHERE Name = 'Moldeo' AND ProductId = @RCId);

IF NOT EXISTS (
    SELECT 1 FROM dbo.TrainingGuides
    WHERE AreaId = @AreaId AND Title = 'Guía R&C Moldeo — Flujo Completo'
)
BEGIN
    INSERT INTO dbo.TrainingGuides (AreaId, OperationId, Title, IsPublished, CreatedAt)
    VALUES (@AreaId, NULL, 'Guía R&C Moldeo — Flujo Completo', 1, SYSUTCDATETIME());

    DECLARE @GuideId INT = SCOPE_IDENTITY();

    -- Apertura
    INSERT INTO dbo.TrainingSteps (TrainingGuideId, StepType, [Order], Weight) VALUES (@GuideId, 0, 1, 0.20); -- ResourceSetup
    INSERT INTO dbo.TrainingSteps (TrainingGuideId, StepType, [Order], Weight) VALUES (@GuideId, 1, 2, 0.04); -- LineClearance (apertura)
    INSERT INTO dbo.TrainingSteps (TrainingGuideId, StepType, [Order], Weight) VALUES (@GuideId, 2, 3, 0.15); -- Search / Start Order
    INSERT INTO dbo.TrainingSteps (TrainingGuideId, StepType, [Order], Weight) VALUES (@GuideId, 1, 4, 0.03); -- LineClearance (LC Moldeo — dentro del Start)
    INSERT INTO dbo.TrainingSteps (TrainingGuideId, StepType, [Order], Weight) VALUES (@GuideId, 4, 5, 0.30); -- DataCollection
    -- Cierre
    INSERT INTO dbo.TrainingSteps (TrainingGuideId, StepType, [Order], Weight) VALUES (@GuideId, 1, 6, 0.03); -- LineClearance (cierre)
    INSERT INTO dbo.TrainingSteps (TrainingGuideId, StepType, [Order], Weight) VALUES (@GuideId, 5, 7, 0.15); -- LogNC
    INSERT INTO dbo.TrainingSteps (TrainingGuideId, StepType, [Order], Weight) VALUES (@GuideId, 6, 8, 0.10); -- Complete
END;
