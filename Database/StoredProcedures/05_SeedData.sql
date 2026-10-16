-- ============================================================
-- 05_SeedData.sql — DATOS DE EJEMPLO. NO USAR EN PRODUCCIÓN.
--
-- Solo para correr y probar el sistema en desarrollo. Las áreas son
-- reales (CLAUDE.md → "Jerarquía"), pero sus flags, la guía de
-- Redefinity y sus catálogos son aproximaciones y NO representan
-- decisiones de negocio. El catálogo real de cada área va en su
-- propio script (ej. 06_RCMoldeoCatalog.sql).
--
--   · Redefinity (Catéteres): Serialized con Assembly y DC, para probar
--     ese camino de punta a punta (spec 008). Guía publicada, 3 códigos
--     NC, 2 grupos DC obligatorios + 1 opcional y un BOM simulado.
--   · Subensamble 1 (Wires): solo el área, sin guía.
--   LoFlo (Sensores) ya no está aquí: su catálogo y sus flags los
--   cargan 08 y 09 (Fase 7).
--
-- Requiere: 00–04.
-- Re-ejecutable: sí. Las áreas solo se insertan si faltan; el
-- catálogo y la guía de Redefinity se actualizan (pesos validados
-- por spTrainingGuideSync).
-- ============================================================
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;

-- ------------------------------------------------------------
-- Products y Areas de ejemplo
-- SerializationType: 0 = Serialized, 1 = NonSerialized
-- ------------------------------------------------------------
INSERT INTO dbo.Products (Name)
SELECT s.Name
FROM   (VALUES (1, N'Wires'), (2, N'Catéteres')) AS s (Seq, Name)
WHERE  NOT EXISTS (SELECT 1 FROM dbo.Products p WHERE p.Name = s.Name)
ORDER  BY s.Seq;

INSERT INTO dbo.Areas (ProductId, Name, SerializationType, RequiresAssembly,
                       RequiresDataCollection, RequiresNCComponent, RequiresStartChecklist)
SELECT p.Id, s.Area, s.Ser, s.Asm, s.Dc, 1, 0
FROM   (VALUES (1, N'Catéteres', N'Redefinity',    0, 1, 1),   -- Serialized, Assembly, DC
               (2, N'Wires',     N'Subensamble 1', 1, 1, 0))   -- NonSerialized, Assembly, sin DC
       AS s (Seq, Product, Area, Ser, Asm, Dc)
JOIN   dbo.Products p ON p.Name = s.Product
WHERE  NOT EXISTS (SELECT 1 FROM dbo.Areas a WHERE a.Name = s.Area)
ORDER  BY s.Seq;

DECLARE @AreaId INT = (SELECT Id FROM dbo.Areas WHERE Name = 'Redefinity');

-- ------------------------------------------------------------
-- Guía de Redefinity (sin SFC Split: el área es Serialized)
-- StepType: 0 ResourceSetup · 1 LineClearance · 2 Search · 3 Assembly
--           4 DataCollection · 5 SFC_Split · 6 LogNC · 7 Complete · 8 Logout
-- ------------------------------------------------------------
DECLARE @Steps TABLE ([Order] INT, StepType TINYINT, Weight DECIMAL(5,2));
INSERT INTO @Steps ([Order], StepType, Weight) VALUES
    (1, 0, 0.20),   -- Resource Setup
    (2, 1, 0.05),   -- Line Clearance (apertura)
    (3, 2, 0.05),   -- Search / Start (directo, sin checklist)
    (4, 3, 0.15),   -- Assembly
    (5, 4, 0.30),   -- Data Collection
    (6, 1, 0.05),   -- Line Clearance (cierre)
    (7, 6, 0.10),   -- Log NC
    (8, 7, 0.05),   -- Complete (spec 015: cede 0.05 a Logout)
    (9, 8, 0.05);   -- Logout

DECLARE @Json NVARCHAR(MAX) = (SELECT [Order], StepType, Weight FROM @Steps ORDER BY [Order] FOR JSON PATH);
EXEC dbo.spTrainingGuideSync @AreaId = @AreaId, @Title = N'Guía Redefinity — Flujo Completo', @Steps = @Json;

-- ------------------------------------------------------------
-- NCCodes de ejemplo. Method (spec 014): 0 desecho rápido, 1 reparación
-- rápida (SIM_FR), 2 extendida (SIM_FX); SIM_FX_GENERAL exige Comments.
-- ------------------------------------------------------------
DECLARE @Nc TABLE (Seq INT IDENTITY, Code NVARCHAR(50), Description NVARCHAR(200),
                   Method TINYINT, RequiresComments BIT);
INSERT INTO @Nc (Code, Description, Method, RequiresComments) VALUES
    (N'SC-001',          N'Daño físico en componente',      0, 0),
    (N'SC-002',          N'Fallo de continuidad eléctrica', 0, 0),
    (N'SC-003',          N'Contaminación de superficie',    0, 0),
    (N'SIM_FR_HUB',      N'Hub dañado',                     1, 0),
    (N'SIM_FR_LUER',     N'Conector luer flojo',            1, 0),
    (N'SIM_FX_GENERAL',  N'Reparación extendida general',   2, 1),
    (N'SIM_FX_PUNTA',    N'Punta deformada',                2, 0);

UPDATE t SET Description = s.Description, Method = s.Method, RequiresComments = s.RequiresComments
FROM   dbo.NCCodes t JOIN @Nc s ON s.Code = t.Code
WHERE  t.AreaId = @AreaId;

INSERT INTO dbo.NCCodes (AreaId, Code, Description, Method, RequiresComments)
SELECT @AreaId, s.Code, s.Description, s.Method, s.RequiresComments
FROM   @Nc s
WHERE  NOT EXISTS (SELECT 1 FROM dbo.NCCodes t WHERE t.AreaId = @AreaId AND t.Code = s.Code)
ORDER  BY s.Seq;

-- ------------------------------------------------------------
-- DataCollectionGroups de ejemplo: 2 obligatorios + el grupo Trainer
-- opcional (real, spec 015), para probar la regla de completitud de DC
-- por pieza (spec 008).
-- ------------------------------------------------------------
-- Spec 015 (D-23): el grupo Trainer reemplaza al opcional
-- DCGTM_CONDICIONES_AMBIENTE/A. Una BD anterior lo renombra en sitio (el Id
-- no cambia) y su parámetro pasa a ser el del nombre; si quedara otro, se
-- borra después de los parámetros: excepción a "nunca borra filas", nada
-- referencia grupos ni parámetros de DC.
IF EXISTS (SELECT 1 FROM dbo.DataCollectionGroups WHERE AreaId = @AreaId AND Code = N'DCGTM_CONDICIONES_AMBIENTE/A')
   AND NOT EXISTS (SELECT 1 FROM dbo.DataCollectionGroups WHERE AreaId = @AreaId AND Code = N'DCGTM_TRAINEE/A')
    UPDATE dbo.DataCollectionGroups SET Code = N'DCGTM_TRAINEE/A'
    WHERE  AreaId = @AreaId AND Code = N'DCGTM_CONDICIONES_AMBIENTE/A';

IF NOT EXISTS (SELECT 1 FROM dbo.DataCollectionParameters WHERE AreaId = @AreaId AND Name = N'NOMBRE_EN_ENTRENAMIENTO')
    UPDATE dbo.DataCollectionParameters SET Name = N'NOMBRE_EN_ENTRENAMIENTO'
    WHERE  AreaId = @AreaId AND Name = N'Humedad relativa';

DECLARE @Grp TABLE (Seq INT IDENTITY, Code NVARCHAR(100), Description NVARCHAR(200),
                    CollectDataAt NVARCHAR(50), IsRequired BIT, DisplayOrder INT);
INSERT INTO @Grp (Code, Description, CollectDataAt, IsRequired, DisplayOrder) VALUES
    (N'DCGTM_GENERAL/A',              N'Data Collection General',      'Anytime',    1, 1),
    (N'DCGTM_DETECTOR_BDR/A',         N'Verificación del detector BDR', 'Post-Start', 1, 2),
    (N'DCGTM_TRAINEE/A',              N'Entrenamiento personal',       'Anytime',    0, 3);

UPDATE t
SET    Description = s.Description, CollectDataAt = s.CollectDataAt,
       IsRequired = s.IsRequired, DisplayOrder = s.DisplayOrder
FROM   dbo.DataCollectionGroups t JOIN @Grp s ON s.Code = t.Code
WHERE  t.AreaId = @AreaId;

INSERT INTO dbo.DataCollectionGroups (AreaId, OperationId, Code, Description, CollectDataAt, IsRequired, DisplayOrder)
SELECT @AreaId, NULL, s.Code, s.Description, s.CollectDataAt, s.IsRequired, s.DisplayOrder
FROM   @Grp s
WHERE  NOT EXISTS (SELECT 1 FROM dbo.DataCollectionGroups t WHERE t.AreaId = @AreaId AND t.Code = s.Code)
ORDER  BY s.Seq;

-- ------------------------------------------------------------
-- DataCollectionParameters de ejemplo (Description: la línea
-- "Description: …" de Data Collection Entry)
-- ------------------------------------------------------------
DECLARE @Prm TABLE (Seq INT IDENTITY, GroupCode NVARCHAR(100), Name NVARCHAR(200), Description NVARCHAR(200),
                    DataType NVARCHAR(20), Unit NVARCHAR(50), MinValue DECIMAL(10,4), MaxValue DECIMAL(10,4),
                    DisplayOrder INT);
INSERT INTO @Prm (GroupCode, Name, Description, DataType, Unit, MinValue, MaxValue, DisplayOrder) VALUES
    (N'DCGTM_GENERAL/A',              N'Posición del detector',       N'Detector position',          'Numeric', 'mm',  10,  15, 1),
    (N'DCGTM_GENERAL/A',              N'Temperatura de proceso',      N'Process temperature',        'Numeric', N'°C', 22,  26, 2),
    (N'DCGTM_DETECTOR_BDR/A',         N'Resistencia del detector',    N'Detector resistance',        'Numeric', N'Ω',  90, 110, 3),
    (N'DCGTM_DETECTOR_BDR/A',         N'¿Detector sin daño visible?', N'Detector visual inspection', 'Boolean', 'N/A', NULL, NULL, 4),
    (N'DCGTM_TRAINEE/A',              N'NOMBRE_EN_ENTRENAMIENTO',     N'SIM: nombre de la persona en entrenamiento', 'PersonName', 'N/A', NULL, NULL, 5);

UPDATE t
SET    DataCollectionGroupId = g.Id, Description = s.Description, DataType = s.DataType, Unit = s.Unit,
       MinValue = s.MinValue, MaxValue = s.MaxValue, DisplayOrder = s.DisplayOrder
FROM   dbo.DataCollectionParameters t
JOIN   @Prm s ON s.Name = t.Name
JOIN   dbo.DataCollectionGroups g ON g.AreaId = @AreaId AND g.Code = s.GroupCode
WHERE  t.AreaId = @AreaId;

INSERT INTO dbo.DataCollectionParameters (AreaId, OperationId, DataCollectionGroupId, Name, Description,
                                          DataType, Unit, MinValue, MaxValue, DisplayOrder)
SELECT @AreaId, NULL, g.Id, s.Name, s.Description, s.DataType, s.Unit, s.MinValue, s.MaxValue, s.DisplayOrder
FROM   @Prm s
JOIN   dbo.DataCollectionGroups g ON g.AreaId = @AreaId AND g.Code = s.GroupCode
WHERE  NOT EXISTS (SELECT 1 FROM dbo.DataCollectionParameters t WHERE t.AreaId = @AreaId AND t.Name = s.Name)
ORDER  BY s.Seq;

-- Spec 015 (D-23): el grupo Trainer tiene solo el parámetro del nombre.
DELETE p
FROM   dbo.DataCollectionParameters p
JOIN   dbo.DataCollectionGroups g ON g.Id = p.DataCollectionGroupId
WHERE  g.AreaId = @AreaId AND g.Code = N'DCGTM_TRAINEE/A' AND p.Name <> N'NOMBRE_EN_ENTRENAMIENTO';

-- ------------------------------------------------------------
-- AssemblyComponents: BOM simulado (prefijo SIM-). Uno con Qty
-- decimal, como los materiales que se consumen por peso o longitud.
-- ------------------------------------------------------------
DECLARE @Bom TABLE (Seq INT IDENTITY, PartNumber NVARCHAR(50), Revision NVARCHAR(10),
                    Description NVARCHAR(200), Qty DECIMAL(18,6), DisplayOrder INT);
INSERT INTO @Bom (PartNumber, Revision, Description, Qty, DisplayOrder) VALUES
    (N'SIM-100001', N'A', N'INNER MEMBER SIMULADO W/ MARKER BANDS', 1,      1),
    (N'SIM-100002', N'A', N'ADHESIVO UV SIMULADO',                  0.0001, 2);

UPDATE t SET Description = s.Description, Qty = s.Qty, DisplayOrder = s.DisplayOrder
FROM   dbo.AssemblyComponents t JOIN @Bom s ON s.PartNumber = t.PartNumber AND s.Revision = t.Revision
WHERE  t.AreaId = @AreaId;

INSERT INTO dbo.AssemblyComponents (AreaId, OperationId, PartNumber, Revision, Description, Qty, DisplayOrder)
SELECT @AreaId, NULL, s.PartNumber, s.Revision, s.Description, s.Qty, s.DisplayOrder
FROM   @Bom s
WHERE  NOT EXISTS (SELECT 1 FROM dbo.AssemblyComponents t
                   WHERE t.AreaId = @AreaId AND t.PartNumber = s.PartNumber AND t.Revision = s.Revision)
ORDER  BY s.Seq;

COMMIT TRANSACTION;
PRINT N'Datos de ejemplo cargados.';
