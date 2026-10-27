# Plan 020: Custom Resource Setup (R3)

**Spec:** `020-custom-resource-setup.md` (aprobada 2026-10-26) · **Sesión:** `Custom-RS`
(híbrida: planea e implementa solo R3) · **Decisiones:** D-180 a D-194.
Cada entrega sigue `docs/process.md` → "2. Cada entrega": compilar (Rebuild), precompilar el
markup si se tocó `.aspx`/`.ascx`/`.master`, tests, `/code-review` con nivel y foco, el
"funciona" del usuario en el navegador **antes** del commit y commit solo por rutas
(`git commit -- <rutas>`).

## Decisiones del plan (usuario, 2026-10-26)

| ID | Decisión |
|---|---|
| D-195 | Página Guías: un selector Estándar / Custom y un Guardar **por fila**, con aviso de confirmación |
| D-196 | Un Save de la pantalla que no corresponde al tipo de la sesión (Standard ↔ Custom) es **excepción, sin intento**: invariante violado, como la Shop Order no numérica en el servidor |
| D-197 | `docs/status.md` y `docs/history.md` los escribe **Desarrollo**: al cerrar cada entrega de R3 se le manda el resumen por mensaje |
| D-198 | (E1) La versión nueva de una guía que crea `spTrainingGuideSync` al cambiar su forma (D-56) **hereda** el `ResourceSetupType` de la que reemplaza |
| D-199 | (E1) La página Guías lista **solo las guías publicadas** (`spTrainingGuideGetPublishedForAdmin`) |

## Reglas de este plan

- Rama `TrainingApp`, en la carpeta real. No se toca el worktree `fase-11` (Desarrollo).
- La BD del usuario es de pruebas: los cambios de esquema van en `00_Schema.sql` y los SP en
  `02`/`03`; se recrea la BD o se corre el `ALTER` que se entrega aparte (`process.md` → 6).
- El script `13` reservado no se usa: el piloto se configura con un script de `Database/Ops/`.
- Sin ORM, sin SQL inline; la lógica en la BLL; el code-behind solo UI.
- `GuidedTest.aspx(.cs)`: cambios mínimos (un control estático más, alternando `Visible`).

## E1 · Datos y dominio ✅ (`144d845`)

- [x] `Domain/ResourceSetupType.cs`: enum `Standard = 0`, `Custom = 1`.
- [x] `00_Schema.sql`: `TrainingGuides.ResourceSetupType TINYINT NOT NULL DEFAULT 0` y
      `TrainingSessions.ResourceSetupType TINYINT NOT NULL DEFAULT 0` (en el `CREATE` y un
      `IF COL_LENGTH(...) IS NULL ALTER TABLE ... ADD` para la BD existente). Comentario de
      los valores como en `Mode` y `Status`.
- [x] `02_spTrainingGuide.sql`: los SP de lectura devuelven `ResourceSetupType`; SP nuevo
      `spTrainingGuideSetResourceSetupType (@Id, @ResourceSetupType)`. `spTrainingGuideSync`
      **no** toca la columna.
- [x] `03_spTrainingSession.sql`: `spTrainingSessionInsert` guarda el tipo y
      `spTrainingSessionGetById` lo devuelve.
- [x] `Domain/TrainingGuide.cs` y `Domain/TrainingSession.cs`: propiedad `ResourceSetupType`.
- [x] `DAL/Repositories/TrainingGuideRepository.cs` y `TrainingSessionRepository.cs`: mapean la
      columna; el repositorio de guías agrega `SetResourceSetupType`.
- [x] `Database/Ops/SetGuideResourceSetupType.sql`: pone el tipo de una guía por título o Id, con
      el SELECT previo y posterior. Es el camino del piloto hasta la E4 (D-194: todas Standard).
- [x] Verificación: scripts en una BD descartable de LocalDB (instalación limpia y re-ejecución
      con sesiones e intentos); las sesiones existentes quedan en Standard.
- [x] `/code-review low` sobre `00`, `02`, `03` y el DAL: valores por defecto, `ALTER` idempotente.

**Hecho cuando:** compila, los scripts corren dos veces sin error y una guía/sesión nueva trae
`Standard`.

## E2 · BLL, calificación y tests ✅ (`a0c7c77`)

- [x] `BLL/Training/CustomResourceSetupProvider.cs`: el único punto de acceso a los datos de
      Custom RS (D-184). `For(PodContext ctx)` devuelve `Site`, `Resource`
      (`ctx.ResourceName`), `Operation` (`ctx.CurrentOperationName`) y los **Tool Groups**: pool
      de 8 (código y descripción de la captura), de 2 a 4 sorteados con `new Random(session.Id)`,
      en orden alfabético por código. Sin tablas todavía; la costura hacia las Operations es el
      cambio de este archivo.
- [x] `TrainingSessionService.Start` copia `guide.ResourceSetupType` a la sesión (D-188).
- [x] `PodContext`: `ResourceSetupType` (de la sesión) y `CustomResourceSetup` (datos del
      proveedor, perezoso).
- [x] `PodService.OpenResourceSetup`: con Custom **no** genera equipos precargados (D-186) y
      respeta la secuencia como hoy.
- [x] `StepValidator.ValidateResourceSetup`: el JSON de Custom es
      `{"setupState","shopOrderAssociated","shopOrder","tools":[{"group","value"}]}`. Correcto
      solo con `setupState = "Set Up"` y `shopOrderAssociated = true` (D-183, D-191). Los Tool
      Numbers no se califican.
- [x] `PodService.SubmitResourceSetup`: la orden se crea con la Shop Order de Custom (también
      en Open, D-193); sin asociar, intento fallido y sin orden (D-191); el avance forzado usa
      la orden por defecto. Valida en la BLL el formato que el cliente debería haber avisado:
      Shop Order numérica y al menos un Tool Number (violarlo es excepción, no intento).
- [x] `AccessPolicy.CanManageGuides` (solo Administrator) y su pareja en `SessionHelper`.
- [x] `BLL/Admin/GuideAdminService.cs`: lista guías con producto, área, título, publicada y tipo;
      `SetResourceSetupType(adminUserId, guideId, type)` verifica `CanManageGuides` en la BLL.
- [x] Respuesta de un tipo que no es el de la sesión: `ArgumentException`, sin intento (D-196).
- [x] Tests (NUnit, en memoria): proveedor (cantidad 2 a 4, estable por sesión, orden, sin
      repetidos), validador de Custom (Set Up, Open, sin asociar, sin herramientas), sesión que
      copia el tipo, `AccessPolicy`, `GuideAdminService` (rol, guía inexistente).
- [x] `ScoreCalculatorTests` y el resto siguen en verde (AC-12).
- [x] `/code-review medium` sobre `BLL/Training` y `BLL/Scoring`: invariantes del servidor,
      respuesta manipulada, sesión Standard que recibe una respuesta Custom (y al revés).

> **E2, resultado del `/code-review medium` (2026-10-26):** se aplicaron: `SubmitStep` público
> rechaza Resource Setup (solo entra por `SubmitResourceSetup`); `spTrainingGuideSetResourceSetupType`
> solo cambia guías publicadas; `shopOrderInput` es solo de la estándar; un solo parseo en
> `TryParse`; `ShopOrderToCreate` como regla pura con tests. **No se aplican** (usuario): validar en el
> servidor el grupo y los `<`/`>` de los Tool Numbers (no se califican ni se guardan), y **B-20**
> (`StartSession` no revisa que la guía esté publicada; ya existía) queda anotado.

**Hecho cuando:** tests verdes y el servidor rechaza o califica correctamente cualquier
respuesta, sin que exista todavía la pantalla.

## E3 · El modal, la guía de Guided y el cableado ✅ (`3a18960`, `82d000d`; enmienda 1 de la spec: D-200, D-201)

- [x] `UI/Controls/Training/CustomResourceSetupControl.ascx(.cs)`: modal Bootstrap con el
      diseño de las capturas (panel celeste, campos con etiqueta, botones Retrieve / Associate /
      Save / Clear con ícono, Code1 arriba a la derecha) y las tres ventanas Information,
      Confirmation y Success (D-190). Implementa `IStepControl`, estático.
- [x] Cliente (un postback, D-189): Retrieve habilita Setup State y Associate; Associate valida
      la Shop Order numérica (aviso de formato, sin intento, D-191), muestra el Information y
      carga la tabla de Tool Groups; Save arma el JSON, valida `<`/`>` y que haya un Tool Number
      si la orden está asociada, y abre la Confirmation; Yes envía el postback; No no hace nada;
      Clear restaura el estado inicial (D-192). Volver a asociar conserva los Tool Numbers. El
      ícono ⧉ es decorativo (D-182).
- [x] CSS propio del modal en un archivo nuevo (`Content/custom-rs.css` o junto a `pod.css`,
      según el patrón del POD), con clases estándar de Bootstrap 5.3.8. Sin tocar `admin.css`.
- [x] `GuidedTest.aspx`: `CtrlCustomResourceSetup` estático con `Visible`;
      `GuidedTest.aspx.cs`: `StepControlFor`, `OpenStepControl`, `ReopenModal`, el nombre del
      modal y el `ViewModel` (datos del proveedor) eligen el control según
      `_ctx.ResourceSetupType`; el éxito abre el Success en vez de "Record changed" (D-190).
- [x] Guía visual (spec 010): anclas `data-guide="crs-*"` (nunca ids) y su guion en
      `GuideScriptBuilder` para la secuencia completa (Retrieve, Set Up, Shop Order, Associate,
      Tool Number, Save, Yes). Assessment y Práctica no reciben el guion. `StepHints` si aplica.
- [x] Probar: guía Standard sin cambios (AC-2); guía Custom en Guided, Assessment y Práctica
      (AC-1, 3 a 6, 10); botón fuera de secuencia, avance forzado y reconstrucción del POD
      (AC-11); recarga de página en medio del modal (los mismos Tool Groups, AC-7); modo oscuro y
      claro (Fase 11).
- [x] Precompilar el markup (`aspnet_compiler`) y `/code-review medium` sobre el control, el
      cliente y el cableado de `GuidedTest`.

**Hecho cuando:** el usuario recorre la secuencia completa en Guided y en Assessment con una
guía puesta en Custom con el script de Ops, y dice "funciona".

## E4 · Página de Guías en Admin ✅ (sin cambios en `admin.css`: alcanzaron las clases de la Fase 11)

- [x] `Pages/Admin/Guides.aspx(.cs)` con el sistema de diseño de la Fase 11 (tokens de
      `admin.css`, Bootstrap Icons): tabla de guías (producto, área, título, publicada, tipo de
      Resource Setup) con un selector Estándar / Custom y Guardar por fila (D-195); confirma con
      un aviso; solo `CanManageGuides`, sin la capacidad al inicio del rol (nunca al login).
- [x] `Admin.Master`: una línea de menú "Guías" visible con la capacidad; `Web.config` ya protege
      `Pages/Admin` por rol.
- [x] Probar AC-8 (cambio con sesión en curso: no cambia; sesión nueva: sí) y AC-9 (Operator).
- [x] Antes de empezar, avisar a Desarrollo: si tiene una entrega abierta que toque `admin.css` o
      `Admin.Master` (las E3 a E5 de la Fase 11 agregan clases), se espera su commit. Las clases
      propias van en un bloque al final de `admin.css` con el comentario `Guías (spec 020)`.
- [x] Coordinar `TrainingApp.csproj` (y `admin.css` si hace falta) con Desarrollo: commit por
      rutas, y el segundo en llegar hace el merge.
- [x] `/code-review low` sobre la página y el servicio.

**Hecho cuando:** el administrador cambia el tipo desde la página y el usuario lo comprueba.

## E5 · Cierre y documentación

- [ ] `docs/domain.md` → Resource Setup: el tipo Custom, la secuencia y qué se califica.
- [ ] `docs/architecture.md`: árbol (proveedor, control, página de Guías) y el mecanismo del
      tipo por sesión.
- [ ] `CLAUDE.md` → "Roles" (`CanManageGuides`) y "Guía visual" si el módulo de guion cambia el
      contrato.
- [ ] `docs/status.md` y `docs/history.md`: los escribe Desarrollo (D-197). Al cerrar **cada**
      entrega se le manda el resumen por mensaje (commit, qué cambió, scripts que el usuario
      corrió en su BD).
- [ ] `docs/ui-reference/inventario-pantallas.md`: la pantalla nueva con sus 5 capturas.
- [ ] `docs/decisions.md`: estado de D-180 a D-194; `docs/roadmap.md`: R3 ✅.
- [ ] `/code-review` final de todo R3 (`medium`) y la spec pasa a "Implemented" con los commits.

## Resumen de riesgos

| Riesgo | Mitigación |
|---|---|
| Choque con la Fase 11 en `TrainingApp.csproj` / `admin.css` | Commits por rutas, merge del segundo en llegar, página de Guías en la última entrega |
| `GuidedTest` crece más | Un solo control más; la elección del control en un método |
| Respuesta manipulada de otro tipo de Resource Setup | El validador revisa la forma contra el tipo de la sesión; test en la E2 |
| El día que haya Operations reales | Solo cambia `CustomResourceSetupProvider` |
