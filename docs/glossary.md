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
| **Product** | Línea de producto de más alto nivel: Wires, Catéteres o Sensores. |
| **Area** | Sublínea de ensamblaje dentro de un Product (ej. R&C, LoFlo). Unidad a la que hoy se asocia una TrainingGuide general. Las áreas reales del roadmap son R&C (Fase 2) y LoFlo/Sensores (Fase 7). |
| **Operation** | [Futuro] Subdivisión de una Area — cada Area puede tener de 3 a 10 Operations, cada una con sus propios pasos específicos. Hoy no se usa para generar guías distintas, pero el esquema debe preverlo. |
| **SerializationType** | Determina cómo la Shop Order genera SFCs en Search/Start y cómo se scrappea en Cierre. `Serialized`: la Shop Order genera múltiples SFC individuales con identidad propia (ej. 989609001101-01, -02, -03); el operador puede procesar y scrapear unidades individualmente. `NonSerialized`: la Shop Order genera un único SFC con cantidad total (ej. SFC = 50012345, Qty = 500); MES controla cantidad total, no unidades. La diferencia NO aplica a Resource Setup — ese paso es idéntico en ambos tipos. |
| **SessionMode** | `Guided` (reintentos ilimitados, sin penalización, práctica) o `Assessment` (puntaje mínimo de aprobación, penalización por reintento). |
| **StepWeight** | Ponderación fija de cada tipo de paso sobre el puntaje final (no varía por Area). |
