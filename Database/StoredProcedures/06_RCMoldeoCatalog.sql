-- ============================================================
-- 06_RCMoldeoCatalog.sql
-- Catálogo validado de R&C / Moldeo — Fase 2.
--
-- Fuente: auditoría del legacy (main.v1.0.0.js + main.aspx +
--         guidedTest.v1.0.0.js). Confirmado con negocio 2026-09-04.
--
-- Jerarquía:
--   Product  : R&C
--   Area     : Moldeo
--   Operation: [futuro — no aplica hoy]
--
-- Flags del área:
--   SerializationType     = 1 (NonSerialized — lote/batch)
--   RequiresAssembly      = 0 (false — el ID de molde es DC, no Assembly)
--   RequiresDataCollection = 1 (true)
--
-- Alcance de este script:
--   Products, Areas, NCCodes, DataCollectionParameters.
--   NO incluye: TrainingGuides, TrainingSteps (Fase 3).
--
-- Ejecutar DESPUÉS de 00_Schema.sql.
-- Seguro para re-ejecución: usa INSERT solo si no existe la fila.
-- ============================================================

-- -------------------------------------------------------
-- Migración de esquema: añadir columnas nuevas de
-- DataCollectionParameters si la tabla ya fue creada con
-- la versión anterior de 00_Schema.sql (sin DataType,
-- MinValue nullable ni DisplayOrder).
-- -------------------------------------------------------
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.DataCollectionParameters') AND name = 'DataType'
)
    ALTER TABLE dbo.DataCollectionParameters
        ADD DataType NVARCHAR(20) NOT NULL DEFAULT 'Numeric';

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.DataCollectionParameters') AND name = 'DisplayOrder'
)
    ALTER TABLE dbo.DataCollectionParameters
        ADD DisplayOrder INT NOT NULL DEFAULT 0;

-- Hacer MinValue/MaxValue nullable si aún son NOT NULL
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.DataCollectionParameters')
      AND name = 'MinValue' AND is_nullable = 0
)
    ALTER TABLE dbo.DataCollectionParameters ALTER COLUMN MinValue DECIMAL(10,4) NULL;

IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.DataCollectionParameters')
      AND name = 'MaxValue' AND is_nullable = 0
)
    ALTER TABLE dbo.DataCollectionParameters ALTER COLUMN MaxValue DECIMAL(10,4) NULL;

-- Ampliar Name a 200 si aún tiene 100
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.DataCollectionParameters')
      AND name = 'Name' AND max_length = 200   -- 100 chars = 200 bytes (nvarchar)
)
    ALTER TABLE dbo.DataCollectionParameters ALTER COLUMN Name NVARCHAR(200) NOT NULL;
GO

-- -------------------------------------------------------
-- Product: R&C
-- -------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.Products WHERE Name = 'R&C')
    INSERT INTO dbo.Products (Name) VALUES ('R&C');

-- -------------------------------------------------------
-- Area: Moldeo
-- SerializationType: 1 = NonSerialized (lote/batch)
-- RequiresAssembly:       0 — el registro del molde es DC, no Assembly
-- RequiresDataCollection: 1 — parámetros MSS + DC post-start obligatorios
-- -------------------------------------------------------
DECLARE @RCId INT = (SELECT Id FROM dbo.Products WHERE Name = 'R&C');

IF NOT EXISTS (SELECT 1 FROM dbo.Areas WHERE Name = 'Moldeo' AND ProductId = @RCId)
    INSERT INTO dbo.Areas (ProductId, Name, SerializationType, RequiresAssembly, RequiresDataCollection)
    VALUES (@RCId, 'Moldeo', 1, 0, 1);

DECLARE @AreaId INT = (SELECT Id FROM dbo.Areas WHERE Name = 'Moldeo' AND ProductId = @RCId);

-- -------------------------------------------------------
-- NCCodes — 30 códigos validados contra el legacy.
-- Notas de formato:
--   · Guiones bajos (_) — sin espacios en los códigos.
--   · Los 4 códigos con espacios en el legacy fueron
--     corregidos: DESECHO_QA, PUNTO_INYEC, RING_DEFORM,
--     TIRO_CORTO.
--   · F_FS_THICKNESS: descripción temporal "NCR" aceptada
--     de forma transitoria (pendiente de revisión).
-- -------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.NCCodes WHERE AreaId = @AreaId AND Code = 'F_FR_GENERAL')
    INSERT INTO dbo.NCCodes (AreaId, Code, Description) VALUES (@AreaId, 'F_FR_GENERAL',     'Fast Repair General');

IF NOT EXISTS (SELECT 1 FROM dbo.NCCodes WHERE AreaId = @AreaId AND Code = 'F_FS_0000')
    INSERT INTO dbo.NCCodes (AreaId, Code, Description) VALUES (@AreaId, 'F_FS_0000',         'Pruebas de ingeniería');

IF NOT EXISTS (SELECT 1 FROM dbo.NCCodes WHERE AreaId = @AreaId AND Code = 'F_FS_BRK_EMITTER')
    INSERT INTO dbo.NCCodes (AreaId, Code, Description) VALUES (@AreaId, 'F_FS_BRK_EMITTER',  'Wire Broken');

IF NOT EXISTS (SELECT 1 FROM dbo.NCCodes WHERE AreaId = @AreaId AND Code = 'F_FS_BURBUJAS')
    INSERT INTO dbo.NCCodes (AreaId, Code, Description) VALUES (@AreaId, 'F_FS_BURBUJAS',     'Burbujas');

IF NOT EXISTS (SELECT 1 FROM dbo.NCCodes WHERE AreaId = @AreaId AND Code = 'F_FS_CONTAMINADA')
    INSERT INTO dbo.NCCodes (AreaId, Code, Description) VALUES (@AreaId, 'F_FS_CONTAMINADA',  'Contaminadas');

IF NOT EXISTS (SELECT 1 FROM dbo.NCCodes WHERE AreaId = @AreaId AND Code = 'F_FS_D_MONTAJE')
    INSERT INTO dbo.NCCodes (AreaId, Code, Description) VALUES (@AreaId, 'F_FS_D_MONTAJE',    'Daño de la pieza por montaje incorrecto');

IF NOT EXISTS (SELECT 1 FROM dbo.NCCodes WHERE AreaId = @AreaId AND Code = 'F_FS_DEFORMES')
    INSERT INTO dbo.NCCodes (AreaId, Code, Description) VALUES (@AreaId, 'F_FS_DEFORMES',     'Deformes');

IF NOT EXISTS (SELECT 1 FROM dbo.NCCodes WHERE AreaId = @AreaId AND Code = 'F_FS_DESECHO_QA')
    INSERT INTO dbo.NCCodes (AreaId, Code, Description) VALUES (@AreaId, 'F_FS_DESECHO_QA',   'Desecho de calidad');

IF NOT EXISTS (SELECT 1 FROM dbo.NCCodes WHERE AreaId = @AreaId AND Code = 'F_FS_DISBONDING')
    INSERT INTO dbo.NCCodes (AreaId, Code, Description) VALUES (@AreaId, 'F_FS_DISBONDING',   'Disbonding');

IF NOT EXISTS (SELECT 1 FROM dbo.NCCodes WHERE AreaId = @AreaId AND Code = 'F_FS_ELECT_FAIL')
    INSERT INTO dbo.NCCodes (AreaId, Code, Description) VALUES (@AreaId, 'F_FS_ELECT_FAIL',   'Fallo Eléctrico');

IF NOT EXISTS (SELECT 1 FROM dbo.NCCodes WHERE AreaId = @AreaId AND Code = 'F_FS_ESTAB')
    INSERT INTO dbo.NCCodes (AreaId, Code, Description) VALUES (@AreaId, 'F_FS_ESTAB',        'Estabilización (ajuste de proceso)');

IF NOT EXISTS (SELECT 1 FROM dbo.NCCodes WHERE AreaId = @AreaId AND Code = 'F_FS_FALT_MAT')
    INSERT INTO dbo.NCCodes (AreaId, Code, Description) VALUES (@AreaId, 'F_FS_FALT_MAT',     'Falta de Material');

IF NOT EXISTS (SELECT 1 FROM dbo.NCCodes WHERE AreaId = @AreaId AND Code = 'F_FS_FLASH')
    INSERT INTO dbo.NCCodes (AreaId, Code, Description) VALUES (@AreaId, 'F_FS_FLASH',        'Flash');

IF NOT EXISTS (SELECT 1 FROM dbo.NCCodes WHERE AreaId = @AreaId AND Code = 'F_FS_GLUE_EXC')
    INSERT INTO dbo.NCCodes (AreaId, Code, Description) VALUES (@AreaId, 'F_FS_GLUE_EXC',     'Exceso de Goma o Adhesivo');

IF NOT EXISTS (SELECT 1 FROM dbo.NCCodes WHERE AreaId = @AreaId AND Code = 'F_FS_MAL_CURADO')
    INSERT INTO dbo.NCCodes (AreaId, Code, Description) VALUES (@AreaId, 'F_FS_MAL_CURADO',   'Mal Curado');

IF NOT EXISTS (SELECT 1 FROM dbo.NCCodes WHERE AreaId = @AreaId AND Code = 'F_FS_MANCHAS')
    INSERT INTO dbo.NCCodes (AreaId, Code, Description) VALUES (@AreaId, 'F_FS_MANCHAS',      'Manchas');

IF NOT EXISTS (SELECT 1 FROM dbo.NCCodes WHERE AreaId = @AreaId AND Code = 'F_FS_NCR')
    INSERT INTO dbo.NCCodes (AreaId, Code, Description) VALUES (@AreaId, 'F_FS_NCR',          'NCR');

IF NOT EXISTS (SELECT 1 FROM dbo.NCCodes WHERE AreaId = @AreaId AND Code = 'F_FS_NOMATCH')
    INSERT INTO dbo.NCCodes (AreaId, Code, Description) VALUES (@AreaId, 'F_FS_NOMATCH',      'NOMATCH');

IF NOT EXISTS (SELECT 1 FROM dbo.NCCodes WHERE AreaId = @AreaId AND Code = 'F_FS_P_IMPRES_IN')
    INSERT INTO dbo.NCCodes (AreaId, Code, Description) VALUES (@AreaId, 'F_FS_P_IMPRES_IN',  'Pieza Impresa Incorrectamente');

IF NOT EXISTS (SELECT 1 FROM dbo.NCCodes WHERE AreaId = @AreaId AND Code = 'F_FS_PARTICULAS')
    INSERT INTO dbo.NCCodes (AreaId, Code, Description) VALUES (@AreaId, 'F_FS_PARTICULAS',   'Partículas');

IF NOT EXISTS (SELECT 1 FROM dbo.NCCodes WHERE AreaId = @AreaId AND Code = 'F_FS_PIEL_NARANJ')
    INSERT INTO dbo.NCCodes (AreaId, Code, Description) VALUES (@AreaId, 'F_FS_PIEL_NARANJ',  'Piel Naranja');

IF NOT EXISTS (SELECT 1 FROM dbo.NCCodes WHERE AreaId = @AreaId AND Code = 'F_FS_PUNTO_INYEC')
    INSERT INTO dbo.NCCodes (AreaId, Code, Description) VALUES (@AreaId, 'F_FS_PUNTO_INYEC',  'Punto Inyección');

IF NOT EXISTS (SELECT 1 FROM dbo.NCCodes WHERE AreaId = @AreaId AND Code = 'F_FS_RAYAS')
    INSERT INTO dbo.NCCodes (AreaId, Code, Description) VALUES (@AreaId, 'F_FS_RAYAS',        'Rayas');

IF NOT EXISTS (SELECT 1 FROM dbo.NCCodes WHERE AreaId = @AreaId AND Code = 'F_FS_RING_DEFORM')
    INSERT INTO dbo.NCCodes (AreaId, Code, Description) VALUES (@AreaId, 'F_FS_RING_DEFORM',  'Ring Deformado');

IF NOT EXISTS (SELECT 1 FROM dbo.NCCodes WHERE AreaId = @AreaId AND Code = 'F_FS_ROTAS')
    INSERT INTO dbo.NCCodes (AreaId, Code, Description) VALUES (@AreaId, 'F_FS_ROTAS',        'Rotas');

IF NOT EXISTS (SELECT 1 FROM dbo.NCCodes WHERE AreaId = @AreaId AND Code = 'F_FS_THICKNESS')
    INSERT INTO dbo.NCCodes (AreaId, Code, Description) VALUES (@AreaId, 'F_FS_THICKNESS',    'NCR');  -- descripción temporal; revisar contra catálogo real

IF NOT EXISTS (SELECT 1 FROM dbo.NCCodes WHERE AreaId = @AreaId AND Code = 'F_FS_TIRO_CORTO')
    INSERT INTO dbo.NCCodes (AreaId, Code, Description) VALUES (@AreaId, 'F_FS_TIRO_CORTO',   'Tiro Corto');

IF NOT EXISTS (SELECT 1 FROM dbo.NCCodes WHERE AreaId = @AreaId AND Code = 'F_FS_TNT_CORRIDA')
    INSERT INTO dbo.NCCodes (AreaId, Code, Description) VALUES (@AreaId, 'F_FS_TNT_CORRIDA',  'Tinta Corrida');

IF NOT EXISTS (SELECT 1 FROM dbo.NCCodes WHERE AreaId = @AreaId AND Code = 'F_FS_TNT_NO_CURA')
    INSERT INTO dbo.NCCodes (AreaId, Code, Description) VALUES (@AreaId, 'F_FS_TNT_NO_CURA',  'Tinta no Curada');

IF NOT EXISTS (SELECT 1 FROM dbo.NCCodes WHERE AreaId = @AreaId AND Code = 'F_FX_GENERAL')
    INSERT INTO dbo.NCCodes (AreaId, Code, Description) VALUES (@AreaId, 'F_FX_GENERAL',      'Fast Extended Repair General');

-- -------------------------------------------------------
-- DataCollectionParameters — 15 parámetros de Moldeo
--
-- Estructura temporal del paso de Data Collection en el legacy:
--   · Sección 1 (Pre-Start): ID del Molde                (DCGTM_ID_MOLDE/A)
--   · Sección 2 (Pre-Start): LC Moldeo — 3 preguntas booleanas
--     *** NO son DataCollectionParameters — son Line Clearance.
--         El StepValidator (Fase 3) las gestiona como un paso LC
--         adicional dentro del Start, hardcodeado por área. ***
--   · Sección 3 (Pre-Start): 10 parámetros MSS            (DCGTM_PARAMETROS_MSS_D000281359/A)
--   · Sección 4 (Pre-Start): Verificación IDMold/IDIMM    (DCGTM_VERIFICACION_MOLDEO_MAQUINA_OP/A)
--   · Post-Start (DC regular): Fecha Expiración Kerosene  (DCGTM_FECHA_EXPIRACION_NIR/A)
--   · Post-Start (DC regular): First Shot OPE             (DCGTM_ENTREGA_PRIMERA_PIEZA/A)
--   · Post-Start (DC regular): First Shot NIR OPE         (DCGTM_ENTREGA_PRIMERA_PIEZA_NIR/A)
--
-- MinValue/MaxValue: NULL para parámetros no numéricos y para
--   parámetros numéricos cuyo rango exacto está pendiente de
--   confirmación contra la MSS D000281359.
-- -------------------------------------------------------

-- 1. ID del Molde (Sección 1 / Pre-Start)
IF NOT EXISTS (SELECT 1 FROM dbo.DataCollectionParameters WHERE AreaId = @AreaId AND Name = 'ID del Molde')
    INSERT INTO dbo.DataCollectionParameters (AreaId, OperationId, Name, DataType, Unit, MinValue, MaxValue, DisplayOrder)
    VALUES (@AreaId, NULL, 'ID del Molde', 'Numeric', 'N/A', NULL, NULL, 1);
    -- DCGTM_ID_MOLDE/A. Rango pendiente de revisión contra MSS.

-- 2–11. Parámetros MSS D000281359 (Sección 3 / Pre-Start)
IF NOT EXISTS (SELECT 1 FROM dbo.DataCollectionParameters WHERE AreaId = @AreaId AND Name = 'Colling Time según MSS D000281359')
    INSERT INTO dbo.DataCollectionParameters (AreaId, OperationId, Name, DataType, Unit, MinValue, MaxValue, DisplayOrder)
    VALUES (@AreaId, NULL, 'Colling Time según MSS D000281359', 'Numeric', 'N/A', NULL, NULL, 2);

IF NOT EXISTS (SELECT 1 FROM dbo.DataCollectionParameters WHERE AreaId = @AreaId AND Name = 'Injection speed según MSS D000281359')
    INSERT INTO dbo.DataCollectionParameters (AreaId, OperationId, Name, DataType, Unit, MinValue, MaxValue, DisplayOrder)
    VALUES (@AreaId, NULL, 'Injection speed según MSS D000281359', 'Numeric', 'N/A', NULL, NULL, 3);

IF NOT EXISTS (SELECT 1 FROM dbo.DataCollectionParameters WHERE AreaId = @AreaId AND Name = 'Intensidad de Pad Printing')
    INSERT INTO dbo.DataCollectionParameters (AreaId, OperationId, Name, DataType, Unit, MinValue, MaxValue, DisplayOrder)
    VALUES (@AreaId, NULL, 'Intensidad de Pad Printing', 'Numeric', 'N/A', NULL, NULL, 4);

IF NOT EXISTS (SELECT 1 FROM dbo.DataCollectionParameters WHERE AreaId = @AreaId AND Name = 'Temperatura Zona 1')
    INSERT INTO dbo.DataCollectionParameters (AreaId, OperationId, Name, DataType, Unit, MinValue, MaxValue, DisplayOrder)
    VALUES (@AreaId, NULL, 'Temperatura Zona 1', 'Numeric', '°C', NULL, NULL, 5);

IF NOT EXISTS (SELECT 1 FROM dbo.DataCollectionParameters WHERE AreaId = @AreaId AND Name = 'Temperatura Zona 2')
    INSERT INTO dbo.DataCollectionParameters (AreaId, OperationId, Name, DataType, Unit, MinValue, MaxValue, DisplayOrder)
    VALUES (@AreaId, NULL, 'Temperatura Zona 2', 'Numeric', '°C', NULL, NULL, 6);

IF NOT EXISTS (SELECT 1 FROM dbo.DataCollectionParameters WHERE AreaId = @AreaId AND Name = 'Temperatura Zona 3')
    INSERT INTO dbo.DataCollectionParameters (AreaId, OperationId, Name, DataType, Unit, MinValue, MaxValue, DisplayOrder)
    VALUES (@AreaId, NULL, 'Temperatura Zona 3', 'Numeric', '°C', NULL, NULL, 7);

IF NOT EXISTS (SELECT 1 FROM dbo.DataCollectionParameters WHERE AreaId = @AreaId AND Name = 'Temperatura Zona 4')
    INSERT INTO dbo.DataCollectionParameters (AreaId, OperationId, Name, DataType, Unit, MinValue, MaxValue, DisplayOrder)
    VALUES (@AreaId, NULL, 'Temperatura Zona 4', 'Numeric', '°C', NULL, NULL, 8);

IF NOT EXISTS (SELECT 1 FROM dbo.DataCollectionParameters WHERE AreaId = @AreaId AND Name = 'Temperatura Zona 5')
    INSERT INTO dbo.DataCollectionParameters (AreaId, OperationId, Name, DataType, Unit, MinValue, MaxValue, DisplayOrder)
    VALUES (@AreaId, NULL, 'Temperatura Zona 5', 'Numeric', '°C', NULL, NULL, 9);

IF NOT EXISTS (SELECT 1 FROM dbo.DataCollectionParameters WHERE AreaId = @AreaId AND Name = 'Temperatura Zona 6')
    INSERT INTO dbo.DataCollectionParameters (AreaId, OperationId, Name, DataType, Unit, MinValue, MaxValue, DisplayOrder)
    VALUES (@AreaId, NULL, 'Temperatura Zona 6', 'Numeric', '°C', NULL, NULL, 10);

IF NOT EXISTS (SELECT 1 FROM dbo.DataCollectionParameters WHERE AreaId = @AreaId AND Name = 'Tiempo de curado de Pad Printing')
    INSERT INTO dbo.DataCollectionParameters (AreaId, OperationId, Name, DataType, Unit, MinValue, MaxValue, DisplayOrder)
    VALUES (@AreaId, NULL, 'Tiempo de curado de Pad Printing', 'Numeric', 'N/A', NULL, NULL, 11);

-- 12. Verificación IDMold & IDIMM (Sección 4 / Pre-Start)
IF NOT EXISTS (SELECT 1 FROM dbo.DataCollectionParameters WHERE AreaId = @AreaId AND Name = '¿Se verificó el IDMold & IDIMM según MSS?')
    INSERT INTO dbo.DataCollectionParameters (AreaId, OperationId, Name, DataType, Unit, MinValue, MaxValue, DisplayOrder)
    VALUES (@AreaId, NULL, '¿Se verificó el IDMold & IDIMM según MSS?', 'Boolean', 'N/A', NULL, NULL, 12);
    -- DCGTM_VERIFICACION_MOLDEO_MAQUINA_OP/A

-- 13. Fecha de Expiración Kerosene (DC post-Start)
IF NOT EXISTS (SELECT 1 FROM dbo.DataCollectionParameters WHERE AreaId = @AreaId AND Name = 'Fecha de Expiración Kerosene')
    INSERT INTO dbo.DataCollectionParameters (AreaId, OperationId, Name, DataType, Unit, MinValue, MaxValue, DisplayOrder)
    VALUES (@AreaId, NULL, 'Fecha de Expiración Kerosene', 'Date', 'MM/DD/AA', NULL, NULL, 13);
    -- DCGTM_FECHA_EXPIRACION_NIR/A

-- 14. First Shot entregado - OPE 
IF NOT EXISTS (SELECT 1 FROM dbo.DataCollectionParameters WHERE AreaId = @AreaId AND Name = 'First Shot entregado - OPE')
    INSERT INTO dbo.DataCollectionParameters (AreaId, OperationId, Name, DataType, Unit, MinValue, MaxValue, DisplayOrder)
    VALUES (@AreaId, NULL, 'First Shot entregado - OPE', 'Boolean', 'N/A', NULL, NULL, 14);
    -- DCGTM_ENTREGA_PRIMERA_PIEZA/A

-- 15. First Shot NIR entregado - OPE 
IF NOT EXISTS (SELECT 1 FROM dbo.DataCollectionParameters WHERE AreaId = @AreaId AND Name = 'First Shot NIR entregado - OPE')
    INSERT INTO dbo.DataCollectionParameters (AreaId, OperationId, Name, DataType, Unit, MinValue, MaxValue, DisplayOrder)
    VALUES (@AreaId, NULL, 'First Shot NIR entregado - OPE', 'Boolean', 'N/A', NULL, NULL, 15);
    -- DCGTM_ENTREGA_PRIMERA_PIEZA_NIR/A
-- 16. Fecha de Expiración de Tintas (DC post-Start)
IF NOT EXISTS (SELECT 1 FROM dbo.DataCollectionParameters WHERE AreaId = @AreaId AND Name = 'Fecha de Expiración de Tintas')
    INSERT INTO dbo.DataCollectionParameters (AreaId, OperationId, Name, DataType, Unit, MinValue, MaxValue, DisplayOrder)
    VALUES (@AreaId, NULL, 'Fecha de Expiración de Tintas', 'Date', 'MM/DD/AA', NULL, NULL, 16);
    -- DCGTM_FECHA_EXPIRACION_NIR/A