-- ============================================================
-- 00_Schema.sql
-- Crea todas las tablas del esquema TrainingApp.
-- Seguro para re-ejecución: usa IF NOT EXISTS / IF OBJECT_ID.
-- ============================================================

-- -------------------------------------------------------
-- Products
-- -------------------------------------------------------
IF OBJECT_ID('dbo.Products', 'U') IS NULL
CREATE TABLE dbo.Products (
    Id   INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL
);

-- -------------------------------------------------------
-- Areas
-- SerializationType: 0=Serialized, 1=NonSerialized
-- -------------------------------------------------------
IF OBJECT_ID('dbo.Areas', 'U') IS NULL
CREATE TABLE dbo.Areas (
    Id                    INT IDENTITY(1,1) PRIMARY KEY,
    ProductId             INT NOT NULL REFERENCES dbo.Products(Id),
    Name                  NVARCHAR(100) NOT NULL,
    SerializationType     TINYINT NOT NULL DEFAULT 0,
    RequiresAssembly      BIT NOT NULL DEFAULT 0,
    RequiresDataCollection BIT NOT NULL DEFAULT 0
);

-- -------------------------------------------------------
-- Operations  (futuro — FK nullable en TrainingGuides)
-- -------------------------------------------------------
IF OBJECT_ID('dbo.Operations', 'U') IS NULL
CREATE TABLE dbo.Operations (
    Id     INT IDENTITY(1,1) PRIMARY KEY,
    AreaId INT NOT NULL REFERENCES dbo.Areas(Id),
    Name   NVARCHAR(100) NOT NULL
);

-- -------------------------------------------------------
-- Operators
-- -------------------------------------------------------
IF OBJECT_ID('dbo.Operators', 'U') IS NULL
CREATE TABLE dbo.Operators (
    Id           INT IDENTITY(1,1) PRIMARY KEY,
    Code1        NVARCHAR(50)  NOT NULL,
    Name         NVARCHAR(200) NOT NULL,
    PasswordHash NVARCHAR(500) NOT NULL,
    IsFirstLogin BIT           NOT NULL DEFAULT 1,
    CreatedAt    DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT UQ_Operators_Code1 UNIQUE (Code1)
);

-- -------------------------------------------------------
-- TrainingGuides
-- OperationId nullable: previsto para el futuro, hoy siempre NULL.
-- -------------------------------------------------------
IF OBJECT_ID('dbo.TrainingGuides', 'U') IS NULL
CREATE TABLE dbo.TrainingGuides (
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    AreaId      INT           NOT NULL REFERENCES dbo.Areas(Id),
    OperationId INT           NULL     REFERENCES dbo.Operations(Id),
    Title       NVARCHAR(200) NOT NULL,
    IsPublished BIT           NOT NULL DEFAULT 0,
    CreatedAt   DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME()
);

-- -------------------------------------------------------
-- TrainingSteps
-- StepType: 0=ResourceSetup, 1=LineClearance, 2=Search,
--           3=Assembly, 4=DataCollection, 5=LogNC, 6=Complete
-- Weight: peso porcentual del paso (0.00-1.00), ej. 0.20
-- -------------------------------------------------------
IF OBJECT_ID('dbo.TrainingSteps', 'U') IS NULL
CREATE TABLE dbo.TrainingSteps (
    Id              INT IDENTITY(1,1) PRIMARY KEY,
    TrainingGuideId INT          NOT NULL REFERENCES dbo.TrainingGuides(Id),
    StepType        TINYINT      NOT NULL,
    [Order]         INT          NOT NULL,
    Weight          DECIMAL(5,2) NOT NULL,
    CONSTRAINT UQ_TrainingSteps_GuideOrder UNIQUE (TrainingGuideId, [Order])
);

-- -------------------------------------------------------
-- TrainingSessions
-- Mode:   0=Guided, 1=Assessment
-- Status: 0=InProgress, 1=Completed, 2=Abandoned
-- -------------------------------------------------------
IF OBJECT_ID('dbo.TrainingSessions', 'U') IS NULL
CREATE TABLE dbo.TrainingSessions (
    Id               INT IDENTITY(1,1) PRIMARY KEY,
    OperatorId       INT          NOT NULL REFERENCES dbo.Operators(Id),
    TrainingGuideId  INT          NOT NULL REFERENCES dbo.TrainingGuides(Id),
    Mode             TINYINT      NOT NULL,
    Status           TINYINT      NOT NULL DEFAULT 0,
    CurrentStepOrder INT          NOT NULL DEFAULT 1,
    FinalScore       DECIMAL(5,2) NOT NULL DEFAULT 0,
    StartedAt        DATETIME2    NOT NULL DEFAULT SYSUTCDATETIME(),
    CompletedAt      DATETIME2    NULL
);

-- -------------------------------------------------------
-- TrainingSessionStepAttempts
-- -------------------------------------------------------
IF OBJECT_ID('dbo.TrainingSessionStepAttempts', 'U') IS NULL
CREATE TABLE dbo.TrainingSessionStepAttempts (
    Id            INT IDENTITY(1,1) PRIMARY KEY,
    SessionId     INT       NOT NULL REFERENCES dbo.TrainingSessions(Id),
    StepId        INT       NOT NULL REFERENCES dbo.TrainingSteps(Id),
    AttemptNumber INT       NOT NULL,
    IsCorrect     BIT       NOT NULL,
    Timestamp     DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_StepAttempts_Session'
               AND object_id = OBJECT_ID('dbo.TrainingSessionStepAttempts'))
    CREATE INDEX IX_StepAttempts_Session ON dbo.TrainingSessionStepAttempts (SessionId);

-- -------------------------------------------------------
-- NCCodes  (varían por Area)
-- -------------------------------------------------------
IF OBJECT_ID('dbo.NCCodes', 'U') IS NULL
CREATE TABLE dbo.NCCodes (
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    AreaId      INT           NOT NULL REFERENCES dbo.Areas(Id),
    Code        NVARCHAR(50)  NOT NULL,
    Description NVARCHAR(200) NOT NULL
);

-- -------------------------------------------------------
-- DataCollectionParameters  (varían por Area; a futuro por Operation)
-- DataType: 'Numeric' | 'Boolean' | 'Date'
-- MinValue/MaxValue: NULL para tipos no numéricos, o cuando el rango
--   está pendiente de confirmación contra la MSS del área.
-- DisplayOrder: orden de presentación al operador dentro del paso.
-- -------------------------------------------------------
IF OBJECT_ID('dbo.DataCollectionParameters', 'U') IS NULL
CREATE TABLE dbo.DataCollectionParameters (
    Id           INT IDENTITY(1,1) PRIMARY KEY,
    AreaId       INT            NOT NULL REFERENCES dbo.Areas(Id),
    OperationId  INT            NULL     REFERENCES dbo.Operations(Id),
    Name         NVARCHAR(200)  NOT NULL,
    DataType     NVARCHAR(20)   NOT NULL DEFAULT 'Numeric',
    Unit         NVARCHAR(50)   NOT NULL DEFAULT 'N/A',
    MinValue     DECIMAL(10,4)  NULL,
    MaxValue     DECIMAL(10,4)  NULL,
    DisplayOrder INT            NOT NULL DEFAULT 0
);
