-- ============================================================
-- 11_AddRequiresNCComponentToAreas.sql
-- Agrega Areas.RequiresNCComponent (spec 007).
--
-- Motivo: NC Data Entry pide el campo Component solo en las áreas que lo usan.
-- La norma (general-flow.md) es pedirlo; Moldeo es la excepción.
--
-- Valores:
--   Moldeo (R&C)            = 0
--   Resto (incl. ejemplos)  = 1 (DEFAULT)
--   LoFlo                   = 1 provisorio — se confirma en Fase 7.
--
-- Prerequisitos: 00_Schema.sql, 06_RCMoldeoCatalog.sql
-- En una BD existente: ejecutar este script y DESPUÉS 02_spTrainingGuide.sql
-- (spAreaGetById / spAreaGetByProduct devuelven la columna nueva).
-- Seguro para re-ejecución: ALTER solo si la columna no existe; UPDATE idempotente.
-- ============================================================

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.Areas') AND name = 'RequiresNCComponent'
)
    ALTER TABLE dbo.Areas
    ADD RequiresNCComponent BIT NOT NULL CONSTRAINT DF_Areas_RequiresNCComponent DEFAULT 1;
GO

UPDATE a
SET    a.RequiresNCComponent = 0
FROM   dbo.Areas a
JOIN   dbo.Products p ON p.Id = a.ProductId
WHERE  p.Name = 'R&C' AND a.Name = 'Moldeo' AND a.RequiresNCComponent = 1;
GO
