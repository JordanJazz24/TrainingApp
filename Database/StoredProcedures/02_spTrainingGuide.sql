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
-- nuevas); la app no lo llama. Deja la guía publicada (@AreaId, @Title) con los
-- pasos de @Steps, un JSON [{"Order":1,"StepType":0,"Weight":0.20}, …]:
--   · si no hay una guía publicada con ese título, la crea publicada con sus pasos;
--   · si existe con la misma forma, actualiza el Weight de cada paso;
--   · si cambió de forma (otro número de pasos, o un paso con otro StepType), la
--     despublica y crea una guía nueva publicada con el mismo título (spec 014,
--     D-56): las sesiones en curso y el historial siguen con la vieja, porque sus
--     intentos apuntan a esos pasos.
-- Aborta sin cambiar nada si los pesos no suman 1.00 (el puntaje no normaliza:
-- docs/domain.md → "Ponderación de pasos") o si el orden no es 1..N.
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
    IF EXISTS (SELECT 1 FROM @S WHERE StepType > 10 OR Weight < 0 OR Weight > 1)
        THROW 50012, N'spTrainingGuideSync: StepType fuera de 0-10 o Weight fuera de 0.00-1.00.', 1;
    IF @Sum <> 1.00
    BEGIN
        DECLARE @MsgSum NVARCHAR(200) = CONCAT(N'spTrainingGuideSync: los pesos de "', @Title, N'" suman ', @Sum, N'; tienen que sumar 1.00.');
        THROW 50013, @MsgSum, 1;
    END;

    BEGIN TRANSACTION;

    -- La versión vigente: la publicada más nueva con ese título (las anteriores
    -- quedan despublicadas por D-56).
    DECLARE @GuideId INT = (SELECT TOP (1) Id FROM dbo.TrainingGuides WITH (UPDLOCK, HOLDLOCK)
                            WHERE AreaId = @AreaId AND Title = @Title AND IsPublished = 1
                            ORDER BY Id DESC);

    IF @GuideId IS NOT NULL AND (
           EXISTS (SELECT 1 FROM dbo.TrainingSteps t
                   LEFT JOIN @S s ON s.[Order] = t.[Order]
                   WHERE  t.TrainingGuideId = @GuideId
                   AND    (s.[Order] IS NULL OR s.StepType <> t.StepType))
        OR (SELECT COUNT(*) FROM dbo.TrainingSteps WHERE TrainingGuideId = @GuideId) <> @Count)
    BEGIN
        -- D-56: cambió la forma. La vieja se despublica y queda para su historial.
        UPDATE dbo.TrainingGuides SET IsPublished = 0 WHERE Id = @GuideId;
        PRINT CONCAT(N'Guía "', @Title, N'" (Id ', @GuideId, N'): cambió de forma; se despublica y se crea una versión nueva.');
        SET @GuideId = NULL;
    END;

    IF @GuideId IS NULL
    BEGIN
        INSERT INTO dbo.TrainingGuides (AreaId, OperationId, Title, IsPublished, CreatedAt)
        VALUES (@AreaId, NULL, @Title, 1, SYSUTCDATETIME());
        SET @GuideId = SCOPE_IDENTITY();
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
