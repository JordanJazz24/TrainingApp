# Auditoría final antes de la 6.6 — 2026-10-06 ✅

> Pedida por el usuario el 2026-10-05 para asegurar que no se escapó nada grave antes de
> los overlays. Plan propuesto por Claude en un `/btw` y **aprobado por el usuario el
> 2026-10-06**. Continúa la numeración de `2026-10-01-auditoria.md`: hallazgos desde
> **AUD-33**, decisiones desde **D-13**. Los criterios de severidad, los falsos positivos y
> lo marcado como "limpio, no tocar" de esa auditoría siguen vigentes.

## Alcance

La auditoría anterior fue completa y reciente; desde entonces el cambio grande fue la spec
009 (`b2db442..df08c44`). Por eso es una auditoría enfocada en ese cambio más una regresión
completa en el navegador, con el mismo esquema A → B → C, más liviana.

**Costo:** un `/code-review high` sobre todo el cambio en vez de seis, medium en las
entregas; lo demás (arch-check, código muerto) es barato.

## Plan

- **Fase A · Revisión, solo lectura (no se corrige nada mientras se audita):**
  1. **Un solo `/code-review high`** sobre todo el cambio de la spec 009 (no uno por entrega
     ni por carpeta). Foco: las reglas de `PodService` (secuencia, guardar el estado después
     del intento, reconstrucción); las capas (que la página no decida nada); la redirección
     de P-1.
  2. `/dotnet-claude-kit:arch-check` (dependencias entre capas; es barato).
  3. Código muerto después del refactor, con Roslyn: helpers de `GuidedTest` sin uso, claves
     de `StepViewModel` sin lectores, `IStepControl`/`CompleteControl`.
  4. `/security-review` acotado a lo que tocó la Session y el login: logout con D-9, la
     redirección de P-1 y `SessionPodStateStore`.
  5. Docs contra código: que `CLAUDE.md` (árbol y convenciones), el roadmap y el glosario
     describan lo que existe hoy.
  6. Repaso de la deuda abierta (abajo), para decidir qué se arregla ya y qué se difiere.
- **Fase B · Lista única:** hallazgos nuevos AUD-33 en adelante, con severidad, vía (plan
  directo, decisión o diferido) y entrega. Las decisiones se preguntan juntas, numeradas desde
  **D-13**, cada una con una propuesta.
- **Fase C · Entregas chicas, solo si hay hallazgos Alto o Medio.** Mismo ciclo: arreglo →
  MSBuild + `aspnet_compiler` → `/code-review medium` → checklist → prueba del usuario →
  commit.
- **Fase D · Pasada final en el navegador** (criterio de cierre antes de la 6.6): un solo
  checklist de punta a punta, un recorrido por combinación: Moldeo en Guided y en Assessment
  (con errores, clicks fuera de secuencia y avances forzados); Redefinity en Guided y en
  Assessment; pérdida de la Session; sesión única y logout. Así la 6.6 arranca sobre una base
  probada entera, no por entregas sueltas.

## Deuda abierta a repasar (Fase A, punto 6)

- El Start de NonSerialized acepta cualquier SFC escrito (`docs/specs/009-plan.md`, E2).
- B-2 de Log NC: solo se da con manipulación del cliente (`docs/specs/009-plan.md`, E4).
- Guardar por request el BOM y los grupos DC.
- El botón Atrás del navegador todavía puede reenviar un POST viejo (riesgo aceptado hasta
  ahora; F5 sí está resuelto).
- AUD-28/29: siguen para la Fase 9 (D-7).

**Decisión a tomar en esta auditoría:** los tests siguen fuera por decisión del usuario.
Ahora las reglas del POD viven en `PodService`, pero no se pueden probar sin BD porque el
servicio crea sus repositorios internamente. Si algún día se retoma la Fase 5, conviene
decidir ahora si se prepara eso o se deja como está.

## Fase A · Resultados (2026-10-06) ✅

1. **`/code-review high`** sobre `b2db442..df08c44` (todo el cambio de la spec 009): 9
   hallazgos, ninguno Alto. Verificados contra el código: AUD-33, 34 y 36 a 42.
2. **`arch-check`: conforme.** Ninguna dependencia en dirección prohibida (BLL sin
   `System.Web` ni presentación; DAL sin BLL; Domain sin dependencias; Pages/UI sin DAL),
   ningún ciclo. Info: la página calcula los intentos restantes y aprobado/reprobado con
   `ScoreCalculator` (va con AUD-38).
3. **Código muerto:** los 31 símbolos de `find_dead_code` son handlers enlazados en el
   markup (falsos positivos, verificado uno por uno). Todas las claves de
   `StepViewModel.ExtraData` tienen lector. A mano: AUD-43.
4. **`/security-review`** (Session y login): **ninguna vulnerabilidad nueva.** Dueño y
   estado validados en `LoadContext`, secuencia y número de intento revalidados en
   `spStepAttemptRecord`, ids del cliente solo se cruzan con la orden del servidor, CSRF
   validado antes de los handlers, P-1 sin redirección abierta ni bucle. Hallazgos de
   corrección: AUD-33, 34 y 35.
5. **Docs contra código:** AUD-44 (detalles menores).
6. **Deuda abierta:** repasada en las decisiones D-13 a D-16.

**Falsos positivos, no volver a reportar** (además de los de la auditoría anterior): la
redirección P-1 a `Request.RawUrl` (misma app, solo en un postback autenticado y propio);
el `PodState` vacío que `LoadContext` guarda la primera vez (inofensivo).

## Lista única de hallazgos (AUD-33 en adelante)

Severidad como en la auditoría anterior: Alto = rompe el flujo, el puntaje o expone un
secreto · Medio = riesgo real en condiciones normales · Bajo = higiene o caso raro · Info.

| ID | Sev. | Hallazgo | Dónde | Arreglo propuesto | Vía |
|---|---|---|---|---|---|
| AUD-33 | Medio | **El logout no cierra la sesión** si la sesión de entrenamiento ya no está `InProgress`: `Page_Load` llama a `LoadContext`, que redirige a `StartSession` antes de que corra `BtnLogout_ServerClick`. Pasa siempre en el panel final (el ícono sigue visible) y en una pestaña cuya sesión abandonó D-9. `GuidedTest` es la única página con logout, así que el operador queda autenticado; en una PC compartida el siguiente entrena con su identidad. Ya pasaba antes de la spec 009, pero ahora también impide D-9. | `GuidedTest.aspx.cs:137, 845`; `StartSession.aspx` (sin logout) | Logout que no dependa del contexto del POD (ej. una página `Logout.aspx` propia), disponible también en `StartSession`. **D-13** | E1 |
| AUD-34 | Bajo | P-1 descarta también los clicks de Logout y Abandon tras una reconstrucción (no usan filas de la orden): la página solo se recarga y el operador tiene que repetir el click. | `GuidedTest.aspx.cs:144` | Exceptuar Logout y Abandon de P-1 (Logout queda resuelto con AUD-33). | E1 |
| AUD-35 | Bajo | El logout depende de la BD: si `AbandonAllActive` falla, sale la página de error y el operador queda autenticado. | `GuidedTest.aspx.cs:848` | `SignOut` y `Session.Abandon` en un `finally`. | E1 |
| AUD-36 | Bajo | `SubmitResourceSetup` registra el intento y avanza **antes** de crear la orden con una Shop Order que la BLL no valida: si llegara una Shop Order nula, se guardaría el intento y después explotaría en `SimulatedOrder.DeriveSFCCode` (página de error; el siguiente request reconstruye con la orden por defecto). Hoy no se puede: el code-behind del control vuelve a validar el formato en el servidor; falta que la BLL proteja su invariante (`CLAUDE.md` → "La BLL protege sus invariantes"). | `PodService.cs:144` | Regla de formato en la BLL (`SimulatedOrder.IsValidShopOrder`), exigida antes de registrar el intento (excepción, no intento) y reutilizada por el control. | E2 |
| AUD-37 | Bajo | `SubmitStartChecklist` no vuelve a validar la selección de Browse Work List que validó `ClickStart`. Latente: hoy el único área con checklist (Moldeo) es NonSerialized y no activa piezas; un área Serialized con checklist podría quedar sin piezas activas y trabada en Guided. | `PodService.cs:202` | Repetir `PieceSelectionRule` en el submit del checklist. | E2 |
| AUD-38 | Bajo | La página todavía deriva estado del flujo: `IsSearchCompleted` duplica `PodService.IsResolved` y 4 métodos recalculan el paso actual con `guide.Steps.First(Order == CurrentStepOrder)` en vez de `_ctx.CurrentStep`; también calcula intentos restantes y aprobado/reprobado (arch-check). | `GuidedTest.aspx.cs:1083` y otros | Exponer `IsResolved(StepType)`, intentos restantes y resultado final desde `PodContext`/`PodService`. | E3 |
| AUD-39 | Bajo | Catálogos consultados varias veces por request: un Save de grupo DC lee grupos y parámetros 3 veces cada uno (~10 conexiones); Assembly lee el BOM 3 veces; `CreateOrder` y el panel final cargan todos los Products para encontrar uno. Es la deuda "guardar por request el BOM y los grupos DC". | `GuidedTest.aspx.cs:745`, `PodService.CreateOrder` | Cargar los catálogos una vez por request (perezoso, en el contexto) y `GetProduct(id)`. | E3 |
| AUD-40 | Info | `PodContext.State` expone un `PodState` mutable (setters y listas públicas): "solo `PodService` modifica el estado" es solo convención. | `PodContext.cs:28`, `PodState.cs`, `SimulatedOrder.cs` | Setters `internal` / vistas de solo lectura. | Diferido (D-16) |
| AUD-41 | Bajo | D-9 abandona en BD las otras sesiones del operador, pero sus `PodState` siguen en la Session hasta que expira (solo Abandon los borra). | `TrainingSessionService.cs:18`, `SessionPodStateStore` | Al iniciar una guía, borrar los estados de las otras sesiones. | E4 |
| AUD-42 | Info | La orden no es reproducible (RowIds `Guid.NewGuid()`, piezas y SFCs con `new Random()`): por eso existe P-1, y tras una reconstrucción en Serialized el operador puede ver otras piezas (3-5) que las que tenía. | `SimulatedOrder.cs:32`, `SerialSfcGenerator` | Semilla determinista por sesión (haría la reconstrucción transparente). | Diferido (D-16) |
| AUD-43 | Bajo | Código muerto tras el refactor: `SimulatedOrder.FindRow` (sin llamadas); `AssemblyProgress.Covers` (solo lo usan los tests; producción usa `CoversWith`); `CompleteControl` sigue siendo un stub y 3 controles tienen el evento con accessors vacíos (separar `IStepControl` no entró en la spec 009). | `SimulatedOrder.cs:80`, `AssemblyProgress.cs:19`, `CompleteControl.ascx.cs` | Borrar `FindRow`; `Covers` se queda con sus tests; `IStepControl` sin cambios (inofensivo). | E4 |
| AUD-44 | Bajo | Docs: al árbol de `CLAUDE.md` le faltan `PodCommandResult`, `SplitSfcGenerator`, `StepSubmitResult` y `StepValidationContext`; el historial S2 del roadmap nombra `TrainingSessionService.RegisterOutOfSequence` (hoy en `PodService`); en `009-plan.md` el checklist de la E5 quedó sin marcar aunque se verificó con el de la E6. | `CLAUDE.md`, `docs/roadmap.md`, `docs/specs/009-plan.md` | Corregir. | E4 |

## Decisiones del usuario (D-13 en adelante)

Cerradas por el usuario el 2026-10-06:
- **D-13 (AUD-33):** el ícono ⏻ del POD **cierra la sesión siempre**, también después de
  "Session Complete" o en una pestaña cuya guía ya se abandonó, y **`StartSession` gana un
  enlace de logout** para salir sin cerrar el navegador. Abandon y el logout con la guía en
  curso no cambian (D-9: el logout marca la sesión `InProgress` como abandonada).
- **D-14 (deuda, D-12 de la spec 009):** el Start de NonSerialized **queda como está**:
  `TxtSFC` es de solo lectura y el SFC llega desde Browse Work List, así que escribir otro
  exige manipular la página. Sale de la deuda.
- **D-15 (Fase 5):** **no se prepara ahora**: no bloquea nada; la inyección de repositorios
  en `PodService` se hace al retomar la Fase 5.
- **D-16 (alcance):** se hacen **E1 a E4**. AUD-40 y AUD-42 se difieren; B-2 de Log NC y el
  botón Atrás siguen como riesgo aceptado (la BLL ya rechaza un POST viejo:
  `spStepAttemptRecord` revalida paso e intento).

## Entregas (Fase C)

| Entrega | Contenido | Revisión |
|---|---|---|
| E1 · Logout ✅ | AUD-33, 34, 35. `SessionHelper.Logout()` único (abandona las `InProgress` y, aunque falle, `Session.Abandon` + `SignOut` en un `finally`); `GuidedTest` atiende ⏻ antes de `LoadContext`; P-1 deja pasar Abandon; "Cerrar sesión" en `StartSession` (D-13). Build 0 warnings + `aspnet_compiler` OK; revisión de seguridad del diff sin hallazgos. Verificada por el usuario (2026-10-06). | `/security-review` |
| E2 · Invariantes de la BLL ✅ | AUD-36, 37. `SimulatedOrder.IsValidShopOrder` (la usan el control y `PodService.SubmitResourceSetup`, que la exige antes del intento); `SubmitStartChecklist` revalida la selección de Browse en Serialized (helper `IsBrowseSelectionValid`, compartido con `ClickStart`). Build 0 warnings + `aspnet_compiler` OK + 17 tests OK. `/code-review medium`: sin bugs; su único caso (Session perdida entre Start y el checklist en Serialized) no aplica porque P-1 recarga antes de los handlers. Verificada por el usuario (2026-10-06). | `/code-review medium` |
| E3 · Página sin decisiones y consultas ✅ | AUD-38, 39. `PodContext` expone `IsResolved(StepType)`, `CurrentStepOrDefault`, `IsPassed` y los catálogos del área (grupos y parámetros DC, NC, BOM, Product), perezosos y una vez por request; `StepSubmitResult.RemainingScoredAttempts`; `BuildValidator` pasa a `PodService` con esos catálogos. `GuidedTest` ya no usa `ScoreCalculator` ni `TrainingGuideService`. Save de grupo DC: de ~10 a ~6 conexiones; Add de Assembly: de ~7 a ~5. Build 0 warnings + `aspnet_compiler` OK + 17 tests OK. `/code-review medium`: sin bugs (un comentario fuera de lugar, corregido). Verificada por el usuario (2026-10-06). | `/code-review medium` |
| E4 · Limpieza ✅ | AUD-41, 43, 44. `PodService.StartSession` inicia la guía y descarta el estado del POD de las sesiones anteriores (`IPodStateStore.RemoveAll`); `StartSession` lo usa. Se borra `SimulatedOrder.FindRow`. Docs: árbol y convención de `CLAUDE.md`, historial S2 del roadmap, checklist de la E5 en `009-plan.md`. Build 0 warnings + `aspnet_compiler` OK + 17 tests OK. `/code-review medium`: sin hallazgos. Verificada por el usuario (2026-10-06). | `/code-review medium` |

## Pasada final en el navegador (Fase D) ✅

Criterio de cierre antes de la 6.6: un recorrido por combinación, con el código de
`d94f744` (E1-E4 incluidas). Los puntajes esperados salen de los pesos de
`07_RCMoldeoGuide.sql` y `05_SeedData.sql` y de la penalización 100/80/60/0 % (solo en
Assessment; Guided no penaliza, D-2).

**D1 · Moldeo en Assessment, con errores** (RS 20 · LC 5 · Start 15 · DC 30 · LC 5 ·
Split 5 · Log NC 10 · Complete 10):

| Paso | Qué hacer | Puntaje del paso |
|---|---|---|
| Resource Setup | Guardar con "Open" (intento 1), después "Set Up" | 20 × 0.8 = 16 |
| Line Clearance | Click en Start (fuera de secuencia, intento 1), después LC "Sí" | 5 × 0.8 = 4 |
| Start | Checklist completo al primer intento | 15 |
| Data Collection | 4 Saves con un campo vacío (cuenta como intento): "Te quedan 2… 1…", al 4.º avance forzado | 0 |
| Line Clearance | "Sí" | 5 |
| SFC Split | Un split válido y cerrar | 5 |
| Log NC | Fila del split + código NC al primer intento | 10 |
| Complete | Complete sin fila (intento 1, "Select 1 row to complete"), después con la fila | 10 × 0.8 = 8 |

Esperado: **63.0 % · ✗ REPROBADO**. El score del header va sumando en cada paso.

**D2 · Moldeo en Guided:** un error en cada paso que lo permita (LC "No", Setup State
"Open", un campo de DC vacío) → aparece el hint y se puede reintentar; los
botones que no son del paso están deshabilitados. Final: **100.0 %**, sin "Result".

**D3 · Redefinity en Assessment** (RS 20 · LC 5 · Start 5 · Assembly 15 · DC 30 · LC 5 ·
Log NC 10 · Complete 10):
- Start: elegir solo algunas piezas en Browse → aviso "Selecciona todas las piezas"
  (sin intento); después todas → 5.
- Assembly: click en Data Collection (fuera de secuencia, intento 1) → después ensamblar
  todo el BOM → 15 × 0.8 = 12.
- Todo lo demás al primer intento (DC por pieza con "SFC … (n/N)").
- Esperado: **97.0 % · ✓ APROBADO**.

**D4 · Redefinity en Guided:** de punta a punta sin errores; Assembly y DC por pieza;
Log NC y Complete con todas las piezas (selección parcial = aviso). Final: **100.0 %**.

**D5 · Pérdida de la Session** (guardar `Web.config` sin cambios reinicia la app):
- Moldeo en Assessment detenido en Data Collection: reiniciar → click en Data Collection →
  la página solo se recarga (P-1); el POD muestra la orden por defecto (Shop Order
  `303406950`) ya iniciada; el score no cambia; se puede seguir hasta el final.
- Redefinity a mitad de DC: reiniciar → DC vuelve a empezar desde la primera pieza, sin
  penalización.
- Abandon después de reiniciar → va directo a StartSession (AUD-34).

**D6 · Sesión única y logout:**
- Iniciar otra guía desde StartSession → la anterior queda `Abandoned` (2) y la nueva
  arranca vacía; en la pestaña vieja cualquier click lleva a StartSession.
- ⏻ con la guía en curso → Login y la sesión queda `Abandoned`.
- ⏻ en "Session Complete" → Login (AUD-33). "Cerrar sesión" en StartSession → Login.
- Después del logout, ir a StartSession por URL o historial → Login.

**Resultado:** verificado por el usuario el 2026-10-06 ("funciona"). **Auditoría cerrada**: la 6.6 arranca sobre esta base. Quedan diferidos AUD-40 y AUD-42 (D-16) y, de la auditoría anterior, AUD-28/29 para la Fase 9.
