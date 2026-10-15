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
| **LoFlo** (Sensores) | `08_SensoresLoFloCatalog.sql`, guía en `09` (Fase 7) | Confirmado 2026-10-07 para **todas las áreas de Sensores**: Serialized · Assembly sí · DC sí · Component en NC sí · checklist de Start no. DC Groups, parámetros, códigos NC y BOM: simulados por ahora |
| **Redefinity** (Catéteres) | **datos de ejemplo** en `05` ("NO USAR EN PRODUCCIÓN"), para probar el camino Serialized en dev | Serialized · Assembly sí · DC sí (no son decisiones de negocio) |
| **Subensamble 1** (Wires) | ejemplo en `05` | no son decisiones de negocio |

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
- El login a SAP NetWeaver **no** se evalúa: es solo carga de escenario.

## Flujo estándar de una TrainingGuide

Los 3 primeros pasos de Apertura y los de Cierre son iguales en todas las Areas; solo cambia
el catálogo (códigos NC, parámetros de DC), nunca la secuencia de tipos de paso.

```
Apertura
  1. Resource Setup   Asocia el recurso (estación) a la Shop Order y las herramientas
                      (ej. R_SCN_MASK_1, Shop Order 303406950, Tools 10033058 y 10087734).
                      No define qué piezas se procesan (eso es Search/Start). Captura libre,
                      sin validar contra catálogo. Se califica por SECUENCIA y por
                      Setup State = "Set Up" (guardar en "Open" es intento fallido); los
                      recursos capturados no se califican, salvo los precargados.
                      · Equipos precargados (spec 015): el control abre con una Shop Order
                        y 2 herramientas "del turno anterior", que genera la BLL en cada
                        apertura. Guardar con alguno en la tabla (por valor, también
                        reescrito a mano) es decisión equivocada; vale cualquier forma de
                        quitarlos (Remove All o Remove Selected).
  2. Line Clearance   Pregunta sí/no.
  3. Search/Start     Se califica por SECUENCIA. Aquí aparece la diferencia por
                      SerializationType:
                      · Serialized: 3-5 piezas por orden en el simulador; Start, Assembly,
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
  5. Data Collection  Si RequiresDataCollection. Parámetros de catálogo por Area, por pieza.
Cierre
  6. Line Clearance   Igual que el de apertura.
  7. SFC Split        Solo en guías NonSerialized: divide filas del lote antes de seguir.
  8. Log NC/Scrap     Catálogo de códigos por Area.
  9. Complete         Cierra el proceso de manufactura: las filas completadas salen del
                      POD. Ya no cierra la sesión de entrenamiento (spec 015).
 10. Logout           Clic en el ícono Logout del header (spec 015). Simulado: registra el
                      paso, cierra la sesión de entrenamiento y muestra el resumen final;
                      no cierra la sesión de la app.
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

## Qué cuenta como intento

- **Formato vs. decisión.** Un error de *formato* (campo requerido vacío, Shop Order o
  barcode no numérico, filas faltantes, marcado `<x`) lo bloquea el control y **no** cuenta
  como intento. Toda *decisión* del operador (Sí/No, Setup State, código NC, valores de DC,
  elegir la fila en Log NC y Complete) se envía siempre al servidor y la califica
  `StepValidator`. Nunca rechazar una decisión en el control ni revelar la respuesta correcta
  en Assessment (el mensaje solo muestra los intentos restantes). Excepción: en Data
  Collection un campo vacío sí es intento fallido, salvo el nombre opcional del grupo
  Trainer (`PersonName`, spec 015).
- **Precargado antes que formato** (spec 015, D-26): si la tabla de Resource Setup trae
  algún equipo precargado, el control no aplica sus avisos de formato (ej. "exactamente 1
  Shop Order") y envía el Save, que el servidor califica como decisión equivocada. Sin
  precargados, el formato sigue igual. Con un precargado y sin una Shop Order válida y
  propia, el intento cuenta pero la orden no se crea.
- **Acción incorrecta** (spec 007): botón equivocado dentro de un paso (ej. Add o Done en
  vez de Add-Done) o gate de fila sin cumplir → `StepAnswers.Invalid` a la BLL, intento
  fallido. Complete acepta solo `"complete"`; Logout, solo `"logout"`.
- **Secuencia en Assessment (S2):** todos los botones del POD están habilitados, incluidos
  los que no aplican al área y las acciones del MES sin lógica (Work Instructions, Reports,
  Place Back in Queue, Tool List, las opciones de relleno de Activities). Un click en un
  botón que no es el del paso actual es un intento fallido **del paso actual**, con el mismo
  mensaje genérico (no revela que fue de secuencia), y no abre su control. La secuencia se
  evalúa antes que los avisos de formato. Activities → LC entra en la regla, y también el
  ícono Logout antes del último paso (spec 015; en Guided se ignora).
- **Nunca cuentan:** navegación (Browse Work List, selección de filas, Abandon) y salir de
  un control sin responder (✗ / Return to POD). Logout dejó de ser navegación (spec 015):
  es el último paso. Con la sesión de entrenamiento ya cerrada (resumen final), el ícono
  cierra la sesión de la app (D-21). Para salir a mitad de una sesión queda Abandon.
- **Avance forzado:** Resource Setup forzado carga la orden por defecto (Shop Order
  `303406950`), ignorando lo capturado; Start forzado deja el POD como si Start se hubiera
  ejecutado; Complete forzado quita las filas del POD; Logout forzado cierra la sesión con su
  resumen.

## Ponderación de pasos (`StepWeight`) — la fuente de verdad es la BD

Cada paso tiene su peso en `TrainingSteps.Weight` (0.00-1.00) y `ScoreCalculator` usa ese
valor. Los pesos de una guía **deben sumar 1.00**: el cálculo no normaliza (una guía que
suma 0.90 nunca llega a 100%). Pesos actuales (verificados contra `05`, `07` y `09`, 2026-10-14):

```
Paso                        Moldeo (07)   LoFlo (09)   Redefinity (05, ejemplo)
Resource Setup                 0.20         0.20          0.20
Line Clearance (apertura)      0.05         0.05          0.05
Search / Start                 0.15         0.05          0.05
Assembly                        —           0.15          0.15
Data Collection                0.30         0.30          0.30
Line Clearance (cierre)        0.05         0.05          0.05
SFC Split (NonSerialized)      0.05          —             —
Log NC/Scrap                   0.10         0.10          0.10
Complete                       0.05         0.05          0.05
Logout                         0.05         0.05          0.05
Total                          1.00         1.00          1.00
```

En Moldeo, Search absorbe el peso de Assembly (que no tiene) menos el de SFC Split (que sí
tiene). LoFlo tiene la forma de Redefinity y sus mismos pesos. Una guía nueva define sus
pesos en su script SQL y debe sumar 1.00 (`spTrainingGuideSync` lo valida al cargarla).
Logout (0.05) salió de Complete (spec 015); se revisa con T6.

## Catálogos en BD (nunca hardcodeados en código)

- `NCCodes`: códigos de scrap/NC por Area.
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
- Flags de `Areas`: `SerializationType`, `RequiresAssembly`, `RequiresDataCollection`,
  `RequiresNCComponent` y `RequiresStartChecklist`.
- Datos de simulación (preguntas LC, parámetros de DC, códigos NC) van en scripts de
  `Database/StoredProcedures/`, nunca en arrays de la BLL ni en `Web.config`. Excepción
  aceptada (D-6): la pregunta de Line Clearance ("¿La limpieza de linea se ejecuto?") es la
  misma en todas las operaciones y queda en el código.
