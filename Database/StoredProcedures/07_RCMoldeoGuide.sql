-- ============================================================
-- 07_RCMoldeoGuide.sql — guía real de R&C / Moldeo.
-- Requiere: 00–04 y 06_RCMoldeoCatalog.sql.
-- Re-ejecutable: sí. Crea la guía si falta y, si existe, actualiza
-- los pesos (spTrainingGuideSync). Aborta si no suman 1.00.
--
-- Pesos: CLAUDE.md → "Ponderación de pasos". Search absorbe el peso
-- de Assembly (Moldeo no tiene) menos el de SFC Split (sí tiene).
-- StepType: 0 ResourceSetup · 1 LineClearance · 2 Search · 3 Assembly
--           4 DataCollection · 5 SFC_Split · 6 LogNC · 7 Complete
-- ============================================================
SET NOCOUNT ON;

DECLARE @AreaId INT = (SELECT a.Id FROM dbo.Areas a JOIN dbo.Products p ON p.Id = a.ProductId
                       WHERE p.Name = 'R&C' AND a.Name = 'Moldeo');

DECLARE @Steps TABLE ([Order] INT, StepType TINYINT, Weight DECIMAL(5,2));
INSERT INTO @Steps ([Order], StepType, Weight) VALUES
    -- Apertura
    (1, 0, 0.20),   -- Resource Setup
    (2, 1, 0.05),   -- Line Clearance (apertura)
    (3, 2, 0.15),   -- Search / Start (con checklist de 4 modales)
    (4, 4, 0.30),   -- Data Collection (obligatorio: DCGTM_FECHA_EXPIRACION_NIR/A)
    -- Cierre
    (5, 1, 0.05),   -- Line Clearance (cierre)
    (6, 5, 0.05),   -- SFC Split (NonSerialized)
    (7, 6, 0.10),   -- Log NC
    (8, 7, 0.10);   -- Complete

DECLARE @Json NVARCHAR(MAX) = (SELECT [Order], StepType, Weight FROM @Steps ORDER BY [Order] FOR JSON PATH);
EXEC dbo.spTrainingGuideSync @AreaId = @AreaId, @Title = N'Guía R&C Moldeo — Flujo Completo', @Steps = @Json;
