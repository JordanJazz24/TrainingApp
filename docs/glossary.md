# Domain Glossary — MESTrainingSimulator

> Extraído de `CLAUDE.md`. Las reglas de negocio cerradas viven en `CLAUDE.md` → Reglas de Negocio del Dominio.

Términos del dominio MES que aplican tanto al legacy como a la nueva plataforma.

| Término | Descripción |
|---|---|
| **SFC** | Shop Floor Control — unidad de trabajo que el operador abre y ejecuta (equivale a una orden de manufactura en piso) |
| **LC** | Line Clearance — verificación de limpieza de línea; serie de preguntas booleanas que el operador debe responder antes de iniciar |
| **Resource Setup** | Asocia el recurso (estación de trabajo) a la Shop Order y las herramientas/equipos requeridos para la operación. No define qué piezas se procesan — eso ocurre en Search/Start. Ejemplo: Resource = R_SCN_MASK_1, Shop Order = 303406950, Tool = 10033058, Tool = 10087734. |
| **DataCollection** | Captura de parámetros de proceso (temperatura, tiempo, presión, etc.) agrupados por tipo de proceso |
| **NC / Scrap** | Registro de material no conforme o scrap con código de causa |
| **TrainingGuide** | Plantilla de pasos ejecutables que define el flujo del operador: qué pasos, en qué orden, con qué validaciones. Es inmutable una vez publicada. |
| **TrainingSession** | Instancia activa de una TrainingGuide para un operador específico. Contiene estado en tiempo real: paso actual, respuestas, tiempo. Al completarse guarda el puntaje final. |
| **Guided Test** | Flujo de entrenamiento secuencial con pasos validados por el servidor; el operador no puede avanzar sin completar el paso actual |
| **Shop Order** | Orden de fabricación asociada a una línea y producto; identificador principal del contexto de trabajo |
| **SFO** | Shop Floor Order — sinónimo de Shop Order en algunos contextos del legacy |
| **Employee** | Registro de empleado obtenido de la API pública de la empresa (`Constants.EmployeeApiUrl`). Controla quién puede acceder al sistema — si el `code1` existe en la lista devuelta por la API, el empleado es válido. No se almacena localmente en la BD. La lista se cachea en memoria con fallback indefinido ante fallas de la API. |
| **Operator** | Empleado que ya completó el primer login y cambió su contraseña. Tiene cuenta activa en el sistema. |
| **Product** | Línea de producto de más alto nivel: Wires, Catéteres, Sensores o R&C. Lista oficial de áreas por producto: `CLAUDE.md` → Jerarquía. |
| **Area** | Sublínea de ensamblaje dentro de un Product (ej. Moldeo en R&C, LoFlo en Sensores, Redefinity en Catéteres). Unidad a la que hoy se asocia una TrainingGuide general. Las áreas del roadmap son Moldeo (Fase 2) y LoFlo (Fase 7). |
| **Operation** | [Futuro] Subdivisión de una Area — cada Area puede tener de 3 a 10 Operations, cada una con sus propios pasos específicos. Hoy no se usa para generar guías distintas, pero el esquema debe preverlo. |
| **SerializationType** | Determina cómo la Shop Order genera SFCs en Search/Start y cómo se scrappea en Cierre. `Serialized`: la Shop Order genera múltiples SFC individuales con identidad propia (ej. 989609001101-01, -02, -03); el operador puede procesar y scrapear unidades individualmente. `NonSerialized`: la Shop Order genera un único SFC con cantidad total (ej. SFC = 50012345, Qty = 500); MES controla cantidad total, no unidades. La diferencia NO aplica a Resource Setup — ese paso es idéntico en ambos tipos. |
| **SessionMode** | `Guided` (reintentos ilimitados, sin penalización, práctica) o `Assessment` (puntaje mínimo de aprobación, penalización por reintento). |
| **StepWeight** | Ponderación fija de cada tipo de paso sobre el puntaje final (no varía por Area). |
| **POD** | Production Operator Dashboard: la pantalla principal (`GuidedTest.aspx`) con la tabla de filas de la orden, los botones de acción, la sidebar y el footer. |
| **Browse Work List** | Modal "SFCS QUEUED" desde el que el operador elige qué SFC (o qué piezas, en Serialized) va a iniciar con Start. |
| **Pieza (Serialized)** | Cada SFC individual de una Shop Order serializada. En el simulador, Resource Setup genera 3-5 piezas `{ShopOrder}-{sufijo}` en estado `New`; Start las pasa a `Active` y aparecen en el POD (spec 008). |
| **[N SELECTED]** | Texto que muestra `TxtSFC` cuando hay varias filas seleccionadas (ej. `[3 SELECTED]`), igual que el MES real. Con una fila muestra su SFC. |
| **Start con checklist** | `Areas.RequiresStartChecklist = 1`: Start abre la secuencia de 4 modales (hoy solo Moldeo). Con 0, Start es directo y se califica solo por secuencia. |
| **BOM / Component List** | Lista de materiales (números de parte) que forman el producto de la operación. En Assembly se muestra como Component List (`AssemblyComponents`: PartNumber/Revision, Qty decimal por unidad). Hoy es simulado. |
| **Process Lot / Batch Number** | Lote al que pertenecen las piezas de la orden (columna Process Lot del POD). Es el valor que se carga en ★ Batch Number del modal Assemble Components. |
| **DC Group** | Grupo de parámetros de Data Collection (ej. `DCGTM_ID_MOLDE/A`), en `DataCollectionGroups`. Puede ser obligatorio u opcional (`IsRequired`); una pieza completa DC al guardar todos los obligatorios, o uno cualquiera si el área no tiene obligatorios. |
| **Formato vs. decisión** | Convención de los controles de paso: un error de formato (campo vacío, barcode no numérico, filas faltantes) es un aviso sin intento; una decisión del operador (Sí/No, Setup State, valores, código NC) siempre la califica el servidor. Ver `CLAUDE.md` → Key Conventions. |
| **Secuencia (S2)** | En Assessment todos los botones del POD están habilitados; un click en un botón que no es el del paso actual es un intento fallido de ese paso. |
| **Avance forzado (force-advance)** | En Assessment, al 4.º intento fallido el paso se marca fallido (0 puntos) y el sistema avanza solo al siguiente. |
