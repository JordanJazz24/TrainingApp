-- ============================================================
-- 02_spTrainingGuide.sql — SPs de lectura de guías y catálogos
-- (TrainingGuides, TrainingSteps, Products, Areas, NCCodes,
-- DataCollectionGroups, DataCollectionParameters, AssemblyComponents)
-- y spTrainingGuideSync, que usan solo los scripts de guía.
-- Requiere: 00_Schema.sql · Re-ejecutable: sí (CREATE OR ALTER).
-- ============================================================

-- spTrainingGuideGetById
-- Devuelve DOS result sets:
--   1. Una fila con los datos de la guía.
--   2. Las filas de TrainingSteps de esa guía ordenadas por [Order].
-- El repositorio usa reader.NextResult() para leer ambos.
CREATE OR ALTER PROCEDURE dbo.spTrainingGuideGetById
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

-- spTrainingGuideGetByArea
-- Devuelve lista de guías publicadas para un Area.
-- No incluye pasos (carga bajo demanda con GetById).
CREATE OR ALTER PROCEDURE dbo.spTrainingGuideGetByArea
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

-- spProductGetAll
CREATE OR ALTER PROCEDURE dbo.spProductGetAll
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Name
    FROM   dbo.Products
    ORDER  BY Name;
END;
GO

-- spAreaGetByProduct
CREATE OR ALTER PROCEDURE dbo.spAreaGetByProduct
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

-- spAreaGetById
CREATE OR ALTER PROCEDURE dbo.spAreaGetById
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

-- spNCCodeGetByArea
CREATE OR ALTER PROCEDURE dbo.spNCCodeGetByArea
    @AreaId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, AreaId, Code, Description, Method, RequiresComments
    FROM   dbo.NCCodes
    WHERE  AreaId = @AreaId
    ORDER  BY Code;
END;
GO

-- spDataCollectionGroupGetByArea
-- DC Groups del área en el orden de la Data Collection List.
CREATE OR ALTER PROCEDURE dbo.spDataCollectionGroupGetByArea
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

-- spDataCollectionParameterGetByArea
-- Ordenado por DisplayOrder para respetar la secuencia operativa.
CREATE OR ALTER PROCEDURE dbo.spDataCollectionParameterGetByArea
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

-- spAssemblyComponentGetByArea
-- BOM de la operación (hoy por Area) en el orden de la Component List.
CREATE OR ALTER PROCEDURE dbo.spAssemblyComponentGetByArea
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

-- spTrainingGuideSync — SOLO para los scripts de guía (05, 07 y los de áreas
-- nuevas); la app no lo llama. Deja la guía (@AreaId, @Title) con los pasos de
-- @Steps, un JSON [{"Order":1,"StepType":0,"Weight":0.20}, …]:
--   · si la guía no existe, la crea publicada con sus pasos;
--   · si existe, actualiza el Weight de cada paso y agrega los que falten.
-- Aborta sin cambiar nada si los pesos no suman 1.00 (el puntaje no normaliza:
-- CLAUDE.md → "Ponderación de pasos"), si el orden no es 1..N, o si la guía
-- existente tiene un paso que el script ya no trae o con otro StepType: los
-- intentos guardados apuntan a esos pasos, así que cambiar la estructura de una
-- guía en uso se resuelve a mano.
CREATE OR ALTER PROCEDURE dbo.spTrainingGuideSync
    @AreaId INT,
    @Title  NVARCHAR(200),
    @Steps  NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @S TABLE ([Order] INT NOT NULL PRIMARY KEY, StepType TINYINT NOT NULL, Weight DECIMAL(5,2) NOT NULL);
    INSERT INTO @S ([Order], StepType, Weight)
    SELECT [Order], StepType, Weight
    FROM   OPENJSON(@Steps) WITH ([Order] INT, StepType TINYINT, Weight DECIMAL(5,2));

    DECLARE @Count INT, @Max INT, @Sum DECIMAL(9,2);
    SELECT @Count = COUNT(*), @Max = MAX([Order]), @Sum = SUM(Weight) FROM @S;

    IF NOT EXISTS (SELECT 1 FROM dbo.Areas WHERE Id = @AreaId)
        THROW 50010, N'spTrainingGuideSync: el área no existe (¿se corrió su script de catálogo?).', 1;
    IF @Count = 0 OR @Max <> @Count OR EXISTS (SELECT 1 FROM @S WHERE [Order] < 1)
        THROW 50011, N'spTrainingGuideSync: el orden de los pasos tiene que ser 1..N, sin huecos.', 1;
    IF EXISTS (SELECT 1 FROM @S WHERE StepType > 8 OR Weight < 0 OR Weight > 1)
        THROW 50012, N'spTrainingGuideSync: StepType fuera de 0-8 o Weight fuera de 0.00-1.00.', 1;
    IF @Sum <> 1.00
    BEGIN
        DECLARE @MsgSum NVARCHAR(200) = CONCAT(N'spTrainingGuideSync: los pesos de "', @Title, N'" suman ', @Sum, N'; tienen que sumar 1.00.');
        THROW 50013, @MsgSum, 1;
    END;

    BEGIN TRANSACTION;

    DECLARE @GuideId INT = (SELECT Id FROM dbo.TrainingGuides WITH (UPDLOCK, HOLDLOCK)
                            WHERE AreaId = @AreaId AND Title = @Title);
    IF @GuideId IS NULL
    BEGIN
        INSERT INTO dbo.TrainingGuides (AreaId, OperationId, Title, IsPublished, CreatedAt)
        VALUES (@AreaId, NULL, @Title, 1, SYSUTCDATETIME());
        SET @GuideId = SCOPE_IDENTITY();
    END;

    IF EXISTS (SELECT 1 FROM dbo.TrainingSteps t
               LEFT JOIN @S s ON s.[Order] = t.[Order]
               WHERE  t.TrainingGuideId = @GuideId
               AND    (s.[Order] IS NULL OR s.StepType <> t.StepType))
    BEGIN
        DECLARE @MsgShape NVARCHAR(300) = CONCAT(N'spTrainingGuideSync: "', @Title, N'" ya tiene pasos que el script no trae o con otro StepType. Cambiar la estructura de una guía existente se hace a mano.');
        THROW 50014, @MsgShape, 1;
    END;

    UPDATE t
    SET    Weight = s.Weight
    FROM   dbo.TrainingSteps t
    JOIN   @S s ON s.[Order] = t.[Order]
    WHERE  t.TrainingGuideId = @GuideId
    AND    t.Weight <> s.Weight;

    INSERT INTO dbo.TrainingSteps (TrainingGuideId, StepType, [Order], Weight)
    SELECT @GuideId, s.StepType, s.[Order], s.Weight
    FROM   @S s
    WHERE  NOT EXISTS (SELECT 1 FROM dbo.TrainingSteps t
                       WHERE t.TrainingGuideId = @GuideId AND t.[Order] = s.[Order])
    ORDER  BY s.[Order];

    COMMIT TRANSACTION;

    PRINT CONCAT(N'Guía "', @Title, N'" (Id ', @GuideId, N'): ', @Count, N' pasos, pesos = ', @Sum, N'.');
END;
GO
