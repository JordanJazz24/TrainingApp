# Reuniones con negocio — MESTrainingSimulator

> **Para la sesión "Reuniones y dudas"** (`docs/process.md` → "Tres sesiones de trabajo"): qué
> reuniones hubo, con quién y qué mejoras salieron. Es el índice y el contexto rápido; el
> detalle de cada reunión y el estado vivo de cada tarea están en el roadmap (enlaces abajo).
> Se agrega una fila y una sección al cerrar cada reunión. Creado el 2026-10-23.

## Contexto rápido

- **Para qué son las reuniones:** validar que el simulador sea fiel al SAP MES real y que
  enseñe lo que se audita en planta. Hoy no hay entrenamiento formal de MES (enseña el
  compañero de al lado) y unas 1600 personas usan el sistema.
- **Quiénes:**

| Persona | Rol | Qué aporta |
|---|---|---|
| Ernesto | MES, dueño del alcance | Decide qué entra; su evaluación práctica en papel (QD001043644, parte de una CAPA) es el mínimo que debe evaluar Assessment. Dio acceso al ambiente de testing |
| Diego | Ingeniero de SAP MES | Fidelidad de pantallas y variantes del piso; recomendó cerrar el alcance con Ernesto |
| Carla | Trainer, Wires línea final | Cómo se enseña y los errores más graves en planta; aprobó los mensajes de la guía |
| Técnicos de MES | Validación de uso | Aprobaron el flujo y la interfaz; pidieron la preparación antes de SAP |
| Paola y su equipo | Product owner | Reportes de avance de los viernes; decidieron guías por área |

- **Cómo se trabaja una reunión:**
  1. Antes: guion y preguntas (página o doc de Claude).
  2. Se graba con Riverside; la transcripción llega como `.txt` en `Downloads`.
  3. Esta sesión la analiza, el usuario decide.
  4. Lo decidido se manda a Arquitectura y Planeación, que lo registra en `docs/roadmap.md` y lo
     convierte en spec o plan.
  5. Se agrega aquí la fila y la sección de la reunión.
- **Lo que funcionó para sacar más de una reunión:** que el experto use el simulador y piense
  en voz alta (técnicos), y que la trainer enseñe al usuario como a un operador nuevo (de ahí
  salieron casi todas las notas de la guía).
- **Material para negocio:**
  [hoja de requerimientos](https://claude.ai/artifact/KWR4BeT8nUuEu97q83tCKq) (RF-01 a RF-28,
  versión del 2026-10-22; la del 2026-10-09 quedó como foto vieja).

## Reuniones

| Fecha | Con | Resultado | Detalle |
|---|---|---|---|
| 2026-10-09 | Diego (SAP MES), 30 min | Flujo general confirmado; pidió escanear la orden, los tres tipos de scrap y piezas de otras órdenes en la cola | roadmap → "Revisión con SAP MES (2026-10-09)" |
| 2026-10-09 | Ernesto, 68 min | Validó el enfoque; salieron las tareas T1 a T9 | roadmap → "Revisión con Ernesto, MES (2026-10-09)" |
| 2026-10-12 | Técnicos de MES (sin grabación) | Aprobaron flujo e interfaz; salió T10 (preparación antes de SAP) | roadmap → "Validación con técnicos de MES (2026-10-12)" |
| 2026-10-13 | Carla (trainer) | Los 3 errores más graves, Logout, grupo Trainer y los mensajes de la guía | roadmap → "Retroalimentación de la trainer" |
| 2026-10-22 | Ernesto, demo de mejoras | Aprobó el contenido; salieron R1 a R3 | roadmap → "Revisión con Ernesto, MES (2026-10-22)" |

## Mejoras que salieron y su estado

Estado al 2026-10-23. El estado vivo está en el roadmap; aquí solo se actualiza al registrar una
reunión nueva.

### Diego (2026-10-09)
- ✅ Escanear la Shop Order para dar Start (spec 011).
- ✅ Tres métodos de Log NC (spec 014).
- ✅ Mensajes reales del MES, capturados en el ambiente de testing (spec 017).
- ⏸ Piezas de otras órdenes en la cola (RF-16): diferido; Ernesto prefirió no agregar
  variabilidad. Lo cubren la alerta de Search/Start y el escaneo de la orden.
- ✖ Data Collection "aplicar a todas las piezas": descartado (se queda pieza por pieza).

### Ernesto (2026-10-09)
- ✅ T1 versión de prueba (el MVP se publicó el 2026-10-22).
- ✅ T2 escanear la Shop Order (spec 011).
- ✅ T3 modo práctica: "Práctica libre" (spec 012).
- ✅ T4 tres tipos de scrap (spec 014).
- ✅ T5 decidido: guías por área, no dos escenarios.
- ✅ T6 su prueba en papel comparada con Assessment: lo cubre todo, sin cambiar pesos
  (`docs/ui-reference/evaluacion-practica-mes.md`).
- ✅ T7 mensajes de la guía revisados con los trainers.
- ▶ T8 dashboard con el historial por operador: Fase 10.
- ✅ T9 acceso al ambiente de testing.

### Técnicos de MES (2026-10-12)
- ✅ T10 preparación antes de SAP: script de limpieza, Repositorio, Recursos MES y login de
  MES, más la limpieza al salir (spec 016).
- ✖ Tool List: descartado (lo usan técnicos, no el operador).
- ✖ Variantes de Complete: solo el flujo directo.
- La orden no se escanea antes de Resource Setup (aclaración del usuario).

### Carla (2026-10-13)
- ✅ Error 1, scrapear la pieza equivocada: el hallazgo indica la pieza defectuosa y otra es
  error en Assessment (spec 014).
- ⏸ Error 2, iniciar piezas ajenas: alerta en Search/Start; RF-16 sigue diferido.
- ✅ Error 3, no hacer Remove All: Resource Setup arranca con equipos precargados (spec 015).
- ✅ Logout como último paso (spec 015).
- ✅ Grupo de DC Trainer `DCGTM_TRAINEE/A`, opcional (spec 015).
- ✅ 4-5 piezas por orden Serialized (spec 014).
- ✅ Alertas en rojo y mensajes de la guía (T7).
- ⏳ Validación final con ella: pendiente.

### Ernesto (2026-10-22)
- ⏳ R1 quitar Hora a Hora del Ingreso a MES: el flujo es Repositorio → Recursos MES → login.
  Contradice lo que enseñaba Carla; manda Ernesto. Enmienda de la spec 016, después de la
  Fase 10.
- ⏳ R2 quitar "acciones disciplinarias" de los textos de la guía ("no nos corresponde"):
  plan directo, después de la Fase 10.
- ⏳ R3 Custom Resource Setup elegible por guía (flag de administrador; la estándar queda
  como predeterminada): spec, después de la Fase 10.
- Pesos, el 70 % y la regla de aprobación los deja al equipo de training.
- El link del MVP va a Ernesto, Diego y otros testers que él nombre, con la hoja de
  requerimientos. Presentación a Paola: 2026-10-23 o 2026-10-26, con las propuestas del
  dashboard. Dashboard a Ernesto la semana siguiente.

## Abierto con negocio

- Contraseña del login de MES simulado (¿la misma de la app?): sin respuesta (2026-10-22).
- ¿Assessment será evidencia de la CAPA o reemplazará la prueba en papel? Si sí, se revisa el
  riesgo de entrar con la cuenta de otro (D-20).
- Catálogos reales de LoFlo (DC, códigos NC, BOM): quién los da.
- W3 (operaciones sin herramientas en Resource Setup) y W7 (de dónde sale el LC de cierre).
- Regla de aprobación con el equipo de training (por ejemplo, Guided obligatorias antes de
  Assessment).
