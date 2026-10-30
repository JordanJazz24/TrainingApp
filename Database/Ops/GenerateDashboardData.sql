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
-- Genera 100 operadores (Code1 'GEN' + número, sin login: el hash no es de ninguna contraseña)
-- con un perfil fijo por número, para ver Operadores y Seguimiento con todos los estados:
--   GEN00001-00006  Sin área (sin sesiones);
--   GEN00007-00021  Sin iniciar (tienen área, ninguna sesión);
--   GEN00022-00046  Aprobado (un Assessment completado con 70 % o más);
--   GEN00047-00056  Reprobado con 2 Assessment fallidos (regla S-1 de Seguimiento);
--   GEN00057-00066  Reprobado con 1 Assessment fallido;
--   GEN00067-00100  En entrenamiento (Guided y Práctica libre; algunos con un Assessment
--                   abandonado).
-- Además:
--   · cada operador con área la tiene en la guía que le toca (se reparten entre las guías
--     publicadas) y entrena ahí; solo 10 (GEN00024, 26, 28, 30, 50, 52, 60, 70, 80 y 90) tienen
--     sesiones también en otra área (entrenamiento cruzado);
--   · los números múltiplos de 3 llevan 15 días o más sin actividad (regla S-2); los demás, su
--     primera sesión en los últimos 13 días;
--   · GEN00047-00052 se atoran dos veces en el mismo paso de su Assessment (regla S-3);
--   · el resto de las sesiones son Guided o Práctica libre, completadas, abandonadas o una en
--     curso; los fallos se concentran en algunos pasos (Resource Setup, Assembly, Data
--     Collection y Log NC), con su motivo (AttemptReason) y su acción; en Assessment, 3 fallos
--     pueden forzar el avance (sin acierto); en Práctica libre, algunos pasos se saltan
--     (Reason 6).
-- Re-ejecutable: sí. Borra antes lo que generó (usuarios 'GEN%' con sus sesiones e intentos).
-- ============================================================
SET NOCOUNT ON;
SET XACT_ABORT ON;

-- ---------- Parámetros ----------
DECLARE @Operators       INT = 100;   -- operadores generados (los perfiles están fijados para 100)
DECLARE @Months          INT = 3;     -- antigüedad de las sesiones
DECLARE @AbandonedPct    INT = 12;    -- % de sesiones abandonadas (las que no son Assessment fijos)
DECLARE @InProgressPct   INT = 4;     -- % de sesiones en curso
-- ---------------------------------

IF DB_NAME() <> N'TrainingApp_DashboardTest'
    THROW 50020, N'GenerateDashboardData.sql solo corre en la BD TrainingApp_DashboardTest (LocalDB descartable).', 1;

DECLARE @Now  DATETIME2 = SYSUTCDATETIME();

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
SELECT g.Id AS GuideId, g.AreaId, ROW_NUMBER() OVER (ORDER BY g.Id) AS GuideIndex,
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
       DATEADD(DAY, -(95 + ABS(CAST(CHECKSUM(NEWID()) AS BIGINT)) % 25), @Now),  -- alta anterior a toda sesión
       1, NULL
FROM #Numbers n
WHERE n.N <= @Operators;

-- Perfil por número (ver la cabecera): 0 Sin área, 1 Sin iniciar, 2 Aprobado, 3 Reprobado con
-- 2 Assessment, 4 Reprobado con 1, 5 En entrenamiento. Los números al azar van en columnas
-- aparte (NEWID() dentro de un CASE puede evaluarse más de una vez por fila).
SELECT u.Id AS UserId, CAST(RIGHT(u.Code1, 5) AS INT) AS OpN,
       ABS(CAST(CHECKSUM(NEWID()) AS BIGINT)) % 100 AS R1,
       ABS(CAST(CHECKSUM(NEWID()) AS BIGINT)) % 100 AS R2
INTO #Base
FROM dbo.Users u
WHERE u.Code1 LIKE N'GEN%';

SELECT b.UserId, b.OpN,
       CAST(CASE WHEN b.OpN <= 6 THEN 0 WHEN b.OpN <= 21 THEN 1 WHEN b.OpN <= 46 THEN 2
                 WHEN b.OpN <= 56 THEN 3 WHEN b.OpN <= 66 THEN 4 ELSE 5 END AS TINYINT) AS Profile,
       -- 0 = flojo, 0.99 = muy bueno: los aprobados son buenos y los reprobados, flojos.
       CASE WHEN b.OpN BETWEEN 22 AND 46 THEN 0.75 + (b.R1 % 25) / 100.0
            WHEN b.OpN BETWEEN 47 AND 66 THEN (b.R1 % 30) / 100.0
            ELSE b.R1 / 100.0 END AS Skill,
       CASE WHEN b.OpN IN (24, 26, 28, 30, 50, 52, 60, 70, 80, 90) THEN 4 + b.R2 % 3   -- cruzados: sesiones de sobra
            WHEN b.OpN <= 21 THEN 0
            WHEN b.OpN <= 46 THEN 2 + b.R2 % 5
            WHEN b.OpN <= 56 THEN 3 + b.R2 % 4
            WHEN b.OpN <= 66 THEN 2 + b.R2 % 4
            ELSE 1 + b.R2 % 6 END AS SessionCount,
       1 + (b.OpN % @GuideCount) AS MainGuide,                                  -- la guía de su área
       CAST(CASE WHEN b.OpN IN (24, 26, 28, 30, 50, 52, 60, 70, 80, 90) THEN 1 ELSE 0 END AS BIT) AS CrossTrain,
       CAST(CASE WHEN b.OpN % 3 = 0 THEN 1 ELSE 0 END AS BIT) AS Inactive,
       CAST(CASE WHEN b.OpN BETWEEN 47 AND 52 THEN 1 ELSE 0 END AS BIT) AS Stuck
INTO #Ops
FROM #Base b;

-- Su área: la de su guía principal (Sin área: ninguna).
UPDATE u SET AreaId = g.AreaId
FROM dbo.Users u
JOIN #Ops o ON o.UserId = u.Id
JOIN #Guides g ON g.GuideIndex = o.MainGuide
WHERE o.Profile <> 0;

-- ---------- Sesiones ----------
-- Las fijas (Forced): 1 = Assessment completado (aprobado o reprobado según el perfil; el 2.º de
-- los Reprobados con 2), 2 = Assessment abandonado (en la mitad de los En entrenamiento con 2
-- sesiones o más). Las demás: Guided o Práctica libre. Todas en la guía de su área, salvo el
-- entrenamiento cruzado (la última sesión y la 3.ª, en otra guía). Fechas: los Inactive, entre
-- 90 y 15 días atrás; los demás, la primera sesión en los últimos 13 días.
SELECT ROW_NUMBER() OVER (ORDER BY o.UserId, n.N) AS RowId,
       o.UserId, o.Skill, o.Profile, o.Stuck, o.MainGuide, n.N AS Ordinal,
       CAST(CASE WHEN o.Profile IN (2, 3, 4) AND n.N = 1 THEN 1
                 WHEN o.Profile = 3 AND n.N = 2 THEN 1
                 WHEN o.Profile = 5 AND n.N = 2 AND o.OpN % 2 = 0 THEN 2
                 ELSE 0 END AS TINYINT) AS Forced,
       CASE WHEN o.CrossTrain = 1 AND (n.N = o.SessionCount OR n.N = 3) THEN 1 ELSE 0 END AS GoOther,
       1 + (o.MainGuide - 1 + 1 + ABS(CAST(CHECKSUM(NEWID()) AS BIGINT)) % NULLIF(@GuideCount - 1, 0)) % @GuideCount AS OtherGuide,
       ABS(CAST(CHECKSUM(NEWID()) AS BIGINT)) % 100            AS ModeRoll,
       ABS(CAST(CHECKSUM(NEWID()) AS BIGINT)) % 100            AS StatusRoll,
       ABS(CAST(CHECKSUM(NEWID()) AS BIGINT)) % 100            AS ScoreRoll,
       ABS(CAST(CHECKSUM(NEWID()) AS BIGINT))                  AS DateRoll,
       CASE WHEN o.Inactive = 1 THEN DATEADD(DAY, -90, @Now)
            WHEN n.N = 1 THEN DATEADD(DAY, -13, @Now) ELSE DATEADD(DAY, -(@Months * 20), @Now) END AS EarliestStart,
       CASE WHEN o.Inactive = 1 THEN DATEADD(DAY, -15, @Now) ELSE DATEADD(HOUR, -1, @Now) END AS LatestStart
INTO #SessRaw
FROM #Ops o
JOIN #Numbers n ON n.N <= o.SessionCount;

SELECT r.RowId, r.UserId, r.Skill, r.Profile, r.Forced, r.ScoreRoll,
       CAST(CASE WHEN r.Stuck = 1 AND r.Forced = 1 THEN 1 ELSE 0 END AS BIT) AS StuckRun,
       g.GuideId, g.StepCount,
       CAST(CASE WHEN r.Forced > 0 THEN 1 WHEN r.ModeRoll < 50 THEN 0 ELSE 2 END AS TINYINT) AS Mode,
       CAST(CASE WHEN r.Forced = 1 THEN 1
                 WHEN r.Forced = 2 THEN 2
                 WHEN r.StatusRoll < @InProgressPct THEN 0
                 WHEN r.StatusRoll < @InProgressPct + @AbandonedPct THEN 2
                 ELSE 1 END AS TINYINT) AS Status,
       DATEADD(MINUTE, CAST(r.DateRoll % DATEDIFF(MINUTE, r.EarliestStart, r.LatestStart) AS INT),
               r.EarliestStart) AS StartedAt
INTO #Sess
FROM #SessRaw r
JOIN #Guides g ON g.GuideIndex = CASE WHEN r.GoOther = 1 AND r.Forced = 0 THEN ISNULL(r.OtherGuide, r.MainGuide) ELSE r.MainGuide END;
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
-- S-3: los Reprobados "atorados" fallan el paso 3 de sus dos Assessment (avance forzado).
UPDATE r SET Failures = 3, EndKind = 1
FROM #StepRuns r JOIN #Sess s ON s.RowId = r.RowId
WHERE s.StuckRun = 1 AND r.[Order] = 3;
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
         WHEN s.Forced = 1 THEN CAST(CASE WHEN s.Profile = 2 THEN 0.72 + (s.ScoreRoll % 28) / 100.0
                                          ELSE 0.30 + (s.ScoreRoll % 38) / 100.0 END AS DECIMAL(5,2))
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
       (SELECT COUNT(*) FROM #Ops WHERE SessionCount = 0)            AS SinSesiones,
       (SELECT COUNT(*) FROM #SessionMap)                            AS Sesiones,
       (SELECT COUNT(*) FROM dbo.TrainingSessionStepAttempts a JOIN #SessionMap m ON m.SessionId = a.SessionId) AS Intentos;

SELECT CASE s.Mode WHEN 0 THEN N'Guided' WHEN 1 THEN N'Assessment' ELSE N'Práctica libre' END AS Modo,
       CASE s.Status WHEN 0 THEN N'En curso' WHEN 1 THEN N'Completada' ELSE N'Abandonada' END AS Estado,
       COUNT(*) AS Sesiones,
       CAST(AVG(CASE WHEN s.Status = 1 THEN s.FinalScore END) AS DECIMAL(5,2)) AS PuntajePromedio
FROM #Sess s
GROUP BY s.Mode, s.Status
ORDER BY s.Mode, s.Status;

SELECT CASE o.Profile WHEN 0 THEN N'Sin área' WHEN 1 THEN N'Sin iniciar' WHEN 2 THEN N'Aprobado'
                      WHEN 3 THEN N'Reprobado (2 Assessment)' WHEN 4 THEN N'Reprobado (1 Assessment)'
                      ELSE N'En entrenamiento' END AS Perfil,
       COUNT(*) AS Operadores
FROM #Ops o
GROUP BY o.Profile
ORDER BY o.Profile;
