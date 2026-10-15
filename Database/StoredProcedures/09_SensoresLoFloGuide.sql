-- ============================================================
-- 09_SensoresLoFloGuide.sql — guía de Sensores / LoFlo.
-- Requiere: 00–04 y 08_SensoresLoFloCatalog.sql.
-- Re-ejecutable: sí. Crea la guía si falta y, si existe, actualiza
-- los pesos (spTrainingGuideSync). Aborta si no suman 1.00.
--
-- Pesos: CLAUDE.md → "Ponderación de pasos". Serialized con Assembly
-- y sin SFC Split: la misma forma (y los mismos pesos) que Redefinity.
-- StepType: 0 ResourceSetup · 1 LineClearance · 2 Search · 3 Assembly
--           4 DataCollection · 5 SFC_Split · 6 LogNC · 7 Complete · 8 Logout
-- ============================================================
SET NOCOUNT ON;

DECLARE @AreaId INT = (SELECT a.Id FROM dbo.Areas a JOIN dbo.Products p ON p.Id = a.ProductId
                       WHERE p.Name = N'Sensores' AND a.Name = N'LoFlo');

DECLARE @Steps TABLE ([Order] INT, StepType TINYINT, Weight DECIMAL(5,2));
INSERT INTO @Steps ([Order], StepType, Weight) VALUES
    -- Apertura
    (1, 0, 0.20),   -- Resource Setup
    (2, 1, 0.05),   -- Line Clearance (apertura)
    (3, 2, 0.05),   -- Search / Start (directo, sin checklist)
    (4, 3, 0.15),   -- Assembly
    (5, 4, 0.30),   -- Data Collection (obligatorios: EQUIPO y FUNCIONAL)
    -- Cierre
    (6, 1, 0.05),   -- Line Clearance (cierre)
    (7, 6, 0.10),   -- Log NC (con Component)
    (8, 7, 0.05),   -- Complete (spec 015: cede 0.05 a Logout)
    (9, 8, 0.05);   -- Logout

DECLARE @Json NVARCHAR(MAX) = (SELECT [Order], StepType, Weight FROM @Steps ORDER BY [Order] FOR JSON PATH);
EXEC dbo.spTrainingGuideSync @AreaId = @AreaId, @Title = N'Guía Sensores LoFlo — Flujo Completo', @Steps = @Json;
