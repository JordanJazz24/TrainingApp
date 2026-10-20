-- ============================================================
-- 07_RCMoldeoGuide.sql — guía real de S&RC / Moldeo.
-- Requiere: 00–04 y 06_RCMoldeoCatalog.sql.
-- Re-ejecutable: sí. Crea la guía si falta y, si existe, actualiza
-- los pesos (spTrainingGuideSync). Aborta si no suman 1.00.
--
-- Pesos: CLAUDE.md → "Ponderación de pasos". Search absorbe el peso
-- de Assembly (Moldeo no tiene) menos el de SFC Split (sí tiene).
-- StepType: 0 ResourceSetup · 1 LineClearance · 2 Search · 3 Assembly
--           4 DataCollection · 5 SFC_Split · 6 LogNC · 7 Complete · 8 Logout
--           11 MesLogin (Ingreso a MES, spec 016)
-- ============================================================
SET NOCOUNT ON;

DECLARE @AreaId INT = (SELECT a.Id FROM dbo.Areas a JOIN dbo.Products p ON p.Id = a.ProductId
                       WHERE p.Name = 'S&RC' AND a.Name = 'Moldeo');

-- El título anterior (con 'R&C', hasta el 2026-10-14) se renombra en sitio: spTrainingGuideSync
-- busca la guía por título y, si no, crearía otra.
UPDATE dbo.TrainingGuides SET Title = N'Guía S&RC Moldeo — Flujo Completo'
WHERE AreaId = @AreaId AND Title = N'Guía R&C Moldeo — Flujo Completo'
  AND NOT EXISTS (SELECT 1 FROM dbo.TrainingGuides WHERE AreaId = @AreaId AND Title = N'Guía S&RC Moldeo — Flujo Completo');

DECLARE @Steps TABLE ([Order] INT, StepType TINYINT, Weight DECIMAL(5,2));
INSERT INTO @Steps ([Order], StepType, Weight) VALUES
    -- Apertura
    (1, 11, 0.05),  -- Ingreso a MES (spec 016: script, Repositorio, Hora a Hora, Recursos MES y login)
    (2, 0, 0.20),   -- Resource Setup
    (3, 1, 0.05),   -- Line Clearance (apertura)
    (4, 2, 0.15),   -- Search / Start (con checklist de 4 modales)
    (5, 4, 0.25),   -- Data Collection (obligatorio: DCGTM_FECHA_EXPIRACION_NIR/A; spec 016: 0.30 → 0.25)
    -- Cierre
    (6, 1, 0.05),   -- Line Clearance (cierre)
    (7, 5, 0.05),   -- SFC Split (NonSerialized)
    (8, 6, 0.10),   -- Log NC
    (9, 7, 0.05),   -- Complete (spec 015: cede 0.05 a Logout)
    (10, 8, 0.05);  -- Logout

DECLARE @Json NVARCHAR(MAX) = (SELECT [Order], StepType, Weight FROM @Steps ORDER BY [Order] FOR JSON PATH);
EXEC dbo.spTrainingGuideSync @AreaId = @AreaId, @Title = N'Guía S&RC Moldeo — Flujo Completo', @Steps = @Json;
