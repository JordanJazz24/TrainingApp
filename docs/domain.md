# Reglas de negocio del dominio — MESTrainingSimulator

> Movido desde `CLAUDE.md` el 2026-10-14 para que CLAUDE.md, que se carga en cada turno, quede
> con las reglas generales y los punteros. Las referencias `CLAUDE.md → "Jerarquía"`,
> "Ejes de variación", "Flujo estándar", "Modos de sesión", "Qué cuenta como intento",
> "Ponderación de pasos" o "Catálogos en BD" de specs, auditorías y el roadmap (registro
> histórico) apuntan a la sección del mismo nombre de este archivo.

Cerradas con negocio. Son la fuente de verdad para `Domain/`, `BLL/Scoring/` y los stored
procedures de catálogo. No re-derivarlas del legacy ni asumir variantes sin confirmar.

## Jerarquía

```
Product (línea de manufactura)
  └─ Area (sublínea de ensamblaje)
       └─ [FUTURO] Operation: hoy las guías son generales por Area. Cada Area tendrá de 3 a
          10 Operations; el esquema ya lo prevé (FK nullable) aunque no se use.
```

Desde la spec 014 (D-58), `Operations` tiene 2 filas simuladas (`SIM`) por cada área con
reparación extendida, solo para el banner de la disposición (ver "Catálogos en BD"). Las
guías siguen siendo por Area.

**Products y Areas oficiales** (confirmado con negocio 2026-09-25):

| Product | Areas |
|---|---|
| **Wires** | Subensamble 1, Subensamble 2, Composite Core, Línea Final, Testing, Backend |
| **Catéteres** | Redefinity, EEP, Ice, PV 35, PV 18, Otw |
| **Sensores** | LoFlo, Capnotrack, Mainstream, Source, Reusables, Detector |
| **S&RC** | Moldeo |

Un área se carga en BD cuando se migra su catálogo (sus flags son `NOT NULL` y solo se
conocen al validarla con negocio). Áreas en BD y sus flags:

| Área | Origen | Flags |
|---|---|---|
| **Moldeo** (S&RC) | catálogo real (`06_RCMoldeoCatalog.sql`, guía en `07`) | NonSerialized · Assembly no · DC sí · checklist de Start sí |
| **LoFlo** (Sensores) | `08_SensoresLoFloCatalog.sql`, guía en `09` (Fase 7) | Confirmado 2026-10-07 para **todas las áreas de Sensores**: Serialized · Assembly sí · DC sí · Component en NC: lo decide el método del código (spec 014) · checklist de Start no. DC Groups, parámetros, códigos NC y BOM: simulados por ahora |
| **Redefinity** (Catéteres) | **datos de ejemplo** en `05`; se ofrece a los operadores como cualquier área con guía publicada (D-178) | Serialized · Assembly sí · DC sí (no son decisiones de negocio) |
| **Subensamble 1** (Wires) | `11_WiresSubensamble1Catalog.sql`, guía en `12` (2026-10-26, MVP para los trainers) | Serialized (usuario, 2026-10-26: el flujo es general y solo se divide en Serialized y NonSerialized; Subensamble 1 entrena el Serialized) · Assembly sí · DC sí · checklist de Start no, con la forma de LoFlo. DC Groups, parámetros, códigos NC de desecho y BOM: simulados |

**Particularidades de Moldeo:**
- El ID del molde (`DCGTM_ID_MOLDE/A`) es un **DataCollectionParameter**, no Assembly
  (`RequiresAssembly = false`, confirmado con negocio).
- Las 3 preguntas de LC Moldeo (`DCGTM_LC_MOLDEO/A`, dentro del Start) **no** son
  DataCollectionParameters: son parte del checklist de Start (4 modales en `SearchControl`)
  y las califica `StepValidator` en el paso Search.

## Ejes de variación (independientes: ninguno se deriva de otro)

- **SerializationType** — la diferencia aparece **solo en Search/Start y en Cierre/Scrap**.
  Resource Setup es idéntico en ambos tipos.
  - `Serialized`: la Shop Order genera varios SFC individuales con identidad y trazabilidad
    propias (ej. 989609001101-01, -02, -03). Se escanean uno a uno o se eligen varios en
    Browse Work List. Scrap: selección individual.
  - `NonSerialized`: un único SFC con cantidad (ej. SFC 50012345, Qty 500); no existen SFCs
    tipo 50012345-001. MES controla la cantidad total. Scrap: split de la cantidad del lote.
- **RequiresAssembly** y **RequiresDataCollection** (`bool`, hoy por Area; se moverán a
  Operation cuando exista): independientes de SerializationType. Una operación serializada
  puede no requerir DC (confirmado con negocio). Nunca asumir "serializado ⇒ Assembly".
- El login de la app (neutro, spec 016, D-62) **no** se evalúa: es solo la cuenta. El login
  de MES simulado (diseño SAP NetWeaver) es la última pantalla del paso Ingreso a MES y sí se
  califica.

## Flujo estándar de una TrainingGuide

Los 3 primeros pasos de Apertura y los de Cierre son iguales en todas las Areas; solo cambia
el catálogo (códigos NC, parámetros de DC), nunca la secuencia de tipos de paso.

```
Antes de SAP (spec 016)
  0. Ingreso a MES    Primer paso de todas las guías (`MesLogin`), en pantalla completa: el
                      escritorio de Windows (solo el script y el Repositorio), el script de
                      limpieza con el Code1 (9 dígitos), el Repositorio en Edge, Recursos
                      MES (estación con etiqueta simulada, Code1, Buscar) y el login de MES
                      (Code1 y contraseña, Log On). Hora a Hora no es parte del Ingreso
                      (D-151): sus tarjetas se ven y no hacen nada. Se califican la
                      SECUENCIA y el CODE1 PROPIO; no la estación ni la contraseña (solo
                      formato). Cada pantalla se califica al enviarla y un fallo vuelve a esa pantalla (D-68). Se hace al inicio del
                      turno y al cambiar de estación.
Apertura
  1. Resource Setup   Asocia el recurso (estación) a la Shop Order y las herramientas
                      (ej. R_SCN_MASK_1, Shop Order 303406950, Tools 10033058 y 10087734).
                      No define qué piezas se procesan (eso es Search/Start). Captura libre,
                      sin validar contra catálogo. Se califica por SECUENCIA y por
                      Setup State = "Set Up" (guardar en "Open" es intento fallido); los
                      recursos capturados no se califican, salvo los precargados.
                      · Setup Type (spec 017, D-94, D-99): cada fila de herramienta puede ser
                        Tool Number o Material, los dos valen en todos los modos; la fila de
                        la orden tiene que ser Shop Order, como formato (D-107). La guía
                        enseña Tool Number.
                      · Equipos precargados (spec 015): el control abre con una Shop Order
                        y 2 herramientas "del turno anterior", que genera la BLL en cada
                        apertura. Guardar con alguno en la tabla (por valor, también
                        reescrito a mano) es decisión equivocada; vale cualquier forma de
                        quitarlos (Remove All o Remove Selected).
                      · Dos pantallas (spec 020, D-180): cada guía usa la estándar (la de
                        arriba, predeterminada) o **Custom Resource Setup**, el piloto de la
                        planta; la elige el administrador por guía y la sesión copia la de
                        la guía al empezar (D-188). Mismo paso, mismo peso. En Custom: Retrieve
                        → Set Up → ShopOrder → Associate New ShopOrder (OK) → un Tool Number
                        por cada uno de los 2 Tool Groups (D-200) → Save → Yes (D-185). Se
                        califica SECUENCIA, Setup State = "Set Up" y la orden ASOCIADA (Save
                        sin asociar es intento fallido; con "Open" falla y crea igual la
                        orden, D-193). Formato sin intento: ShopOrder no numérica al asociar
                        y, con la orden asociada, una fila sin Tool Number (D-191, D-201). Los
                        Tool Numbers son de captura libre y no se califican (D-182). Sin
                        equipos precargados (D-186). Resource y Operation son los simulados;
                        los Tool Groups salen de un pool simulado de 8 (D-184).
  2. Line Clearance   Pregunta sí/no.
  3. Search/Start     Se califica por SECUENCIA. Aquí aparece la diferencia por
                      SerializationType:
                      · Serialized: 4-5 piezas por orden en el simulador (spec 014: alcanzan
                        para los tres métodos de Log NC y Complete); Start, Assembly,
                        DC y Complete trabajan todas las piezas juntas (selección parcial =
                        aviso sin penalización); TxtSFC muestra "[N SELECTED]".
                      · NonSerialized: se selecciona el único SFC una vez; el POD lo carga
                        con su cantidad.
                      · Con checklist de Start (RequiresStartChecklist, hoy solo Moldeo):
                        Start abre 4 modales y se califican sus respuestas.
  4. Assembly         Si RequiresAssembly. Component List = BOM de la operación
                      (AssemblyComponents, simulado). Un escaneo numérico por componente
                      cubre toda su Qty y se aplica a todas las piezas ("Apply to all
                      SFCs"). Termina con todo el BOM ensamblado. No valida el barcode
                      contra el número de parte: se califica solo por secuencia.
                      Batch Number = Process Lot de la orden (la Assembly Data por tipo de
                      material, W13, quedó diferida: enmienda 6 de la spec 014). Lo
                      ensamblado se conserva para Log NC (Browse y Remove component).
  5. Data Collection  Si RequiresDataCollection. Parámetros de catálogo por Area, por pieza.
Cierre
  6. Line Clearance   Igual que el de apertura.
  7. SFC Split        Solo en guías NonSerialized: separa del lote las N unidades del
                      hallazgo (spec 014; N se califica, ver "Qué cuenta como intento").
  8. Log NC           Tres métodos (spec 014), cada uno un paso de la guía, en orden fijo:
                      8a. Desecho rápido (`LogNC`): la pieza sale de la orden.
                      8b. Reparación rápida (`LogNCFastRepair`): quita el componente
                          dañado (Scrap) y agrega uno nuevo; la pieza sigue en el POD.
                      8c. Reparación extendida (`LogNCExtendedRepair`): Local Rework (la
                          pieza sigue) o Return to any previous (la pieza sale del POD).
                      Qué métodos entrena un área lo deciden los pasos de su guía (D-30):
                      LoFlo y Redefinity, los tres; Moldeo (sin Assembly), solo el desecho
                      (D-53). Cada paso tiene un **hallazgo** que genera la BLL al dar
                      Start: la pieza defectuosa (Serialized: una pieza distinta por paso;
                      NonSerialized: N unidades del lote, de 1 a lote − 1), el defecto y la
                      disposición (componente a reemplazar, disposición de la extendida y,
                      si el código exige Comments, un MPI simulado). Una nota de hallazgo
                      lo muestra en Guided y Assessment, de a uno ("Hallazgo n de N").
  9. Complete         Cierra el proceso de manufactura: las filas completadas salen del
                      POD. Ya no cierra la sesión de entrenamiento (spec 015).
 10. Logout           Clic en el ícono Logout del header (spec 015) y la limpieza al salir
                      (spec 016, D-65): "You have logged off" en Edge → cerrar Edge → el
                      script con el Code1 en el escritorio. El script cierra el paso y la
                      sesión de entrenamiento; el resumen final sale al cerrar la consola.
                      No cierra la sesión de la app.
```

## Modos de sesión (`TrainingSession.Mode`)

- **Guided** — práctica: reintentos ilimitados, sin penalización ni bloqueo. La guía visual
  no deja enviar la decisión incorrecta, así que casi siempre queda un intento por paso: es
  lo esperado (decisión del usuario, 2026-10-07). Los pasos que más fallan se miden con
  Assessment.
- **Assessment** — evaluación con puntaje mínimo de aprobación (`ScoreCalculator.PassingScore`
  = 0.70). Cada paso se completa antes de desbloquear el siguiente, con penalización por
  reintento:

  ```
  PenaltyFactor(intento 1)  = 1.00
  PenaltyFactor(intento 2)  = 0.80
  PenaltyFactor(intento 3)  = 0.60
  PenaltyFactor(intento 4+) = 0.00 → el paso queda fallido y el sistema FUERZA el avance
                                     al siguiente (la evaluación nunca queda bloqueada)

  StepScore = StepWeight × PenaltyFactor(intento en que acertó)
  ```

  Aplica igual a pasos con contenido (Data Collection, NC) y a pasos de secuencia (Resource
  Setup, Search): en estos, cada intento fuera de secuencia es un intento fallido.
- **Práctica libre** (`FreePractice`, `Mode` = 2, spec 012) — practicar solo y explorar: sin
  guía visual y sin mostrar en qué paso va, con todos los botones del POD habilitados.
  Califica dentro del paso sin penalizar: cada decisión o botón equivocado es un intento
  fallido con "Respuesta incorrecta.", sin intentos restantes ni avance forzado (el paso se
  resuelve al acertar o al saltarlo). Herramientas del simulador:
  - **Ver pista**, siempre disponible: el paso actual, el texto del último fallo si el
    control lo trajo y la pista del paso (la de Guided; por tramo en el Ingreso a MES y en
    Logout). Sin postback ni registro.
  - **Saltar paso** e **Ir a…** (solo hacia adelante, con confirmación): cada paso adelantado
    queda registrado como saltado y el POD como tras el avance forzado (ver "Qué cuenta como
    intento" → "Avance forzado").
  - El `FinalScore` se calcula sin penalización (un paso saltado vale 0) y no se muestra: ni
    en el header ni en el resumen, que muestra los intentos por paso (1 = acertó a la
    primera; "Saltado" o "Saltado (N fallos)"). Los intentos quedan separados por el modo de
    la sesión.

Qué hace cada modo vive en un solo lugar, `ModePolicy` (`BLL/Scoring/`): `CLAUDE.md` →
"Capas".

## Qué cuenta como intento

- **Formato vs. decisión.** Un error de *formato* (campo requerido vacío, Shop Order o
  barcode no numérico, filas faltantes, marcado `<x`) lo bloquea el control y **no** cuenta
  como intento. Toda *decisión* del operador (Sí/No, Setup State, código NC, valores de DC,
  elegir la fila en Log NC y Complete) se envía siempre al servidor y la califica
  `StepValidator`. Nunca rechazar una decisión en el control ni revelar la respuesta correcta
  en Assessment (el mensaje solo muestra los intentos restantes, más el mensaje real del MES
  si lo hay: ver "Mensajes del MES"). Excepción: en Data
  Collection un campo vacío sí es intento fallido, salvo el nombre opcional del grupo
  Trainer (`PersonName`, spec 015).
- **Precargado antes que formato** (spec 015, D-26): si la tabla de Resource Setup trae
  algún equipo precargado, el control no aplica sus avisos de formato (ej. "exactamente 1
  Shop Order") y envía el Save, que el servidor califica como decisión equivocada. Sin
  precargados, el formato sigue igual. Con un precargado y sin una Shop Order válida y
  propia, el intento cuenta pero la orden no se crea.
- **Acción incorrecta** (spec 007): botón equivocado dentro de un paso (ej. Add o Done en
  vez de Add-Done) o gate de fila sin cumplir → `StepAnswers.Invalid` a la BLL, intento
  fallido. Complete acepta solo `"complete"`.
- **Secuencia en Assessment (S2):** todos los botones del POD están habilitados, incluidos
  los que no aplican al área y las acciones del MES sin lógica (Work Instructions, Reports,
  Place Back in Queue, Tool List, las opciones de relleno de Activities). Un click en un
  botón que no es el del paso actual es un intento fallido **del paso actual**, con el mismo
  mensaje genérico, y no abre su control. Enmendada por la spec 017 (D-98): si el MES
  rechazaría esa acción en ese estado del POD, además se muestra su mensaje real (ver
  "Mensajes del MES"); si no, solo el genérico. La secuencia se evalúa antes que los avisos de
  formato. Activities → LC entra en la regla, y también el ícono Logout antes del último paso
  (spec 015; en Guided se ignora).
- **Secuencia en Práctica libre (spec 012, P-4 y P-5):** el botón de otro paso, el ícono
  Logout antes del último paso y el Repositorio fuera de su turno (en el Ingreso y en la
  limpieza) muestran "Ese no es el paso actual." **sin intento** y no abren nada; si el MES
  tiene un mensaje para ese botón en ese estado, se muestra ese mensaje en su lugar (spec
  017, D-101). Los botones del MES sin lógica no hacen nada, sin aviso ni intento.
- **Ingreso a MES y limpieza al salir (spec 016):** cada pantalla se califica al enviarla.
  - **Decisión:** el Code1 en el script, en Recursos MES y en el login de MES
    (el propio está bien; uno ajeno es intento fallido, `WrongDecision`, y se queda en esa
    pantalla); abrir el Repositorio antes del script (en el Ingreso) o en el escritorio de la
    limpieza (intento fallido de Logout, `OutOfSequence`; en Guided se ignora, como un botón
    fuera de secuencia). El login de MES solo se abre con Recursos MES hecho: un Log On sin
    ese registro es un envío inválido, sin intento (D-151).
  - **Formato, sin intento:** Code1 vacío; en el script, un Code1 que no tiene exactamente 9
    dígitos; estación o contraseña vacías; `<`, `>` o `&#`.
  - **No cuentan:** cerrar Edge (lo hecho se conserva), cerrar la consola sin responder,
    cambiar de pestaña, abrir una tarjeta (Hora-Hora 1.0 y 3.0 no hacen nada, D-151) y Log On
    Again.
- **Log NC (spec 014):** el operador ejecuta el método del paso sobre la pieza del hallazgo.
  Todo se califica junto en **un solo envío** (Add-Done, o la decisión equivocada que lo
  termina), contra el hallazgo; cualquier fallo es `WrongDecision` con el mensaje genérico
  y no aplica nada sobre las piezas. Abrir Log NC con otra pieza y salir con Return to POD
  no cuenta.
  - **Común:** exactamente la pieza del hallazgo (Serialized; una pieza de más es intento
    fallido) o el SFC del split (NonSerialized; el SFC del lote es intento fallido), y el
    **código exacto**: en el desecho, el del defecto del hallazgo (otro de desecho también
    falla); en las reparaciones, el general del método (`F_FR_GENERAL` o `F_FX_GENERAL`).
    Un código de otro método falla. Sin fila seleccionada sigue siendo intento fallido.
  - **Reparación rápida:** además, el componente del hallazgo en el Browse, **Scrap** en
    Remove component y **Yes** a agregar. Return, No a agregar y Send to Routing son
    intento fallido; No a remover, Cancel y ✕ son salir sin responder.
  - **Reparación extendida:** además, la disposición del hallazgo (Local Rework o Return to
    any previous); la otra es intento fallido. Cancel y ✕ de Select Disposition Function no
    cuentan. El componente (opcional) no se califica.
  - **Formato, sin intento:** Comments vacío con un código que lo exige (`RequiresComments`;
    el contenido no se califica), Qty to Remove y Qty to Assemble no numéricos o vacíos,
    Component, Version o Batch Number vacíos en Add Component, `<` o `>` en Component o
    Comments.
  - Add y Done siguen siendo botón equivocado (spec 007): Add-Done es el único camino.
- **Cantidad del split en Moldeo (spec 014, enmienda 3):** se califica al presionar Split
  contra las N unidades del hallazgo. En Assessment, otra cantidad es intento fallido del
  paso SFC Split (`WrongDecision`) y el split no se aplica; en Guided la guía no deja
  presionarlo. El Split con N no es intento (Close cierra el paso). Un segundo Split después
  del correcto es un aviso de formato y no se aplica.
- **Nunca cuentan:** navegación (Browse Work List, selección de filas, Abandon) y salir de
  un control sin responder (✗ / Return to POD). Logout dejó de ser navegación (spec 015):
  es el último paso; en ese paso el clic en el ícono no cuenta, empieza la limpieza (spec
  016). Con la sesión de entrenamiento ya cerrada (resumen final), el ícono cierra la sesión
  de la app (D-21). Para salir a mitad de una sesión queda Abandon (en el escritorio, desde la
  barra del simulador).
- **Avance forzado:** Ingreso a MES forzado deja ver el POD como si se hubiera entrado (spec
  016, P-5); Resource Setup forzado carga la orden por defecto (Shop Order
  `303406950`), ignorando lo capturado; Start forzado deja el POD como si Start se hubiera
  ejecutado; SFC Split forzado separa las N unidades del hallazgo; un paso de Log NC forzado
  aplica la disposición del hallazgo a su pieza (desecho: sale de la orden; reparación
  rápida: queda con el componente reemplazado; extendida: según la disposición); Complete
  forzado quita las filas del POD; Logout forzado (4.º fallo en la limpieza) cierra la sesión
  con su resumen.
- **Mensajes del MES (spec 017):** cuando el operador provoca una situación que el MES real
  rechaza, se muestra su mensaje, en inglés y con número, en el banner del POD o en la franja
  de la ventana, **en los tres modos y sin cambiar la calificación** (D-92): lo que era
  intento fallido lo sigue siendo, y lo que era formato también. Solo errores reactivos: no
  se inyectan escenarios (D-96).
  - **Formato con texto del MES** (D-103): Start sin SFC (`22506`) o con uno que no es del
    lote (`22507`, D-106); Assemble, DC Collect y SFC Split sin fila (`12906`); barcode no
    numérico (`17150`); Qty del Split vacía, 0 o mayor a la disponible (`13875`, `13213`,
    `13885`). Los avisos propios del simulador (seleccionar todas las piezas, split
    requerido…) siguen en español.
  - **Botón de otro paso** (C-1): el mensaje sale solo si en el MES esa acción, en ese
    estado, daría ese error. Orden de los chequeos (C-2): Resource Setup pendiente (Start,
    texto provisional, D-111) → Line Clearance de apertura pendiente (`22501`; "status is
    NO" si su última respuesta fue No, C-3; el LC de cierre no cuenta) → sin SFC (`22506`
    en Start y Complete, `12906` en el resto). Sin SFC = el Start todavía no se resolvió o
    no hay ninguna fila seleccionada. Complete con DC obligatorio pendiente da un `15246` por
    pieza seleccionada (D-113, D-119).
  - **Dentro del paso:** Data Collection muestra el error del primer parámetro con fallo
    (C-6: vacío `10015`, texto en un numérico `12205`, límites `15232`/`15233`); Complete y
    Log NC sin ninguna fila suman `22506` y `12906` a su intento fallido.
  - **Explicación en español** (D-102): la guía de Guided pone una nota junto al mensaje; en
    Práctica libre, un botón "¿Qué significa?" al final del mensaje abre un aviso de
    información con la explicación (elección del usuario al probar la E4: Ver pista muestra
    solo el paso y su pista). Assessment no la recibe.
  - Fuera: M-10, M-17 y M-18 no se pueden provocar en el simulador (D-118, D-120).
- **Paso saltado (Práctica libre, spec 012, P-9):** Saltar paso e Ir a… reusan el avance
  forzado: cada paso adelantado es un intento no correcto con `Reason = Skipped` (6) y
  `Action = Practice.Skip`, que resuelve el paso y deja el POD igual que el avance forzado
  (lo parcial de Assembly o DC se descarta). Saltar Logout cierra la sesión con su resumen.
  Un destino que ya no está adelante (doble clic, F5) se ignora; uno fuera de la guía, o un
  salto en Guided o Assessment, es un invariante violado (excepción), no un intento.

## Ponderación de pasos (`StepWeight`) — la fuente de verdad es la BD

Cada paso tiene su peso en `TrainingSteps.Weight` (0.00-1.00) y `ScoreCalculator` usa ese
valor. Los pesos de una guía **deben sumar 1.00**: el cálculo no normaliza (una guía que
suma 0.90 nunca llega a 100%). Pesos actuales (verificados contra `05`, `07` y `09`, 2026-10-19):

```
Paso                            Moldeo (07)   LoFlo (09)   Redefinity (05, ejemplo)
Ingreso a MES                      0.05         0.05          0.05
Resource Setup                     0.20         0.20          0.20
Line Clearance (apertura)          0.05         0.05          0.05
Search / Start                     0.15         0.05          0.05
Assembly                            —           0.15          0.15
Data Collection                    0.25         0.20          0.20
Line Clearance (cierre)            0.05         0.05          0.05
SFC Split (NonSerialized)          0.05          —             —
Log NC · desecho rápido            0.10         0.05          0.05
Log NC · reparación rápida          —           0.05          0.05
Log NC · reparación extendida       —           0.05          0.05
Complete                           0.05         0.05          0.05
Logout                             0.05         0.05          0.05
Total                              1.00         1.00          1.00
```

En Moldeo, Search absorbe el peso de Assembly (que no tiene) menos el de SFC Split (que sí
tiene); Moldeo no cambió con la spec 014 (D-53). LoFlo tiene la forma de Redefinity y sus
mismos pesos: los tres métodos de Log NC valen 0.05 cada uno y DC bajó de 0.30 a 0.25
(spec 014, D-29). El Ingreso a MES (0.05) salió de DC en las tres guías (spec 016, D-66).
`ScoreCalculatorTests` compara sus copias de las guías con estos scripts. Una guía nueva define sus pesos en su script SQL y debe sumar 1.00
(`spTrainingGuideSync` lo valida al cargarla). Logout (0.05) salió de Complete (spec 015);
los pesos de Logout, Log NC y DC se revisan con T6. Cambiar la forma de una guía ya
cargada crea una versión nueva (D-56, `docs/architecture.md` → "Versión de las guías").

## Catálogos en BD (nunca hardcodeados en código)

- `NCCodes`: códigos NC por Area, con su **`Method`** (`FastScrap`, `FastRepair`,
  `ExtendedRepair`; spec 014) y **`RequiresComments`**. El método lo decide el código que
  elige el operador, no el defecto ni el prefijo (enmienda 5):
  - Desecho rápido: un código por defecto posible. Real en Moldeo (`F_FS_{DEFECTO}`);
    simulado en LoFlo y Redefinity (`SIM_FS_…`).
  - Reparaciones: los genéricos reales `F_FR_GENERAL` (rápida, sin Comments) y
    `F_FX_GENERAL` (extendida, con Comments obligatorio), en las tres áreas. En Moldeo
    aparecen en NC Selection pero nunca son correctos (D-53).
  - La familia `F` a secas (fallas de DC, DC-DC ESign Exceptions) no es un método de Log NC
    y no está en el simulador.
- `Operations`: 2 operaciones simuladas (`SIM`) por área con reparación extendida (D-58),
  con `Sequence` y `Description` (ej. `010 O_LOFLO_SIM_PREP` "Preparación (SIM)" y
  `020 O_LOFLO_SIM_ENSAMBLE` "Ensamble (SIM)"). La actual es la de mayor `Sequence` y la
  anterior, la que le sigue; las nombra el banner de la extendida (Local Rework → la
  actual, Return to any previous → la anterior). Nada más las usa todavía.
- `DataCollectionGroups`: por Area (a futuro, por Operation), con `IsRequired`. Una pieza
  completa Data Collection al guardar todos los grupos obligatorios; si el área no tiene
  ninguno, basta uno cualquiera. Moldeo: solo `DCGTM_FECHA_EXPIRACION_NIR/A` es obligatorio.
  Grupo **Trainer** `DCGTM_TRAINEE/A` "Entrenamiento personal" (`Anytime`, real del MES,
  spec 015): opcional en todas las áreas con DC, para el nombre de la persona en
  entrenamiento. En Guided la guía no pasa por él: solo la nota "Solo cuando se está
  entrenando".
- `DataCollectionParameters`: cada uno pertenece a un grupo (`DataCollectionGroupId`).
  `DataType`: `Numeric`, `Boolean`, `Date` o `PersonName` (nombre del grupo Trainer: letras y
  espacios, de 2 a 60 caracteres, vacío válido; lo que no cumple es formato, sin intento;
  `PersonNameRule`). El parámetro `NOMBRE_EN_ENTRENAMIENTO` es provisional (SIM).
- `AssemblyComponents`: BOM de la operación (hoy por Area, `OperationId` nullable),
  `PartNumber`/`Revision`, `Description`, `Qty` decimal. Simulado por ahora. No hay conteo
  esperado de escaneos (`ExpectedComponentCount` se eliminó).
- Flags de `Areas`: `SerializationType`, `RequiresAssembly`, `RequiresDataCollection` y
  `RequiresStartChecklist`. Component en Log NC no es un flag del área: lo decide el método
  del código (spec 014, enmienda 4): aparece solo con los códigos de reparación (obligatorio
  en la rápida, opcional en la extendida) y el desecho no lo muestra en ningún área.
  `RequiresNCComponent` se quitó en la fila 13 de la Fase 8.5 (D-124).
- Datos de simulación (preguntas LC, parámetros de DC, códigos NC) van en scripts de
  `Database/StoredProcedures/`, nunca en arrays de la BLL ni en `Web.config`. Excepción
  aceptada (D-6): la pregunta de Line Clearance ("¿La limpieza de linea se ejecuto?") es la
  misma en todas las operaciones y queda en el código.

## Área del operador (spec 021)

- Cada operador entrena en **una sola área** (`Users.AreaId`; D-165). Solo el rol Operator la
  tiene: un administrador nunca (P-2). Sin área = todavía no la eligió.
- **Áreas elegibles:** las que tienen al menos una guía publicada (D-176). Si un área se queda
  sin guías publicadas, sus operadores la conservan y su inicio dice que no hay guías (P-6).
- **El operador la elige una vez**, después del cambio de contraseña, en "Elige tu área", y la
  confirma (D-169, D-177). Pasa por ahí todo operador sin área: autorregistrado, creado por un
  admin o existente (D-170). Después solo un **admin la cambia** en Usuarios (D-171, D-172); no
  se quita (D-208). **Entrenamiento cruzado:** el admin le cambia el área y la regresa después
  (D-165).
- **Qué decide:** solo qué guías puede **iniciar** (las publicadas de su área, D-167). Iniciar
  una guía de otra área, una no publicada o inexistente, o sin área, es una excepción, no un
  intento (D-173, B-20). Una sesión en curso de otra área (el admin cambió el área) se termina y
  cuenta como siempre (D-174).
- **Texto:** "Producto · Área" en todas partes; "Sin área" para un operador sin área y "—" para
  un administrador (P-9).
- **En el dashboard** (spec 024; enmienda D-175): Operadores y Operador se miran por **su área
  actual**, sin historial de cambios (D-271/P-1); Resumen, Pasos y Seguimiento siguen por la
  guía. Reglas: "Dashboard de Training" → "Operadores y Operador por área".

## Dashboard de Training (spec 018)

Solo lectura; cambian lo que se **muestra**, nunca cómo se entrena o se califica. Implementación:
`docs/architecture.md` → "Dashboard de Training".

- **Unidad: la guía publicada.** Las no publicadas no aparecen nunca, ni sus sesiones.
  Operadores = usuarios con rol Operator (DA-9).
- **Estado por operador y guía** (`ReadinessPolicy`, D-136; usa **todo el historial**, DA-1):
  - **Aprobado:** al menos un Assessment **completado** de la guía con `FinalScore` ≥
    `ScoreCalculator.PassingScore` (0.70). Una vez aprobado, sigue aprobado.
  - **Reprobado:** Assessment completados de la guía y ninguno aprobado.
  - **En entrenamiento:** sesiones de la guía, pero ningún Assessment completado (Guided,
    Práctica libre, o Assessment en curso o abandonado).
  - **Sin iniciar** (estado general, spec 024, D-263): el operador tiene área, su área tiene guía
    publicada y no tiene ninguna sesión en las guías publicadas **de su área** (aunque haya
    entrenado otra). Un operador sin área o con su área sin guía publicada no es Sin iniciar.
- **Período** de las vistas, por la fecha de **inicio** de la sesión. El estado y Seguimiento
  lo ignoran. Operadores y Operador usan todo el historial. Pasos y Resumen (spec 023,
  D-209 a D-218):
  - **atajos:** Hoy, Esta semana (lunes a hoy), Este mes (al entrar), Mes anterior, Últimos 30
    días (hoy y los 29 previos), Todo y Personalizado (Inicio y Fin; Fin ≥ Inicio y ≤ hoy; uno
    inválido vale Este mes con un aviso). Cortan a medianoche de la planta y el día de Fin
    entra completo;
  - **comparación** con el período anterior (P-4): los mismos días antes (Este mes contra los
    mismos días del mes pasado, hasta su último día si es más corto; Mes anterior contra el
    previo). Con Todo, no hay. Muestran su cambio la tasa de aprobación (en puntos), las
    sesiones del período (en %) y el % con fallo de cada paso (en puntos), siempre con la base
    de los dos períodos;
  - **gráfica de actividad:** por día hasta 31 días, por semana hasta 366 y por mes si es más
    largo.
- **Tasa de aprobación** (guía × período), sin denominador = "—", nunca 0 %:
  - **% aprobados:** de los operadores con algún Assessment completado en el período, los que
    aprobaron alguno en el período;
  - **% al primer intento:** de los operadores cuyo primer Assessment completado de la guía (en
    todo su historial, por fecha de inicio) cae en el período, los que lo aprobaron;
  - **puntaje promedio:** de los Assessment completados del período.
- **Pasos** (por paso de la guía; con "Todas", el mismo paso de cada guía en un renglón por su
  nombre de `StepNames`, D-147):
  - **% de sesiones con fallo:** de las sesiones con algún intento en el paso (el salto
    incluido), las que tienen algún intento fallido en él;
  - **intento fallido:** `IsCorrect = 0` sin contar los saltos;
  - **terminó sin acierto:** el paso quedó atrás sin un intento correcto: avance forzado en
    Assessment (también en el último paso de una sesión abandonada) o saltado con fallos en
    Práctica libre. El salto siempre resuelve el paso (D-83); sin fallos no es "sin acierto";
  - **saltos:** solo en Práctica libre, aparte, nunca como fallo;
  - **duración de un paso (DA-2):** desde el último intento del paso anterior (o el inicio de
    la sesión) hasta su último intento; en Pasos, la **mediana** de los pasos resueltos sin
    salto. Duración de la sesión: `CompletedAt − StartedAt` ("—" en curso).
  - **top 5 del Resumen:** los primeros de Pasos en Assessment con el mismo período y guía,
    solo pasos con algún fallo.
- **Operadores y Operador por área** (spec 024, D-260 a D-271). Hoy cada operador tiene **una
  guía que entrenar**: la publicada de su área; las áreas de un producto que comparten contenido
  se cargan con una copia por área (D-268).
  - **Operadores:** una columna "Estado en su área" (sin columnas por guía, D-261); filtros
    Buscar, **Área** (Todas, las áreas con guía publicada agrupadas por producto, y Sin área;
    D-262, D-267) y Estado, evaluado en las guías de su área (con varias, "alguna", P-6). Sin
    filtro de guía (D-264); las guías de otras áreas no salen en la lista (D-265, D-266). "Sin
    área" y "Sin guía publicada" no tienen insignia y solo salen con Estado = Todos (D-267, P-5).
    El Excel, igual que la pantalla (P-7).
  - **Operador:** "Su área" (sus guías, también Sin iniciar) y "Otras áreas (entrenamiento
    cruzado)" con las guías de otras áreas que tienen sesiones, cerrada (abierta si no tiene
    área); Reforzar y curvas, su área primero; Sesiones de todas las áreas con su área (D-269,
    P-8). En la ficha, las otras áreas en una línea (D-270).
- **Pasos a reforzar** (detalle del Operador): en sus Assessment de cada guía, los pasos que
  terminaron sin acierto o con 2 o más intentos fallidos en una sesión; por veces sin acierto
  y después por intentos fallidos, hasta 5 por guía, con el motivo más frecuente.
- **Seguimiento** (D-141; por operador y guía, sin período, DA-3a). Quien aprueba la guía sale
  con todas sus reglas. Umbrales: constantes de `FollowUpRules`.
  - **S-1:** 2 o más Assessment completados y reprobados de la guía, sin aprobar ninguno;
  - **S-2:** 14 días o más desde el inicio de su última sesión de **esa** guía (de cualquier
    modo, aunque siga en curso); practicar otras guías no lo reinicia;
  - **S-3:** el **mismo paso de la guía** (por paso, no por nombre, DA-3b) terminó sin acierto
    en 2 o más Assessment, abandonados incluidos.
- **Curva de aprendizaje** (operador × guía): sus sesiones de Práctica libre y Assessment en
  orden, con sus intentos fallidos y el puntaje de cada Assessment; **Guided no se grafica**.
  El dato: cuántas sesiones de práctica (**Guided y Práctica libre**) hizo antes de su primer
  Assessment aprobado.
- **Puntaje de una sesión:** se muestra si está completada y su modo lo muestra (D-90).
- **Ficha imprimible (D-150):** estado y puntajes por guía, pasos a reforzar y sesiones, sin
  menú ni botones. **No es un registro oficial de entrenamiento** (el simulador no es un
  sistema validado).
