# Plan — Spec 015: Ajustes de la trainer

**Spec:** `015-ajustes-trainer.md` (aprobada 2026-10-14) · **Estado:** **aprobado por el
usuario (2026-10-14)**, con las decisiones D-21 a D-26.
**Autoría:** borrador de la sesión de desarrollo (verificado contra el código), completado y
cerrado por la sesión de diseño (`docs/process.md` → "Dos sesiones"). Lo ejecuta desarrollo.

Tres entregas en el orden del flujo (Resource Setup → Data Collection → Logout). Cada una
con el ciclo de `docs/process.md` §2: rebuild, `aspnet_compiler`, tests, review calibrado,
checklist en el navegador, "funciona" y commit con el roadmap.

## Lo que se verificó en el código antes de planear

- **Cierre de la sesión:** `PodService.RecordAttempt` cierra la sesión cuando resuelve el
  **último** paso de la guía (`step.Order + 1 > Steps.Count`). Con Logout al final, Complete
  deja de cerrar sin tocar esa regla: pasa a `Correct` y avanza a Logout.
- **Avance forzado de Logout:** misma regla: forzar el último paso cierra la sesión con su
  resumen (edge case de la spec, sin código extra).
- **`spTrainingGuideSync`** ya **agrega** los pasos que faltan al final de una guía existente
  y solo aborta si un paso existente cambia de `StepType`. Agregar Logout al final es válido
  sobre la BD del usuario. Hay que subir su chequeo `StepType > 7` a `> 8`.
- **Sesiones en curso al desplegar:** la guía se lee en cada request (`LoadContext`), así que
  una sesión en curso ve el paso nuevo: al resolver Complete avanza a Logout y termina ahí.
  Las sesiones completadas guardan su `FinalScore` y nada lo recalcula. Sin riesgo extra.
- **Retrieve de Resource Setup** es solo de cliente (`rsRetrieve`): los precargados se
  generan en el servidor **al abrir el control** (`OpenResourceSetup`) y viajan con él. Se
  respeta "un postback por paso" y reabrir el control los regenera (edge case de la spec).
- **Validación de formato de Resource Setup en el cliente:** "exactamente 1 Shop Order". Si el
  operador deja la Shop Order precargada y agrega la suya, hoy el control lo bloquearía como
  formato (sin intento), y eso contradice AC-2 → D-26.
- **Logout del header** hoy cierra la sesión de la app en `Page_Load`, antes de cargar el
  contexto (AUD-33), y tiene `data-guide-free`. `StartSession.aspx` tiene su propio Logout:
  para salir de la app a mitad de una sesión queda Abandon → Logout de StartSession.
- **Grupos opcionales a reemplazar** (BD del usuario): Redefinity
  `DCGTM_CONDICIONES_AMBIENTE/A` (1 parámetro) y LoFlo `DCGTM_SIM_CONDICIONES_AMBIENTE/A`
  (2 parámetros). Ninguna tabla referencia grupos ni parámetros de DC (los intentos guardan
  solo el paso).
- **"En DC un campo vacío es intento fallido"** vive en `StepValidator.ValidateParamValue`:
  `PersonName` vacío es la excepción.
- **Complete** no tiene mensaje documentado en `general-flow.md` (Paso 9) ni captura.

## Decisiones (usuario, 2026-10-14)

- **D-21 · Logout con la sesión ya cerrada** (resumen en pantalla): el ícono cierra la sesión
  de la app como hoy. Durante la sesión de entrenamiento, nunca.
- **D-22 · El POD después de Complete:** las filas completadas salen del POD (la Work List
  queda vacía, como el SFC que pasa a la siguiente operación) y aparece un **banner genérico**
  con el formato de los demás banners del POD: `SFC {sfc} completed` (NonSerialized) o
  `{N} SFCs completed` (Serialized). Texto **provisional** hasta tener la captura real
  (`docs/process.md` §8); se anota como pendiente en `general-flow.md` → Paso 9.
- **D-23 · Reemplazar el tercer grupo opcional** en `05` y `08`: renombrar en sitio el grupo
  viejo a `DCGTM_TRAINEE/A` (conserva su Id, como S&RC en T7), convertir su primer parámetro
  en el de nombre y **borrar** los parámetros sobrantes de ese grupo (el segundo de LoFlo).
  Excepción a "los scripts nunca borran filas" (nada referencia grupos ni parámetros de DC);
  se anota en `docs/process.md` §6. El usuario aclara que hoy la BD es local de pruebas y su
  integridad de datos no es una restricción.
- **D-24 · Nota del grupo Trainer en Guided:** un subpaso Info sobre la fila del grupo Trainer
  en la Data Collection List, con la nota "Solo cuando se está entrenando" y "Continuar",
  **una sola vez** (al abrir DC con la primera pieza), antes del Collect del primer grupo
  obligatorio.
- **D-25 · AC-5 en Guided:** los subpasos de captura de Shop Order y herramienta rechazan los
  valores precargados ("Continuar" deshabilitado, como un formato), con la lista que manda el
  servidor **solo en Guided** (Assessment no la recibe).
- **D-26 · Precargado antes que formato:** si la tabla trae **algún** valor precargado, el
  control **no** aplica sus avisos de formato (por ejemplo, "exactamente 1 Shop Order") y
  envía el Save al servidor, que lo califica como `WrongDecision` (AC-2). Mismo criterio que
  S2 (la secuencia antes que el formato): el error que se entrena manda sobre el formato y el
  aviso no le revela el problema en Assessment. Sin precargados, los avisos de formato siguen
  igual. Los precargados ya están en el cliente (campo oculto de la Entrega 1), así que no
  hace falta otra llamada.

## Entrega 1 — Resource Setup con equipos precargados (A, AC-1 a AC-5) ✅ (2026-10-14)

- [x] `BLL/Training/PreloadedEquipment.cs` (`[Serializable]`: `ShopOrder`, `ToolNumbers`) y
      `PreloadedEquipmentGenerator` (como `SerialSfcGenerator`): Shop Order de 9 dígitos y 2
      herramientas de 8, aleatorios y distintos de `PodDefaults.DefaultShopOrder` y de los
      ejemplos de la guía (`10033058`, `10087734`).
- [x] `PodState.PreloadedEquipment`: lo fija `PodService.OpenResourceSetup` cuando la
      secuencia es correcta (cada apertura lo regenera); se descarta al resolver el paso.
- [x] `StepValidationContext.PreloadedValues` y `StepValidator.ValidateResourceSetup`:
      incorrecto si alguna fila trae un valor precargado (comparación por valor, sin
      espacios), además de Setup State. Motivo `WrongDecision`, mensaje genérico. El avance
      forzado no cambia.
- [x] `GuidedTest` pasa los precargados en el ViewModel; `ResourceSetupControl` los guarda en
      un campo oculto y los pinta en la tabla **al abrir el control** (Shop Order + 2 Tool
      Number); Retrieve solo habilita la edición (enmienda 2026-10-14 de la spec; antes:
      "rsRetrieve los agrega en el primer Retrieve"). Tras un postback se restauran desde
      `HfRows`, como hoy.
- [x] Save con un precargado y sin una Shop Order válida y propia: el intento fallido se
      registra pero no se crea la orden simulada (decisión del `/code-review` de E1).
- [x] D-26: la validación de formato del control se salta si alguna fila trae un valor
      precargado; el Save se envía igual.
- [x] Guía (D-25): `Reject` en los subpasos `shopOrder` y `tool`; `pod-guide.js` lo revisa
      como formato. La alerta de T7 en REMOVE ALL no cambia.
- [x] `GetHint` de Resource Setup: mencionar quitar los equipos anteriores (Guided).

## Entrega 2 — Grupo de DC Trainer (C, AC-14 a AC-18)

- [ ] `BLL/Scoring/PersonNameRule.cs`: letras Unicode (`\p{L}`) y espacios, de 2 a 60
      caracteres tras recortar; vacío es válido. Una sola regla para la BLL y el control.
- [ ] `StepValidator.ValidateParamValue`: `DataType = "PersonName"` → vacío válido o nombre
      válido. `ValidateDataCollectionGroup` deja de tratar como fallido el vacío de este tipo
      (la regla "un campo vacío es intento fallido" sigue para los demás tipos).
- [ ] `PodService.SubmitDataCollectionGroup`: un nombre inválido que llega al servidor es una
      manipulación (el control ya lo bloquea): sin intento (`Ignored`), como AUD-36.
- [ ] `DataCollectionControl`: input de texto para `PersonName` (`maxlength=60`); validación de
      formato en el cliente con el mismo patrón (aviso, sin postback) y en el servidor antes
      de enviar.
- [ ] SQL (D-23): `05` (Redefinity) y `08` (LoFlo) renombran en sitio el tercer grupo a
      `DCGTM_TRAINEE/A` "Entrenamiento personal", `Anytime`, `IsRequired = 0`, con el
      parámetro `NOMBRE_EN_ENTRENAMIENTO` (marca `SIM` en la descripción: el nombre real no se
      conoce) y borran los parámetros sobrantes del grupo; `06` (Moldeo) lo **agrega** al
      final. `00`: comentario de `DataType` con `PersonName`. Probado en LocalDB (limpia,
      re-ejecución, comparación).
- [ ] Guía (D-24): subpaso Info sobre la fila del grupo Trainer con la nota.

## Entrega 3 — Logout como último paso (B, AC-6 a AC-13)

- [ ] `StepType.Logout = 8`; `StepAnswers.Logout = "logout"`; `StepValidator`: correcto solo con
      `"logout"`; `AttemptActions.Logout = "POD.Logout"` (+ `ForSubmit`).
- [ ] `PodService.ClickLogout(ctx)`: `CheckSequence(StepType.Logout, …)` (Assessment fuera de
      turno = intento fallido del paso actual; Guided se ignora) y, en el paso Logout,
      `SubmitStep(ctx, StepAnswers.Logout)`, que cierra la sesión por ser el último paso.
- [ ] `GuidedTest`: el ícono Logout pasa por `Pod.ClickLogout` y `ApplySubmitResult` (resumen
      final con `SessionCompleted`); `Page_Load` solo llama `SessionHelper.Logout()` sin
      sesión en curso (D-21). `SetActiveButton`: en Guided el ícono solo se habilita en el paso
      Logout; en Assessment siempre. Ancla `data-guide="pod-logout"` en lugar de
      `data-guide-free`. Complete resuelto (D-22): las filas salen del POD y aparece el banner
      provisional `SFC {sfc} completed` / `{N} SFCs completed`.
- [ ] Guía: guion de Logout (subpaso 1 sobre el ícono, con la nota literal del punto 9 de la
      spec) y `GetHint` de Logout. El globo final sigue apareciendo al cerrar la sesión.
- [ ] SQL: `02` (`spTrainingGuideSync`: `StepType > 8`), `05`, `07` y `09` con Logout después
      de Complete y Complete 0.05 (suman 1.00); comentario de `StepType` en `00`, `07` y `09`.
      Probado en LocalDB, también sobre una BD con una sesión en curso.
- [ ] `ScoreCalculatorTests` siguen en verde (AC-13). Tests nuevos de la BLL (validador de
      Resource Setup y de Logout, `PersonNameRule`) solo si el usuario los pide
      (`docs/process.md` §5).

## Al cerrar la spec

- [ ] `CLAUDE.md`: flujo estándar (paso 10 Logout; Complete ya no cierra), "Qué cuenta como
      intento" (Logout deja de ser navegación; Abandon sigue; precargado antes que formato,
      D-26), tabla de pesos, Resource Setup (precargados) y catálogos (`DCGTM_TRAINEE/A`,
      `PersonName`).
- [ ] `docs/architecture.md` (precargados en `PodState`, Logout simulado) y
      `docs/ui-reference/general-flow.md` (pasos Resource Setup, Complete y Logout).
- [ ] Roadmap: fila 5 de la Fase 8.5, "Estado actual", "Historial" y "BD de desarrollo".

## Review

- E1 y E2: `medium`, foco en `StepValidator`, `PodService` y la regla de formato vs. decisión.
- E3: `high` (cambia el cierre de la sesión, el Logout de la app y `Page_Load`).
