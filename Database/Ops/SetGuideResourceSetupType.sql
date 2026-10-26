-- ============================================================
-- SetGuideResourceSetupType.sql — elige el Resource Setup (Standard o
-- Custom) de una guía publicada (spec 020). Vale para las sesiones que
-- empiecen después; las que están en curso conservan el suyo (D-188).
--
-- Para el piloto antes de la página Guías de Admin; después la
-- reemplaza esa página (D-187), y el script queda para cuando no hay
-- un admin disponible.
-- Uso: ejecutar una vez con @GuideId = NULL para ver la lista, poner
-- el Id y el tipo y volver a ejecutar.
--
-- Requiere: 00_Schema.sql y 02_spTrainingGuide.sql · Re-ejecutable: sí.
-- ============================================================
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @GuideId           INT     = NULL;  -- ← Id de la guía (de la lista)
DECLARE @ResourceSetupType TINYINT = 1;     -- ← 0 = Standard, 1 = Custom

EXEC dbo.spTrainingGuideGetPublishedForAdmin;

IF @GuideId IS NULL
BEGIN
    PRINT N'Sin @GuideId: solo se muestra la lista. Pon el Id y vuelve a ejecutar.';
    RETURN;
END;

IF NOT EXISTS (SELECT 1 FROM dbo.TrainingGuides WHERE Id = @GuideId AND IsPublished = 1)
BEGIN
    RAISERROR(N'No existe una guía publicada con Id %d.', 16, 1, @GuideId);
    RETURN;
END;

DECLARE @Rows TABLE (Affected INT);
INSERT INTO @Rows EXEC dbo.spTrainingGuideSetResourceSetupType @GuideId, @ResourceSetupType;

SELECT Id, Title, ResourceSetupType,
       CASE ResourceSetupType WHEN 1 THEN N'Custom' ELSE N'Standard' END AS Tipo
FROM   dbo.TrainingGuides
WHERE  Id = @GuideId;

PRINT CONCAT(N'Guía ', @GuideId, N': Resource Setup = ',
             CASE @ResourceSetupType WHEN 1 THEN N'Custom' ELSE N'Standard' END, N'.');
