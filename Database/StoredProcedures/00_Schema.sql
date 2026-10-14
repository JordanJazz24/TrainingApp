-- ============================================================
-- 00_Schema.sql — tablas del esquema de TrainingApp.
-- Re-ejecutable: sí (crea solo las tablas e índices que faltan).
--
-- Scripts de Database/StoredProcedures, en orden numérico:
--   00_Schema.sql            tablas
--   01_spUser.sql            SPs de Users
--   02_spTrainingGuide.sql   SPs de guías y catálogos + spTrainingGuideSync
--   03_spTrainingSession.sql SPs de sesiones
--   04_spStepAttempt.sql     SPs de intentos por paso
--   05_SeedData.sql          DATOS DE EJEMPLO (solo desarrollo)
--   06_RCMoldeoCatalog.sql   catálogo real de S&RC / Moldeo
--   07_RCMoldeoGuide.sql     guía real de S&RC / Moldeo
--   08_SensoresLoFloCatalog.sql  catálogo de Sensores / LoFlo (flags reales; NC, DC y BOM simulados)
--   09_SensoresLoFloGuide.sql    guía de Sensores / LoFlo
-- Todos se pueden volver a correr sobre una BD existente. Reglas para
-- escribirlos (esquema, SPs, datos, áreas nuevas): docs/process.md →
-- "Base de datos".
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
-- RequiresNCComponent: NC Data Entry pide el campo Component
--   (la norma es pedirlo; Moldeo es la excepción).
-- RequiresStartChecklist: Start abre la secuencia de 4 modales
--   (ID de molde, LC, MSS, verificación); 0 = Start directo (spec 008).
-- -------------------------------------------------------
IF OBJECT_ID('dbo.Areas', 'U') IS NULL
CREATE TABLE dbo.Areas (
    Id                     INT IDENTITY(1,1) PRIMARY KEY,
    ProductId              INT NOT NULL REFERENCES dbo.Products(Id),
    Name                   NVARCHAR(100) NOT NULL,
    SerializationType      TINYINT NOT NULL DEFAULT 0,
    RequiresAssembly       BIT NOT NULL DEFAULT 0,
    RequiresDataCollection BIT NOT NULL DEFAULT 0,
    RequiresNCComponent    BIT NOT NULL CONSTRAINT DF_Areas_RequiresNCComponent DEFAULT 1,
    RequiresStartChecklist BIT NOT NULL CONSTRAINT DF_Areas_RequiresStartChecklist DEFAULT 0
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
-- Users  (cuenta local de un empleado de la API; antes Operators, D-17)
-- Role: 1=Operator, 2=Administrator (spec 013; roles excluyentes)
-- LastLoginAt / LockedUntil en UTC. FailedLoginCount: fallos seguidos.
-- -------------------------------------------------------
IF OBJECT_ID('dbo.Users', 'U') IS NULL
CREATE TABLE dbo.Users (
    Id               INT IDENTITY(1,1) PRIMARY KEY,
    Code1            NVARCHAR(50)  NOT NULL,
    Name             NVARCHAR(200) NOT NULL,
    PasswordHash     NVARCHAR(500) NOT NULL,
    IsFirstLogin     BIT           NOT NULL DEFAULT 1,
    CreatedAt        DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
    Role             TINYINT       NOT NULL DEFAULT 1,
    LastLoginAt      DATETIME2     NULL,
    FailedLoginCount INT           NOT NULL DEFAULT 0,
    LockedUntil      DATETIME2     NULL,
    CONSTRAINT UQ_Users_Code1 UNIQUE (Code1)
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
--           3=Assembly, 4=DataCollection, 5=SFC_Split, 6=LogNC, 7=Complete
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
    UserId           INT          NOT NULL REFERENCES dbo.Users(Id),
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
-- Reason: AttemptReason de la BLL (0=None, 1=OutOfSequence, 2=WrongDecision,
--         3=InvalidAction, 4=MissingSelection, 5=EmptyRequiredField); Action: código de
--         la acción del operador (AttemptActions). Null en los intentos anteriores a la
--         spec 013.
-- -------------------------------------------------------
IF OBJECT_ID('dbo.TrainingSessionStepAttempts', 'U') IS NULL
CREATE TABLE dbo.TrainingSessionStepAttempts (
    Id            INT IDENTITY(1,1) PRIMARY KEY,
    SessionId     INT          NOT NULL REFERENCES dbo.TrainingSessions(Id),
    StepId        INT          NOT NULL REFERENCES dbo.TrainingSteps(Id),
    AttemptNumber INT          NOT NULL,
    IsCorrect     BIT          NOT NULL,
    Timestamp     DATETIME2    NOT NULL DEFAULT SYSUTCDATETIME(),
    Reason        TINYINT      NULL,
    Action        NVARCHAR(50) NULL
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
-- DataCollectionGroups  (DC Groups por Area; a futuro por Operation)
-- Code: código MES del grupo (ej. 'DCGTM_ID_MOLDE/A'); único por Area.
-- CollectDataAt: 'Pre-Start' | 'Post-Start' | 'Anytime'.
-- IsRequired: la pieza queda completa cuando guardó todos los grupos
--   obligatorios; si el área no tiene ninguno, basta un grupo cualquiera
--   (spec 008). La UI no indica cuáles son obligatorios.
-- DisplayOrder: orden en la Data Collection List.
-- -------------------------------------------------------
IF OBJECT_ID('dbo.DataCollectionGroups', 'U') IS NULL
CREATE TABLE dbo.DataCollectionGroups (
    Id            INT IDENTITY(1,1) PRIMARY KEY,
    AreaId        INT            NOT NULL REFERENCES dbo.Areas(Id),
    OperationId   INT            NULL     REFERENCES dbo.Operations(Id),
    Code          NVARCHAR(100)  NOT NULL,
    Description   NVARCHAR(200)  NOT NULL,
    CollectDataAt NVARCHAR(50)   NOT NULL CONSTRAINT DF_DCGroup_CollectDataAt DEFAULT 'Anytime',
    IsRequired    BIT            NOT NULL CONSTRAINT DF_DCGroup_IsRequired    DEFAULT 0,
    DisplayOrder  INT            NOT NULL CONSTRAINT DF_DCGroup_DisplayOrder  DEFAULT 0,
    CONSTRAINT UQ_DCGroup_Area_Code UNIQUE (AreaId, Code)
);

-- -------------------------------------------------------
-- DataCollectionParameters  (varían por Area; a futuro por Operation)
-- DataType: 'Numeric' | 'Boolean' | 'Date'
-- MinValue/MaxValue: NULL para tipos no numéricos, o cuando el rango
--   está pendiente de confirmación contra la MSS del área.
-- DisplayOrder: orden de presentación al operador dentro del paso.
-- DataCollectionGroupId: DC Group al que pertenece el parámetro;
--   "Collect" muestra solo las preguntas del grupo seleccionado.
-- Description: texto corto que el MES muestra bajo la pregunta en Data
--   Collection Entry ("Description: …"). Opcional: sin texto no se muestra
--   (Fase 6.5, entrega 8).
-- -------------------------------------------------------
IF OBJECT_ID('dbo.DataCollectionParameters', 'U') IS NULL
CREATE TABLE dbo.DataCollectionParameters (
    Id                    INT IDENTITY(1,1) PRIMARY KEY,
    AreaId                INT            NOT NULL REFERENCES dbo.Areas(Id),
    OperationId           INT            NULL     REFERENCES dbo.Operations(Id),
    DataCollectionGroupId INT            NOT NULL REFERENCES dbo.DataCollectionGroups(Id),
    Name                  NVARCHAR(200)  NOT NULL,
    Description           NVARCHAR(200)  NULL,
    DataType              NVARCHAR(20)   NOT NULL DEFAULT 'Numeric',
    Unit                  NVARCHAR(50)   NOT NULL DEFAULT 'N/A',
    MinValue              DECIMAL(10,4)  NULL,
    MaxValue              DECIMAL(10,4)  NULL,
    DisplayOrder          INT            NOT NULL DEFAULT 0
);

-- -------------------------------------------------------
-- AssemblyComponents  (BOM de la operación; hoy por Area, a futuro
--   por Operation — spec 008). La Component List de Assembly muestra
--   PartNumber/Revision, Qty por unidad y Description.
-- Qty: decimal (materiales por peso o longitud, ej. 0.0001).
-- -------------------------------------------------------
IF OBJECT_ID('dbo.AssemblyComponents', 'U') IS NULL
CREATE TABLE dbo.AssemblyComponents (
    Id           INT IDENTITY(1,1) PRIMARY KEY,
    AreaId       INT            NOT NULL REFERENCES dbo.Areas(Id),
    OperationId  INT            NULL     REFERENCES dbo.Operations(Id),
    PartNumber   NVARCHAR(50)   NOT NULL,
    Revision     NVARCHAR(10)   NOT NULL CONSTRAINT DF_AsmComp_Revision     DEFAULT 'A',
    Description  NVARCHAR(200)  NOT NULL,
    Qty          DECIMAL(18,6)  NOT NULL CONSTRAINT DF_AsmComp_Qty          DEFAULT 1,
    DisplayOrder INT            NOT NULL CONSTRAINT DF_AsmComp_DisplayOrder DEFAULT 0,
    CONSTRAINT UQ_AsmComp_Area_Part UNIQUE (AreaId, PartNumber, Revision)
);
