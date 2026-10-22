# Domain Glossary — MESTrainingSimulator

> Solo definiciones. Las reglas viven en `CLAUDE.md` y `docs/domain.md` (cada entrada indica
> dónde); no copiarlas aquí.

| Término | Definición |
|---|---|
| **SFC** | Shop Floor Control: unidad de trabajo que el operador abre y ejecuta en piso. |
| **Shop Order** | Orden de fabricación asociada a una línea y producto; identificador principal del contexto de trabajo. |
| **SFO** | Shop Floor Order: sinónimo de Shop Order en algunos contextos del legacy. |
| **POD** | Production Operator Dashboard: la pantalla principal (`GuidedTest.aspx`) con la tabla de filas de la orden, los botones de acción, el panel derecho y el pie. |
| **LC** | Line Clearance: verificación de limpieza de línea (pregunta sí/no) al abrir y al cerrar. |
| **Resource Setup** | Paso que asocia el recurso (estación) a la Shop Order y las herramientas. Reglas: `docs/domain.md` → "Flujo estándar". |
| **Search / Start** | Paso en que el operador elige el SFC (o las piezas) y ejecuta Start. |
| **Start con checklist** | `Areas.RequiresStartChecklist = 1`: Start abre la secuencia de 4 modales (hoy solo Moldeo). Con 0, Start es directo. |
| **Browse Work List** | Modal "SFCS QUEUED" desde el que se elige qué SFC (o qué piezas, en Serialized) se inicia con Start. |
| **Pieza (Serialized)** | Cada SFC individual de una Shop Order serializada, `{12NC}-{serial}`: el 12NC del material y un serial consecutivo dentro de la orden (W23, D-121, D-122). La Shop Order no forma parte del SFC. |
| **[N SELECTED]** | Texto de `TxtSFC` con varias filas seleccionadas (ej. `[3 SELECTED]`), como el MES real. Con una fila muestra su SFC. |
| **Process Lot / Batch Number** | Lote al que pertenecen las piezas de la orden (columna Process Lot del POD); es el valor de ★ Batch Number en Assemble Components. |
| **BOM / Component List** | Lista de materiales de la operación (`AssemblyComponents`); en Assembly se muestra como Component List. |
| **DataCollection (DC)** | Captura de parámetros de proceso (temperatura, tiempo, presión…) agrupados en DC Groups. |
| **DC Group** | Grupo de parámetros de DC (ej. `DCGTM_ID_MOLDE/A`), obligatorio u opcional (`IsRequired`). Regla de completitud: `docs/domain.md` → "Catálogos en BD". |
| **NC / Scrap** | Registro de material no conforme o scrap con código de causa. |
| **SFC Split** | Divide la cantidad de un SFC de lote (NonSerialized) creando un SFC nuevo de la misma Shop Order. |
| **Employee** | Empleado de la API de la empresa (`Constants.EmployeeApiUrl`); no se guarda en la BD. Si su `code1` está en la lista, puede activar su cuenta. |
| **User** | Cuenta local de un Employee (tabla `Users`): se crea en su primer acceso y queda activa al cambiar la contraseña por defecto. No llamarla Operator: "operador" es la persona en la línea (D-17). |
| **Product / Area / Operation** | Línea de manufactura / sublínea de ensamblaje / [futuro] subdivisión del Area. Jerarquía y lista oficial: `docs/domain.md` → "Jerarquía". |
| **TrainingGuide** | Plantilla de pasos que define el flujo del operador (qué pasos, en qué orden, con qué pesos). Publicada, su forma no cambia: otra forma crea una versión nueva y los pesos se actualizan en sitio (D-56, `docs/architecture.md` → "Versión de las guías"). No llamarla WorkflowDefinition. |
| **TrainingSession** | Ejecución de una TrainingGuide por un operador: paso actual, intentos y, al terminar, el puntaje final. |
| **Guided Test** | Nombre de la pantalla de entrenamiento (`GuidedTest.aspx`), en cualquiera de los tres modos. |
| **SerializationType** | `Serialized` (varios SFC individuales) o `NonSerialized` (un SFC con cantidad). Reglas: `docs/domain.md` → "Ejes de variación". |
| **SessionMode** | `Guided` (tutorial con guía), `Assessment` (evaluación) o `FreePractice` ("Práctica libre": sin guía ni puntaje, con pistas y saltos, spec 012). Lo que hace cada uno lo decide `ModePolicy`. Reglas: `docs/domain.md` → "Modos de sesión". |
| **StepWeight** | Peso de un paso sobre el puntaje final (`TrainingSteps.Weight`). Pesos y reglas: `docs/domain.md` → "Ponderación de pasos". |
| **Intento** | Cada envío que califica el servidor. Qué cuenta y qué no (formato vs. decisión, acción incorrecta, secuencia S2, avance forzado): `docs/domain.md` → "Qué cuenta como intento". |
| **Avance forzado (force-advance)** | En Assessment, al 4.º intento (`ScoreCalculator.ShouldForceAdvance`) el paso queda fallido con 0 puntos y el sistema avanza solo al siguiente. |
| **12NC** | Número de parte Philips de 12 dígitos: identifica un material o producto (no una unidad). Prefijo del SFC serializado real, `{12NC}-{serial}` (`ui-reference/wi-produccion-disposables.md` → W23). |
| **MPI** | Manufacturing Process Instruction: instrucción de proceso de cada operación. El MES la abre desde [Work Instruction]; los parámetros de DC se recolectan según ella. |
| **eDHR / eLHR** | Registro electrónico histórico del dispositivo (producto terminado) / del lote (sub-ensamble). Lo que el MES registra queda en ellos. |
| **Log Buyoff** | Aprobación electrónica al completar (usuario y contraseña o tarjeta, luego [Accept]) en operaciones de 50 SFC o menos por transacción. El simulador no la tiene: `ui-reference/wi-produccion-disposables.md` → W17. |
| **Firma electrónica (eSignature)** | Firma con usuario y contraseña ("Data Collection Authentication") al completar operaciones que mueven toda la orden. |
| **Fast scrap / Fast repair / Extended fast repair** | Métodos de Log NC. En el MES suelen reconocerse por el prefijo del código: `F_FS` desecha la unidad, `F_FR` quita o cambia un componente (Return o Scrap), `F_FX` regresa la unidad a una operación anterior. En el simulador el método lo define el catálogo (`NCCodes.Method`), no el prefijo: `docs/domain.md` → "Catálogos en BD". |
| **Hold** | Retención de una Shop Order o un Process Lot (Activities → SFC Place Hold / Release Hold; Future Hold, hasta una operación). La usa Calidad. |
| **Time-Based Assembly** | Ensamble automático de componentes al hacer Start, desde ranuras cargadas en la estación. Solo en algunas estaciones; no se simula. |
| **Guía visual** | Overlay de Guided (Fase 6.6) que lleva al operador clic por clic. Mecanismo: `docs/architecture.md`. |
