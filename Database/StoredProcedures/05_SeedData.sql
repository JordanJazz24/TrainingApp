-- ============================================================
-- 05_SeedData.sql
-- DATOS DE EJEMPLO — NO USAR EN PRODUCCIÓN.
-- Este script existe únicamente para permitir correr el sistema
-- localmente sin catálogo real. El catálogo definitivo de cada
-- Area (pasos, pesos, NCCodes, DataCollectionParameters) se
-- construye en Fase 2 mediante auditoría del legacy, no antes.
-- Los valores de esta guía de "Redefinity" son aproximaciones
-- estructurales y NO representan el catálogo validado con negocio.
-- ============================================================
--
-- Ejecutar DESPUÉS de 00_Schema.sql.
-- Seguro para re-ejecución: usa INSERT solo si no existe la fila.
-- ============================================================

-- -------------------------------------------------------
-- Products
-- -------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.Products WHERE Name = 'Wires')
    INSERT INTO dbo.Products (Name) VALUES ('Wires');

IF NOT EXISTS (SELECT 1 FROM dbo.Products WHERE Name = 'Catéteres')
    INSERT INTO dbo.Products (Name) VALUES ('Catéteres');

IF NOT EXISTS (SELECT 1 FROM dbo.Products WHERE Name = 'Sensores')
    INSERT INTO dbo.Products (Name) VALUES ('Sensores');

-- -------------------------------------------------------
-- Areas (ejemplos; ajustar SerializationType, RequiresAssembly,
--         RequiresDataCollection según la configuración real)
-- SerializationType: 0=Serialized, 1=NonSerialized
-- -------------------------------------------------------
DECLARE @WiresId INT = (SELECT Id FROM dbo.Products WHERE Name = 'Wires');

IF NOT EXISTS (SELECT 1 FROM dbo.Areas WHERE Name = 'Redefinity')
    INSERT INTO dbo.Areas (ProductId, Name, SerializationType, RequiresAssembly, RequiresDataCollection)
    VALUES (@WiresId, 'Redefinity', 0, 1, 1);  -- Serialized, Assembly, DataCollection

IF NOT EXISTS (SELECT 1 FROM dbo.Areas WHERE Name = 'LoFlo')
    INSERT INTO dbo.Areas (ProductId, Name, SerializationType, RequiresAssembly, RequiresDataCollection)
    VALUES (@WiresId, 'LoFlo', 0, 0, 1);  -- Serialized, sin Assembly, con DataCollection

IF NOT EXISTS (SELECT 1 FROM dbo.Areas WHERE Name = 'Subensamble 1')
    INSERT INTO dbo.Areas (ProductId, Name, SerializationType, RequiresAssembly, RequiresDataCollection)
    VALUES (@WiresId, 'Subensamble 1', 1, 1, 0);  -- NonSerialized, Assembly, sin DataCollection

-- -------------------------------------------------------
-- TrainingGuide de ejemplo — Redefinity (flujo completo)
-- DATOS DE EJEMPLO — NO USAR EN PRODUCCIÓN.
-- Los pesos, pasos y catálogo de esta guía son estructuralmente
-- correctos (siguen la secuencia definida en CLAUDE.md) pero
-- NO han sido auditados contra el legacy ni validados con negocio.
-- La guía se crea con IsPublished = 0 (no visible para operadores).
-- -------------------------------------------------------
DECLARE @AreaId INT = (SELECT Id FROM dbo.Areas WHERE Name = 'Redefinity');

IF NOT EXISTS (SELECT 1 FROM dbo.TrainingGuides WHERE AreaId = @AreaId AND Title = 'Guía Redefinity — Flujo Completo')
BEGIN
    INSERT INTO dbo.TrainingGuides (AreaId, OperationId, Title, IsPublished, CreatedAt)
    VALUES (@AreaId, NULL, 'Guía Redefinity — Flujo Completo', 0, SYSUTCDATETIME());

    DECLARE @GuideId INT = SCOPE_IDENTITY();

    -- Apertura
    INSERT INTO dbo.TrainingSteps (TrainingGuideId, StepType, [Order], Weight) VALUES (@GuideId, 0, 1, 0.20); -- ResourceSetup
    INSERT INTO dbo.TrainingSteps (TrainingGuideId, StepType, [Order], Weight) VALUES (@GuideId, 1, 2, 0.10); -- LineClearance
    INSERT INTO dbo.TrainingSteps (TrainingGuideId, StepType, [Order], Weight) VALUES (@GuideId, 2, 3, 0.00); -- Search (secuencia pura)
    INSERT INTO dbo.TrainingSteps (TrainingGuideId, StepType, [Order], Weight) VALUES (@GuideId, 3, 4, 0.15); -- Assembly
    INSERT INTO dbo.TrainingSteps (TrainingGuideId, StepType, [Order], Weight) VALUES (@GuideId, 4, 5, 0.30); -- DataCollection
    -- Cierre
    INSERT INTO dbo.TrainingSteps (TrainingGuideId, StepType, [Order], Weight) VALUES (@GuideId, 1, 6, 0.00); -- LineClearance cierre
    INSERT INTO dbo.TrainingSteps (TrainingGuideId, StepType, [Order], Weight) VALUES (@GuideId, 5, 7, 0.15); -- LogNC
    INSERT INTO dbo.TrainingSteps (TrainingGuideId, StepType, [Order], Weight) VALUES (@GuideId, 6, 8, 0.10); -- Complete
END;

-- -------------------------------------------------------
-- NCCodes de ejemplo para Redefinity
-- -------------------------------------------------------
SET @AreaId = (SELECT Id FROM dbo.Areas WHERE Name = 'Redefinity');

IF NOT EXISTS (SELECT 1 FROM dbo.NCCodes WHERE AreaId = @AreaId AND Code = 'SC-001')
    INSERT INTO dbo.NCCodes (AreaId, Code, Description) VALUES (@AreaId, 'SC-001', 'Daño físico en componente');

IF NOT EXISTS (SELECT 1 FROM dbo.NCCodes WHERE AreaId = @AreaId AND Code = 'SC-002')
    INSERT INTO dbo.NCCodes (AreaId, Code, Description) VALUES (@AreaId, 'SC-002', 'Fallo de continuidad eléctrica');

IF NOT EXISTS (SELECT 1 FROM dbo.NCCodes WHERE AreaId = @AreaId AND Code = 'SC-003')
    INSERT INTO dbo.NCCodes (AreaId, Code, Description) VALUES (@AreaId, 'SC-003', 'Contaminación de superficie');

-- -------------------------------------------------------
-- DataCollectionParameters de ejemplo para Redefinity
-- -------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.DataCollectionParameters WHERE AreaId = @AreaId AND Name = 'Posición del detector')
    INSERT INTO dbo.DataCollectionParameters (AreaId, OperationId, Name, Unit, MinValue, MaxValue)
    VALUES (@AreaId, NULL, 'Posición del detector', 'mm', 10.0000, 15.0000);

IF NOT EXISTS (SELECT 1 FROM dbo.DataCollectionParameters WHERE AreaId = @AreaId AND Name = 'Temperatura de proceso')
    INSERT INTO dbo.DataCollectionParameters (AreaId, OperationId, Name, Unit, MinValue, MaxValue)
    VALUES (@AreaId, NULL, 'Temperatura de proceso', '°C', 22.0000, 26.0000);
