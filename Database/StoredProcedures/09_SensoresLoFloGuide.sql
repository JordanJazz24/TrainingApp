-- ============================================================
-- 09_SensoresLoFloGuide.sql — guía de Sensores / LoFlo.
-- Requiere: 00–04 y 08_SensoresLoFloCatalog.sql.
-- Re-ejecutable: sí. Crea la guía si falta y, si existe, actualiza
-- los pesos (spTrainingGuideSync). Aborta si no suman 1.00. Si cambió
-- la forma, crea una versión nueva y despublica la vieja (D-56).
--
-- Pesos: CLAUDE.md → "Ponderación de pasos". Serialized con Assembly
-- y sin SFC Split: la misma forma (y los mismos pesos) que Redefinity.
-- StepType: 0 ResourceSetup · 1 LineClearance · 2 Search · 3 Assembly
--           4 DataCollection · 5 SFC_Split · 6 LogNC (desecho rápido) · 7 Complete
--           8 Logout · 9 LogNCFastRepair · 10 LogNCExtendedRepair (spec 014)
--           11 MesLogin (Ingreso a MES, spec 016)
-- ============================================================
SET NOCOUNT ON;

DECLARE @AreaId INT = (SELECT a.Id FROM dbo.Areas a JOIN dbo.Products p ON p.Id = a.ProductId
                       WHERE p.Name = N'Sensores' AND a.Name = N'LoFlo');

DECLARE @Steps TABLE ([Order] INT, StepType TINYINT, Weight DECIMAL(5,2));
INSERT INTO @Steps ([Order], StepType, Weight) VALUES
    -- Apertura
    (1, 11, 0.05),  -- Ingreso a MES (spec 016: script, Repositorio, Hora a Hora, Recursos MES y login)
    (2, 0, 0.20),   -- Resource Setup
    (3, 1, 0.05),   -- Line Clearance (apertura)
    (4, 2, 0.05),   -- Search / Start (directo, sin checklist)
    (5, 3, 0.15),   -- Assembly
    (6, 4, 0.20),   -- Data Collection (obligatorios: EQUIPO y FUNCIONAL; spec 016: 0.25 → 0.20)
    -- Cierre
    (7, 1, 0.05),   -- Line Clearance (cierre)
    (8, 6, 0.05),   -- Log NC · desecho rápido
    (9, 9, 0.05),   -- Log NC · reparación rápida (spec 014)
    (10, 10, 0.05), -- Log NC · reparación extendida (spec 014)
    (11, 7, 0.05),  -- Complete (spec 015: cede 0.05 a Logout)
    (12, 8, 0.05);  -- Logout

DECLARE @Json NVARCHAR(MAX) = (SELECT [Order], StepType, Weight FROM @Steps ORDER BY [Order] FOR JSON PATH);
EXEC dbo.spTrainingGuideSync @AreaId = @AreaId, @Title = N'Guía Sensores LoFlo — Flujo Completo', @Steps = @Json;
