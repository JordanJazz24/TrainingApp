# Spec 018: Dashboard de Training e historial por operador (Fase 10, T8)

**Status:** In Review
**Date:** 2026-10-22 · siete rondas de preguntas con el usuario el 2026-10-22

## Problem

Training, gerencia y el desarrollador no ven cómo les va a los operadores en el simulador:
quién está listo para producción, en qué pasos se falla y si el entrenamiento avanza. Los
datos ya se guardan (sesiones; intentos por paso con hora, motivo y acción, spec 013), pero
solo se ven en el resumen final de cada sesión. T8 (revisión con Ernesto, 2026-10-09) pidió
el historial por operador para ver dónde fallan antes de producción. La Fase 10 entra en la
versión final de la 2.ª semana de noviembre (D-129), junto con la E2 de la Fase 9 (D-135).

## Usuarios y preguntas que responde

- **Quién lo mira:** gerencia, trainers y el desarrollador. Solo con la capacidad nueva
  `CanViewReports`, que hoy tiene solo Administrator. **El operador no ve su historial.**
- **Tres preguntas, con el mismo peso:**
  1. ¿Este operador está listo? → Operadores, Operador y Seguimiento.
  2. ¿Dónde se falla más? → Pasos y el top 5 del Resumen.
  3. ¿Cómo avanza el entrenamiento? → Resumen.
- **Propuestas de valor para Training** que eligió el usuario:
  - estado "listo";
  - pasos con más fallos;
  - ficha imprimible;
  - exportar a Excel;
  - curva de aprendizaje;
  - lista de seguimiento.

## Scope

### In

Cinco vistas en `Pages/Admin`, con un menú en `Admin.Master`: **Resumen · Operadores ·
Pasos · Seguimiento · Usuarios**. Desde la lista de Operadores, Pasos o Seguimiento se llega
al detalle de un **Operador**.

1. **Resumen** (gerencia). Es el **inicio del administrador**: `AdminHomeUrl` pasa de
   `Users.aspx` al Resumen. Lleva:
   - operadores por estado y área (Aprobado, Reprobado, En entrenamiento; solo quienes
     entrenaron esa guía);
   - la tasa de aprobación de Assessment por área (aprobados / completados) y el puntaje
     promedio;
   - una gráfica de la actividad en el tiempo: sesiones por semana y por modo;
   - el top 5 de pasos con más fallos en Assessment, con enlace a Pasos.
2. **Operadores:**
   - lista con el estado por área, el mejor y el último puntaje de Assessment y la última
     actividad;
   - búsqueda por nombre o Code1;
   - filtro por estado y por área;
   - **50 por página, paginada en el servidor**.
3. **Operador** (detalle):
   - su estado por área;
   - sus sesiones: fecha, área, modo, estado, puntaje, duración e intentos fallidos;
   - sus pasos a reforzar;
   - su **curva de aprendizaje** por área;
   - su **ficha imprimible**.
4. **Pasos:** el ranking de pasos con más fallos por área, con:
   - intentos fallidos;
   - las veces que el paso terminó sin acierto;
   - sus motivos y acciones registrados;
   - la mediana de su duración;
   - las veces que se saltó, cuando el filtro de modo incluye Práctica libre.
5. **Seguimiento:** los operadores que requieren atención, con el motivo de cada uno.
6. **Exportar a `.xlsx`** (ClosedXML) desde Operadores, Sesiones (del período), Pasos y
   Seguimiento, con los filtros aplicados.
7. **Gráficas con Chart.js**, guardado en `Scripts/` sin CDN, como JsBarcode. Se suma al
   inventario de AUD-29.
8. **Filtros:**
   - período, con los últimos 30 días al entrar y la opción "Todo";
   - área (guía), con "Todas";
   - modo, solo en Pasos y con Assessment al entrar.
9. **Capacidad `CanViewReports`** en `AccessPolicy`. `Web.config` y cada página la revisan.

### Out

- Que el operador vea su propio historial.
- El área del operador, distinta del área de la guía (ver "Deferred Decisions").
- El re-entrenamiento por vencimiento.
- Abandonar sesiones como regla de seguimiento.
- El detalle por paso en el resumen final del operador (sigue la deuda de la spec 007).
- Líneas de firma en la ficha. La ficha **no** es un registro oficial de entrenamiento: el
  simulador no es un sistema validado.
- Cambiar cómo se entrena o se califica; tablas nuevas.
- Un rol Trainer: la capacidad nueva lo deja preparado, pero no se crea.

## Domain Model

Sin tablas nuevas. Se agregan **índices** para las consultas por operador, por guía y por
fecha (ver "Non-Functional").

- **Área = la de la guía practicada.** No se agrega área al usuario.
- **Operadores = usuarios con rol Operator.** Los administradores no entrenan (spec 013) y no
  aparecen. Un usuario borrado se lleva su historial (spec 013), así que tampoco aparece.
- **Estado por operador y área.** La regla vive solo en una política de la BLL, la
  **`ReadinessPolicy`**, como `ModePolicy` y `AccessPolicy`. El dashboard le pregunta el
  estado y nunca repite la regla. El usuario avisó que la regla puede cambiar.
  - **Aprobado:** al menos un Assessment *completado* de esa guía con `FinalScore` ≥
    `ScoreCalculator.PassingScore` (0.70).
  - **Reprobado:** tiene Assessment completados de esa guía y ninguno aprobado.
  - **En entrenamiento:** solo tiene sesiones de Guided o Práctica libre, o un Assessment
    en curso o abandonado.
  - **Sin iniciar:** sin sesiones en esa guía. No se lista por área, porque el área sale
    de la guía.
  - El estado usa **todo el historial**, sin importar el período elegido.
- **Sesiones abandonadas o en curso:** se listan con su estado. No cuentan como reprobado
  ni entran en la tasa de aprobación, pero sus intentos fallidos sí cuentan en Pasos. Una
  sesión en curso no tiene duración ("—").
- **Duración:**
  - **de la sesión:** `CompletedAt − StartedAt`;
  - **de un paso:** desde el último intento del paso anterior (o desde `StartedAt` en el
    primero) hasta el último intento del paso. Se calcula con las horas de los intentos que
    ya se guardan;
  - en Pasos se muestra la **mediana**, que no se infla si alguien dejó la sesión abierta.
- **Paso fallido en una sesión:** terminó sin acierto (avance forzado en Assessment, o
  saltado con fallos en Práctica libre). **Intentos fallidos:** `IsCorrect = 0`. **Saltos**
  (Práctica libre): se cuentan aparte, nunca como fallo.
- **Lista de seguimiento.** Sus umbrales son constantes de la BLL. Ignora el período. Un
  operador entra si cumple al menos una regla, y se muestran todas las que cumple:
  - **S-1:** reprobó **2 o más** Assessment de un área sin aprobar ninguno;
  - **S-2:** entrenó un área, no la aprobó y lleva **14 días** sin sesiones;
  - **S-3:** falló el **mismo paso** en **2 o más** Assessment (el paso terminó sin acierto).
- **Curva de aprendizaje** (operador × área):
  - una gráfica con sus sesiones en orden (el color indica el modo) y los intentos fallidos
    de cada una, con el puntaje de cada Assessment;
  - el dato: cuántas sesiones de **práctica** (Guided y Práctica libre) hizo antes de su
    primer Assessment aprobado.
- **Ficha imprimible:** estado y puntajes por área, sesiones y pasos a reforzar. Cómo se
  genera (impresión desde el navegador o un PDF del servidor) se decide al implementarla
  (DA-6, ver "Deferred Decisions").
- **Horas:** la base guarda en UTC y se muestra en la hora de la planta, con
  `SessionHelper.ToLocal`, como el último acceso en Usuarios. Formato `dd/MM/yyyy HH:mm`.
  Las semanas de la gráfica de actividad empiezan el lunes.

## API Contract

No hay API pública: son páginas Web Forms en `Pages/Admin`, siguiendo las capas de
`CLAUDE.md`.

- **SPs nuevos de solo lectura que agregan sin reglas.** Devuelven, por ejemplo, las
  sesiones con su conteo de intentos fallidos o los fallos por paso. Ninguno decide
  aprobado, estado ni seguimiento.
- **BLL:** un servicio del dashboard aplica las reglas (`ReadinessPolicy` y las de
  seguimiento) y arma las vistas. Verifica `CanViewReports` en cada método, como la
  gestión de usuarios verifica que quien llama es admin.
- **Excel:** un `IHttpHandler` (`.ashx`) o la descarga desde un postback devuelve el
  `.xlsx`, sin `[WebMethod]`. Verifica la capacidad igual que las páginas.

## Authorization

- `AccessPolicy.CanViewReports`, que hoy solo es `true` para Administrator.
- `Web.config` protege las páginas nuevas y cada página y la descarga lo revisan otra vez.
  Sin la capacidad, `SessionHelper.RedirectHome`, nunca el login (`CLAUDE.md` → Auth).
- Solo lectura: el dashboard no modifica nada.

## Edge Cases & Failure Modes

- Sin datos (BD recién creada, período vacío): cada vista muestra "Sin datos para este
  período", sin gráficas rotas ni división por cero en las tasas.
- Un operador con sesiones en varias áreas: un estado por cada área que entrenó.
- Una guía sin Assessment completados: tasa "—", no 0 %.
- Un paso de una guía cuya estructura cambió: `spTrainingGuideSync` no deja cambiarla en
  una guía existente (`docs/process.md` §6), así que los pasos son estables.
- La regla S-3 se cuenta por paso de la misma guía.
- Exportar con 0 filas: un `.xlsx` con solo encabezados.
- Si **ClosedXML** no funciona en .NET Framework 4.7.2 con `packages.config`: **se para y
  decide el usuario** (no hay plan B automático).

## Non-Functional Requirements

- **Volumen de referencia:** unos 1600 operadores posibles, con un año de datos (del orden
  de decenas de miles de sesiones y cientos de miles de intentos).
- **Meta:** cada vista responde en **menos de 3 s** con ese volumen.
- **Índices nuevos:** sesiones por operador y por guía con fecha. Se verifica con datos
  generados en una BD de LocalDB descartable, con un script de prueba en `Database/Ops/`.
- Los datos del MVP se descartan (D-130), así que los índices y los SPs nuevos entran
  recreando la BD, sin `ALTER`.

## Integrations

- **Chart.js** (MIT): versión fija, en `Scripts/`, sin CDN.
- **ClosedXML** (MIT) y sus dependencias por NuGet (`packages.config`).
- Las dos se suman al inventario de AUD-29.
- Sin servicios externos nuevos.

## Acceptance Criteria

**Acceso y navegación**
- **AC-1:** Given un Administrator, When entra a la app, Then cae en el Resumen y el menú
  muestra Resumen · Operadores · Pasos · Seguimiento · Usuarios.
- **AC-2:** Given un Operator, When abre cualquier URL del dashboard o de la descarga, Then
  vuelve a su inicio (`RedirectHome`), no al login, y no recibe datos.

**Estado**
- **AC-3:** Given un operador con un Assessment completado de LoFlo con 0.70, When se ve su
  estado, Then LoFlo = Aprobado.
- **AC-4:** Given un operador con dos Assessment completados de Moldeo con 0.55 y 0.65,
  Then Moldeo = Reprobado y aparece en Seguimiento por S-1.
- **AC-5:** Given un operador que aprobó Moldeo y después reprobó, Then Moldeo sigue
  Aprobado, con el último puntaje 0.xx y el mejor 0.yy.
- **AC-6:** Given un operador con solo Guided, o con un Assessment abandonado o en curso,
  Then el estado es En entrenamiento y no entra en la tasa de aprobación.
- **AC-7:** Given que se cambia la regla de `ReadinessPolicy`, Then el estado cambia en
  todas las vistas, el Excel y la ficha sin tocar nada más (se comprueba con tests de la
  política).

**Resumen**
- **AC-8:** Given el período de 30 días, Then el Resumen muestra:
  - operadores por estado y área;
  - la tasa de aprobación y el puntaje promedio por área;
  - la gráfica de sesiones por semana y por modo;
  - el top 5 de pasos con más fallos en Assessment, con enlace a Pasos.
- **AC-9:** Given una BD sin sesiones, Then cada bloque dice "Sin datos para este período"
  y no hay errores.

**Operadores y detalle**
- **AC-10:** Given 120 operadores, When abre Operadores, Then ve 50 por página y puede
  buscar por nombre o Code1 y filtrar por estado y área.
- **AC-11:** Given un operador, When abre su detalle, Then ve:
  - su estado por área;
  - sus sesiones con fecha (hora de la planta), área, modo, estado, puntaje, duración e
    intentos fallidos;
  - sus pasos a reforzar.
- **AC-12:** Given un operador con 3 sesiones de Guided, 1 Assessment reprobado y 1
  aprobado en LoFlo, Then su curva de LoFlo muestra las 5 sesiones en orden, con sus
  intentos fallidos, y dice "3 sesiones de práctica antes del primer aprobado".
- **AC-13:** Given el detalle de un operador, When elige la ficha imprimible, Then obtiene
  una ficha (impresa o en PDF, según DA-6) con estado y puntajes por área, sesiones y pasos
  a reforzar, sin menú ni botones.

**Pasos**
- **AC-14:** Given el filtro Assessment, Then Pasos lista por área los pasos ordenados por
  intentos fallidos, con:
  - las veces que terminaron sin acierto;
  - los motivos y acciones;
  - la mediana de duración.
- **AC-15:** Given el filtro en Práctica libre, Then aparece la columna de saltos, que no
  suma a los intentos fallidos.

**Seguimiento**
- **AC-16:** Given un operador que entrenó Redefinity, no lo aprobó y lleva 15 días sin
  sesiones, Then aparece en Seguimiento con el motivo S-2.
- **AC-17:** Given un operador que terminó sin acierto el mismo paso en 2 Assessment, Then
  aparece con S-3 y el nombre del paso.

**Exportar**
- **AC-18:** Given los filtros aplicados en Operadores, Sesiones, Pasos o Seguimiento, When
  presiona Exportar, Then descarga un `.xlsx` con esas filas y columnas, fechas en la hora de
  la planta y encabezados en español. Con 0 filas, solo los encabezados.

**Rendimiento**
- **AC-19:** Given la BD de prueba con unos 1600 operadores y un año de datos generados, Then
  cada vista responde en menos de 3 s.

## Decisiones que propuso Arquitectura y Planeación (confirmadas por el usuario, 2026-10-22)

Las preguntó el usuario una por una antes de aprobar:

- **DA-1 ✔:** el estado usa todo el historial, no el período elegido.
- **DA-2 ✔:** la duración del paso se mide desde el último intento del paso anterior, y en
  Pasos se muestra la mediana, no el promedio.
- **DA-3 ✔:** la lista de Seguimiento ignora el período (a), y S-3 se cuenta por paso de
  una misma guía, no por tipo de paso (b).
- **DA-4 ✔:** el top 5 del Resumen mezcla áreas y cada paso lleva el nombre de su área; con
  el filtro de área, se ve el top 5 de una sola.
- **DA-5 ✔:** Excel exporta lo filtrado, no todo.
- **DA-6 → diferida:** cómo se genera la ficha se decide al implementarla (ver "Deferred
  Decisions").
- **DA-7 ✔:** índices nuevos y un script de datos de prueba en `Database/Ops/` para medir
  AC-19.
- **DA-8 ✔:** hora de la planta (`SessionHelper.ToLocal`), formato `dd/MM/yyyy HH:mm`, y
  las semanas de la gráfica empiezan el lunes.
- **DA-9 ✔:** los administradores no aparecen como operadores. Según el usuario,
  "los administradores/trainers son los que analizan y monitorean todo, no tienen por qué
  hacer entrenamiento".

## Deferred Decisions

| Decisión | Fallback elegido ahora | Se revisa cuando |
|---|---|---|
| Área del operador (no de la guía) | Se agrupa por el área de la guía practicada; quien nunca entrenó no aparece por área | Training pida ver quién de cada área falta por entrenar ("por guía ahora, área después") |
| Regla de "Aprobado" | Aprobó alguna vez (≥ 0.70), en `ReadinessPolicy` | Training o negocio cambie el criterio: se cambia solo la política |
| Rol Trainer (ve reportes, no gestiona usuarios) | Solo Administrator tiene `CanViewReports` | Training pida acceso sin administrar cuentas |
| DA-6: cómo se genera la ficha imprimible | Ninguno: Desarrollo **consulta al usuario antes de implementarla**, con las dos opciones (impresión desde el navegador, sin dependencias; o "Descargar PDF" en el servidor, con una librería más en AUD-29) | Al llegar a la entrega de la ficha. Es lo primero que se recorta si falta tiempo |

## Recorte si no alcanza el tiempo

Primero la **curva de aprendizaje** y la **ficha imprimible**. Se conservan el Resumen,
Operadores, Operador (sin curva ni ficha), Pasos, Seguimiento y Excel.

## Open Questions

Ninguna. DA-1 a DA-9 están confirmadas o diferidas con su condición.
