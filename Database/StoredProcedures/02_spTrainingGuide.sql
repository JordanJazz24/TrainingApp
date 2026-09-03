-- ============================================================
-- 02_spTrainingGuide.sql
-- Stored procedures para TrainingGuides y TrainingSteps.
-- ============================================================

-- -------------------------------------------------------
-- spTrainingGuideGetById
-- Devuelve DOS result sets:
--   1. Una fila con los datos de la guía.
--   2. Las filas de TrainingSteps de esa guía ordenadas por [Order].
-- El repositorio usa reader.NextResult() para leer ambos.
-- -------------------------------------------------------
IF OBJECT_ID('dbo.spTrainingGuideGetById', 'P') IS NOT NULL
    DROP PROCEDURE dbo.spTrainingGuideGetById;
GO

CREATE PROCEDURE dbo.spTrainingGuideGetById
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    -- Result set 1: guide
    SELECT Id, AreaId, OperationId, Title, IsPublished, CreatedAt
    FROM   dbo.TrainingGuides
    WHERE  Id = @Id;

    -- Result set 2: steps (siempre poblado — una guía sin pasos es inválida)
    SELECT Id, TrainingGuideId, StepType, [Order], Weight
    FROM   dbo.TrainingSteps
    WHERE  TrainingGuideId = @Id
    ORDER  BY [Order];
END;
GO

-- -------------------------------------------------------
-- spTrainingGuideGetByArea
-- Devuelve lista de guías publicadas para un Area.
-- No incluye pasos (carga bajo demanda con GetById).
-- -------------------------------------------------------
IF OBJECT_ID('dbo.spTrainingGuideGetByArea', 'P') IS NOT NULL
    DROP PROCEDURE dbo.spTrainingGuideGetByArea;
GO

CREATE PROCEDURE dbo.spTrainingGuideGetByArea
    @AreaId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, AreaId, OperationId, Title, IsPublished, CreatedAt
    FROM   dbo.TrainingGuides
    WHERE  AreaId      = @AreaId
    AND    IsPublished = 1
    ORDER  BY CreatedAt DESC;
END;
GO
