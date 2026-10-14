-- ============================================================
-- 06_RCMoldeoCatalog.sql — catálogo real de S&RC / Moldeo:
-- Product, Area (flags), NCCodes, DataCollectionGroups y
-- DataCollectionParameters. La guía está en 07_RCMoldeoGuide.sql.
-- Requiere: 00_Schema.sql.
-- Re-ejecutable: sí. Este script es la fuente de verdad del catálogo
-- de Moldeo: agrega lo que falta y actualiza lo que existe (no borra
-- filas: los intentos y las sesiones pueden apuntar a ellas).
--
-- Fuente: auditoría del legacy (main.v1.0.0.js, main.aspx,
-- guidedTest.v1.0.0.js), confirmada con negocio el 2026-09-04.
-- Grupo obligatorio de DC confirmado con el usuario el 2026-09-28.
-- ============================================================
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;

-- ------------------------------------------------------------
-- Product y Area
--   SerializationType      1 = NonSerialized (lote)
--   RequiresAssembly       0 — el ID del molde es un parámetro de DC
--   RequiresDataCollection 1
--   RequiresNCComponent    0 — NC Data Entry no pide Component
--   RequiresStartChecklist 1 — Start abre la secuencia de 4 modales
-- ------------------------------------------------------------
-- El producto se llamaba 'R&C' hasta el 2026-10-14 (T7: su nombre es S&RC, confirmado
-- por el usuario). Una BD anterior se renombra en sitio: el Id no cambia.
IF EXISTS (SELECT 1 FROM dbo.Products WHERE Name = 'R&C')
   AND NOT EXISTS (SELECT 1 FROM dbo.Products WHERE Name = 'S&RC')
    UPDATE dbo.Products SET Name = 'S&RC' WHERE Name = 'R&C';

IF NOT EXISTS (SELECT 1 FROM dbo.Products WHERE Name = 'S&RC')
    INSERT INTO dbo.Products (Name) VALUES ('S&RC');

DECLARE @ProductId INT = (SELECT Id FROM dbo.Products WHERE Name = 'S&RC');

IF NOT EXISTS (SELECT 1 FROM dbo.Areas WHERE ProductId = @ProductId AND Name = 'Moldeo')
    INSERT INTO dbo.Areas (ProductId, Name, SerializationType, RequiresAssembly,
                           RequiresDataCollection, RequiresNCComponent, RequiresStartChecklist)
    VALUES (@ProductId, 'Moldeo', 1, 0, 1, 0, 1);
ELSE
    UPDATE dbo.Areas
    SET    SerializationType = 1, RequiresAssembly = 0, RequiresDataCollection = 1,
           RequiresNCComponent = 0, RequiresStartChecklist = 1
    WHERE  ProductId = @ProductId AND Name = 'Moldeo';

DECLARE @AreaId INT = (SELECT Id FROM dbo.Areas WHERE ProductId = @ProductId AND Name = 'Moldeo');

-- ------------------------------------------------------------
-- NCCodes — 30 códigos del legacy, con guion bajo en vez de espacios
-- (se corrigieron DESECHO_QA, PUNTO_INYEC, RING_DEFORM y TIRO_CORTO).
-- Pendiente: F_FS_THICKNESS tiene la descripción temporal "NCR".
-- ------------------------------------------------------------
DECLARE @Nc TABLE (Seq INT IDENTITY, Code NVARCHAR(50), Description NVARCHAR(200));
INSERT INTO @Nc (Code, Description) VALUES
    (N'F_FR_GENERAL',     N'Fast Repair General'),
    (N'F_FS_0000',        N'Pruebas de ingeniería'),
    (N'F_FS_BRK_EMITTER', N'Wire Broken'),
    (N'F_FS_BURBUJAS',    N'Burbujas'),
    (N'F_FS_CONTAMINADA', N'Contaminadas'),
    (N'F_FS_D_MONTAJE',   N'Daño de la pieza por montaje incorrecto'),
    (N'F_FS_DEFORMES',    N'Deformes'),
    (N'F_FS_DESECHO_QA',  N'Desecho de calidad'),
    (N'F_FS_DISBONDING',  N'Disbonding'),
    (N'F_FS_ELECT_FAIL',  N'Fallo Eléctrico'),
    (N'F_FS_ESTAB',       N'Estabilización (ajuste de proceso)'),
    (N'F_FS_FALT_MAT',    N'Falta de Material'),
    (N'F_FS_FLASH',       N'Flash'),
    (N'F_FS_GLUE_EXC',    N'Exceso de Goma o Adhesivo'),
    (N'F_FS_MAL_CURADO',  N'Mal Curado'),
    (N'F_FS_MANCHAS',     N'Manchas'),
    (N'F_FS_NCR',         N'NCR'),
    (N'F_FS_NOMATCH',     N'NOMATCH'),
    (N'F_FS_P_IMPRES_IN', N'Pieza Impresa Incorrectamente'),
    (N'F_FS_PARTICULAS',  N'Partículas'),
    (N'F_FS_PIEL_NARANJ', N'Piel Naranja'),
    (N'F_FS_PUNTO_INYEC', N'Punto Inyección'),
    (N'F_FS_RAYAS',       N'Rayas'),
    (N'F_FS_RING_DEFORM', N'Ring Deformado'),
    (N'F_FS_ROTAS',       N'Rotas'),
    (N'F_FS_THICKNESS',   N'NCR'),          -- descripción temporal; revisar contra el catálogo real
    (N'F_FS_TIRO_CORTO',  N'Tiro Corto'),
    (N'F_FS_TNT_CORRIDA', N'Tinta Corrida'),
    (N'F_FS_TNT_NO_CURA', N'Tinta no Curada'),
    (N'F_FX_GENERAL',     N'Fast Extended Repair General');

UPDATE t SET Description = s.Description
FROM   dbo.NCCodes t JOIN @Nc s ON s.Code = t.Code
WHERE  t.AreaId = @AreaId;

INSERT INTO dbo.NCCodes (AreaId, Code, Description)
SELECT @AreaId, s.Code, s.Description
FROM   @Nc s
WHERE  NOT EXISTS (SELECT 1 FROM dbo.NCCodes t WHERE t.AreaId = @AreaId AND t.Code = s.Code)
ORDER  BY s.Seq;

-- ------------------------------------------------------------
-- DataCollectionGroups — 7 grupos. Obligatorio solo
-- DCGTM_FECHA_EXPIRACION_NIR/A (fechas de kerosene y tintas): el paso
-- DC se completa al guardarlo. La UI no indica cuál es obligatorio.
-- DCGTM_TRAINEE/A: grupo Trainer, opcional (spec 015).
-- ------------------------------------------------------------
DECLARE @Grp TABLE (Seq INT IDENTITY, Code NVARCHAR(100), Description NVARCHAR(200),
                    CollectDataAt NVARCHAR(50), IsRequired BIT, DisplayOrder INT);
INSERT INTO @Grp (Code, Description, CollectDataAt, IsRequired, DisplayOrder) VALUES
    (N'DCGTM_ID_MOLDE/A',                       N'ID del Molde',                'Pre-Start',  0, 1),
    (N'DCGTM_PARAMETROS_MSS_D000281359/A',      N'Parámetros MSS D000281359',   'Pre-Start',  0, 2),
    (N'DCGTM_VERIFICACION_MOLDEO_MAQUINA_OP/A', N'Verificación IDMold & IDIMM', 'Pre-Start',  0, 3),
    (N'DCGTM_FECHA_EXPIRACION_NIR/A',           N'Fechas de Expiración',        'Post-Start', 1, 4),
    (N'DCGTM_ENTREGA_PRIMERA_PIEZA/A',          N'First Shot Entregado',        'Post-Start', 0, 5),
    (N'DCGTM_ENTREGA_PRIMERA_PIEZA_NIR/A',      N'First Shot NIR Entregado',    'Post-Start', 0, 6),
    (N'DCGTM_TRAINEE/A',                        N'Entrenamiento personal',      'Anytime',    0, 7);

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
-- DataCollectionParameters — 17 parámetros, cada uno en su grupo.
--   · Name: la pregunta. Description: la línea "Description: …" de Data
--     Collection Entry, tomada del legacy (main.aspx, main.v1.0.0.js); las
--     de First Shot se redactaron desde su pregunta.
--   · MinValue/MaxValue en NULL: rangos pendientes de la MSS D000281359.
--   · "Colling Time" se escribe así en el legacy.
--   · Las 3 preguntas de LC Moldeo del checklist de Start NO son
--     parámetros: viven en SearchControl y las califica StepValidator.
--   · NOMBRE_EN_ENTRENAMIENTO (grupo Trainer, spec 015): nombre provisional
--     (SIM); el del MES real no se conoce.
-- ------------------------------------------------------------
DECLARE @Prm TABLE (Seq INT IDENTITY, GroupCode NVARCHAR(100), Name NVARCHAR(200), Description NVARCHAR(200),
                    DataType NVARCHAR(20), Unit NVARCHAR(50), DisplayOrder INT);
INSERT INTO @Prm (GroupCode, Name, Description, DataType, Unit, DisplayOrder) VALUES
    (N'DCGTM_ID_MOLDE/A',                       N'ID del Molde',                              N'ID Molde',                                       'Numeric', 'N/A',      1),
    (N'DCGTM_PARAMETROS_MSS_D000281359/A',      N'Colling Time según MSS D000281359',         N'Colling Time según MSS D000281359',              'Numeric', 'N/A',      2),
    (N'DCGTM_PARAMETROS_MSS_D000281359/A',      N'Injection speed según MSS D000281359',      N'Resultado según MSS D000281359 VERT 1',          'Numeric', 'N/A',      3),
    (N'DCGTM_PARAMETROS_MSS_D000281359/A',      N'Intensidad de Pad Printing',                N'Ingrese la intensidad de la Pad Printing',       'Numeric', 'N/A',      4),
    (N'DCGTM_PARAMETROS_MSS_D000281359/A',      N'Temperatura Zona 1',                        N'Temperatura de Molde Zona 1',                    'Numeric', N'°C',      5),
    (N'DCGTM_PARAMETROS_MSS_D000281359/A',      N'Temperatura Zona 2',                        N'Temperatura de Molde Zona 2',                    'Numeric', N'°C',      6),
    (N'DCGTM_PARAMETROS_MSS_D000281359/A',      N'Temperatura Zona 3',                        N'Temperatura de Molde Zona 3',                    'Numeric', N'°C',      7),
    (N'DCGTM_PARAMETROS_MSS_D000281359/A',      N'Temperatura Zona 4',                        N'Temperatura de Molde Zona 4',                    'Numeric', N'°C',      8),
    (N'DCGTM_PARAMETROS_MSS_D000281359/A',      N'Temperatura Zona 5',                        N'Temperatura de Molde Zona 5',                    'Numeric', N'°C',      9),
    (N'DCGTM_PARAMETROS_MSS_D000281359/A',      N'Temperatura Zona 6',                        N'Temperatura de Molde Zona 6',                    'Numeric', N'°C',     10),
    (N'DCGTM_PARAMETROS_MSS_D000281359/A',      N'Tiempo de curado de Pad Printing',          N'Ingrese el tiempo de curado de la Pad Printing', 'Numeric', 'N/A',     11),
    (N'DCGTM_VERIFICACION_MOLDEO_MAQUINA_OP/A', N'¿Se verificó el IDMold & IDIMM según MSS?', N'ID Mold & IDIMM segun MSS?',                     'Boolean', 'N/A',     12),
    (N'DCGTM_FECHA_EXPIRACION_NIR/A',           N'Fecha de Expiración Kerosene',              N'Ingrese la fecha de expiración kerosene',        'Date',    'MM/DD/AA', 13),
    (N'DCGTM_FECHA_EXPIRACION_NIR/A',           N'Fecha de Expiración de Tintas',             N'Ingrese la fecha de expiración de Priscone',     'Date',    'MM/DD/AA', 16),
    (N'DCGTM_ENTREGA_PRIMERA_PIEZA/A',          N'First Shot entregado - OPE',                N'Entrega de First Shot a calidad (OPE)',          'Boolean', 'N/A',     14),
    (N'DCGTM_ENTREGA_PRIMERA_PIEZA_NIR/A',      N'First Shot NIR entregado - OPE',            N'Entrega de First Shot NIR a calidad (OPE)',      'Boolean', 'N/A',     15),
    (N'DCGTM_TRAINEE/A',                        N'NOMBRE_EN_ENTRENAMIENTO',                   N'SIM: nombre de la persona en entrenamiento',     'PersonName', 'N/A',  17);

UPDATE t
SET    DataCollectionGroupId = g.Id, Description = s.Description, DataType = s.DataType,
       Unit = s.Unit, MinValue = NULL, MaxValue = NULL, DisplayOrder = s.DisplayOrder
FROM   dbo.DataCollectionParameters t
JOIN   @Prm s ON s.Name = t.Name
JOIN   dbo.DataCollectionGroups g ON g.AreaId = @AreaId AND g.Code = s.GroupCode
WHERE  t.AreaId = @AreaId;

INSERT INTO dbo.DataCollectionParameters (AreaId, OperationId, DataCollectionGroupId, Name, Description,
                                          DataType, Unit, MinValue, MaxValue, DisplayOrder)
SELECT @AreaId, NULL, g.Id, s.Name, s.Description, s.DataType, s.Unit, NULL, NULL, s.DisplayOrder
FROM   @Prm s
JOIN   dbo.DataCollectionGroups g ON g.AreaId = @AreaId AND g.Code = s.GroupCode
WHERE  NOT EXISTS (SELECT 1 FROM dbo.DataCollectionParameters t WHERE t.AreaId = @AreaId AND t.Name = s.Name)
ORDER  BY s.Seq;

COMMIT TRANSACTION;
PRINT N'Catálogo de S&RC / Moldeo cargado.';
