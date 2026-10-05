# Plan de implementación — Spec 009

**Spec:** `docs/specs/009-motor-pod.md` (Approved 2026-10-05)
**Plan:** aprobado (2026-10-05), con las propuestas P-1 a P-4
**Arquitectura:** N-Tier (Pages → BLL → DAL → Domain) + stored procedures. Sin capas nuevas:
el motor del POD pasa de `GuidedTest.aspx.cs` a `BLL/Training`.

Seis entregas, en el orden aprobado en la spec. Cada una deja la app funcionando y se prueba
recorriendo Moldeo (NonSerialized, con checklist) y Redefinity (Serialized, Assembly) hasta
el paso que toca, en Guided y en Assessment. Cada entrega: implementar → MSBuild Rebuild +
`aspnet_compiler` + compilar `TrainingApp.Test` → `/code-review` del diff → checklist → el
usuario prueba → commit `refactor: ... (spec 009, entrega N)` directo en `TrainingApp`.

## Decisiones de diseño

- **Un servicio dueño del POD: `BLL/Training/PodService`.** Absorbe de
  `TrainingSessionService` los métodos que registran intentos (`SubmitStep`,
  `RegisterOutOfSequence`, `AssembleComponent`, `SubmitDataCollectionGroup`,
  `GetCurrentScore` y `RecordAttempt`). `TrainingSessionService` se queda con lo que no es
  del POD: `Start`, `Abandon` y, en la E6, `AbandonAllActive`.
- **Contexto cargado una vez por request: `PodContext`.** `PodService.LoadContext(sessionId,
  operatorId)` carga sesión, guía, área e intentos (4 consultas) y el estado del POD, y
  calcula el puntaje. Devuelve `null` si la sesión no existe, es de otro operador o no está
  en curso, y la página redirige a `StartSession` como hoy. `GuidedTest` lo carga en
  `Page_Load`, lo guarda en un campo y los handlers usan ese contexto.
- **Los comandos reciben el contexto.** No vuelven a leer la sesión: `spStepAttemptRecord`
  ya rechaza dentro de su transacción un intento sobre una sesión que cambió (estado, paso o
  número de intento). Después de registrar el intento, el servicio actualiza el contexto en
  memoria (paso actual, estado, puntaje final y puntaje actual), así que la página no
  recarga la sesión.
- **Puntaje:** el widget muestra `PodContext.Score` (calculado con los intentos del
  contexto) o el `CurrentScore` del intento recién registrado. Se elimina `GetCurrentScore`.
- **Estado del POD: `PodState`** (orden simulada + avance de Assembly + avance de DC), un
  solo objeto bajo **una sola clave de Session** (`GDT_Pod_{sessionId}`), en lugar de las 3
  claves actuales (propuesta P-2). Lo lee y escribe solo `PodService` a través de
  `IPodStateStore` (BLL). La implementación sobre `HttpSessionState`,
  `Pages/Training/SessionPodStateStore`, vive en presentación y se le pasa al servicio en
  el constructor. Así la BLL no depende de `System.Web`.
- **Regla de escritura (AUD-06):** el estado del POD se modifica **después** de que el
  intento quedó registrado. Session InProc guarda referencias: un objeto modificado antes
  de un `Record` que falla quedaría adelantado. Es la misma regla que ya siguen
  `AssembleComponent` y `SubmitDataCollectionGroup`.
- **La orden tiene su comportamiento.** `SimulatedOrder` y `SimulatedOrderRow` pasan a
  `BLL/Training`, con métodos que hoy están en la página: filas activas, filtrar una
  selección (ids del cliente → filas existentes y Active), iniciar piezas, split, scrap y
  `HasSplit`.
- **`PodDefaults` (D-11):**
  - Shop Order `303406950`, split de 100, lote de 250, Qty 1 por pieza;
  - Site y prefijo de SFC `CR9A`, recurso = área + `-01`, `LOT-yyyyMMdd`;
  - descripción de material `Producto / Área`;
  - la pregunta de Line Clearance (D-6: sigue en el código, ahora en la BLL).
  Mismos valores que hoy.
- **Avisos de formato como códigos.** Los comandos devuelven un `PodNotice` (ej.
  `SelectAllPieces`, `SelectOneRow`, `SfcRequired`, `SplitRequired`, `NoPieceLeft`). La
  página lo traduce a su SweetAlert con **los mismos textos de hoy**. Los textos son UI; la
  decisión de cuándo hay aviso es de la BLL.
- **Selección del cliente:** la página sigue leyendo los hidden fields (`HfSelectedRowIds`,
  `HfBrowseRowIds`, `TxtSFC`/`HfSelectedSFC`) y pasa los ids al comando. La BLL los filtra
  contra la orden, como hoy.
- **Restauración en un solo camino:** `PodService` aplica los efectos de un paso resuelto
  (Correct o FailedForced) en un único método. Lo usan el submit y el avance forzado por
  secuencia:
  - Resource Setup forzado → orden por defecto;
  - Start → piezas Active (todas si fue forzado);
  - Split forzado → split por defecto de 100;
  - Log NC por submit → scrap de las filas válidas;
  - Assembly/DC → se descarta el avance.

  Hoy los dos caminos difieren en un caso: el Start forzado por submit no inicia todas las
  piezas. Solo puede pasar en un área Serialized con checklist, que no existe, así que
  unificarlo no cambia nada visible.
- **`StepAnswers`** pasa a `BLL/Training` (el validador compara contra esos valores). Lo
  siguen usando `LogNCControl` y la página.
- **Lo que no cambia:** `IStepControl`/`CompleteControl` (fuera de alcance), el markup de
  `GuidedTest.aspx`, los textos, `StepValidator` y `ScoreCalculator`.

## Conexiones por click (AC-7)

Conteo estático por el camino del código: cada llamada a un repositorio abre una conexión.
El `/code-review` de la E1 lo revisa línea por línea. Si quieres verlo en ejecución, se
puede agregar un `Debug.WriteLine` temporal en `Database`, visible en la ventana Output de
VS y fuera del commit.

| Submit correcto de Line Clearance | Hoy | Después |
|---|---|---|
| Cargar la sesión, guía y área | 3 (Page_Load) + 3 (handler) + 2 (`SubmitStep`) | 3 + intentos 1 = **4** (una vez) |
| Puntaje del widget | 3 antes + 3 después | **0** (contexto / `CurrentScore`) |
| Validador (área, NC, parámetros DC) | 3 | **0** (el área está en el contexto; solo Log NC carga los códigos NC y DC sus parámetros) |
| Registrar el intento (intentos + `Record`) | 2 | **1** (los intentos ya están en el contexto) |
| Recargar la sesión después del submit | 2 | **0** |
| **Total** | **~21** | **5** |

## Entregas

### Entrega 1 — Contexto único, estado y puntaje (AC-7; base de AC-6) — verificada (2026-10-05)
- [x] Mover `SimulatedOrder`, `SimulatedOrderRow` y `StepAnswers` a `BLL/Training`, con
      `[Serializable]` y los métodos de la orden. Actualizar el `.csproj` y los `using`.
- [x] `PodDefaults` con los datos de simulación (D-11). `SimulatedOrder.Create` y la página
      los usan; se borran las constantes de `GuidedTest`.
- [x] `PodState`, `IPodStateStore` (BLL) y `SessionPodStateStore` (presentación). En
      `Constants.SessionKeys`, una clave `PodStatePrefix` reemplaza las 3 actuales.
- [x] `PodContext` + `PodService.LoadContext` (sesión, guía, área, intentos, estado y
      puntaje). `TrainingGuideService.BuildValidator(area)` usa el área del contexto.
- [x] Mover a `PodService` `SubmitStep`, `RegisterOutOfSequence`, `AssembleComponent`,
      `SubmitDataCollectionGroup` y `RecordAttempt`, recibiendo el contexto y actualizándolo
      en memoria. Se borra `GetCurrentScore`.
- [x] `GuidedTest`: el contexto se carga una vez en `Page_Load` y reemplaza a
      `LoadAndValidateSession` y `GetContext` en los handlers. El widget usa el puntaje del
      contexto o el del resultado. Se eliminan las recargas de sesión de
      `HandleCorrectSubmit` y `ContinueAfterStepResolved`. La página accede al estado vía
      `PodService`, todavía con su lógica actual (se mueve en E2-E4).
- [x] Adelantado de la E2 por el `/code-review` (la orden se creaba antes de registrar el
      intento): `PodService.SubmitResourceSetup` registra el intento y después crea la orden
      (la genérica si el paso avanzó forzado). `ResourceSetupControl` ya no escribe Session y
      envía la Shop Order en el evento. Reemplazar la orden descarta el avance de Assembly/DC.
- [x] `BuildValidator(area, tipoDePaso)` carga solo el catálogo del paso (Log NC: códigos NC;
      DC: parámetros): Line Clearance queda en 5 conexiones.
- [x] `SimulatedOrder.HasSplit` = hay una fila de split (`IsFromSplit`); mismo resultado que
      "más de una fila" en el paso SFC Split, que solo existe en NonSerialized.
- [x] `CLAUDE.md`: árbol de `BLL/Training` y convención "La BLL protege sus invariantes"
      (el dueño se valida al cargar el contexto; el SP revalida estado, paso e intento).
- Hallazgos del `/code-review` que van con su entrega: guardar el estado con
  `IPodStateStore.Save` después de cada cambio y que la página deje de modificar la orden
  y el avance (E2-E4); `AssembleComponent`/`SubmitDataCollectionGroup` leen el avance del
  `PodState` en vez de recibirlo (E3).
- [x] **Checklist del usuario:** iniciar sesiones nuevas (la clave de Session cambia).
      Moldeo completo en Guided y en Assessment con un fallo de 2 intentos (puntaje 80% de
      ese paso, mensajes iguales). Redefinity completo en Guided. El widget de puntaje se
      actualiza igual que antes en cada paso.

### Entrega 2 — Resource Setup, Start y SFC Split (AC-1, AC-3 y AC-4 en esos pasos) — verificada (2026-10-05)
- [x] (hecho en la E1) `ResourceSetupControl` ya no escribe Session: valida el formato (filas, Shop Order
      numérica) y envía la Shop Order en el evento. `PodService.SubmitResourceSetup` crea la
      orden (también con "Open", como hoy), registra el intento y, si fue forzado, la
      reemplaza por la orden por defecto. Se quitan del ViewModel de Resource Setup las
      claves `TrainingSessionId`, `MaterialDescription`, `InitialQty` y `SerializationType`.
- [x] Start:
      - `PodService.ClickStart(ctx, browseIds, sfcEscaneado)` decide, en este orden: (fuera
        de secuencia: sigue en la página hasta la E4) → orden ausente (aviso) → selección (aviso: todas las piezas o SFC
        requerido) → abrir el checklist si `RequiresStartChecklist` → Start directo.
      - `SubmitStartChecklist` para el checklist de Moldeo.
      - Los dos inician las piezas en la BLL. La página solo pinta `TxtSFC`, el header y el
        POD.
- [x] SFC Split:
      - `OpenSplit` (aviso si no hay una fila seleccionada);
      - `Split(ctx, rowId, qty)`: fila y cantidad válidas, SFC nuevo, sin intento;
      - `CloseSplit` (aviso `SplitRequired` si no hay split).
      `HasSuccessfulSplit` sale de la página.
- [x] Adelantado de la E4: `PodService.RestoreAfterForcedAdvance` aplica los efectos del avance
      forzado por secuencia (orden por defecto, piezas iniciadas, split por defecto, avance
      de Assembly/DC descartado); la página solo vuelve a pintar el POD.
- [x] `/code-review`: los comandos validan que corresponden al paso actual (excepción, no
      intento); `SubmitStartChecklist` exige área con checklist y orden; `Split` solo acepta
      filas Active; un solo mapa de textos para los avisos (`NoticeText`).
- **Para decidir aparte (D-12):** en NonSerialized el Start acepta cualquier SFC escrito en
      `TxtSFC` (no lo compara con el SFC del lote). Es así desde la spec 005; validarlo sería
      una regla nueva.
- [x] **Checklist del usuario:**
      - Moldeo: Resource Setup con "Open" (intento) y con "Set Up".
      - Start: sin SFC (aviso), checklist con un "No" (intento) y correcto.
      - Split: cantidad inválida, split válido (SFC nuevo) y Close sin split (aviso).
      - Redefinity: Start con selección parcial (aviso) y con todas las piezas.
      - Assessment: Resource Setup forzado (4 fallos) → orden por defecto `303406950`.

### Entrega 3 — Assembly y Data Collection (AC-4; AC-1 en DC) — verificada (2026-10-05)
- [x] `OpenAssembly` y `OpenDataCollection` (aviso de selección). `OpenDataCollection` fija
      las piezas en la primera apertura.
- [x] `AssembleComponent` y `SubmitDataCollectionGroup` toman el avance del `PodState`: la
      página deja de leer, crear y limpiar el avance. El ViewModel de Assembly y de DC se
      arma desde el contexto.
- [x] `/code-review`: el avance de Assembly/DC se descarta en un solo lugar, `RecordAttempt`,
      cuando el paso avanza (correcto, forzado o fin de sesión); un Save de DC sin avance
      (DC no abierto) no aplica, en vez de armar un avance con la selección del momento; un
      solo helper de avisos de selección (Assembly, DC y SFC Split) y una sola regla de "una
      fila" en la BLL. No aplican: cachear por request el BOM y los grupos DC que se leen
      varias veces en un Add/Save (rendimiento fuera de AC-7, anotado); que el banner
      "Next:" lea el avance del contexto (el contexto es el estado después del comando, por
      diseño); que un componente ya ensamblado devuelva otro resultado (cambiaría lo que ve
      el operador ante un doble click, D-12).
- [x] **Checklist del usuario:**
      - Redefinity: Assembly con un componente y Return to POD (el avance se conserva);
        completar el BOM; DC por pieza con grupos obligatorios (`SFC: … (n/N)`); un valor
        incorrecto (intento, el avance se conserva); F5 a mitad de DC.
      - Moldeo: DC con el grupo de fechas.

### Entrega 4 — Log NC, Complete, secuencia y restauración única (AC-2, AC-3, AC-5, AC-6) — verificada (2026-10-05)
- [x] Log NC:
      - `OpenLogNC`: fuera de secuencia → aviso `NoPieceLeft` (Serialized) → sin fila
        scrapeable = intento fallido con el feedback de hoy → abrir.
      - `SubmitLogNC(ctx, respuesta, selección)`: registra el intento y, en Correct o
        FailedForced, scrapea en la BLL. Devuelve las filas scrapeadas para el banner.
- [x] `Complete(ctx, selección)`: aviso por selección parcial (Serialized); intento fallido
      sin selección ("Select all rows to complete" / "Select 1 row to complete"); respuesta
      `complete`.
- [x] Secuencia: un único chequeo de secuencia al inicio de cada comando de botón (también
      Resource Setup, Activities y las acciones que no son de paso). La restauración tras
      avance forzado se hace en un solo método de la BLL; se borran
      `RestorePodAfterForcedAdvance`, `ReplaceWithDefaultOrder`, `ApplyDefaultSplit`,
      `ScrapSelectedRows` y `FindScrappableRows` de la página. Hecho: `PodService.CheckSequence`
      (privado) en cada comando de botón (`OpenResourceSetup`, `OpenLineClearance`,
      `ClickStart`, `OpenAssembly`, `OpenDataCollection`, `OpenSplit`, `OpenLogNC`,
      `Complete`, `ClickNonStepAction`); la restauración y `RegisterOutOfSequence` quedan
      privados. La página solo aplica el resultado (`HandleOutOfSequence`) y repinta el POD.
      Se borraron también `ProcessSubmit` y `ActiveRowCount`. El motivo de un intento
      fallido sin validador (Log NC sin fila scrapeable, Complete sin selección) llega como
      `PodAttemptReason` y la página elige el texto de siempre.
- [x] Revisión AC-6: `GuidedTest.aspx.cs` y los controles no leen ni escriben claves de
      Session del POD (salvo `OperatorId`/`Code1`) ni modifican la orden. Grep de
      `SessionKeys.PodState` y de `.Rows.` en `Pages/` y `UI/`. Resultado: la clave solo
      aparece en `SessionPodStateStore` (lo usa únicamente `PodService`); en `Pages/` y `UI/`
      nadie modifica la orden ni el avance; la Session de la página solo se lee para
      `OperatorId`/`Code1`. `GuidedTest.aspx.cs`: 1,429 → 1,265 líneas (lo que queda es UI:
      ViewModels de los controles, header, binding y modales); no llega a la meta orientativa
      de la mitad, que no era criterio de aceptación.
- [x] `/code-review`: Log NC evalúa la selección una sola vez (`ScrappableRows` con
      `noPieceLeft`); los textos de "todas las piezas" pasan al mapa de avisos; comentario de
      `StepAnswers.Invalid` actualizado. No aplican: que `CheckSequence` ignore el click con la
      sesión cerrada (es lo que hacía `RegisterOutOfSequence`); tests de `PodService` (Fase 5
      en pausa, decisión del usuario).
- **Bugs heredados que aparecieron (D-12, para decidir):**
      - **B-1:** un avance forzado por secuencia con la Component List o la Data Collection
        List abierta no cierra ese control: en el request siguiente queda abierto con el
        ViewModel del paso nuevo. Además ese camino no limpia la selección cuando el paso
        siguiente es SFC Split, como sí hace el submit normal. Arreglo propuesto: al forzar
        por secuencia, cerrar el control abierto y aplicar la misma regla de selección.
        **El usuario pidió arreglarlo (2026-10-05): va con la entrega 5.**
      - **B-2 (solo con manipulación del cliente):** Log NC vuelve a leer la selección al
        hacer Add-Done; cambiando el hidden field con el modal abierto se puede acertar el
        paso sin scrapear. Desde la UI no se puede (el POD queda detrás del control). Queda
        anotado.
- [x] **Checklist del usuario:**
      - Moldeo en Assessment: todo correcto = 100% y "Aprobado" (AC-2).
      - Un paso fallado 1, 2 y 4 veces y otro fuera de secuencia, con el mismo puntaje y los
        mismos mensajes que antes (AC-3).
      - Log NC sin la fila del split = intento (AC-5).
      - Split forzado → fila de 100 → Log NC y Complete.
      - Redefinity: Log NC con todas las piezas (aviso), scrap de una pieza y Complete
        parcial (aviso) y sin selección (intento).

### Entrega 5 — Reconstrucción del estado (AC-8, AC-9, AC-10)
- [ ] `LoadContext`: si el estado no tiene orden y Resource Setup ya se resolvió, rearma la
      orden por defecto y le aplica los efectos de los pasos resueltos, en el orden de la
      tabla de la spec (Domain Model → Reconstrucción). Assembly/DC a medias empiezan
      vacíos. `PodContext.Rebuilt = true`; sin mensaje ni intento.
- [ ] **Propuesta P-1:** si la reconstrucción ocurre en un **postback**, la página no
      ejecuta el comando del click: los ids seleccionados son de la orden perdida. Si se
      ejecutara, un Complete o un Log NC sin fila válida contaría como intento fallido por
      algo que el operador no hizo. En ese caso cierra el control abierto, pinta el POD con
      la orden reconstruida y no hace nada más. El operador repite el click. En un GET (F5 o
      volver a cargar) no aplica.
- [ ] Para probarlo en desarrollo sin reciclar IIS: botón temporal o querystring
      `?dropPodState=1` que borre la clave, **solo en `DEBUG`**, y se elimina antes del
      commit. Alternativa: reciclar IIS Express tocando `Web.config`.
- [ ] **Checklist del usuario:** AC-8 (Moldeo detenido en Log NC tras el split), AC-9
      (Redefinity a mitad de DC) y AC-10 (sesión en Resource Setup), cada uno en Assessment
      y comparando el puntaje antes y después.

### Entrega 6 — Sesión única (D-9) y cierre (AC-11, AC-12, AC-13)
- [ ] `03_spTrainingSession.sql`: `spTrainingSessionAbandonActiveByOperator` sin
      `@TrainingGuideId` (abandona todas las `InProgress` del operador).
      `TrainingSessionRepository.AbandonActiveByOperator(operatorId)`. **Lo corre el
      usuario antes de probar**: el SP viejo pide 2 parámetros.
- [ ] `TrainingSessionService.Start` abandona todas las sesiones del operador.
      `AbandonAllActive(operatorId)` se usa en `BtnLogout_ServerClick` antes de
      `Session.Abandon()`.
- [ ] Docs:
      - `CLAUDE.md`: árbol de arquitectura (BLL/Training: `PodService`, `PodContext`,
        `PodState`, `PodDefaults`, `SimulatedOrder`; Models sin la orden) y Key Conventions
        (estado del POD solo vía `PodService`).
      - `docs/roadmap.md`: AUD-16/25/26 ✅, deuda "Pérdida de la Session" resuelta y estado
        actual.
      - La spec 009 pasa a cerrada.
- [ ] **Checklist del usuario:** AC-11 (iniciar otra guía abandona la anterior), AC-12
      (logout y volver por el historial → `StartSession`), AC-13 (cerrar la ventana: sigue
      `InProgress` hasta iniciar).

## Propuestas (aprobadas por el usuario con el plan, 2026-10-05)

- **P-1 (E5):** tras una reconstrucción en un postback, el click no se ejecuta (ver E5).
- **P-2 (E1):** una sola clave de Session para todo el estado del POD. Así la orden y el
  avance se pierden o se conservan juntos, y nunca queda un avance que apunte a filas que
  ya no existen.
- **P-3 (E1):** la pregunta de Line Clearance pasa de la página a `PodDefaults`. Sigue en el
  código, así que D-6 se respeta.
- **P-4 (AC-7):** el conteo de conexiones se verifica por código (tabla de arriba) en el
  `/code-review`, no con instrumentación permanente.

## Riesgos

- **Regresión en un refactor grande.** Mitigación: las entregas siguen el flujo de la guía,
  cada checklist repite Moldeo y Redefinity, y D-12 prohíbe arreglar bugs "de paso" (se
  anotan y se deciden aparte).
- **Orden de eventos de Web Forms.** El contexto se carga en `Page_Load` de la página, antes
  de los eventos de los controles; los handlers no deben volver a cargarlo. Si un handler
  cambia el paso, la página usa el contexto actualizado en memoria.
- **Modificar la orden antes del intento** (referencias en Session InProc). Mitigación: la
  regla de escritura de arriba, revisada en cada `/code-review`.
- **Cambio de clave de Session (E1):** una sesión abierta antes del deploy pierde su estado
  y, hasta la E5, queda sin salida. En desarrollo basta con iniciar una sesión nueva.
- **Script `03` (E6):** si el código nuevo corre contra el SP viejo, el Start falla por
  cantidad de parámetros. El checklist de la E6 empieza por correr el script.
- **`TrainingApp.Test`** referencia `AssemblyProgress` y `DataCollectionProgress` (no se
  mueven) y `StepValidator`. Se compila en cada entrega; no se agregan tests (Fase 5 en
  pausa).
