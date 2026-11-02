-- ============================================================
-- GenerateDashboardVolume.sql — volumen para medir el rendimiento del dashboard (spec 022, E9, AC-7).
--
-- GenerateDashboardData.sql genera 100 operadores con perfiles fijos (no sirve para medir
-- volumen). Este script va DESPUÉS de ese: multiplica sus datos hasta ~1 600 operadores,
-- ~19 400 sesiones y ~247 000 intentos, más que la referencia de la Fase 10 (1 600 operadores,
-- ~22 900 sesiones, ~100 000 intentos). Solo corre en TrainingApp_DashboardTest (LocalDB descartable).
--
-- Uso (desde la carpeta que contiene Database/), después de GenerateDashboardData.sql:
--   sqlcmd -S "(localdb)\MSSQLLocalDB" -d TrainingApp_DashboardTest -E -i Database\Ops\GenerateDashboardVolume.sql
-- Imprime el Id del administrador y del operador con más sesiones: son las variables de entorno
-- MES_PERF_ADMIN y MES_PERF_OPERATOR de TrainingApp.Test\DashboardPerformanceTests.cs.
-- Al terminar, borrar la BD (docs/process.md §6).
--
-- Qué hace: crea el administrador PERFADM (sin login: el hash no es de ninguna contraseña); 15
-- copias de los 100 operadores GEN (GEN00101 a GEN01600, con su área); y a cada copia le da 4
-- veces las sesiones e intentos del original, desplazadas 0, 7, 14 y 21 días hacia atrás.
-- Los operadores 'GEN%' los borra GenerateDashboardData.sql al volver a correrse, así que el
-- orden para rehacer es: primero ese y después este. PERFADM queda hasta borrar la BD.
-- ============================================================
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF DB_NAME() <> N'TrainingApp_DashboardTest'
    THROW 50020, N'GenerateDashboardVolume.sql solo corre en la BD TrainingApp_DashboardTest (LocalDB descartable).', 1;

IF EXISTS (SELECT 1 FROM dbo.Users WHERE Code1 = N'GEN00101')
    THROW 50022, N'Ya hay volumen generado: volver a correr GenerateDashboardData.sql antes (borra los GEN%).', 1;

BEGIN TRANSACTION;

IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Code1 = N'PERFADM')
    INSERT INTO dbo.Users (Code1, Name, PasswordHash, IsFirstLogin, CreatedAt, Role, LastLoginAt)
    VALUES (N'PERFADM', N'Admin de medición', N'generado-sin-contrasena', 0, SYSUTCDATETIME(), 2, NULL);

-- Los 100 operadores originales y 15 copias.
SELECT TOP (100) ROW_NUMBER() OVER (ORDER BY u.Id) AS N, u.Id AS SrcId, u.AreaId
INTO #Src
FROM dbo.Users u WHERE u.Code1 LIKE N'GEN[0-9][0-9][0-9][0-9][0-9]' AND u.Role = 1 ORDER BY u.Id;

SELECT TOP (15) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS K
INTO #Copies FROM sys.all_objects;

-- Usuarios clonados: GEN00101 .. GEN01600.
INSERT INTO dbo.Users (Code1, Name, PasswordHash, IsFirstLogin, CreatedAt, Role, LastLoginAt, AreaId)
SELECT N'GEN' + RIGHT(N'00000' + CAST(100 * c.K + s.N AS NVARCHAR(10)), 5),
       N'Operador Generado ' + CAST(100 * c.K + s.N AS NVARCHAR(10)),
       N'generado-sin-contrasena', 0, u.CreatedAt, 1, NULL, s.AreaId
FROM #Copies c CROSS JOIN #Src s JOIN dbo.Users u ON u.Id = s.SrcId;

SELECT c.K, s.N, s.SrcId,
       (SELECT Id FROM dbo.Users WHERE Code1 = N'GEN' + RIGHT(N'00000' + CAST(100 * c.K + s.N AS NVARCHAR(10)), 5)) AS NewId
INTO #UserMap
FROM #Copies c CROSS JOIN #Src s;

-- Sesiones: cada usuario clonado recibe 4 copias de las del original.
SELECT m.NewId AS UserId, ss.Id AS OldId, ss.TrainingGuideId, ss.Mode, ss.Status, ss.CurrentStepOrder,
       ss.FinalScore, ss.ResourceSetupType, r.R AS Rep,
       DATEADD(DAY, -7 * r.R, ss.StartedAt) AS StartedAt,
       DATEADD(DAY, -7 * r.R, ss.CompletedAt) AS CompletedAt
INTO #ToCopy
FROM #UserMap m
JOIN dbo.TrainingSessions ss ON ss.UserId = m.SrcId
CROSS JOIN (VALUES (0), (1), (2), (3)) r (R);

-- MERGE con ON 1 = 0 para conocer el Id nuevo de cada sesión copiada.
CREATE TABLE #SessionMap (OldId INT, Rep INT, NewId INT);
MERGE dbo.TrainingSessions t
USING #ToCopy s ON 1 = 0
WHEN NOT MATCHED THEN
    INSERT (UserId, TrainingGuideId, Mode, Status, CurrentStepOrder, FinalScore, StartedAt, CompletedAt, ResourceSetupType)
    VALUES (s.UserId, s.TrainingGuideId, s.Mode, s.Status, s.CurrentStepOrder, s.FinalScore, s.StartedAt, s.CompletedAt, s.ResourceSetupType)
OUTPUT s.OldId, s.Rep, inserted.Id INTO #SessionMap;

INSERT INTO dbo.TrainingSessionStepAttempts (SessionId, StepId, AttemptNumber, IsCorrect, Timestamp, Reason, Action)
SELECT sm.NewId, a.StepId, a.AttemptNumber, a.IsCorrect, DATEADD(DAY, -7 * sm.Rep, a.Timestamp), a.Reason, a.Action
FROM #SessionMap sm JOIN dbo.TrainingSessionStepAttempts a ON a.SessionId = sm.OldId;

COMMIT;

SELECT (SELECT COUNT(*) FROM dbo.Users) AS Users,
       (SELECT COUNT(*) FROM dbo.TrainingSessions) AS Sessions,
       (SELECT COUNT(*) FROM dbo.TrainingSessionStepAttempts) AS Attempts,
       (SELECT Id FROM dbo.Users WHERE Code1 = N'PERFADM') AS MES_PERF_ADMIN,
       (SELECT TOP 1 UserId FROM dbo.TrainingSessions GROUP BY UserId ORDER BY COUNT(*) DESC) AS MES_PERF_OPERATOR;
