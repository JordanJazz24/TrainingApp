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
--   RequiresNCComponent    = 0 (false — NC Data Entry no pide Component)
--   RequiresStartChecklist = 1 (true — Start abre la secuencia de 4 modales)
--
-- Alcance de este script:
--   Products, Areas, NCCodes, DataCollectionGroups, DataCollectionParameters.
--   NO incluye: TrainingGuides, TrainingSteps (ver 07_RCMoldeoGuide.sql).
--
-- Prerequisitos: 00_Schema.sql
-- Seguro para re-ejecución: usa INSERT solo si no existe la fila.
-- ============================================================

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
-- RequiresNCComponent:    0 — Moldeo es la excepción a la norma
-- RequiresStartChecklist: 1 — Start abre la secuencia de 4 modales (spec 008)
-- -------------------------------------------------------
DECLARE @RCId INT = (SELECT Id FROM dbo.Products WHERE Name = 'R&C');

IF NOT EXISTS (SELECT 1 FROM dbo.Areas WHERE Name = 'Moldeo' AND ProductId = @RCId)
    INSERT INTO dbo.Areas (ProductId, Name, SerializationType, RequiresAssembly, RequiresDataCollection,
                           RequiresNCComponent, RequiresStartChecklist)
    VALUES (@RCId, 'Moldeo', 1, 0, 1, 0, 1);

DECLARE @AreaId INT = (SELECT Id FROM dbo.Areas WHERE Name = 'Moldeo' AND ProductId = @RCId);

-- El catálogo es la fuente de verdad del flag, también en una BD ya existente.
UPDATE dbo.Areas SET RequiresStartChecklist = 1 WHERE Id = @AreaId AND RequiresStartChecklist = 0;

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
-- DataCollectionGroups — 6 DC Groups de Moldeo (spec 008).
--
-- Obligatorio: solo DCGTM_FECHA_EXPIRACION_NIR/A (registro de las
--   fechas del kerosene; confirmado con el usuario 2026-09-28). El
--   paso DC se completa al guardarlo; los demás grupos son opcionales.
-- -------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.DataCollectionGroups WHERE AreaId = @AreaId AND Code = 'DCGTM_ID_MOLDE/A')
    INSERT INTO dbo.DataCollectionGroups (AreaId, OperationId, Code, Description, CollectDataAt, IsRequired, DisplayOrder)
    VALUES (@AreaId, NULL, 'DCGTM_ID_MOLDE/A', 'ID del Molde', 'Pre-Start', 0, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.DataCollectionGroups WHERE AreaId = @AreaId AND Code = 'DCGTM_PARAMETROS_MSS_D000281359/A')
    INSERT INTO dbo.DataCollectionGroups (AreaId, OperationId, Code, Description, CollectDataAt, IsRequired, DisplayOrder)
    VALUES (@AreaId, NULL, 'DCGTM_PARAMETROS_MSS_D000281359/A', 'Parámetros MSS D000281359', 'Pre-Start', 0, 2);

IF NOT EXISTS (SELECT 1 FROM dbo.DataCollectionGroups WHERE AreaId = @AreaId AND Code = 'DCGTM_VERIFICACION_MOLDEO_MAQUINA_OP/A')
    INSERT INTO dbo.DataCollectionGroups (AreaId, OperationId, Code, Description, CollectDataAt, IsRequired, DisplayOrder)
    VALUES (@AreaId, NULL, 'DCGTM_VERIFICACION_MOLDEO_MAQUINA_OP/A', 'Verificación IDMold & IDIMM', 'Pre-Start', 0, 3);

IF NOT EXISTS (SELECT 1 FROM dbo.DataCollectionGroups WHERE AreaId = @AreaId AND Code = 'DCGTM_FECHA_EXPIRACION_NIR/A')
    INSERT INTO dbo.DataCollectionGroups (AreaId, OperationId, Code, Description, CollectDataAt, IsRequired, DisplayOrder)
    VALUES (@AreaId, NULL, 'DCGTM_FECHA_EXPIRACION_NIR/A', 'Fechas de Expiración', 'Post-Start', 1, 4);

IF NOT EXISTS (SELECT 1 FROM dbo.DataCollectionGroups WHERE AreaId = @AreaId AND Code = 'DCGTM_ENTREGA_PRIMERA_PIEZA/A')
    INSERT INTO dbo.DataCollectionGroups (AreaId, OperationId, Code, Description, CollectDataAt, IsRequired, DisplayOrder)
    VALUES (@AreaId, NULL, 'DCGTM_ENTREGA_PRIMERA_PIEZA/A', 'First Shot Entregado', 'Post-Start', 0, 5);

IF NOT EXISTS (SELECT 1 FROM dbo.DataCollectionGroups WHERE AreaId = @AreaId AND Code = 'DCGTM_ENTREGA_PRIMERA_PIEZA_NIR/A')
    INSERT INTO dbo.DataCollectionGroups (AreaId, OperationId, Code, Description, CollectDataAt, IsRequired, DisplayOrder)
    VALUES (@AreaId, NULL, 'DCGTM_ENTREGA_PRIMERA_PIEZA_NIR/A', 'First Shot NIR Entregado', 'Post-Start', 0, 6);

-- El catálogo es la fuente de verdad de qué grupo es obligatorio, también en una
-- BD ya existente (donde los grupos pudieron crearse antes con otro valor).
UPDATE dbo.DataCollectionGroups
SET    IsRequired = CASE WHEN Code = 'DCGTM_FECHA_EXPIRACION_NIR/A' THEN 1 ELSE 0 END
WHERE  AreaId = @AreaId;

DECLARE @GrpMolde   INT = (SELECT Id FROM dbo.DataCollectionGroups WHERE AreaId = @AreaId AND Code = 'DCGTM_ID_MOLDE/A');
DECLARE @GrpMss     INT = (SELECT Id FROM dbo.DataCollectionGroups WHERE AreaId = @AreaId AND Code = 'DCGTM_PARAMETROS_MSS_D000281359/A');
DECLARE @GrpVerif   INT = (SELECT Id FROM dbo.DataCollectionGroups WHERE AreaId = @AreaId AND Code = 'DCGTM_VERIFICACION_MOLDEO_MAQUINA_OP/A');
DECLARE @GrpFechas  INT = (SELECT Id FROM dbo.DataCollectionGroups WHERE AreaId = @AreaId AND Code = 'DCGTM_FECHA_EXPIRACION_NIR/A');
DECLARE @GrpShot    INT = (SELECT Id FROM dbo.DataCollectionGroups WHERE AreaId = @AreaId AND Code = 'DCGTM_ENTREGA_PRIMERA_PIEZA/A');
DECLARE @GrpShotNir INT = (SELECT Id FROM dbo.DataCollectionGroups WHERE AreaId = @AreaId AND Code = 'DCGTM_ENTREGA_PRIMERA_PIEZA_NIR/A');

-- -------------------------------------------------------
-- DataCollectionParameters — 16 parámetros de Moldeo, cada uno en su
-- DC Group ("Collect" muestra solo las preguntas del grupo seleccionado).
--
-- Estructura temporal del paso de Data Collection en el legacy:
--   · Sección 1 (Pre-Start): ID del Molde                     (DCGTM_ID_MOLDE/A)
--   · Sección 2 (Pre-Start): LC Moldeo — 3 preguntas booleanas
--     *** NO son DataCollectionParameters — son Line Clearance.
--         El StepValidator las gestiona como un paso LC
--         adicional dentro del Start, hardcodeado por área. ***
--   · Sección 3 (Pre-Start): 10 parámetros MSS                 (DCGTM_PARAMETROS_MSS_D000281359/A)
--   · Sección 4 (Pre-Start): Verificación IDMold/IDIMM         (DCGTM_VERIFICACION_MOLDEO_MAQUINA_OP/A)
--   · Post-Start: Fechas de Expiración (Kerosene + Tintas)     (DCGTM_FECHA_EXPIRACION_NIR/A)
--   · Post-Start: First Shot OPE                               (DCGTM_ENTREGA_PRIMERA_PIEZA/A)
--   · Post-Start: First Shot NIR OPE                           (DCGTM_ENTREGA_PRIMERA_PIEZA_NIR/A)
--
-- MinValue/MaxValue: NULL para parámetros no numéricos y para
--   parámetros numéricos cuyo rango exacto está pendiente de
--   confirmación contra la MSS D000281359.
-- -------------------------------------------------------

-- 1. ID del Molde (Sección 1 / Pre-Start). Rango pendiente de revisión contra MSS.
IF NOT EXISTS (SELECT 1 FROM dbo.DataCollectionParameters WHERE AreaId = @AreaId AND Name = 'ID del Molde')
    INSERT INTO dbo.DataCollectionParameters (AreaId, OperationId, Name, DataType, Unit, MinValue, MaxValue, DisplayOrder, DataCollectionGroupId)
    VALUES (@AreaId, NULL, 'ID del Molde', 'Numeric', 'N/A', NULL, NULL, 1, @GrpMolde);

-- 2–11. Parámetros MSS D000281359 (Sección 3 / Pre-Start)
IF NOT EXISTS (SELECT 1 FROM dbo.DataCollectionParameters WHERE AreaId = @AreaId AND Name = 'Colling Time según MSS D000281359')
    INSERT INTO dbo.DataCollectionParameters (AreaId, OperationId, Name, DataType, Unit, MinValue, MaxValue, DisplayOrder, DataCollectionGroupId)
    VALUES (@AreaId, NULL, 'Colling Time según MSS D000281359', 'Numeric', 'N/A', NULL, NULL, 2, @GrpMss);

IF NOT EXISTS (SELECT 1 FROM dbo.DataCollectionParameters WHERE AreaId = @AreaId AND Name = 'Injection speed según MSS D000281359')
    INSERT INTO dbo.DataCollectionParameters (AreaId, OperationId, Name, DataType, Unit, MinValue, MaxValue, DisplayOrder, DataCollectionGroupId)
    VALUES (@AreaId, NULL, 'Injection speed según MSS D000281359', 'Numeric', 'N/A', NULL, NULL, 3, @GrpMss);

IF NOT EXISTS (SELECT 1 FROM dbo.DataCollectionParameters WHERE AreaId = @AreaId AND Name = 'Intensidad de Pad Printing')
    INSERT INTO dbo.DataCollectionParameters (AreaId, OperationId, Name, DataType, Unit, MinValue, MaxValue, DisplayOrder, DataCollectionGroupId)
    VALUES (@AreaId, NULL, 'Intensidad de Pad Printing', 'Numeric', 'N/A', NULL, NULL, 4, @GrpMss);

IF NOT EXISTS (SELECT 1 FROM dbo.DataCollectionParameters WHERE AreaId = @AreaId AND Name = 'Temperatura Zona 1')
    INSERT INTO dbo.DataCollectionParameters (AreaId, OperationId, Name, DataType, Unit, MinValue, MaxValue, DisplayOrder, DataCollectionGroupId)
    VALUES (@AreaId, NULL, 'Temperatura Zona 1', 'Numeric', '°C', NULL, NULL, 5, @GrpMss);

IF NOT EXISTS (SELECT 1 FROM dbo.DataCollectionParameters WHERE AreaId = @AreaId AND Name = 'Temperatura Zona 2')
    INSERT INTO dbo.DataCollectionParameters (AreaId, OperationId, Name, DataType, Unit, MinValue, MaxValue, DisplayOrder, DataCollectionGroupId)
    VALUES (@AreaId, NULL, 'Temperatura Zona 2', 'Numeric', '°C', NULL, NULL, 6, @GrpMss);

IF NOT EXISTS (SELECT 1 FROM dbo.DataCollectionParameters WHERE AreaId = @AreaId AND Name = 'Temperatura Zona 3')
    INSERT INTO dbo.DataCollectionParameters (AreaId, OperationId, Name, DataType, Unit, MinValue, MaxValue, DisplayOrder, DataCollectionGroupId)
    VALUES (@AreaId, NULL, 'Temperatura Zona 3', 'Numeric', '°C', NULL, NULL, 7, @GrpMss);

IF NOT EXISTS (SELECT 1 FROM dbo.DataCollectionParameters WHERE AreaId = @AreaId AND Name = 'Temperatura Zona 4')
    INSERT INTO dbo.DataCollectionParameters (AreaId, OperationId, Name, DataType, Unit, MinValue, MaxValue, DisplayOrder, DataCollectionGroupId)
    VALUES (@AreaId, NULL, 'Temperatura Zona 4', 'Numeric', '°C', NULL, NULL, 8, @GrpMss);

IF NOT EXISTS (SELECT 1 FROM dbo.DataCollectionParameters WHERE AreaId = @AreaId AND Name = 'Temperatura Zona 5')
    INSERT INTO dbo.DataCollectionParameters (AreaId, OperationId, Name, DataType, Unit, MinValue, MaxValue, DisplayOrder, DataCollectionGroupId)
    VALUES (@AreaId, NULL, 'Temperatura Zona 5', 'Numeric', '°C', NULL, NULL, 9, @GrpMss);

IF NOT EXISTS (SELECT 1 FROM dbo.DataCollectionParameters WHERE AreaId = @AreaId AND Name = 'Temperatura Zona 6')
    INSERT INTO dbo.DataCollectionParameters (AreaId, OperationId, Name, DataType, Unit, MinValue, MaxValue, DisplayOrder, DataCollectionGroupId)
    VALUES (@AreaId, NULL, 'Temperatura Zona 6', 'Numeric', '°C', NULL, NULL, 10, @GrpMss);

IF NOT EXISTS (SELECT 1 FROM dbo.DataCollectionParameters WHERE AreaId = @AreaId AND Name = 'Tiempo de curado de Pad Printing')
    INSERT INTO dbo.DataCollectionParameters (AreaId, OperationId, Name, DataType, Unit, MinValue, MaxValue, DisplayOrder, DataCollectionGroupId)
    VALUES (@AreaId, NULL, 'Tiempo de curado de Pad Printing', 'Numeric', 'N/A', NULL, NULL, 11, @GrpMss);

-- 12. Verificación IDMold & IDIMM (Sección 4 / Pre-Start)
IF NOT EXISTS (SELECT 1 FROM dbo.DataCollectionParameters WHERE AreaId = @AreaId AND Name = '¿Se verificó el IDMold & IDIMM según MSS?')
    INSERT INTO dbo.DataCollectionParameters (AreaId, OperationId, Name, DataType, Unit, MinValue, MaxValue, DisplayOrder, DataCollectionGroupId)
    VALUES (@AreaId, NULL, '¿Se verificó el IDMold & IDIMM según MSS?', 'Boolean', 'N/A', NULL, NULL, 12, @GrpVerif);

-- 13 y 16. Fechas de Expiración (Post-Start; Kerosene + Tintas comparten grupo)
IF NOT EXISTS (SELECT 1 FROM dbo.DataCollectionParameters WHERE AreaId = @AreaId AND Name = 'Fecha de Expiración Kerosene')
    INSERT INTO dbo.DataCollectionParameters (AreaId, OperationId, Name, DataType, Unit, MinValue, MaxValue, DisplayOrder, DataCollectionGroupId)
    VALUES (@AreaId, NULL, 'Fecha de Expiración Kerosene', 'Date', 'MM/DD/AA', NULL, NULL, 13, @GrpFechas);

IF NOT EXISTS (SELECT 1 FROM dbo.DataCollectionParameters WHERE AreaId = @AreaId AND Name = 'Fecha de Expiración de Tintas')
    INSERT INTO dbo.DataCollectionParameters (AreaId, OperationId, Name, DataType, Unit, MinValue, MaxValue, DisplayOrder, DataCollectionGroupId)
    VALUES (@AreaId, NULL, 'Fecha de Expiración de Tintas', 'Date', 'MM/DD/AA', NULL, NULL, 16, @GrpFechas);

-- 14. First Shot entregado - OPE (Post-Start)
IF NOT EXISTS (SELECT 1 FROM dbo.DataCollectionParameters WHERE AreaId = @AreaId AND Name = 'First Shot entregado - OPE')
    INSERT INTO dbo.DataCollectionParameters (AreaId, OperationId, Name, DataType, Unit, MinValue, MaxValue, DisplayOrder, DataCollectionGroupId)
    VALUES (@AreaId, NULL, 'First Shot entregado - OPE', 'Boolean', 'N/A', NULL, NULL, 14, @GrpShot);

-- 15. First Shot NIR entregado - OPE (Post-Start)
IF NOT EXISTS (SELECT 1 FROM dbo.DataCollectionParameters WHERE AreaId = @AreaId AND Name = 'First Shot NIR entregado - OPE')
    INSERT INTO dbo.DataCollectionParameters (AreaId, OperationId, Name, DataType, Unit, MinValue, MaxValue, DisplayOrder, DataCollectionGroupId)
    VALUES (@AreaId, NULL, 'First Shot NIR entregado - OPE', 'Boolean', 'N/A', NULL, NULL, 15, @GrpShotNir);

-- Description de cada parámetro (línea "Description: …" de Data Collection Entry,
-- Fase 6.5). Tomadas del legacy (UI/Dashboard/main.aspx y main.v1.0.0.js); las de
-- First Shot no existen en el legacy a nivel de parámetro y se redactaron a partir de
-- su pregunta ("Se entrega First Shot a calidad"). La define este script, también en
-- una BD ya existente.
UPDATE dbo.DataCollectionParameters
SET    Description = CASE Name
           WHEN 'ID del Molde'                              THEN 'ID Molde'
           WHEN 'Colling Time según MSS D000281359'         THEN 'Colling Time según MSS D000281359'
           WHEN 'Injection speed según MSS D000281359'      THEN 'Resultado según MSS D000281359 VERT 1'
           WHEN 'Intensidad de Pad Printing'                THEN 'Ingrese la intensidad de la Pad Printing'
           WHEN 'Temperatura Zona 1'                        THEN 'Temperatura de Molde Zona 1'
           WHEN 'Temperatura Zona 2'                        THEN 'Temperatura de Molde Zona 2'
           WHEN 'Temperatura Zona 3'                        THEN 'Temperatura de Molde Zona 3'
           WHEN 'Temperatura Zona 4'                        THEN 'Temperatura de Molde Zona 4'
           WHEN 'Temperatura Zona 5'                        THEN 'Temperatura de Molde Zona 5'
           WHEN 'Temperatura Zona 6'                        THEN 'Temperatura de Molde Zona 6'
           WHEN 'Tiempo de curado de Pad Printing'          THEN 'Ingrese el tiempo de curado de la Pad Printing'
           WHEN '¿Se verificó el IDMold & IDIMM según MSS?' THEN 'ID Mold & IDIMM segun MSS?'
           WHEN 'Fecha de Expiración Kerosene'              THEN 'Ingrese la fecha de expiración kerosene'
           WHEN 'Fecha de Expiración de Tintas'             THEN 'Ingrese la fecha de expiración de Priscone'
           WHEN 'First Shot entregado - OPE'                THEN 'Entrega de First Shot a calidad (OPE)'
           WHEN 'First Shot NIR entregado - OPE'            THEN 'Entrega de First Shot NIR a calidad (OPE)'
       END
WHERE  AreaId = @AreaId;
