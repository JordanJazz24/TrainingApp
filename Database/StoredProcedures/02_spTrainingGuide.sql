-- ============================================================
-- 02_spTrainingGuide.sql
-- Stored procedures para TrainingGuides, TrainingSteps,
-- Areas, NCCodes, DataCollectionGroups, DataCollectionParameters
-- y AssemblyComponents.
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

-- -------------------------------------------------------
-- spProductGetAll
-- -------------------------------------------------------
IF OBJECT_ID('dbo.spProductGetAll', 'P') IS NOT NULL
    DROP PROCEDURE dbo.spProductGetAll;
GO

CREATE PROCEDURE dbo.spProductGetAll
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Name
    FROM   dbo.Products
    ORDER  BY Name;
END;
GO

-- -------------------------------------------------------
-- spAreaGetByProduct
-- -------------------------------------------------------
IF OBJECT_ID('dbo.spAreaGetByProduct', 'P') IS NOT NULL
    DROP PROCEDURE dbo.spAreaGetByProduct;
GO

CREATE PROCEDURE dbo.spAreaGetByProduct
    @ProductId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, ProductId, Name, SerializationType, RequiresAssembly, RequiresDataCollection,
           RequiresNCComponent, RequiresStartChecklist
    FROM   dbo.Areas
    WHERE  ProductId = @ProductId
    ORDER  BY Name;
END;
GO

-- -------------------------------------------------------
-- spAreaGetById
-- -------------------------------------------------------
IF OBJECT_ID('dbo.spAreaGetById', 'P') IS NOT NULL
    DROP PROCEDURE dbo.spAreaGetById;
GO

CREATE PROCEDURE dbo.spAreaGetById
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, ProductId, Name, SerializationType, RequiresAssembly, RequiresDataCollection,
           RequiresNCComponent, RequiresStartChecklist
    FROM   dbo.Areas
    WHERE  Id = @Id;
END;
GO

-- -------------------------------------------------------
-- spNCCodeGetByArea
-- -------------------------------------------------------
IF OBJECT_ID('dbo.spNCCodeGetByArea', 'P') IS NOT NULL
    DROP PROCEDURE dbo.spNCCodeGetByArea;
GO

CREATE PROCEDURE dbo.spNCCodeGetByArea
    @AreaId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, AreaId, Code, Description
    FROM   dbo.NCCodes
    WHERE  AreaId = @AreaId
    ORDER  BY Code;
END;
GO

-- -------------------------------------------------------
-- spDataCollectionGroupGetByArea
-- DC Groups del área en el orden de la Data Collection List.
-- -------------------------------------------------------
IF OBJECT_ID('dbo.spDataCollectionGroupGetByArea', 'P') IS NOT NULL
    DROP PROCEDURE dbo.spDataCollectionGroupGetByArea;
GO

CREATE PROCEDURE dbo.spDataCollectionGroupGetByArea
    @AreaId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, AreaId, OperationId, Code, Description, CollectDataAt, IsRequired, DisplayOrder
    FROM   dbo.DataCollectionGroups
    WHERE  AreaId = @AreaId
    ORDER  BY DisplayOrder, Id;
END;
GO

-- -------------------------------------------------------
-- spDataCollectionParameterGetByArea
-- Ordenado por DisplayOrder para respetar la secuencia operativa.
-- -------------------------------------------------------
IF OBJECT_ID('dbo.spDataCollectionParameterGetByArea', 'P') IS NOT NULL
    DROP PROCEDURE dbo.spDataCollectionParameterGetByArea;
GO

CREATE PROCEDURE dbo.spDataCollectionParameterGetByArea
    @AreaId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, AreaId, OperationId, DataCollectionGroupId, Name, Description, DataType, Unit,
           MinValue, MaxValue, DisplayOrder
    FROM   dbo.DataCollectionParameters
    WHERE  AreaId = @AreaId
    ORDER  BY DisplayOrder;
END;
GO

-- -------------------------------------------------------
-- spAssemblyComponentGetByArea
-- BOM de la operación (hoy por Area) en el orden de la Component List.
-- -------------------------------------------------------
IF OBJECT_ID('dbo.spAssemblyComponentGetByArea', 'P') IS NOT NULL
    DROP PROCEDURE dbo.spAssemblyComponentGetByArea;
GO

CREATE PROCEDURE dbo.spAssemblyComponentGetByArea
    @AreaId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, AreaId, OperationId, PartNumber, Revision, Description, Qty, DisplayOrder
    FROM   dbo.AssemblyComponents
    WHERE  AreaId = @AreaId
    ORDER  BY DisplayOrder, Id;
END;
GO
