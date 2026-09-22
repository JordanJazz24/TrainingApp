# Spec 006: Controles de paso estáticos en GuidedTest

**Status:** In Review
**Date:** 2026-09-22
**Fase:** 6 (cierre)
**Relacionada con:** Spec 004 (contrato de UserControls), Spec 005 (POD con orden dinámica)

---

## Problem

`GuidedTest.aspx` carga el UserControl de cada paso con `LoadControl(path)` cuando
el operador hace clic en un botón de la botonera, y lo reconstruye en cada postback
posterior. En Web Forms eso exige recrear el mismo control, en la misma posición del
árbol, antes de `LoadViewState`. Fallar en eso produce errores difíciles de rastrear.

En una sola sesión de desarrollo (2026-09-22) aparecieron cuatro defectos distintos
con esa misma raíz:

1. `CreateChildControls` nunca se ejecutaba en el `Page` (el parser deja
   `ChildControlsCreated = true`), así que ningún control se reconstruía en los
   postbacks internos → **DC Collect no hacía nada** (evento descartado).
2. El guard de lanzadores leía `Request.Form[UniqueID]`, que `HtmlButton` nunca
   envía (hace postback por `__EVENTTARGET`).
3. Se grababa en ViewState un path que no correspondía al control realmente en el
   árbol → **`HttpException` "No se pudo cargar viewstate" en SFC Split**.
4. El control residual de un modal-overlay guardaba ViewState que el request
   siguiente no podía aplicar.

El fix vigente (restauración en `OnInit` + path en `HfActiveCtrlPath`) destraba los
flujos pero agrega un problema de seguridad: **el path viaja sin firma y el servidor
le hace `LoadControl()`**. Además, cualquier control o postback interno nuevo puede
volver a destapar esta clase de bugs, y en la Fase 8 eso se traduce en sesiones
rotas a mitad de una evaluación, con operadores reales.

El conjunto de controles es **cerrado y conocido en tiempo de compilación**: un
control por `StepType`, que es un `enum`. La carga dinámica resuelve un problema que
el sistema no tiene. Confirmado con negocio (2026-09-22):

- Los admins **no** crearán tipos de paso desde la UI; solo guías que combinan tipos
  existentes.
- Un mismo `StepType` **no** necesitará UIs estructuralmente distintas por Area; las
  variaciones son de datos (parámetros, códigos, preguntas) y viajan en el `StepViewModel`.

Por eso esta spec es la arquitectura definitiva de carga de controles, no un paso
intermedio.

---

## Scope

### Prerrequisito

- El fix vigente (`OnInit` + `HfActiveCtrlPath`) se **prueba en el navegador y se
  commitea** antes de empezar la implementación, como punto de rollback estable.
  La migración parte de un estado que funciona y reemplaza ese código.

### In

1. **Declarar los 8 UserControls de paso de forma estática** en `GuidedTest.aspx`,
   con `Visible="false"` por defecto:

   | Control | StepType | Contenedor | Modal | Lanzador |
   |---|---|---|---|---|
   | `ResourceSetupControl` | ResourceSetup | host de modales | `modalResourceSetup` | `BtnResourceSetup` |
   | `LineClearanceControl` | LineClearance | host de modales | `modalDCStandalone` | `BtnActivities` |
   | `SearchControl` | Search | host de modales | propio (secuencia de 4 modales) | `BtnStart` |
   | `SFCSplitControl` | SFC_Split | host de modales | `modalSFCSplit` | `BtnSFCSplit` |
   | `AssemblyControl` | Assembly | reemplazo del POD | `modalAssemble` | `BtnAssemble` |
   | `DataCollectionControl` | DataCollection | reemplazo del POD | `modalDCEntry` (interno) | `BtnDCCollection` |
   | `LogNCControl` | LogNC | reemplazo del POD | — | `BtnLogNC` |
   | `CompleteControl` | Complete | — | — | (`BtnComplete` hace submit directo) |

   Se conserva la distinción de dos contenedores: los modal-overlay dejan visible el
   POD detrás del modal; los de reemplazo cubren el POD Work List.

2. **Visibilidad controlada por el servidor.** El postback del lanzador valida en
   servidor (paso activo, fila seleccionada donde aplique), arma un `StepViewModel`
   fresco, reinicia el control y lo hace visible. Como mucho un control de paso está
   visible por request.

3. **Eventos cableados en el markup** (`OnStepSubmit="..."`, `OnSplitRequested="..."`)
   en lugar de `+=` en code-behind.

4. **`CompleteControl` implementa `IStepControl`** (hoy no lo implementa). Sin cambio
   funcional: `BtnComplete` sigue haciendo submit directo con Answer `"complete"`.

5. **Contrato de reinicio:** `Initialize()` pasa a ser el único punto de reinicio de
   cada control y debe dejarlo en el mismo estado que una instancia recién creada
   (inputs vacíos y estado interno limpio: grupo activo de DC, parámetros renderizados,
   confirmación de split, contador de escaneos, etc.). Todo lo que vive en el servidor
   (orden simulada, splits hechos, intentos, puntaje) se reconstruye desde el `StepViewModel`.

6. **Regla de tolerancia:** todo control debe tolerar `ViewModel == null` en
   `Page_Load` e `Initialize()`, porque los controles invisibles igual recorren
   Init/Load en cada request.

7. **Eliminar la infraestructura de carga dinámica** de `GuidedTest.aspx.cs`:
   restauración en `OnInit`, `HfActiveCtrlPath` (markup + designer),
   `IsPostBackFrom`, `IsStepLauncherPostBack`, `LoadControl` y la parte de
   `LoadAndShowControl` que crea instancias, `RemoveStepControl`, `IsModalOverlayPath`
   (matching por strings), `WireStepControl` y el hack de `EnableViewState = false`
   en `ClearActiveControlViewState`.

8. **Guía de prueba solo para dev:** script SQL nuevo en `Database/StoredProcedures/`,
   marcado `DATOS DE PRUEBA — NO USAR EN PRODUCCIÓN` (mismo criterio que
   `05_SeedData.sql`), que carga pasos a la guía de Redefinity (GuideId 1, hoy sin pasos).
   Su único fin es poder recorrer `AssemblyControl` en el navegador. Idempotente.

   Secuencia propuesta (Redefinity: Serialized, RequiresAssembly=true,
   RequiresDataCollection=true; sin SFC Split por ser Serialized):

   | Order | StepType | Weight |
   |---|---|---|
   | 1 | ResourceSetup | 0.20 |
   | 2 | LineClearance | 0.05 |
   | 3 | Search | 0.05 |
   | 4 | Assembly | 0.15 |
   | 5 | DataCollection | 0.30 |
   | 6 | LineClearance | 0.05 |
   | 7 | LogNC | 0.10 |
   | 8 | Complete | 0.10 |
   | | **Total** | **1.00** |

### Out

- Desarrollo del paso Complete (UI y comportamiento propios): tendrá su propia spec.
- Cualquier cambio visible para el operador (**paridad total de UX**, ver AC-1).
- Cambios en BLL, DAL, Domain, scoring, stored procedures o catálogos reales.
- Los huecos que ya existían en `AssemblyControl` y `ExpectedComponentCount`
  (ver Deferred Decisions).
- Corregir bugs del camino Serialized que se descubran al recorrer la guía de prueba
  y que no sean causados por la migración: se registran, no se arreglan acá.
- La discrepancia de `RequiresAssembly` de LoFlo (BD=false, CLAUDE.md=true): Fase 7.

---

## Domain Model

Sin cambios. No se tocan entidades, tablas ni stored procedures, salvo el script de
datos de prueba del punto 8.

### Contrato de UI (sin cambios de firma)

```csharp
public interface IStepControl
{
    StepViewModel ViewModel { get; set; }
    event EventHandler<StepSubmitEventArgs> OnStepSubmit;
    void Initialize();   // semántica reforzada: reinicio completo (Scope In #5)
}
```

`SFCSplitControl` conserva además `OnSplitRequested`.

---

## API Contract

No aplica: no hay endpoints HTTP. La interacción sigue siendo por postback de Web Forms.

Contrato de postback resultante:

| Postback | Qué hace el servidor |
|---|---|
| Click de un lanzador | Valida el paso activo y las precondiciones → arma `StepViewModel` → `Initialize()` → `Visible = true` → abre el modal si corresponde |
| Postback interno de un control (Collect, Split, Add, 4 modales de Search) | El control sigue visible con su estado; no se reinicia |
| `OnStepSubmit` | `TrainingSessionService.SubmitStep` → feedback igual que hoy → el control se oculta cuando corresponde (ver Edge Cases) |

`StepViewModel` se arma **solo para el control que se abre o que está visible**,
nunca para los 8 en cada request (`BuildStepViewModel` consulta la BD).

---

## Authorization

Sin cambios: FormsAuth + `LoadAndValidateSession` (operador dueño de la sesión y
sesión `InProgress`).

- `EnableEventValidation` queda en su valor por defecto (`true`; verificado: no está
  desactivado en ningún archivo del proyecto). ASP.NET rechaza los eventos dirigidos
  a controles que no se renderizaron en la respuesta anterior.
- La BLL (`SubmitStep`) sigue validando paso y secuencia. No se agrega un guard
  explícito en `GuidedTest`.
- Desaparece la carga de un path enviado por el cliente (`HfActiveCtrlPath` → `LoadControl`).

---

## Edge Cases & Failure Modes

| # | Caso | Comportamiento requerido |
|---|---|---|
| E1 | El operador cierra un modal con la X (solo cliente) y vuelve a abrir el mismo paso | El control arranca limpio (`Initialize()`); lo que está en el servidor se conserva |
| E2 | F5 / recarga por GET | POD sin ningún control abierto; progreso y puntaje intactos desde el servidor |
| E3 | Submit incorrecto | Igual que hoy: el control sigue visible, se reabre el modal si aplica, SweetAlert con hint |
| E4 | Force-advance en Assessment (4º intento) | Igual que hoy: el paso se marca fallido, el control se oculta y se habilita el lanzador siguiente |
| E5 | Sesión completada | Todos los controles ocultos, panel de completion visible |
| E6 | Postback manipulado contra un control invisible | Rechazado por EventValidation o por la BLL; el progreso no cambia |
| E7 | Postbacks internos (DC Collect, SFC Split → Split, Assembly → Add, 4 modales de Search) | El control sigue visible y conserva su estado entre esos postbacks |
| E8 | Las dos instancias de Line Clearance (apertura y cierre) | Usan el mismo `LineClearanceControl`; cada apertura reconstruye su ViewModel |
| E9 | Confirmación después del submit de un modal-overlay (ResourceSetup reabre su modal con banner; LC) | Igual que hoy: el control se muestra en esa respuesta y en el siguiente request ya no está visible |
| E10 | Un bug del camino Serialized aparece con la guía de prueba | Se registra y queda fuera de alcance, salvo que lo cause la migración |
| E11 | Un control invisible recorre Init/Load sin ViewModel | Sin excepción y sin consultas a BD (regla de Scope In #6) |

---

## Non-Functional Requirements

- **Sin consultas extra a la BD por request:** solo el control abierto o visible arma su ViewModel.
- **Peso de página:** los controles invisibles no renderizan HTML. El ViewState agregado
  por 8 controles declarados es marginal; no hay presupuesto de latencia nuevo.
- **Mantenibilidad:** agregar un `StepType` futuro = crear el `.ascx` + una línea de
  markup + un `case`. Sin infraestructura de carga que tocar.

---

## Integrations

Ninguna. Sin servicios externos, eventos ni efectos colaterales nuevos.

---

## Acceptance Criteria

Las páginas ASPX no se pueden testear con NUnit en este stack. La verificación es
**manual en navegador**, documentada como checklist, en modo **Guided y Assessment**.

- **AC-1 (paridad):** Dado el recorrido completo de R&C/Moldeo, cuando se ejecuta en
  Guided y en Assessment, entonces cada pantalla, modal, banner, SweetAlert y
  reemplazo del POD es visualmente y funcionalmente igual al de antes de la migración.
- **AC-2 (sin carga dinámica):** Dado el código de `GuidedTest.aspx.cs`, entonces no
  hay ninguna llamada a `LoadControl`, no existe `HfActiveCtrlPath` y no se lee ningún
  path de control desde `Request.Form`.
- **AC-3 (declaración estática):** Dado `GuidedTest.aspx`, entonces los 8 controles
  de la tabla de Scope In #1 están declarados con `Visible="false"` y sus eventos
  cableados en el markup.
- **AC-4 (DC Collect):** Dado el paso DataCollection con una fila seleccionada, cuando
  el operador abre DC Collect y hace clic en *Collect* de un grupo, entonces se abre
  `modalDCEntry` con los parámetros de ese grupo; al guardar, el paso se evalúa.
- **AC-5 (SFC Split):** Dado el paso SFC Split, cuando el operador cierra el LC de
  cierre con la X, selecciona una fila y abre SFC Split, entonces el modal abre con la
  Qty disponible correcta, *Split* crea la fila nueva y *Close* completa el paso, sin
  ninguna `HttpException` de ViewState.
- **AC-6 (reapertura limpia):** Dado cualquier control modal, cuando el operador lo
  cierra con la X y vuelve a abrirlo, entonces los inputs y el estado interno arrancan
  vacíos y los datos del servidor se conservan (E1).
- **AC-7 (F5):** Dado cualquier paso en curso, cuando el operador recarga la página,
  entonces ve el POD sin controles abiertos y el paso activo correcto (E2).
- **AC-8 (incorrecto y force-advance):** Dado Assessment, cuando el operador falla un
  paso 1, 2 y 3 veces, entonces ve el hint y el modal se reabre; al 4º intento el
  sistema avanza forzado y habilita el lanzador siguiente (E3, E4).
- **AC-9 (Assembly):** Dada la guía de prueba de Redefinity, cuando el operador abre
  Assemble, escanea y hace clic en *Add* hasta llegar al conteo esperado, entonces el
  paso se completa y se habilita el siguiente.
- **AC-10 (completion):** Dada la última etapa, cuando se completa la sesión, entonces
  todos los controles de paso quedan ocultos y se ve el panel de completion (E5).
- **AC-11 (build):** El proyecto compila sin errores y los tests NUnit existentes pasan.

---

## Deferred Decisions

| Decisión | Fallback elegido | Disparador para retomarla |
|---|---|---|
| X del Component List (`closeAssemblyView`) y `BtnReturnPOD_Click` están vacíos | Se mantienen como están (paridad) | Primera guía real con Assembly, o Fase 8 |
| `ExpectedCount = 3` hardcodeado en `BuildStepViewModel`; el CLAUDE.md exige `ExpectedComponentCount` en BD por Area | Queda hardcodeado | Primera guía real con Assembly (antes de la Fase 7 si LoFlo lo requiere) |
| `RequiresAssembly` de LoFlo: BD=false, CLAUDE.md=true | Sin cambio | Fase 7, al cargar el catálogo real de LoFlo |
| Desarrollo del paso Complete | `CompleteControl` declarado e implementando `IStepControl`, sin UI; `BtnComplete` hace submit directo | Spec propia del paso Complete |

---

## Open Questions

_(ninguna)_
