# Plan — Fase 5: ScoreCalculator, cobertura de tests

Objetivo, alcance y "No incluye": `docs/roadmap.md` → Fase 5. Reglas que se prueban:
`CLAUDE.md` → "Modos de sesión" y "Ponderación de pasos".

**Sin spec:** son tests de reglas ya cerradas con negocio. No cambia el algoritmo ni la
UI. Si un test muestra que el código no cumple una regla, **no se corrige dentro de la
fase**: se le presenta al usuario como decisión (corregir el código o confirmar la regla
y documentarla primero).

**Estado:** aprobado (2026-10-07) · ✅ completada (2026-10-07, 3 entregas).

## Punto de partida

- `TrainingApp/BLL/Scoring/ScoreCalculator.cs` (61 líneas, sin dependencias de BD):
  - `CalculateSessionScore(steps, attempts, mode)`: suma, por paso, `Weight ×
    GetPenaltyFactor(n)`, donde `n` es el **primer intento correcto** del paso (por
    `AttemptNumber`). Un paso sin intento correcto suma 0. No normaliza: si los pesos de
    la guía no suman 1, el máximo tampoco es 1.
  - `GetPenaltyFactor(n, mode)`: Guided → 1.00 siempre; Assessment → 1.00 / 0.80 / 0.60 /
    0.00 (4+).
  - `ShouldForceAdvance(n, mode)`: solo Assessment y `n > MaxScoredAttempts` (3).
  - `IsPassed(score)`: `score >= PassingScore` (0.70).
- Quién lo usa: `PodService.LoadContext` (puntaje en vivo), `PodService.RecordAttempt`
  (puntaje con el intento nuevo, avance forzado, puntaje final), `PodContext.IsPassed` y
  `StepSubmitResult.RemainingAttempts` (usa `MaxScoredAttempts`).
- Pesos reales (en BD, por guía; los tests copian estos valores):

  | Paso | Moldeo (`07`) | Redefinity (`05`, ejemplo) |
  |---|---|---|
  | Resource Setup | 0.20 | 0.20 |
  | Line Clearance (apertura) | 0.05 | 0.05 |
  | Search / Start | 0.15 | 0.05 |
  | Assembly | — | 0.15 |
  | Data Collection | 0.30 | 0.30 |
  | Line Clearance (cierre) | 0.05 | 0.05 |
  | SFC Split | 0.05 | — |
  | Log NC | 0.10 | 0.10 |
  | Complete | 0.10 | 0.10 |
  | **Total** | **1.00** | **1.00** |

- Tests actuales: `TrainingApp.Test/Class1.cs` tiene un solo test de puntaje
  (`ScoreCalculatorPassingTests.IsPassed_UsesSeventyPercentThreshold`).

## Decisiones de organización (propuestas)

- Archivo nuevo `TrainingApp.Test/ScoreCalculatorTests.cs` (el `.csproj` es de estilo
  viejo: hay que agregar su `<Compile Include>`). El test existente de `IsPassed` se mueve
  ahí; el resto de `Class1.cs` no se toca.
- Mismo estilo que `Class1.cs`: NUnit 4, `[TestFixture]`, `Assert.That`, nombres
  `Metodo_Comportamiento`. `[TestCase]` para las tablas de factores.
- Datos en memoria: un helper arma una guía (lista de `TrainingStep` con Id, tipo, orden y
  peso) y los intentos (`TrainingSessionStepAttempt`). Sin BD ni mocks.
- Las guías de Moldeo y Redefinity se copian como constantes en el test, con un
  comentario que apunta al script SQL de origen.

## Entrega 1 — Factores, avance forzado y umbral ✅ (2026-10-07)

Las tres funciones estáticas, con todas sus ramas. `ScoreCalculatorTests.cs`: 27 casos;
suite completa 43/43 en verde.

- [x] `GetPenaltyFactor` en Assessment: intento 1 → 1.00, 2 → 0.80, 3 → 0.60, 4 → 0.00,
      5 y 10 → 0.00.
- [x] `GetPenaltyFactor` en Guided: intentos 1, 2, 3, 4 y 10 → 1.00 (sin penalización).
- [x] `ShouldForceAdvance` en Assessment: intentos 1-3 → no; 4, 5 y 10 → sí.
- [x] `ShouldForceAdvance` en Guided: nunca (intentos 1, 4 y 10).
- [x] Coherencia entre las dos: en Assessment, el primer intento con avance forzado es
      también el primero con factor 0 (`MaxScoredAttempts + 1`).
- [x] `IsPassed`: 0.70 → aprueba; 0.6999 y 0.69 → no; 1.00 → aprueba; 0 → no.
- [x] Constantes fijadas: `PassingScore == 0.70`, `MaxScoredAttempts == 3` (si alguien
      las cambia, un test lo avisa).

## Entrega 2 — `CalculateSessionScore` paso por paso ✅ (2026-10-07)

Guías chicas armadas en el test (2-3 pasos) para aislar cada regla. 12 casos nuevos
(`ScoreCalculatorStepScoreTests`); suite completa 55/55 en verde.

- [x] Sin intentos → 0.
- [x] Un paso correcto al 1.er intento → su peso completo.
- [x] Correcto al 2.º → peso × 0.80; al 3.º → peso × 0.60 (Assessment).
- [x] Correcto al 4.º (Assessment) → 0: el paso se marca fallido aunque acierte.
- [x] 4 fallos y avance forzado (ningún intento correcto) → 0.
- [x] Guided: correcto al 5.º intento → peso completo.
- [x] Cuenta el **primer** correcto: si hay dos intentos correctos del mismo paso, vale el
      de menor `AttemptNumber`, aunque la lista venga desordenada.
- [x] Intentos de un `StepId` que no está en la guía → se ignoran.
- [x] Paso todavía sin intentos (sesión en curso) → suma 0; el puntaje parcial es la suma
      de los pasos ya acertados (es el puntaje en vivo del POD).
- [x] Los intentos de un paso no afectan a otro paso.
- [x] **No normaliza:** una guía cuyos pesos suman 0.90, todo correcto al primer intento,
      da 0.90 (no 1.00). Deja escrito el riesgo de cargar mal los pesos de una guía.

## Entrega 3 — Sesiones completas con las guías reales y cierre de la fase ✅ (2026-10-07)

12 casos nuevos (`ScoreCalculatorGuideTests`); suite completa 67/67 en verde.
Comprobación de que los tests detectan errores: con la penalización del 2.º intento
cambiada a 0.75 fallan 8 tests (cambio revertido). `/code-review low`: 2 hallazgos
válidos aplicados (`GuideCopies_WeightsSumOne` renombrado, porque revisa las copias y no
el SQL; `PerfectRun_…` ahora comprueba que el paso opcional falta de verdad) y 1 falso
positivo (los pesos de `CLAUDE.md` ya se corrigieron en la entrega 1).

- [x] **Pasos opcionales ausentes:** Moldeo (sin Assembly, con SFC Split) y Redefinity
      (con Assembly, sin SFC Split) dan 1.00 con todo correcto al primer intento: el paso
      que no existe en la guía no cuenta ni resta.
- [x] Peor caso en Assessment: todos los pasos forzados → 0.
- [x] Los mismos intentos en Guided y en Assessment dan puntajes distintos (Guided 1.00).
- [x] **Límite de aprobación con Moldeo:** perder Data Collection entera (forzado, −0.30)
      → 0.70 → aprueba; además un reintento en Line Clearance (0.05 × 0.20 = −0.01) →
      0.69 → no aprueba.
- [x] **Completada vs. abandonada:** el cálculo no depende del estado de la sesión. Con
      los intentos de una sesión abandonada a mitad de la guía, el puntaje es la suma de
      los pasos resueltos (lo que se guardaría para el instructor). Que `FinalScore` solo
      se escribe al completar lo hace `spStepAttemptRecord`, no `ScoreCalculator` (fuera
      de esta fase).
- [x] **Barrido exhaustivo** de las dos guías reales: cada paso resuelto en el intento 1,
      2, 3 o forzado (4⁸ = 65 536 combinaciones por guía, en memoria). En todas: el puntaje
      está entre 0 y 1, tiene como máximo 2 decimales (la columna `FinalScore` es
      `DECIMAL(5,2)`: el redondeo de la BD no cambia el resultado) y es igual a la suma de
      `peso × factor` de cada paso.
- [x] Tabla en este plan: cada rama de `ScoreCalculator` → el test que la cubre (abajo).
- [x] Cierre: `docs/roadmap.md` (Fase 5 ✅, "Estado actual" → Fase 7) y `CLAUDE.md`
      (Roadmap → "Ahora").

### Cobertura de ramas de `ScoreCalculator`

| Rama | Test |
|---|---|
| `IsPassed`: `>= 0.70` verdadero / falso | `IsPassed_UsesSeventyPercentThreshold` |
| `CalculateSessionScore`: paso con acierto | `Assessment_StepScoreIsWeightTimesPenalty` y siguientes |
| `CalculateSessionScore`: paso sin acierto (forzado o pendiente) | `Assessment_ForcedWithoutCorrectAttempt_ScoresZero`, `InProgress_ScoresOnlyResolvedSteps` |
| `CalculateSessionScore`: primer acierto por `AttemptNumber` | `FirstCorrectAttemptCounts_EvenIfUnordered` |
| `GetPenaltyFactor`: Guided | `GetPenaltyFactor_Guided_NeverPenalizes` |
| `GetPenaltyFactor`: Assessment, casos 1 / 2 / 3 / default | `GetPenaltyFactor_Assessment_PenalizesEachRetry` |
| `ShouldForceAdvance`: Assessment y `n > 3` (cada operando) | `ShouldForceAdvance_Assessment_FromFourthAttempt`, `ShouldForceAdvance_Guided_Never` |
| Constantes `PassingScore` y `MaxScoredAttempts` | `Constants_MatchBusinessRules` |

## Verificación de cada entrega

1. Rebuild de `TrainingApp` y build de `TrainingApp.Test` (MSBuild, `CLAUDE.md` → "Build
   y validación"). Sin `aspnet_compiler`: no se toca markup.
2. `vstest.console.exe TrainingApp.Test\bin\Debug\TrainingApp.Test.dll`: **todos en
   verde** (los nuevos y los que ya había). En esta fase es el criterio de aceptación.
3. Sin navegador: no cambia nada visible. El usuario revisa la salida de los tests y la
   lista de casos, y confirma antes del commit.
4. `/code-review`: E1 y E2 sin review (solo tests sobre una clase chica ya revisada). Al
   final de la E3, `low --max-findings 5` sobre `ScoreCalculatorTests.cs`, con foco en
   casos que falten.
5. Commit: `test: ... (Fase 5, entrega N)`.

## Preguntas abiertas (para el usuario)

1. ✅ **Pesos: `CLAUDE.md` y la BD no coincidían** (resuelto 2026-10-07: manda la BD). `CLAUDE.md` decía Line Clearance 10% y "fija, no varía por Area"; en la BD cada Line
   Clearance pesa 0.05 y Search 0.05 (Redefinity) o 0.15 (Moldeo). Se corrigieron
   `CLAUDE.md` → "Ponderación de pasos", `docs/glossary.md` y `general-flow.md`.
2. ✅ **Entradas imposibles en el flujo real** (resuelto 2026-10-07): un solo test, el de
   pesos que no suman 1 (entrega 2). Intento 0 o negativo y listas nulas no se prueban:
   `PodService` nunca los envía y validarlos sería cambiar el algoritmo.
3. **Fuera de alcance, para anotar:** la lógica de `PodService.RecordAttempt` (qué
   resultado se devuelve con el avance forzado, cuándo se guarda el puntaje final) usa
   repositorios concretos y no se puede probar sin BD. Si quieres cubrirla, sería otra
   fase (interfaces para los repositorios o tests de integración contra LocalDB).

## Riesgos

- **Pesos copiados en los tests:** si cambian en el SQL, los tests de las guías reales
  siguen probando los valores viejos. Mitigación: un comentario con el script de origen,
  y revisarlos cuando se toquen `05`/`07` o se cargue LoFlo.
- **`decimal` vs. `double`:** todo el cálculo es `decimal`, así que las comparaciones son
  exactas (`Is.EqualTo(0.70m)`, sin tolerancia). Si un test necesitara tolerancia, sería
  señal de un cambio de tipo y habría que revisarlo.
