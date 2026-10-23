-- ============================================================
-- 10_spDashboard.sql — SPs de solo lectura del dashboard de
-- Training (spec 018). Agregan sin reglas (D-139): qué es aprobado,
-- el estado, las tasas y el seguimiento los decide la BLL
-- (ReadinessPolicy). Solo guías publicadas y usuarios con rol
-- Operator (DA-9).
-- Requiere: 00_Schema.sql · Re-ejecutable: sí (CREATE OR ALTER).
--
-- Role: 1 = Operator. Mode: 0 = Guided, 1 = Assessment,
-- 2 = FreePractice. Status: 0 = InProgress, 1 = Completed,
-- 2 = Abandoned. Reason 6 = Skipped (no es un fallo).
-- ============================================================

-- spDashboardGuides — las guías publicadas, con su producto y área.
CREATE OR ALTER PROCEDURE dbo.spDashboardGuides
AS
BEGIN
    SET NOCOUNT ON;
    SELECT g.Id, g.Title, a.Name AS AreaName, p.Name AS ProductName
    FROM dbo.TrainingGuides g
    JOIN dbo.Areas a    ON a.Id = g.AreaId
    JOIN dbo.Products p ON p.Id = a.ProductId
    WHERE g.IsPublished = 1
    ORDER BY p.Name, a.Name, g.Title;
END;
GO

-- spDashboardOperators — los operadores cuyo nombre o Code1 contiene
-- @Text (vacío = todos), incluidos los que nunca entrenaron, por
-- nombre. Sin paginar: el estado por guía, que también filtra, lo
-- calcula la BLL, y la BLL pagina después de filtrar.
CREATE OR ALTER PROCEDURE dbo.spDashboardOperators
    @Text NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    -- Cada [, % o _ escapado ocupa 3: 4000 no trunca ningún texto de búsqueda.
    DECLARE @Pattern NVARCHAR(4000) =
        CASE WHEN NULLIF(LTRIM(RTRIM(@Text)), N'') IS NULL THEN NULL
             ELSE N'%' + REPLACE(REPLACE(REPLACE(LTRIM(RTRIM(@Text)), N'[', N'[[]'), N'%', N'[%]'), N'_', N'[_]') + N'%' END;

    SELECT u.Id, u.Code1, u.Name, u.CreatedAt
    FROM dbo.Users u
    WHERE u.Role = 1
      AND (@Pattern IS NULL OR u.Name LIKE @Pattern OR u.Code1 LIKE @Pattern)
    ORDER BY u.Name, u.Code1;
END;
GO

-- spDashboardSessions — las sesiones de las guías publicadas de los
-- operadores (de uno solo con @UserId). Con @IncludeFailedAttempts = 1,
-- su conteo de intentos fallidos (IsCorrect = 0, sin contar los
-- saltos); si no, NULL: contarlos recorre todos los intentos (~1 s con
-- un año de datos) y el estado no los necesita.
CREATE OR ALTER PROCEDURE dbo.spDashboardSessions
    @UserId                INT = NULL,
    @IncludeFailedAttempts BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    SELECT s.Id AS SessionId, s.UserId, s.TrainingGuideId, s.Mode, s.Status, s.FinalScore,
           s.StartedAt, s.CompletedAt,
           CASE WHEN @IncludeFailedAttempts = 1 THEN
               (SELECT COUNT(*) FROM dbo.TrainingSessionStepAttempts a
                WHERE a.SessionId = s.Id AND a.IsCorrect = 0 AND ISNULL(a.Reason, 0) <> 6)
           END AS FailedAttempts
    FROM dbo.TrainingSessions s
    JOIN dbo.TrainingGuides g ON g.Id = s.TrainingGuideId AND g.IsPublished = 1
    JOIN dbo.Users u          ON u.Id = s.UserId AND u.Role = 1
    WHERE @UserId IS NULL OR s.UserId = @UserId
    OPTION (RECOMPILE);
END;
GO

-- spDashboardGuideSteps — los pasos de las guías publicadas, en orden
-- (los nombres salen de StepNames en la BLL).
CREATE OR ALTER PROCEDURE dbo.spDashboardGuideSteps
AS
BEGIN
    SET NOCOUNT ON;
    SELECT s.Id, s.TrainingGuideId, s.StepType, s.[Order], s.Weight
    FROM dbo.TrainingSteps s
    JOIN dbo.TrainingGuides g ON g.Id = s.TrainingGuideId AND g.IsPublished = 1
    ORDER BY s.TrainingGuideId, s.[Order];
END;
GO

-- spDashboardOperatorAttempts — los intentos de las sesiones de un
-- operador en las guías publicadas, en orden, con el paso, el
-- resultado, el motivo, la acción y la hora (duración por paso y
-- pasos a reforzar, en la BLL).
CREATE OR ALTER PROCEDURE dbo.spDashboardOperatorAttempts
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT a.SessionId, a.StepId, st.[Order] AS StepOrder, a.AttemptNumber, a.IsCorrect,
           a.[Timestamp], a.Reason, a.[Action]
    FROM dbo.TrainingSessions s
    JOIN dbo.TrainingGuides g ON g.Id = s.TrainingGuideId AND g.IsPublished = 1
    JOIN dbo.Users u          ON u.Id = s.UserId AND u.Role = 1
    JOIN dbo.TrainingSessionStepAttempts a ON a.SessionId = s.Id
    JOIN dbo.TrainingSteps st ON st.Id = a.StepId
    WHERE s.UserId = @UserId
    ORDER BY a.SessionId, a.[Timestamp], a.Id;
END;
GO

-- spDashboardStepAttempts — los intentos de las sesiones de un modo
-- que empezaron desde @From (NULL = todo) en las guías publicadas (o
-- en una con @GuideId), con su sesión (guía, estado e inicio). Pasos
-- los agrega en la BLL (StepRuns, D-146): fallos, sin acierto, saltos,
-- motivos y la mediana de duración.
CREATE OR ALTER PROCEDURE dbo.spDashboardStepAttempts
    @Mode    TINYINT,
    @From    DATETIME2 = NULL,
    @GuideId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT a.SessionId, s.TrainingGuideId, s.Mode, s.Status, s.StartedAt,
           a.StepId, st.[Order] AS StepOrder, a.AttemptNumber, a.IsCorrect,
           a.[Timestamp], a.Reason, a.[Action]
    FROM dbo.TrainingSessions s
    JOIN dbo.TrainingGuides g ON g.Id = s.TrainingGuideId AND g.IsPublished = 1
    JOIN dbo.Users u          ON u.Id = s.UserId AND u.Role = 1
    JOIN dbo.TrainingSessionStepAttempts a ON a.SessionId = s.Id
    JOIN dbo.TrainingSteps st ON st.Id = a.StepId
    WHERE s.Mode = @Mode
      AND (@From IS NULL OR s.StartedAt >= @From)
      AND (@GuideId IS NULL OR s.TrainingGuideId = @GuideId)
    -- Sin ORDER BY: la BLL agrupa por sesión y paso (ordenar ~100 000 filas no sirve de nada).
    OPTION (RECOMPILE);
END;
GO