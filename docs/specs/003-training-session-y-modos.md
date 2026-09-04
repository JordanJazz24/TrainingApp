# Spec 003: TrainingSession y Modos — Fase 4

**Status:** Approved (2026-09-04)
**Date:** 2026-09-04

---

## Problem

El motor de validación de pasos (Fase 3) está listo pero no hay capa de
ejecución que orqueste una sesión real: crear sesión, gate-keeping por modo,
persistir intentos y renderizar el flujo al operador. Sin esto el sistema no
es ejecutable por un usuario real.

---

## Scope

### In
- `TrainingSessionService` extendido: gate-keeping Guided vs. Assessment,
  caché de `StepValidator` por AreaId, método `Abandon`, score en tiempo real.
- `TrainingSessionRepository` extendido: métodos `Abandon` y
  `DeleteActiveByOperator`.
- `StepSubmitResult` extendido: campo `CurrentScore`.
- `StartSession.aspx` — nueva página post-login: selección de producto, área,
  guía y modo; inicia la sesión y redirige a `GuidedTest.aspx`.
- `GuidedTest.aspx` — orquestador de UI: renderiza estado del servidor,
  input genérico por paso, feedback por `StepType`, botón de abandono con
  confirmación.
- Nuevos stored procedures: `spTrainingSessionAbandon`,
  `spTrainingSessionDeleteActiveByOperator`.

### Out
- `UserControls` por tipo de paso (Fase 6).
- Dashboard administrativo.
- Área LoFlo (Fase 7).
- Tests NUnit de `ScoreCalculator` (Fase 5).
- Autenticación — ya completa (Fase 1).

---

## Domain Model

Sin cambios al dominio. `SessionStatus.Abandoned` ya existe en el enum.

**`StepSubmitResult`** — se agrega un campo:
```
CurrentScore : decimal   // score acumulado tras este submit (siempre presente)
```

Los repositorios y el dominio existentes no cambian de firma; solo se agregan
métodos nuevos.

---

## Flujo de Navegación

```
Login.aspx
    └─► StartSession.aspx          (selección: Product → Area → Guide → Mode)
            └─► GuidedTest.aspx    (ejecución paso a paso)
                    └─► [SessionCompleted] muestra score final en la misma página
                    └─► [Abandoned]        redirige a StartSession.aspx
```

---

## API Contract (métodos de BLL nuevos o modificados)

| Método | Cambio | Descripción |
|---|---|---|
| `TrainingSessionService.Start(operatorId, guideId, mode)` | Existente — se extiende | Borra sesión `InProgress` previa del mismo operador+guía antes de insertar la nueva |
| `TrainingSessionService.SubmitStep(sessionId, stepId, answer)` | Existente — se extiende | Añade caché de validator, gate-keeping correcto, devuelve `CurrentScore` |
| `TrainingSessionService.Abandon(sessionId)` | **Nuevo** | Marca sesión como `Abandoned` |
| `TrainingSessionRepository.Abandon(sessionId)` | **Nuevo** | Llama `spTrainingSessionAbandon` |
| `TrainingSessionRepository.DeleteActiveByOperator(operatorId, guideId)` | **Nuevo** | Llama `spTrainingSessionDeleteActiveByOperator` |

---

## Gate-Keeping por Modo

### Guided
- Reintentos ilimitados.
- Respuesta incorrecta → `Outcome = Incorrect`. El operador reintenta.
- No hay penalización; `CurrentScore` se calcula igual (sin penaltyFactor).
- Feedback visual: texto fijo por `StepType` (ver sección UI).

### Assessment
- Respuesta incorrecta en intentos 1–3 → `Outcome = Incorrect`. El operador reintenta.
- En intento 4+ incorrecto → `ShouldForceAdvance = true` → `Outcome = FailedForced`.
  La UI muestra mensaje "Agotaste tus intentos para este paso" y avanza.
- `CurrentScore` usa `GetPenaltyFactor(attemptNumber)` sobre el peso del paso.

---

## Caché del StepValidator

`TrainingGuideService.BuildValidatorForGuide` hace 3 queries (Area, NCCodes,
DCParams). Se cachea en `MemoryCache` del servidor, **clave = AreaId**, TTL = 60
minutos.

`TrainingSessionService.SubmitStep` consulta el caché antes de llamar
`BuildValidatorForGuide`. Si hay hit, usa el validator cacheado.

---

## StartSession.aspx

### Flujo de selección
1. Carga lista de `Products` activos (solo R&C en esta fase).
2. Al seleccionar Product → carga `Areas` del producto (solo Moldeo).
3. Al seleccionar Area → carga `TrainingGuides` publicadas de esa área.
4. Operador elige guía y modo (Guided / Assessment).
5. Submit → `TrainingSessionService.Start(operatorId, guideId, mode)` →
   `Response.Redirect("GuidedTest.aspx?sessionId=" + session.Id)`.

### Regla de sesión previa
`Start` llama a `DeleteActiveByOperator` antes de insertar la nueva sesión.
No se pide confirmación al operador — la sesión anterior se descarta silenciosamente.

---

## GuidedTest.aspx

### Elementos de UI
| Elemento | Descripción |
|---|---|
| Header de sesión | Modo (Guided / Assessment), nombre de la guía |
| Progreso | "Paso N de M" |
| Score actual | Decimal 0–1 formateado como porcentaje (siempre visible) |
| Área del paso | `StepType` como etiqueta + input genérico `<asp:TextBox>` |
| Botón Submit | Envía respuesta al servidor via PostBack |
| Área de feedback | Mensaje de resultado (correcto / incorrecto / force-advance) |
| Hint por StepType | Texto fijo por tipo de paso, visible tras respuesta incorrecta |
| Botón Abandonar | Abre modal JS de confirmación antes de llamar `Abandon` |
| Panel de completion | Visible solo cuando `Outcome = SessionCompleted`; muestra score final |

### Texto de hint por StepType (hardcodeado en ASPX code-behind)
| StepType | Hint |
|---|---|
| ResourceSetup | Escribe "ok" para confirmar la configuración del recurso |
| LineClearance | Escribe "true" para confirmar que la línea está limpia |
| Search | Escribe "ok" para confirmar que navegaste al Start Order |
| DataCollection | Envía JSON con los parámetros: {"<id>":"<valor>", ...} |
| LogNC | Escribe el código NC exacto (ej. F_FS_MANCHAS) |
| Assembly | Escribe el número de componentes escaneados |
| Complete | Escribe "ok" para cerrar el proceso |

### Flujo de PostBack (SubmitStep)
1. Llama `TrainingSessionService.SubmitStep(sessionId, stepId, answer)`.
2. Actualiza score visible con `result.CurrentScore`.
3. Si `Outcome = Incorrect` → muestra feedback + hint; mantiene el mismo paso.
4. Si `Outcome = Correct` → oculta feedback; recarga con `result.NextStepOrder`.
5. Si `Outcome = FailedForced` → muestra "Agotaste tus intentos"; avanza al siguiente paso.
6. Si `Outcome = SessionCompleted` → muestra panel de completion con score final; oculta input.

### Abandon
- Botón dispara `confirm()` de JS.
- Si confirmado → PostBack que llama `TrainingSessionService.Abandon(sessionId)` →
  `Response.Redirect("StartSession.aspx")`.

---

## Nuevos Stored Procedures

```sql
-- spTrainingSessionAbandon
UPDATE TrainingingSessions
SET Status = 2, CompletedAt = SYSUTCDATETIME()
WHERE Id = @SessionId

-- spTrainingSessionDeleteActiveByOperator
DELETE FROM TrainingSessions
WHERE OperatorId = @OperatorId
  AND TrainingGuideId = @GuideId
  AND Status = 0  -- InProgress
```

---

## Authorization

- `StartSession.aspx` y `GuidedTest.aspx` requieren `FormsAuthentication`
  activo (igual que las páginas de Auth).
- El `sessionId` en query string se valida contra el `OperatorId` de la sesión
  HTTP — un operador no puede ver la sesión de otro.

---

## Edge Cases & Failure Modes

| Caso | Comportamiento |
|---|---|
| `sessionId` en QS no existe en DB | Redirigir a `StartSession.aspx` |
| `sessionId` pertenece a otro operador | Redirigir a `StartSession.aspx` |
| Sesión ya `Completed` o `Abandoned` | Redirigir a `StartSession.aspx` |
| `stepId` no pertenece a la guía de la sesión | Ignorar submit; re-render mismo paso |
| Área sin guías publicadas | `StartSession.aspx` muestra mensaje "No hay guías disponibles" |
| App Pool reciclado entre pasos | Session HTTP perdida → redirigir a Login (comportamiento estándar de FormsAuth) |

---

## Non-Functional Requirements

- Caché del validator: TTL 60 min, MemoryCache del proceso IIS.
- Sin requisitos de latencia formales — simulador interno, carga baja.

---

## Integrations

Ninguna externa. Todo es interno al proceso IIS + SQL Server local.

---

## Acceptance Criteria

**AC-1** — Inicio de sesión nueva  
Dado que el operador está autenticado y accede a `StartSession.aspx`,  
cuando selecciona R&C → Moldeo → Guía R&C Moldeo → Guided y hace Submit,  
entonces se crea una `TrainingSession` con `Status=InProgress`, `Mode=Guided`,
`CurrentStepOrder=1` y se redirige a `GuidedTest.aspx?sessionId=<id>`.

**AC-2** — Sesión previa descartada  
Dado que el operador tiene una sesión `InProgress` para la misma guía,  
cuando inicia una nueva sesión,  
entonces la sesión anterior se elimina de la BD y la nueva queda como única activa.

**AC-3** — Respuesta correcta en Guided  
Dado que el operador está en el paso 1 (ResourceSetup) en modo Guided,  
cuando envía "ok",  
entonces `Outcome=Correct`, el score se actualiza, y la página muestra el paso 2.

**AC-4** — Reintentos ilimitados en Guided  
Dado que el operador envía una respuesta incorrecta en modo Guided,  
cuando la envía N veces (N > 4),  
entonces siempre recibe `Outcome=Incorrect` con hint; nunca avanza forzadamente.

**AC-5** — Penalización en Assessment  
Dado que el operador está en modo Assessment y responde correctamente al 2do intento,  
cuando el servidor calcula el score del paso,  
entonces el paso contribuye `StepWeight × 0.80` al score total.

**AC-6** — Force-advance en Assessment  
Dado que el operador falla el paso 4 veces en modo Assessment,  
cuando envía el 4to intento incorrecto,  
entonces `Outcome=FailedForced`, la UI muestra "Agotaste tus intentos para este paso"
y avanza al siguiente paso.

**AC-7** — Score en tiempo real  
Dado que el operador completa cualquier paso correctamente,  
cuando el servidor responde,  
entonces `StepSubmitResult.CurrentScore` refleja el score acumulado actualizado
y la UI lo muestra como porcentaje.

**AC-8** — Sesión completada  
Dado que el operador completa el último paso (paso 8, Complete),  
cuando envía "ok",  
entonces `Outcome=SessionCompleted`, `TrainingSession.Status=Completed`,
`FinalScore` persistido, y la UI muestra el panel de completion con el score final.

**AC-9** — Abandono de sesión  
Dado que el operador hace clic en "Abandonar" y confirma el modal,  
cuando el servidor procesa el abandon,  
entonces `TrainingSession.Status=Abandoned`, `CompletedAt` registrado,
y el operador es redirigido a `StartSession.aspx`.

**AC-10** — Seguridad: sesión ajena  
Dado que un operador intenta acceder a `GuidedTest.aspx?sessionId=<id_ajeno>`,  
cuando el servidor valida la sesión,  
entonces redirige a `StartSession.aspx` sin exponer datos de otro operador.

**AC-11** — Caché del validator  
Dado que dos operadores envían pasos de la misma área en el mismo proceso IIS,  
cuando `SubmitStep` es llamado la segunda vez,  
entonces el validator se obtiene del `MemoryCache` sin llamadas adicionales a la BD
(Area, NCCodes, DCParams).

---

## Deferred Decisions

| Decisión | Fallback elegido | Revisitar cuando |
|---|---|---|
| Score en modo Guided ¿tiene penaltyFactor? | Sí, misma fórmula — en Guided todos aciertan en algún intento, el factor real depende del intento en que acertaron | Si negocio decide que Guided siempre da 100% |
| Retroalimentación específica por paso (qué hacer exactamente) | Texto de hint fijo por StepType hardcodeado en ASPX | Fase 6 puede enriquecer con UserControls con UI real |

---

## Open Questions

_(ninguna — listo para aprobación)_
