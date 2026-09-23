# Roadmap — MESTrainingSimulator

> Historial completo de fases, extraído de `CLAUDE.md`. El resumen operativo (fase actual + siguiente) vive en `CLAUDE.md` → sección Roadmap.

Cada fase tiene un alcance cerrado. No mezclar tareas de otras fases — la
causa principal de deuda técnica temprana es que features de fases futuras
se filtran en fases anteriores.

### Fase 0 — Setup ✅ (completada)
**Objetivo:** Proyecto Web Forms creado, N-Tier scaffolded, build verde.
**No incluye:** lógica de negocio, UI funcional, datos reales.

---

### Fase 1 — Auth ✅ (completada)
**Objetivo:** Login.aspx + ChangePassword.aspx funcionales, flujo completo
de primer acceso (API de empleados → Operator local → PBKDF2 → FormsAuth).
**No incluye:** Dashboard, GuidedTest, catálogo de áreas, ningún paso de entrenamiento.

---

### Fase 2 — Dominio y catálogo de R&C ✅ (completada)
**Objetivo:** Auditoría del legacy de R&C + script SQL con el catálogo real:
`Products` (R&C), `Areas` (Moldeo, NonSerialized, RequiresAssembly=false,
RequiresDataCollection=true), `NCCodes` (30 códigos), `DataCollectionParameters`
(15 parámetros: ID molde + 10 params MSS + verificación MSS + fecha kerosene +
2 first-shot). Catálogo en `06_RCMoldeoCatalog.sql`.
ExpectedComponentCount: N/A (RequiresAssembly=false).
`DataCollectionParameters` schema evolucionado: columnas `DataType`, `DisplayOrder`
añadidas; `MinValue`/`MaxValue` son ahora nullable (rangos pendientes de MSS D000281359).
**No incluye:** UI, motor de validación de pasos, TrainingGuideService,
ninguna pantalla de entrenamiento.

---

### Fase 3 — Motor de TrainingGuide ✅ (completada)
**Objetivo:** `TrainingGuideRepository` (dual result set puebla `Steps`),
`TrainingGuideService` con `BuildValidatorForGuide`, `StepValidator` completo
con constructor injection de `StepValidationContext` (Area + NCCodes + DCParams).
Validación implementada para todos los StepTypes: DataCollection (JSON por
param Id, tipado por DataType con InvariantCulture), LogNC (lookup en catálogo),
Assembly (count vs ExpectedComponentCount), LineClearance ("true"), pasos de
secuencia siempre true.
Nuevos repositorios: `AreaRepository`, `NCCodeRepository`,
`DataCollectionParameterRepository`. Nuevos SPs: `spAreaGetById`,
`spNCCodeGetByArea`, `spDataCollectionParameterGetByArea`.
Guía real de R&C/Moldeo publicada en `07_RCMoldeoGuide.sql` (8 pasos, pesos
suman 1.00; Search hereda peso 0.15 de Assembly ausente; 3 LC instancias con
pesos 0.04/0.03/0.03).
LC Moldeo modelada como paso LineClearance en el guide seed (step 4, weight 0.03);
la validación es idéntica al LC estándar — el contenido hardcodeado de sus 3
preguntas es responsabilidad de la UI (Fase 6).
`DataCollectionParameter` POCO evolucionado: añadidos `DataType`, `DisplayOrder`,
`MinValue`/`MaxValue` como `decimal?`.
Tests NUnit diferidos a Fase 5 (cobertura de ScoreCalculator y StepValidator).
**No incluye:** TrainingSession con gate-keeping real, persistencia de intentos
optimizada, UI, ScoreCalculator tests.

---

### Fase 4 — TrainingSession y modos ✅ (completada)
**Objetivo:** `TrainingSessionService` extendido con gate-keeping real Guided vs.
Assessment, `MemoryCache` de validator keyed por `AreaId` (TTL 60 min), método
`Abandon`, score en tiempo real (`CurrentScore` en `StepSubmitResult`).
`TrainingSessionRepository` extendido: `Abandon` + `DeleteActiveByOperator`.
Nuevos SPs: `spTrainingSessionAbandon`, `spTrainingSessionDeleteActiveByOperator`,
`spProductGetAll`, `spAreaGetByProduct`.
`ProductRepository` nuevo; `AreaRepository` extendido con `GetByProduct`.
`TrainingGuideService` extendido con `GetAllProducts` y `GetAreasByProduct`.
`StartSession.aspx` — selección Product → Area → Guide → Mode, inicia sesión.
`GuidedTest.aspx` — input genérico por paso, feedback + hint hardcodeado por
`StepType`, score en vivo, abandon con confirm JS, panel de completion.
Login redirige a `StartSession.aspx` tras autenticación exitosa.
Spec de referencia: `docs/specs/003-training-session-y-modos.md` (Approved 2026-09-04).
**No incluye:** UserControls por tipo de paso, ScoreCalculator tests, LoFlo.

---

### Fase 5 — ScoreCalculator: cobertura de tests ⏸️ (en pausa)
**Objetivo:** `ScoreCalculator` ya está implementado. Esta fase lo blinda
con tests NUnit exhaustivos: todos los casos de penalización, force-advance,
pasos opcionales ausentes, sesiones completadas vs. abandonadas.
Es el componente más sensible del sistema — debe tener cobertura total
antes de que usuarios reales generen puntajes.
**No incluye:** nuevas features, cambios al algoritmo.

---

### Fase 6 — UserControls por tipo de paso 🔄 (en curso, por cerrar)
**Objetivo:** UserControls `.ascx` reutilizables para cada `StepType`:
`ResourceSetup`, `LineClearance`, `SearchOrder`, `Assembly`,
`DataCollection`, `NCScrap`, `Complete`. Cada control recibe el estado
del servidor y emite un submit — sin lógica de negocio interna.
**No incluye:** catálogo de LoFlo, nuevas áreas.

---

### Fase 7 — Catálogo de LoFlo (segunda área)
**Objetivo:** Script SQL con el catálogo real de LoFlo (Sensores).
Solo carga de datos — cero código nuevo. Valida que el motor de Fases 3-4
es verdaderamente genérico y que `RequiresAssembly`/`RequiresDataCollection`
funcionan con la configuración real de LoFlo.
**No incluye:** cambios al motor, nuevas pantallas.

---

### Fase 8 — Validación interna
**Objetivo:** Sesión con trainer/operador real sobre R&C y LoFlo.
Correcciones de UX, bugs de flujo, ajustes de catálogo.
**No incluye:** nuevas áreas, Dashboard admin, reportes.

---

### Fase 9 — Deploy del MVP
**Objetivo:** Deploy en servidor interno con IIS. Configuración de
connection string de producción, App Pool, permisos, prueba de humo.
**No incluye:** CI/CD automatizado, monitoreo avanzado.

