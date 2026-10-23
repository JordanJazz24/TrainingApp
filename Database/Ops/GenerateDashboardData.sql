-- ============================================================
-- GenerateDashboardData.sql — datos de prueba del dashboard (spec 018, DA-7).
--
-- SOLO para una BD descartable de LocalDB llamada TrainingApp_DashboardTest: se niega a
-- correr en cualquier otra. Sirve para ver cada vista con datos realistas (E1 en adelante)
-- y para medir el rendimiento con el volumen de referencia (E7, AC-24).
--
-- Uso (desde la carpeta que contiene Database/):
--   sqlcmd -S "(localdb)\MSSQLLocalDB" -Q "CREATE DATABASE TrainingApp_DashboardTest"
--   .\Database\Ops\RunAllScripts.ps1 -Server "(localdb)\MSSQLLocalDB" -Database TrainingApp_DashboardTest
--   sqlcmd -S "(localdb)\MSSQLLocalDB" -d TrainingApp_DashboardTest -I -f 65001 -i Database\Ops\GenerateDashboardData.sql
-- Al terminar, borrar la BD (docs/process.md §6).
--
-- Genera operadores (Code1 'GEN' + número, sin login: el hash no es de ninguna contraseña)
-- con sesiones de las guías publicadas en los últimos @Months meses:
--   · @NotStartedPct % nunca entrenó (Sin iniciar);
--   · cada operador tiene una habilidad al azar: decide su puntaje y cuántos fallos tiene;
--   · modos: Guided, Práctica libre y Assessment; estados: completadas, abandonadas y en curso;
--   · los fallos se concentran en algunos pasos (Resource Setup, Assembly, Data Collection y
--     Log NC), con su motivo (AttemptReason) y su acción; en Assessment, 3 fallos pueden
--     forzar el avance (sin acierto); en Práctica libre, algunos pasos se saltan (Reason 6).
-- Re-ejecutable: sí. Borra antes lo que generó (usuarios 'GEN%' con sus sesiones e intentos).
-- ============================================================
SET NOCOUNT ON;
SET XACT_ABORT ON;

-- ---------- Parámetros ----------
DECLARE @Operators       INT = 1600;  -- operadores generados
DECLARE @NotStartedPct   INT = 10;    -- % sin sesiones
DECLARE @MaxSessions     INT = 30;    -- sesiones máximas por operador (al azar entre 1 y este valor)
DECLARE @Months          INT = 12;    -- antigüedad de las sesiones
DECLARE @AbandonedPct    INT = 8;     -- % de sesiones abandonadas
DECLARE @InProgressPct   INT = 3;     -- % de sesiones en curso
-- ---------------------------------

IF DB_NAME() <> N'TrainingApp_DashboardTest'
    THROW 50020, N'GenerateDashboardData.sql solo corre en la BD TrainingApp_DashboardTest (LocalDB descartable).', 1;

DECLARE @Now  DATETIME2 = SYSUTCDATETIME();
DECLARE @From DATETIME2 = DATEADD(MONTH, -@Months, @Now);

BEGIN TRANSACTION;

-- Lo generado antes.
DELETE a FROM dbo.TrainingSessionStepAttempts a
    JOIN dbo.TrainingSessions s ON s.Id = a.SessionId
    JOIN dbo.Users u ON u.Id = s.UserId
    WHERE u.Code1 LIKE N'GEN%';
DELETE s FROM dbo.TrainingSessions s JOIN dbo.Users u ON u.Id = s.UserId WHERE u.Code1 LIKE N'GEN%';
DELETE FROM dbo.Users WHERE Code1 LIKE N'GEN%';

-- Números 1..N para generar filas sin cursores.
SELECT TOP (10000) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS N
INTO #Numbers
FROM sys.all_objects a CROSS JOIN sys.all_objects b;

-- Guías publicadas y sus pasos.
SELECT g.Id AS GuideId, ROW_NUMBER() OVER (ORDER BY g.Id) AS GuideIndex,
       (SELECT COUNT(*) FROM dbo.TrainingSteps s WHERE s.TrainingGuideId = g.Id) AS StepCount
INTO #Guides
FROM dbo.TrainingGuides g
WHERE g.IsPublished = 1;

DECLARE @GuideCount INT = (SELECT COUNT(*) FROM #Guides);
IF @GuideCount = 0
    THROW 50021, N'No hay guías publicadas: correr antes los scripts 00-09.', 1;

-- Dificultad por tipo de paso (StepTypes de la BLL): probabilidad base de fallar cada intento.
SELECT StepType, Difficulty, ActionCode
INTO #StepDifficulty
FROM (VALUES
    (0,  0.30, N'ResourceSetup.Save'),   -- Resource Setup
    (1,  0.08, N'LC.Save'),              -- Line Clearance
    (2,  0.10, N'POD.Start'),            -- Search / Start
    (3,  0.25, N'Assembly.Add'),         -- Assembly
    (4,  0.35, N'DC.Save'),              -- Data Collection
    (5,  0.15, N'SFCSplit.Split'),       -- SFC Split
    (6,  0.28, N'LogNC.AddDone'),        -- Log NC
    (7,  0.06, N'POD.Complete'),         -- Complete
    (8,  0.04, N'POD.Logout'),           -- Logout
    (9,  0.20, N'LogNC.Repair.Scrap'),   -- reparación rápida
    (10, 0.12, N'POD.Complete'),         -- cierre de operación
    (11, 0.10, N'POD.Start')             -- Ingreso a MES
) d (StepType, Difficulty, ActionCode);

-- ---------- Operadores ----------
INSERT INTO dbo.Users (Code1, Name, PasswordHash, IsFirstLogin, CreatedAt, Role, LastLoginAt)
SELECT N'GEN' + RIGHT(N'00000' + CAST(n.N AS NVARCHAR(10)), 5),
       N'Operador Generado ' + CAST(n.N AS NVARCHAR(10)),
       N'generado-sin-contrasena', 0,
       DATEADD(DAY, -(1 + ABS(CAST(CHECKSUM(NEWID()) AS BIGINT)) % (@Months * 30 + 30)), @Now),  -- alta de al menos ayer
       1, NULL
FROM #Numbers n
WHERE n.N <= @Operators;

SELECT u.Id AS UserId, u.CreatedAt,
       ABS(CAST(CHECKSUM(NEWID()) AS BIGINT)) % 100 / 100.0 AS Skill,          -- 0 = flojo, 0.99 = muy bueno
       CASE WHEN ABS(CAST(CHECKSUM(NEWID()) AS BIGINT)) % 100 < @NotStartedPct THEN 0
            ELSE 1 + ABS(CAST(CHECKSUM(NEWID()) AS BIGINT)) % @MaxSessions END AS SessionCount,
       1 + ABS(CAST(CHECKSUM(NEWID()) AS BIGINT)) % @GuideCount AS MainGuide    -- la guía de su área
INTO #Ops
FROM dbo.Users u
WHERE u.Code1 LIKE N'GEN%';

-- ---------- Sesiones ----------
-- 70 % en la guía principal del operador; el resto en otra al azar. Modo: 35 % Guided,
-- 30 % Práctica libre, 35 % Assessment. Fecha: entre su alta (o @From) y ahora.
-- Los números al azar se guardan primero como columnas: NEWID() dentro de un JOIN o de un
-- CASE puede evaluarse más de una vez por fila y perder o duplicar filas.
SELECT ROW_NUMBER() OVER (ORDER BY o.UserId, n.N) AS RowId,
       o.UserId, o.Skill, o.MainGuide,
       CASE WHEN o.CreatedAt > @From THEN o.CreatedAt ELSE @From END AS EarliestStart,
       ABS(CAST(CHECKSUM(NEWID()) AS BIGINT)) % 100            AS GuideRoll,
       1 + ABS(CAST(CHECKSUM(NEWID()) AS BIGINT)) % @GuideCount AS OtherGuide,
       ABS(CAST(CHECKSUM(NEWID()) AS BIGINT)) % 100            AS ModeRoll,
       ABS(CAST(CHECKSUM(NEWID()) AS BIGINT)) % 100            AS StatusRoll,
       ABS(CAST(CHECKSUM(NEWID()) AS BIGINT))                  AS DateRoll
INTO #SessRaw
FROM #Ops o
JOIN #Numbers n ON n.N <= o.SessionCount;

SELECT r.RowId, r.UserId, r.Skill,
       g.GuideId, g.StepCount,
       CAST(CASE WHEN r.ModeRoll < 35 THEN 0 WHEN r.ModeRoll < 65 THEN 2 ELSE 1 END AS TINYINT) AS Mode,
       CAST(CASE WHEN r.StatusRoll < @InProgressPct THEN 0
                 WHEN r.StatusRoll < @InProgressPct + @AbandonedPct THEN 2
                 ELSE 1 END AS TINYINT) AS Status,
       -- Hasta una hora antes de ahora: una sesión dura menos que eso y su cierre no queda en el futuro.
       DATEADD(MINUTE, CAST(r.DateRoll % (CASE WHEN DATEDIFF(MINUTE, r.EarliestStart, @Now) > 60
                                               THEN DATEDIFF(MINUTE, r.EarliestStart, @Now) - 60 ELSE 1 END) AS INT),
               r.EarliestStart) AS StartedAt
INTO #Sess
FROM #SessRaw r
JOIN #Guides g ON g.GuideIndex = CASE WHEN r.GuideRoll < 70 THEN r.MainGuide ELSE r.OtherGuide END;
-- Una sola sesión activa por operador (D-9): la más reciente sigue en curso; las demás se
-- abandonaron.
UPDATE s SET Status = 2
FROM #Sess s
WHERE s.Status = 0
  AND EXISTS (SELECT 1 FROM #Sess later
              WHERE later.UserId = s.UserId AND later.Status = 0
                AND (later.StartedAt > s.StartedAt OR (later.StartedAt = s.StartedAt AND later.RowId > s.RowId)));

-- Hasta qué paso llegó: todos si se completó; si no, uno al azar.
ALTER TABLE #Sess ADD ReachedOrder INT NULL;
UPDATE #Sess SET ReachedOrder = CASE WHEN Status = 1 THEN StepCount
                                     ELSE 1 + ABS(CAST(CHECKSUM(NEWID()) AS BIGINT)) % StepCount END;
CREATE UNIQUE CLUSTERED INDEX IX_Sess ON #Sess (RowId);

-- ---------- Intentos por sesión y paso ----------
-- Por paso: fallos según la dificultad del paso, la habilidad y el modo (Guided casi no falla).
SELECT s.RowId, ts.Id AS StepId, ts.[Order], d.ActionCode,
       CASE
           WHEN s.Mode = 0 THEN CASE WHEN ABS(CAST(CHECKSUM(NEWID()) AS BIGINT)) % 100 < 3 THEN 1 ELSE 0 END
           ELSE (CASE WHEN ABS(CAST(CHECKSUM(NEWID()) AS BIGINT)) % 1000 < d.Difficulty * (1.4 - s.Skill) * 1000 THEN 1 ELSE 0 END)
              + (CASE WHEN ABS(CAST(CHECKSUM(NEWID()) AS BIGINT)) % 1000 < d.Difficulty * (1.4 - s.Skill) * 600 THEN 1 ELSE 0 END)
              + (CASE WHEN ABS(CAST(CHECKSUM(NEWID()) AS BIGINT)) % 1000 < d.Difficulty * (1.4 - s.Skill) * 350 THEN 1 ELSE 0 END)
       END AS Failures,
       ABS(CAST(CHECKSUM(NEWID()) AS BIGINT)) % 100 AS EndRoll
INTO #StepRuns
FROM #Sess s
JOIN dbo.TrainingSteps ts ON ts.TrainingGuideId = s.GuideId AND ts.[Order] <= s.ReachedOrder
JOIN #StepDifficulty d ON d.StepType = ts.StepType;

-- Cómo termina el paso: 0 = con acierto; 1 = avance forzado (Assessment: a los 3 fallos se
-- suma un 4.º, que fuerza el avance como ModePolicy.ForcesAdvanceAt; 40 %);
-- 2 = saltado (Práctica libre, 6 % de los pasos); 3 = sin terminar (el paso en curso de una
-- sesión no completada).
ALTER TABLE #StepRuns ADD EndKind TINYINT NULL;
UPDATE r SET EndKind =
    CASE
        WHEN s.Status <> 1 AND r.[Order] = s.ReachedOrder THEN 3
        WHEN s.Mode = 1 AND r.Failures = 3 AND r.EndRoll < 40 THEN 1
        WHEN s.Mode = 2 AND r.EndRoll < 6 THEN 2
        ELSE 0
    END
FROM #StepRuns r JOIN #Sess s ON s.RowId = r.RowId;
CREATE CLUSTERED INDEX IX_StepRuns ON #StepRuns (RowId, [Order]);

-- Tiempo de cada paso: de 60 a 180 s, más 40 s por fallo; los pasos van seguidos, así que
-- cada uno empieza donde terminó el anterior (la duración por paso de DA-2 varía).
ALTER TABLE #StepRuns ADD StepSeconds INT NULL, StepStart INT NULL;
UPDATE #StepRuns SET StepSeconds = 60 + CAST(ABS(CAST(CHECKSUM(NEWID()) AS BIGINT)) % 121 AS INT) + Failures * 40;
UPDATE r SET StepStart = x.StepStart
FROM #StepRuns r
JOIN (SELECT RowId, [Order],
             SUM(StepSeconds) OVER (PARTITION BY RowId ORDER BY [Order] ROWS UNBOUNDED PRECEDING) - StepSeconds AS StepStart
      FROM #StepRuns) x ON x.RowId = r.RowId AND x.[Order] = r.[Order];

-- Puntaje: Assessment según la habilidad (con ruido) y los pasos sin acierto; Guided y
-- Práctica libre altos. Solo las completadas tienen puntaje.
ALTER TABLE #Sess ADD FinalScore DECIMAL(5,2) NOT NULL DEFAULT 0;
UPDATE s SET FinalScore =
    CASE WHEN s.Status <> 1 THEN 0
         WHEN s.Mode = 0 THEN 1.00
         ELSE CAST(CASE
                 WHEN v.Score > 1 THEN 1 WHEN v.Score < 0.20 THEN 0.20 ELSE v.Score END AS DECIMAL(5,2))
    END
FROM #Sess s
CROSS APPLY (SELECT 0.42 + s.Skill * 0.55 + (ABS(CAST(CHECKSUM(NEWID()) AS BIGINT)) % 21 - 10) / 100.0
                    - 0.06 * (SELECT COUNT(*) FROM #StepRuns r WHERE r.RowId = s.RowId AND r.EndKind = 1)
                    + CASE WHEN s.Mode = 2 THEN 0.10 ELSE 0 END AS Score) v;

-- ---------- Insertar sesiones (con su Id real) ----------
CREATE TABLE #SessionMap (RowId INT PRIMARY KEY, SessionId INT NOT NULL);

MERGE dbo.TrainingSessions AS target
USING #Sess AS src ON 1 = 0
WHEN NOT MATCHED THEN
    INSERT (UserId, TrainingGuideId, Mode, Status, CurrentStepOrder, FinalScore, StartedAt, CompletedAt)
    VALUES (src.UserId, src.GuideId, src.Mode, src.Status, src.ReachedOrder, src.FinalScore, src.StartedAt, NULL)
OUTPUT src.RowId, inserted.Id INTO #SessionMap (RowId, SessionId);

-- ---------- Insertar intentos ----------
-- Fallos: uno por número 1..Failures (motivo al azar 1-5); el paso termina con un acierto
-- (EndKind 0), un salto (2), el 4.º fallo que fuerza el avance (1) o nada (3: sin terminar). Los intentos de un
-- paso se reparten en su tiempo (StepSeconds): el último cae al final del paso.
INSERT INTO dbo.TrainingSessionStepAttempts (SessionId, StepId, AttemptNumber, IsCorrect, [Timestamp], Reason, [Action])
SELECT m.SessionId, r.StepId, a.AttemptNumber, a.IsCorrect,
       DATEADD(SECOND, r.StepStart + a.AttemptNumber * r.StepSeconds / (r.Failures + 1), s.StartedAt),
       a.Reason, r.ActionCode
FROM #StepRuns r
JOIN #Sess s        ON s.RowId = r.RowId
JOIN #SessionMap m  ON m.RowId = r.RowId
CROSS APPLY (
    SELECT n.N AS AttemptNumber, CAST(0 AS BIT) AS IsCorrect,
           CAST(1 + ABS(CAST(CHECKSUM(NEWID()) AS BIGINT)) % 5 AS TINYINT) AS Reason
    FROM #Numbers n
    WHERE n.N <= r.Failures
    UNION ALL
    SELECT r.Failures + 1,
           CAST(CASE WHEN r.EndKind = 0 THEN 1 ELSE 0 END AS BIT),
           CAST(CASE r.EndKind WHEN 0 THEN 0 WHEN 2 THEN 6
                ELSE 1 + ABS(CAST(CHECKSUM(NEWID()) AS BIGINT)) % 5 END AS TINYINT)
    WHERE r.EndKind IN (0, 1, 2)
) a;

-- Fecha de cierre de las completadas y las abandonadas (como spTrainingSessionAbandon): su
-- último intento, o un minuto después del inicio si no llegó a intentar nada.
UPDATE ts SET CompletedAt = COALESCE(x.LastAttempt, DATEADD(MINUTE, 1, ts.StartedAt))
FROM dbo.TrainingSessions ts
JOIN #SessionMap m ON m.SessionId = ts.Id
LEFT JOIN (SELECT SessionId, MAX([Timestamp]) AS LastAttempt
           FROM dbo.TrainingSessionStepAttempts GROUP BY SessionId) x ON x.SessionId = ts.Id
WHERE ts.Status IN (1, 2);

COMMIT TRANSACTION;

-- ---------- Resumen ----------
SELECT (SELECT COUNT(*) FROM dbo.Users WHERE Code1 LIKE N'GEN%') AS Operadores,
       (SELECT COUNT(*) FROM #Ops WHERE SessionCount = 0)            AS SinIniciar,
       (SELECT COUNT(*) FROM #SessionMap)                            AS Sesiones,
       (SELECT COUNT(*) FROM dbo.TrainingSessionStepAttempts a JOIN #SessionMap m ON m.SessionId = a.SessionId) AS Intentos;

SELECT CASE s.Mode WHEN 0 THEN N'Guided' WHEN 1 THEN N'Assessment' ELSE N'Práctica libre' END AS Modo,
       CASE s.Status WHEN 0 THEN N'En curso' WHEN 1 THEN N'Completada' ELSE N'Abandonada' END AS Estado,
       COUNT(*) AS Sesiones,
       CAST(AVG(CASE WHEN s.Status = 1 THEN s.FinalScore END) AS DECIMAL(5,2)) AS PuntajePromedio
FROM #Sess s
GROUP BY s.Mode, s.Status
ORDER BY s.Mode, s.Status;
