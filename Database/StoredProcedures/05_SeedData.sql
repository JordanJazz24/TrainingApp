-- ============================================================
-- 05_SeedData.sql
-- DATOS DE EJEMPLO — NO USAR EN PRODUCCIÓN.
-- Este script existe únicamente para correr y probar el sistema
-- localmente. Las áreas existen en la estructura oficial (CLAUDE.md →
-- Jerarquía), pero sus flags, la guía de "Redefinity" y sus catálogos son
-- aproximaciones estructurales y NO representan el catálogo validado con negocio.
-- El catálogo real de cada Area vive en su propio script
-- (ej. 06_RCMoldeoCatalog.sql).
--
-- Redefinity (Serialized, con Assembly y Data Collection) existe para
-- recorrer AssemblyControl en el navegador: ninguna guía real usa
-- Assembly todavía (spec 006).
--
-- LoFlo: flags provisorios; los reales se confirman con negocio en
-- la Fase 7 (su catálogo real se carga en un script propio).
--
-- Prerequisitos: 00_Schema.sql
-- Seguro para re-ejecución: INSERT solo si no existe la fila.
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
-- Areas de ejemplo
-- SerializationType: 0=Serialized, 1=NonSerialized
-- -------------------------------------------------------
DECLARE @WiresId     INT = (SELECT Id FROM dbo.Products WHERE Name = 'Wires');
DECLARE @CateteresId INT = (SELECT Id FROM dbo.Products WHERE Name = 'Catéteres');
DECLARE @SensoresId  INT = (SELECT Id FROM dbo.Products WHERE Name = 'Sensores');

IF NOT EXISTS (SELECT 1 FROM dbo.Areas WHERE Name = 'Redefinity')
    INSERT INTO dbo.Areas (ProductId, Name, SerializationType, RequiresAssembly, RequiresDataCollection)
    VALUES (@CateteresId, 'Redefinity', 0, 1, 1);  -- Serialized, Assembly, DataCollection

IF NOT EXISTS (SELECT 1 FROM dbo.Areas WHERE Name = 'LoFlo')
    INSERT INTO dbo.Areas (ProductId, Name, SerializationType, RequiresAssembly, RequiresDataCollection)
    VALUES (@SensoresId, 'LoFlo', 0, 0, 1);  -- Serialized, sin Assembly, con DataCollection (provisorio)

IF NOT EXISTS (SELECT 1 FROM dbo.Areas WHERE Name = 'Subensamble 1')
    INSERT INTO dbo.Areas (ProductId, Name, SerializationType, RequiresAssembly, RequiresDataCollection)
    VALUES (@WiresId, 'Subensamble 1', 1, 1, 0);  -- NonSerialized, Assembly, sin DataCollection

-- -------------------------------------------------------
-- TrainingGuide de ejemplo — Redefinity (publicada)
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
-- -------------------------------------------------------
DECLARE @AreaId INT = (SELECT Id FROM dbo.Areas WHERE Name = 'Redefinity');

IF NOT EXISTS (SELECT 1 FROM dbo.TrainingGuides WHERE AreaId = @AreaId AND Title = 'Guía Redefinity — Flujo Completo')
    INSERT INTO dbo.TrainingGuides (AreaId, OperationId, Title, IsPublished, CreatedAt)
    VALUES (@AreaId, NULL, 'Guía Redefinity — Flujo Completo', 1, SYSUTCDATETIME());

DECLARE @GuideId INT = (
    SELECT Id FROM dbo.TrainingGuides
    WHERE AreaId = @AreaId AND Title = 'Guía Redefinity — Flujo Completo'
);

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

-- -------------------------------------------------------
-- NCCodes de ejemplo para Redefinity
-- -------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.NCCodes WHERE AreaId = @AreaId AND Code = 'SC-001')
    INSERT INTO dbo.NCCodes (AreaId, Code, Description) VALUES (@AreaId, 'SC-001', 'Daño físico en componente');

IF NOT EXISTS (SELECT 1 FROM dbo.NCCodes WHERE AreaId = @AreaId AND Code = 'SC-002')
    INSERT INTO dbo.NCCodes (AreaId, Code, Description) VALUES (@AreaId, 'SC-002', 'Fallo de continuidad eléctrica');

IF NOT EXISTS (SELECT 1 FROM dbo.NCCodes WHERE AreaId = @AreaId AND Code = 'SC-003')
    INSERT INTO dbo.NCCodes (AreaId, Code, Description) VALUES (@AreaId, 'SC-003', 'Contaminación de superficie');

-- -------------------------------------------------------
-- DataCollectionGroups de ejemplo para Redefinity (spec 008):
-- 2 obligatorios + 1 opcional, para probar la regla de completitud
-- de DC por pieza en el navegador.
-- -------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.DataCollectionGroups WHERE AreaId = @AreaId AND Code = 'DCGTM_GENERAL/A')
    INSERT INTO dbo.DataCollectionGroups (AreaId, OperationId, Code, Description, CollectDataAt, IsRequired, DisplayOrder)
    VALUES (@AreaId, NULL, 'DCGTM_GENERAL/A', 'Data Collection General', 'Anytime', 1, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.DataCollectionGroups WHERE AreaId = @AreaId AND Code = 'DCGTM_DETECTOR_BDR/A')
    INSERT INTO dbo.DataCollectionGroups (AreaId, OperationId, Code, Description, CollectDataAt, IsRequired, DisplayOrder)
    VALUES (@AreaId, NULL, 'DCGTM_DETECTOR_BDR/A', 'Verificación del detector BDR', 'Post-Start', 1, 2);

IF NOT EXISTS (SELECT 1 FROM dbo.DataCollectionGroups WHERE AreaId = @AreaId AND Code = 'DCGTM_CONDICIONES_AMBIENTE/A')
    INSERT INTO dbo.DataCollectionGroups (AreaId, OperationId, Code, Description, CollectDataAt, IsRequired, DisplayOrder)
    VALUES (@AreaId, NULL, 'DCGTM_CONDICIONES_AMBIENTE/A', 'Condiciones de ambiente', 'Anytime', 0, 3);

-- Qué grupo es obligatorio lo define este script, también en una BD ya existente.
UPDATE dbo.DataCollectionGroups
SET    IsRequired = CASE WHEN Code IN ('DCGTM_GENERAL/A', 'DCGTM_DETECTOR_BDR/A') THEN 1 ELSE 0 END
WHERE  AreaId = @AreaId;

DECLARE @GrpGeneral  INT = (SELECT Id FROM dbo.DataCollectionGroups WHERE AreaId = @AreaId AND Code = 'DCGTM_GENERAL/A');
DECLARE @GrpDetector INT = (SELECT Id FROM dbo.DataCollectionGroups WHERE AreaId = @AreaId AND Code = 'DCGTM_DETECTOR_BDR/A');
DECLARE @GrpAmbiente INT = (SELECT Id FROM dbo.DataCollectionGroups WHERE AreaId = @AreaId AND Code = 'DCGTM_CONDICIONES_AMBIENTE/A');

-- -------------------------------------------------------
-- DataCollectionParameters de ejemplo para Redefinity
-- -------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.DataCollectionParameters WHERE AreaId = @AreaId AND Name = 'Posición del detector')
    INSERT INTO dbo.DataCollectionParameters (AreaId, OperationId, Name, DataType, Unit, MinValue, MaxValue, DisplayOrder, DataCollectionGroupId)
    VALUES (@AreaId, NULL, 'Posición del detector', 'Numeric', 'mm', 10.0000, 15.0000, 1, @GrpGeneral);

IF NOT EXISTS (SELECT 1 FROM dbo.DataCollectionParameters WHERE AreaId = @AreaId AND Name = 'Temperatura de proceso')
    INSERT INTO dbo.DataCollectionParameters (AreaId, OperationId, Name, DataType, Unit, MinValue, MaxValue, DisplayOrder, DataCollectionGroupId)
    VALUES (@AreaId, NULL, 'Temperatura de proceso', 'Numeric', '°C', 22.0000, 26.0000, 2, @GrpGeneral);

IF NOT EXISTS (SELECT 1 FROM dbo.DataCollectionParameters WHERE AreaId = @AreaId AND Name = 'Resistencia del detector')
    INSERT INTO dbo.DataCollectionParameters (AreaId, OperationId, Name, DataType, Unit, MinValue, MaxValue, DisplayOrder, DataCollectionGroupId)
    VALUES (@AreaId, NULL, 'Resistencia del detector', 'Numeric', 'Ω', 90.0000, 110.0000, 3, @GrpDetector);

IF NOT EXISTS (SELECT 1 FROM dbo.DataCollectionParameters WHERE AreaId = @AreaId AND Name = '¿Detector sin daño visible?')
    INSERT INTO dbo.DataCollectionParameters (AreaId, OperationId, Name, DataType, Unit, MinValue, MaxValue, DisplayOrder, DataCollectionGroupId)
    VALUES (@AreaId, NULL, '¿Detector sin daño visible?', 'Boolean', 'N/A', NULL, NULL, 4, @GrpDetector);

IF NOT EXISTS (SELECT 1 FROM dbo.DataCollectionParameters WHERE AreaId = @AreaId AND Name = 'Humedad relativa')
    INSERT INTO dbo.DataCollectionParameters (AreaId, OperationId, Name, DataType, Unit, MinValue, MaxValue, DisplayOrder, DataCollectionGroupId)
    VALUES (@AreaId, NULL, 'Humedad relativa', 'Numeric', '%', 30.0000, 60.0000, 5, @GrpAmbiente);

-- -------------------------------------------------------
-- AssemblyComponents de ejemplo para Redefinity (BOM simulado, spec 008).
-- Números de parte ficticios (prefijo SIM-). Uno con Qty decimal para
-- probar materiales que se consumen por peso o longitud.
-- -------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.AssemblyComponents WHERE AreaId = @AreaId AND PartNumber = 'SIM-100001' AND Revision = 'A')
    INSERT INTO dbo.AssemblyComponents (AreaId, OperationId, PartNumber, Revision, Description, Qty, DisplayOrder)
    VALUES (@AreaId, NULL, 'SIM-100001', 'A', 'INNER MEMBER SIMULADO W/ MARKER BANDS', 1, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.AssemblyComponents WHERE AreaId = @AreaId AND PartNumber = 'SIM-100002' AND Revision = 'A')
    INSERT INTO dbo.AssemblyComponents (AreaId, OperationId, PartNumber, Revision, Description, Qty, DisplayOrder)
    VALUES (@AreaId, NULL, 'SIM-100002', 'A', 'ADHESIVO UV SIMULADO', 0.0001, 2);
