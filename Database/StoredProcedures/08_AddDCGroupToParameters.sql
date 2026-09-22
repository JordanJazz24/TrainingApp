-- ============================================================
-- 08_AddDCGroupToParameters.sql
-- Agrega la relación DC Group a DataCollectionParameters.
--
-- Motivo: la Data Collection List debe agrupar los parámetros por
-- DC Group (como en el legacy) para que "Collect" muestre solo las
-- preguntas del grupo seleccionado, no todos los parámetros del área.
--
-- Prerequisitos: 00_Schema.sql, 06_RCMoldeoCatalog.sql (corregido)
-- Seguro para re-ejecución: ALTER solo si la columna no existe;
-- los UPDATE de backfill son idempotentes (filtran por Name/AreaId).
-- ============================================================

-- 1. Columnas nuevas — DEFAULT rellena filas existentes al agregarlas.
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.DataCollectionParameters') AND name = 'DCGroup'
)
    ALTER TABLE dbo.DataCollectionParameters
    ADD DCGroup NVARCHAR(100) NOT NULL CONSTRAINT DF_DCParam_DCGroup DEFAULT 'DCGTM_GENERAL/A';

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.DataCollectionParameters') AND name = 'GroupDescription'
)
    ALTER TABLE dbo.DataCollectionParameters
    ADD GroupDescription NVARCHAR(200) NOT NULL CONSTRAINT DF_DCParam_GroupDescription DEFAULT 'Data Collection General';

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.DataCollectionParameters') AND name = 'CollectDataAt'
)
    ALTER TABLE dbo.DataCollectionParameters
    ADD CollectDataAt NVARCHAR(50) NOT NULL CONSTRAINT DF_DCParam_CollectDataAt DEFAULT 'Anytime';
GO

-- -------------------------------------------------------
-- 2. Backfill — mapeo real de Moldeo (Product = 'R&C', Area = 'Moldeo')
--    según la estructura documentada en 06_RCMoldeoCatalog.sql.
-- -------------------------------------------------------
DECLARE @RCId   INT = (SELECT Id FROM dbo.Products WHERE Name = 'R&C');
DECLARE @AreaId INT = (SELECT Id FROM dbo.Areas   WHERE Name = 'Moldeo' AND ProductId = @RCId);

-- Sección 1 (Pre-Start): ID del Molde
UPDATE dbo.DataCollectionParameters
SET DCGroup = 'DCGTM_ID_MOLDE/A',
    GroupDescription = 'ID del Molde',
    CollectDataAt = 'Pre-Start'
WHERE AreaId = @AreaId AND Name = 'ID del Molde';

-- Sección 3 (Pre-Start): 10 parámetros MSS D000281359
UPDATE dbo.DataCollectionParameters
SET DCGroup = 'DCGTM_PARAMETROS_MSS_D000281359/A',
    GroupDescription = 'Parámetros MSS D000281359',
    CollectDataAt = 'Pre-Start'
WHERE AreaId = @AreaId AND Name IN (
    'Colling Time según MSS D000281359',
    'Injection speed según MSS D000281359',
    'Intensidad de Pad Printing',
    'Temperatura Zona 1',
    'Temperatura Zona 2',
    'Temperatura Zona 3',
    'Temperatura Zona 4',
    'Temperatura Zona 5',
    'Temperatura Zona 6',
    'Tiempo de curado de Pad Printing'
);

-- Sección 4 (Pre-Start): Verificación IDMold & IDIMM
UPDATE dbo.DataCollectionParameters
SET DCGroup = 'DCGTM_VERIFICACION_MOLDEO_MAQUINA_OP/A',
    GroupDescription = 'Verificación IDMold & IDIMM',
    CollectDataAt = 'Pre-Start'
WHERE AreaId = @AreaId AND Name = '¿Se verificó el IDMold & IDIMM según MSS?';

-- Post-Start (DC regular): Fechas de Expiración (Kerosene + Tintas comparten grupo)
UPDATE dbo.DataCollectionParameters
SET DCGroup = 'DCGTM_FECHA_EXPIRACION_NIR/A',
    GroupDescription = 'Fechas de Expiración',
    CollectDataAt = 'Post-Start'
WHERE AreaId = @AreaId AND Name IN ('Fecha de Expiración Kerosene', 'Fecha de Expiración de Tintas');

-- Post-Start (DC regular): First Shot OPE
UPDATE dbo.DataCollectionParameters
SET DCGroup = 'DCGTM_ENTREGA_PRIMERA_PIEZA/A',
    GroupDescription = 'First Shot Entregado',
    CollectDataAt = 'Post-Start'
WHERE AreaId = @AreaId AND Name = 'First Shot entregado - OPE';

-- Post-Start (DC regular): First Shot NIR OPE
UPDATE dbo.DataCollectionParameters
SET DCGroup = 'DCGTM_ENTREGA_PRIMERA_PIEZA_NIR/A',
    GroupDescription = 'First Shot NIR Entregado',
    CollectDataAt = 'Post-Start'
WHERE AreaId = @AreaId AND Name = 'First Shot NIR entregado - OPE';
GO
