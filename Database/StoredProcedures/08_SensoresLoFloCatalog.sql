-- ============================================================
-- 08_SensoresLoFloCatalog.sql — catálogo de Sensores / LoFlo:
-- Product, Area (flags), NCCodes, Operations, DataCollectionGroups,
-- DataCollectionParameters y AssemblyComponents. La guía está en
-- 09_SensoresLoFloGuide.sql.
-- Requiere: 00_Schema.sql.
-- Re-ejecutable: sí. Este script es la fuente de verdad del catálogo
-- de LoFlo: agrega lo que falta y actualiza lo que existe (no borra
-- filas: los intentos y las sesiones pueden apuntar a ellas; las
-- excepciones, donde nada las referencia, están comentadas).
--
-- Fuente: flags confirmados con negocio el 2026-10-07 para todas las
-- áreas de Sensores. Los códigos NC de desecho, los DC Groups con sus
-- parámetros y el BOM son SIMULADOS (marcados con SIM); los de
-- reparación son los generales reales (enmienda 5). Cuando llegue el catálogo
-- real, las filas simuladas no se borran solas (el script no borra);
-- se retiran a mano si ninguna sesión las usa.
-- ============================================================
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;

-- ------------------------------------------------------------
-- Product y Area
--   SerializationType      0 = Serialized (varios SFC por orden)
--   RequiresAssembly       1
--   RequiresDataCollection 1
--   RequiresStartChecklist 0 — Start directo
-- ------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.Products WHERE Name = N'Sensores')
    INSERT INTO dbo.Products (Name) VALUES (N'Sensores');

DECLARE @ProductId INT = (SELECT Id FROM dbo.Products WHERE Name = N'Sensores');

IF NOT EXISTS (SELECT 1 FROM dbo.Areas WHERE ProductId = @ProductId AND Name = N'LoFlo')
    INSERT INTO dbo.Areas (ProductId, Name, SerializationType, RequiresAssembly,
                           RequiresDataCollection, RequiresStartChecklist)
    VALUES (@ProductId, N'LoFlo', 0, 1, 1, 0);
ELSE
    UPDATE dbo.Areas
    SET    SerializationType = 0, RequiresAssembly = 1, RequiresDataCollection = 1,
           RequiresStartChecklist = 0
    WHERE  ProductId = @ProductId AND Name = N'LoFlo';

DECLARE @AreaId INT = (SELECT Id FROM dbo.Areas WHERE ProductId = @ProductId AND Name = N'LoFlo');

-- ------------------------------------------------------------
-- NCCodes. Method (spec 014): 0 desecho rápido, 1 reparación rápida,
-- 2 extendida. Desecho: SIMULADOS (prefijo SIM_FS_), uno por defecto.
-- Reparaciones: los códigos generales REALES de todas las áreas
-- serializadas, como en 06 (enmienda 5): F_FR_GENERAL y F_FX_GENERAL,
-- que exige Comments (MPI y revisión, o NCR).
-- ------------------------------------------------------------
DECLARE @Nc TABLE (Seq INT IDENTITY, Code NVARCHAR(50), Description NVARCHAR(200),
                   Method TINYINT, RequiresComments BIT);
INSERT INTO @Nc (Code, Description, Method, RequiresComments) VALUES
    (N'SIM_FS_FUGA',           N'Fuga en la línea de muestreo',              0, 0),
    (N'SIM_FS_FLUJO',          N'Flujo de muestreo fuera de especificación', 0, 0),
    (N'SIM_FS_LECTURA_CO2',    N'Lectura de CO2 fuera de especificación',    0, 0),
    (N'SIM_FS_CONECTOR',       N'Conector dañado',                           0, 0),
    (N'SIM_FS_CONTAMINACION',  N'Contaminación del módulo',                  0, 0),
    (N'SIM_FS_ELECT_FAIL',     N'Fallo eléctrico',                           0, 0),
    (N'F_FR_GENERAL',          N'Fast Repair General',                       1, 0),
    (N'F_FX_GENERAL',          N'Fast Extended Repair General',              2, 1);

UPDATE t SET Description = s.Description, Method = s.Method, RequiresComments = s.RequiresComments
FROM   dbo.NCCodes t JOIN @Nc s ON s.Code = t.Code
WHERE  t.AreaId = @AreaId;

INSERT INTO dbo.NCCodes (AreaId, Code, Description, Method, RequiresComments)
SELECT @AreaId, s.Code, s.Description, s.Method, s.RequiresComments
FROM   @Nc s
WHERE  NOT EXISTS (SELECT 1 FROM dbo.NCCodes t WHERE t.AreaId = @AreaId AND t.Code = s.Code)
ORDER  BY s.Seq;

-- Enmienda 5 de la spec 014: las reparaciones usan los códigos generales, así que se
-- borran los SIM_FR_ y SIM_FX_ por defecto de una BD anterior. Excepción a "nunca borra
-- filas" (como D-23): nada referencia NCCodes. Las sesiones en curso cuyo hallazgo guarda
-- uno de esos códigos se abandonan antes de correr el script.
DELETE FROM dbo.NCCodes
WHERE  AreaId = @AreaId AND (Code LIKE N'SIM[_]FR[_]%' OR Code LIKE N'SIM[_]FX[_]%');

-- ------------------------------------------------------------
-- Operations — SIMULADAS (spec 014, D-58): el banner de la reparación
-- extendida nombra la actual (mayor Sequence, Local Rework) o la anterior
-- (Return to any previous). Formato real: número + código + descripción
-- (D-52).
-- ------------------------------------------------------------
DECLARE @Op TABLE (Seq INT IDENTITY, Name NVARCHAR(100), Sequence INT, Description NVARCHAR(200));
INSERT INTO @Op (Name, Sequence, Description) VALUES
    (N'O_LOFLO_SIM_PREP',     10, N'Preparación (SIM)'),
    (N'O_LOFLO_SIM_ENSAMBLE', 20, N'Ensamble (SIM)');

UPDATE t SET Sequence = s.Sequence, Description = s.Description
FROM   dbo.Operations t JOIN @Op s ON s.Name = t.Name
WHERE  t.AreaId = @AreaId;

INSERT INTO dbo.Operations (AreaId, Name, Sequence, Description)
SELECT @AreaId, s.Name, s.Sequence, s.Description
FROM   @Op s
WHERE  NOT EXISTS (SELECT 1 FROM dbo.Operations t WHERE t.AreaId = @AreaId AND t.Name = s.Name)
ORDER  BY s.Seq;

-- ------------------------------------------------------------
-- DataCollectionGroups — SIMULADOS. 2 obligatorios + el grupo Trainer
-- opcional (real, spec 015): la pieza completa DC al guardar los dos
-- obligatorios.
-- ------------------------------------------------------------
-- Spec 015 (D-23): el grupo Trainer reemplaza al opcional
-- DCGTM_SIM_CONDICIONES_AMBIENTE/A. Una BD anterior lo renombra en sitio (el
-- Id no cambia), su primer parámetro pasa a ser el del nombre y el sobrante
-- se borra después de los parámetros: excepción a "nunca borra filas", nada
-- referencia grupos ni parámetros de DC.
IF EXISTS (SELECT 1 FROM dbo.DataCollectionGroups WHERE AreaId = @AreaId AND Code = N'DCGTM_SIM_CONDICIONES_AMBIENTE/A')
   AND NOT EXISTS (SELECT 1 FROM dbo.DataCollectionGroups WHERE AreaId = @AreaId AND Code = N'DCGTM_TRAINEE/A')
    UPDATE dbo.DataCollectionGroups SET Code = N'DCGTM_TRAINEE/A'
    WHERE  AreaId = @AreaId AND Code = N'DCGTM_SIM_CONDICIONES_AMBIENTE/A';

IF NOT EXISTS (SELECT 1 FROM dbo.DataCollectionParameters WHERE AreaId = @AreaId AND Name = N'NOMBRE_EN_ENTRENAMIENTO')
    UPDATE dbo.DataCollectionParameters SET Name = N'NOMBRE_EN_ENTRENAMIENTO'
    WHERE  AreaId = @AreaId AND Name = N'Temperatura ambiente';

DECLARE @Grp TABLE (Seq INT IDENTITY, Code NVARCHAR(100), Description NVARCHAR(200),
                    CollectDataAt NVARCHAR(50), IsRequired BIT, DisplayOrder INT);
INSERT INTO @Grp (Code, Description, CollectDataAt, IsRequired, DisplayOrder) VALUES
    (N'DCGTM_SIM_LOFLO_EQUIPO/A',         N'Verificación del equipo de prueba', 'Pre-Start',  1, 1),
    (N'DCGTM_SIM_LOFLO_FUNCIONAL/A',      N'Prueba funcional del módulo',       'Post-Start', 1, 2),
    (N'DCGTM_TRAINEE/A',                  N'Entrenamiento personal',            'Anytime',    0, 3);

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
-- DataCollectionParameters — SIMULADOS, cada uno en su grupo. Cubren
-- los 3 tipos (Numeric con rango, Boolean, Date) y el nombre del grupo
-- Trainer (PersonName; nombre del parámetro provisional, SIM).
-- ------------------------------------------------------------
DECLARE @Prm TABLE (Seq INT IDENTITY, GroupCode NVARCHAR(100), Name NVARCHAR(200), Description NVARCHAR(200),
                    DataType NVARCHAR(20), Unit NVARCHAR(50), MinValue DECIMAL(10,4), MaxValue DECIMAL(10,4),
                    DisplayOrder INT);
INSERT INTO @Prm (GroupCode, Name, Description, DataType, Unit, MinValue, MaxValue, DisplayOrder) VALUES
    (N'DCGTM_SIM_LOFLO_EQUIPO/A',         N'Fecha de vencimiento de calibración', N'Fecha de calibración del equipo de prueba', 'Date',    'MM/DD/AA', NULL, NULL, 1),
    (N'DCGTM_SIM_LOFLO_FUNCIONAL/A',      N'Flujo de muestreo',                   N'Sample flow rate',                         'Numeric', 'mL/min',   45,   55,   2),
    (N'DCGTM_SIM_LOFLO_FUNCIONAL/A',      N'Lectura de CO2 con gas de calibración', N'CO2 reading (5% cal gas)',              'Numeric', '%',        4.8,  5.2,  3),
    (N'DCGTM_SIM_LOFLO_FUNCIONAL/A',      N'¿Prueba de fugas aprobada?',          N'Leak test',                                'Boolean', 'N/A',      NULL, NULL, 4),
    (N'DCGTM_TRAINEE/A',                  N'NOMBRE_EN_ENTRENAMIENTO',             N'SIM: nombre de la persona en entrenamiento', 'PersonName', 'N/A',   NULL, NULL, 5);

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
-- AssemblyComponents — BOM SIMULADO (prefijo SIM-). Uno con Qty
-- decimal (tubo por longitud).
-- ------------------------------------------------------------
DECLARE @Bom TABLE (Seq INT IDENTITY, PartNumber NVARCHAR(50), Revision NVARCHAR(10),
                    Description NVARCHAR(200), Qty DECIMAL(18,6), DisplayOrder INT);
INSERT INTO @Bom (PartNumber, Revision, Description, Qty, DisplayOrder) VALUES
    (N'SIM-200001', N'A', N'MÓDULO LOFLO SIMULADO',          1,    1),
    (N'SIM-200002', N'A', N'SENSOR DE CO2 SIMULADO',         1,    2),
    (N'SIM-200003', N'A', N'TUBO DE MUESTREO SIMULADO (M)',  0.25, 3);

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
PRINT N'Catálogo de Sensores / LoFlo cargado.';
