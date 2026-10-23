# Spec 018: Dashboard de Training e historial por operador (Fase 10, T8)

**Status:** Approved (2026-10-22, usuario: "apruebo la spec") · decisiones D-136 a D-142
en `docs/decisions.md`
**Date:** 2026-10-22 · once rondas de preguntas con el usuario el 2026-10-22: siete de diseño,
la confirmación de DA-1 a DA-9 y cuatro de revisión, porque el usuario pidió "no dejar dudas
pendientes"

> **Enmienda 1 (2026-10-23, usuario, al probar la E3; D-147):** con la guía en **"Todas"**,
> Pasos y el top 5 del Resumen **agrupan por tipo de paso**, no por guía.
> - **Pasos (In 4, AC-17):** con "Todas" sale una sola tabla, "Todas las guías", con un renglón
>   por paso agrupado por el nombre de `StepNames.Display`. Así, "Line Clearance (apertura)" y
>   "(cierre)" y los tres Log NC siguen separados. El %, los conteos y la mediana salen de
>   todas las sesiones juntas, no de promediar guías. Con una guía elegida, su ranking, como
>   antes.
> - **Top 5 del Resumen (In 1, AC-9; reemplaza DA-4):** con "Todas", es el top 5 de esa misma
>   agrupación, sin el nombre de la guía, así sus números coinciden con los de Pasos al seguir el
>   enlace. Con una guía elegida, el top 5 de esa guía.
> - **Excel de Pasos (In 6, DA-5):** trae la misma vista que la pantalla con sus filtros.
> - El usuario eligió agrupar por tipo de paso entre tres opciones: una tabla mezclada con
>   columna Guía, agrupar por tipo de paso o un top mezclado.

> **Enmienda 2 (2026-10-23, usuario, con la medición de Desarrollo en la E6; D-148):** AC-24
> tiene **una excepción**: la descarga de sesiones del Resumen (hojas Sesiones e Intentos por
> paso) con el período **"Todo"**.
> - **Medido:** con un año de datos de prueba (~22 000 sesiones, ~264 000 filas de Intentos por
>   paso, 10 MB), ClosedXML tarda ~16 s en total. Con 30 días tarda ~1 s y cumple.
> - **Ahora:** se acepta que tarde más. La página avisa, junto al botón, que con "Todo" la
>   descarga puede tardar.
> - **En la E7:** se prueba escribir esa descarga en streaming (`OpenXmlWriter`, de
>   `DocumentFormat.OpenXml`, que ya está instalado), con las mismas hojas y columnas. Sobre
>   todo para no cargar el archivo entero en la memoria del servidor: un reciclado del App Pool
>   por memoria cerraría las sesiones de entrenamiento en curso (Session InProc). Si con
>   streaming tampoco llega a 3 s, queda la excepción.
> - Descartado: con "Todo", descargar solo la hoja Sesiones (cambiaba lo que se entrega).

## Problem

Training, gerencia y el desarrollador no ven cómo les va a los operadores en el simulador:
quién está listo para producción, en qué pasos se falla y si el entrenamiento avanza. Los
datos ya se guardan (sesiones; intentos por paso con hora, motivo y acción, spec 013), pero
solo se ven en el resumen final de cada sesión. T8 (revisión con Ernesto, 2026-10-09) pidió
el historial por operador para ver dónde fallan antes de producción. La Fase 10 entra en la
versión final de la 2.ª semana de noviembre (D-129), junto con la E2 de la Fase 9 (D-135).

## Usuarios y preguntas que responde

- **Quién lo mira:** gerencia, trainers y el desarrollador, solo con la capacidad nueva
  `CanViewReports`, que hoy tiene solo Administrator. Los administradores y trainers
  analizan y monitorean; no entrenan. **El operador no ve su historial.**
- **Tres preguntas, con el mismo peso:**
  1. ¿Este operador está listo? → Operadores, Operador y Seguimiento.
  2. ¿Dónde se falla más? → Pasos y el top 5 del Resumen.
  3. ¿Cómo avanza el entrenamiento? → Resumen.
- **Propuestas de valor para Training** que eligió el usuario: estado "listo", pasos con más
  fallos, ficha imprimible, exportar a Excel, curva de aprendizaje y lista de seguimiento.

## Scope

### In

Cinco vistas en `Pages/Admin`, con un menú en `Admin.Master`: **Resumen · Operadores ·
Pasos · Seguimiento · Usuarios**. Desde Operadores, Pasos o Seguimiento se llega al detalle
de un **Operador**. Los textos van en español, como "Gestión de usuarios". Está pensado para
escritorio: en un celular se ve, pero no se optimiza.

1. **Resumen** (gerencia). Es el **inicio del administrador**: `AdminHomeUrl` pasa de
   `Users.aspx` al Resumen. Cada bloque dice a qué período corresponde.
   - **Operadores por estado y guía** (Aprobado, Reprobado, En entrenamiento). Usa todo el
     historial, como foto de hoy.
   - **Tasa de aprobación por guía**, del período, con dos números:
     - % de operadores aprobados entre los que completaron algún Assessment;
     - % aprobados en su primer Assessment.

     Se acompaña del **puntaje promedio** de los Assessment completados.
   - **Actividad:** una gráfica de sesiones por semana (de lunes a domingo) y por modo, del
     período.
   - **Top 5 de pasos** con mayor % de sesiones con fallo en Assessment, del período. Junta
     todas las guías, y cada paso lleva el nombre de su guía. Con el filtro de guía, se ve el
     top 5 de una sola. Tiene enlace a Pasos.
   - **Descargar sesiones** (ver 6).
2. **Operadores:**
   - lista con el estado por guía, el mejor y el último puntaje de Assessment y la
     **última actividad** (la fecha de inicio de su sesión más reciente);
   - incluye a quienes **nunca entrenaron**, con estado general **Sin iniciar** y un filtro
     para verlos;
   - búsqueda por nombre o Code1, filtro por estado y por guía, y **50 por página, paginada
     en el servidor**.
3. **Operador** (detalle):
   - su estado por guía;
   - sus sesiones: fecha, guía, modo, estado, puntaje, duración e intentos fallidos;
   - sus pasos a reforzar;
   - su **curva de aprendizaje** por guía;
   - su **ficha imprimible**.
4. **Pasos:** el ranking de pasos por guía, **ordenado por el % de sesiones con fallo**,
   con:
   - los intentos fallidos totales;
   - las veces que el paso terminó sin acierto;
   - sus motivos y acciones registrados;
   - la mediana de su duración;
   - las veces que se saltó, cuando el filtro de modo incluye Práctica libre.
5. **Seguimiento:** los operadores que requieren atención, por guía, con todas las reglas
   que cumplen.
6. **Exportar a `.xlsx`** (ClosedXML), con los filtros aplicados (DA-5):
   - desde **Operadores**: la lista;
   - desde **Pasos**: el ranking con sus motivos;
   - desde **Seguimiento**: la lista con los motivos;
   - desde el **Resumen**: **sesiones**, en un `.xlsx` con dos hojas, **Sesiones** (una
     fila por sesión) e **Intentos por paso** (una fila por sesión y paso, con los intentos,
     el resultado y la duración).
7. **Gráficas con Chart.js**, guardado en `Scripts/` sin CDN, como JsBarcode. Se suma al
   inventario de AUD-29.
8. **Filtros:**
   - **período**, con los últimos 30 días al entrar y la opción "Todo". Filtra por la fecha
     de inicio de la sesión;
   - **guía**, con "Todas";
   - **modo**, solo en Pasos: **Assessment** (al entrar) o **Práctica libre**, uno a la vez.
     Guided no se ofrece porque casi no tiene fallos.

   El estado y la lista de Seguimiento ignoran el período (DA-1, DA-3).
9. **Capacidad `CanViewReports`** en `AccessPolicy`. `Web.config` y cada página y descarga
   la revisan.
10. **Aviso al borrar un usuario** (`Users.aspx`, spec 013). La confirmación dice cuántas
    sesiones se van a perder y que los indicadores del dashboard cambian. La regla de la
    spec 013 (borrar con su historial) no cambia.

### Out

- Que el operador vea su propio historial.
- El área del operador, distinta de la guía (ver "Deferred Decisions").
- El re-entrenamiento por vencimiento.
- Abandonar sesiones como regla de seguimiento.
- Marcar casos de seguimiento como atendidos: el dashboard es solo lectura.
- El detalle por paso en el resumen final del operador (sigue la deuda de la spec 007).
- Líneas de firma en la ficha. La ficha **no** es un registro oficial de entrenamiento: el
  simulador no es un sistema validado.
- Registrar quién descarga qué.
- Caché: los datos son en vivo.
- Las guías no publicadas.
- Cambiar cómo se entrena o se califica; tablas nuevas.
- Un rol Trainer: la capacidad nueva lo deja preparado, pero no se crea.

## Domain Model

Sin tablas nuevas. Se agregan **índices** para las consultas por operador, por guía y por
fecha (ver "Non-Functional").

- **La unidad es la guía.** El estado, la tasa, los pasos y el seguimiento se calculan **por
  guía publicada** (`TrainingGuides.IsPublished = 1`). Hoy hay una guía por área (Moldeo,
  LoFlo y Redefinity), así que se ve como "por área". Si un área llega a tener varias
  guías, cada una tiene su propio estado. Las guías **no publicadas no aparecen nunca**, ni
  sus sesiones.
- **Operadores = usuarios con rol Operator** (DA-9). Los administradores no aparecen. Un
  usuario borrado se lleva su historial (spec 013), así que tampoco aparece.
- **Estado por operador y guía.** La regla vive solo en una política de la BLL, la
  **`ReadinessPolicy`**, como `ModePolicy` y `AccessPolicy`. El dashboard, el Excel y la
  ficha le preguntan el estado y nunca repiten la regla. El usuario avisó que la regla
  puede cambiar.
  - **Aprobado:** al menos un Assessment *completado* de esa guía con `FinalScore` ≥
    `ScoreCalculator.PassingScore` (0.70).
  - **Reprobado:** tiene Assessment completados de esa guía y ninguno aprobado.
  - **En entrenamiento:** sesiones de esa guía, pero sin Assessment completados (solo
    Guided, Práctica libre o Assessment en curso o abandonado).
  - **Sin iniciar** (estado general del operador): ninguna sesión en ninguna guía
    publicada.
  - El estado usa **todo el historial**, sin importar el período (DA-1).
- **Sesiones abandonadas o en curso:** se listan con su estado. No cuentan como reprobado
  ni como "primer Assessment", y no entran en la tasa ni en el puntaje promedio. Sus intentos
  fallidos sí cuentan en Pasos. Una sesión en curso no tiene duración ("—").
- **Tasa de aprobación** (guía × período):
  - **% de operadores aprobados:** entre los operadores con al menos un Assessment
    completado de la guía en el período, los que tienen al menos uno aprobado en el período;
  - **% aprobados al primer intento:** entre los operadores cuyo **primer** Assessment
    completado de esa guía (en todo su historial) cae en el período, los que lo aprobaron;
  - **puntaje promedio:** el promedio de `FinalScore` de los Assessment completados de la
    guía en el período;
  - sin denominador, "—" (nunca 0 %).
- **Pasos:**
  - **% de sesiones con fallo:** entre las sesiones del período, el modo y la guía que
    tienen algún intento en el paso, las que tienen al menos un intento fallido en él;
  - **intentos fallidos:** `IsCorrect = 0` sin contar los saltos;
  - **terminó sin acierto:** avance forzado en Assessment, o saltado con fallos en
    Práctica libre;
  - **saltos** (`Reason = Skipped`, Práctica libre): se cuentan aparte y nunca como fallo;
  - **motivos:** los de `AttemptReason` (fuera de secuencia, decisión incorrecta, acción
    inválida, falta selección, campo vacío) con su acción;
  - el **nombre** del paso sale de `StepNames.Display`, como en el resumen final.
- **Duración** (DA-2):
  - **de la sesión:** `CompletedAt − StartedAt`;
  - **de un paso:** desde el último intento del paso anterior (o desde `StartedAt` en el
    primero) hasta el último intento del paso;
  - en Pasos se muestra la **mediana**.
- **Lista de seguimiento.** Es por operador y guía, y sus umbrales son constantes de la BLL.
  Ignora el período (DA-3a). Un operador **sale automáticamente al aprobar esa guía**, con
  todas sus reglas.
  - **S-1:** reprobó **2 o más** Assessment de la guía sin aprobar ninguno;
  - **S-2:** tiene sesiones en la guía, no la aprobó y lleva **14 días** sin sesiones **de
    esa guía**, aunque practique otras;
  - **S-3:** el **mismo paso de la guía** terminó sin acierto en **2 o más** Assessment, y
    todavía no la aprobó (DA-3b).
- **Curva de aprendizaje** (operador × guía):
  - una gráfica con sus sesiones de **Práctica libre y Assessment** en orden (el color
    indica el modo) y los intentos fallidos de cada una, con el puntaje de cada Assessment.
    **Guided no entra en la gráfica**: la guía evita los errores, así que casi siempre
    mostraría 0;
  - el dato: cuántas sesiones de práctica (**Guided y Práctica libre**) hizo antes de su
    primer Assessment aprobado.
- **Ficha imprimible:** estado y puntajes por guía, sesiones y pasos a reforzar. Cómo se
  genera se decide al implementarla (DA-6, ver "Deferred Decisions").
- **Horas** (DA-8): la base guarda en UTC y se muestran en la hora de la planta, con
  `SessionHelper.ToLocal`, en formato `dd/MM/yyyy HH:mm`.

## API Contract

No hay API pública: son páginas Web Forms en `Pages/Admin`, siguiendo las capas de
`CLAUDE.md`.

- **SPs nuevos de solo lectura que agregan sin reglas** (decisión del usuario: "SP agregan,
  BLL decide"). Devuelven, por ejemplo, las sesiones con su conteo de intentos fallidos o
  los fallos por paso. Ninguno decide aprobado, estado, tasa ni seguimiento.
- **BLL:**
  - un servicio del dashboard aplica las reglas (`ReadinessPolicy`, la tasa y las reglas de
    seguimiento) y arma las vistas. Verifica `CanViewReports` en cada método;
  - las reglas son funciones puras sobre datos en memoria, para probarlas con tests sin BD.
- **Excel:** un `IHttpHandler` (`.ashx`) o la descarga desde un postback devuelve el
  `.xlsx`, sin `[WebMethod]`. Verifica la capacidad igual que las páginas.
- **Borrar un usuario:** la BLL de gestión de usuarios informa cuántas sesiones tiene el
  usuario, para el aviso.

## Authorization

- `AccessPolicy.CanViewReports`, que hoy solo es `true` para Administrator.
- `Web.config` protege las páginas nuevas, y cada página y la descarga lo revisan otra vez.
  Sin la capacidad, `SessionHelper.RedirectHome`, nunca el login (`CLAUDE.md` → Auth).
- Solo lectura: el dashboard no modifica nada.
- Nombre y Code1 visibles en todas las vistas de personas y en el Excel: el usuario confirmó
  que no hay restricción de RRHH.

## Edge Cases & Failure Modes

- Sin datos (BD recién creada, período vacío): cada vista o bloque muestra "Sin datos para
  este período", sin gráficas rotas ni división por cero.
- Un operador con sesiones en varias guías: un estado por cada guía que entrenó.
- Una guía que se despublica: sus sesiones dejan de verse en todas las vistas y descargas.
- Una sesión en curso con días de antigüedad: sigue "En curso". S-2 cuenta los días desde
  su inicio.
- Un paso de una guía existente no cambia de estructura: `spTrainingGuideSync` lo impide
  (`docs/process.md` §6).
- Exportar con 0 filas: un `.xlsx` con solo encabezados.
- Si **ClosedXML** no funciona en .NET Framework 4.7.2 con `packages.config`: **se para y
  decide el usuario** (no hay plan B automático).
- Borrar un usuario con sesiones: el aviso dice cuántas se pierden. Uno sin sesiones: la
  confirmación de siempre.

## Non-Functional Requirements

- **Volumen de referencia:** unos 1600 operadores posibles, con un año de datos (del orden
  de decenas de miles de sesiones y cientos de miles de intentos).
- **Meta:** cada vista y cada descarga responden en **menos de 3 s** con ese volumen. Los
  datos se consultan en vivo, sin caché.
- **Índices nuevos:** sesiones por operador y por guía con fecha. Se verifica con datos
  generados en una BD de LocalDB descartable, con un script de prueba en `Database/Ops/`
  (DA-7).
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
- **AC-2:** Given un Operator, When abre cualquier URL del dashboard o de una descarga, Then
  vuelve a su inicio (`RedirectHome`), no al login, y no recibe datos.

**Estado** (tests de `ReadinessPolicy`, además del navegador)
- **AC-3:** Given un operador con un Assessment completado de LoFlo con 0.70, Then LoFlo =
  Aprobado.
- **AC-4:** Given un operador con dos Assessment completados de Moldeo con 0.55 y 0.65, Then
  Moldeo = Reprobado y aparece en Seguimiento por S-1.
- **AC-5:** Given un operador que aprobó Moldeo con 0.80 y después reprobó con 0.60, Then
  Moldeo sigue Aprobado, con último 0.60 y mejor 0.80, y no está en Seguimiento.
- **AC-6:** Given un operador con solo Guided, o con un Assessment abandonado o en curso,
  Then el estado es En entrenamiento, y esas sesiones no entran en la tasa ni en el
  promedio.
- **AC-7:** Given un operador registrado sin sesiones, Then aparece en Operadores como Sin
  iniciar y el filtro "Sin iniciar" lo muestra.
- **AC-8:** Given que se cambia la regla de `ReadinessPolicy`, Then el estado cambia en
  todas las vistas, el Excel y la ficha sin tocar nada más (tests de la política).

**Resumen**
- **AC-9:** Given el período de 30 días, Then el Resumen muestra:
  - operadores por estado y guía, con todo el historial;
  - del período: la tasa de operadores aprobados, la tasa al primer intento y el puntaje
    promedio por guía;
  - del período: la gráfica de sesiones por semana (desde el lunes) y por modo;
  - del período: el top 5 de pasos con mayor % de sesiones con fallo en Assessment, con el
    nombre de la guía y el enlace a Pasos.

  Cada bloque indica su período.
- **AC-10:** Given 3 operadores cuyo primer Assessment de LoFlo cae en el período, y 2 de
  ellos lo aprobaron, Then "aprobados al primer intento" de LoFlo = 67 %.
- **AC-11:** Given una BD sin sesiones, Then cada bloque dice "Sin datos para este período"
  y las tasas muestran "—", sin errores.
- **AC-12:** Given una guía no publicada con sesiones, Then no aparece en ninguna vista ni
  descarga.

**Operadores y detalle**
- **AC-13:** Given 120 operadores, When abre Operadores, Then ve 50 por página y puede
  buscar por nombre o Code1 y filtrar por estado y guía. La última actividad es la fecha de
  su sesión más reciente.
- **AC-14:** Given un operador, When abre su detalle, Then ve:
  - su estado por guía;
  - sus sesiones con fecha (hora de la planta, `dd/MM/yyyy HH:mm`), guía, modo, estado,
    puntaje, duración e intentos fallidos;
  - sus pasos a reforzar.
- **AC-15:** Given un operador con 3 sesiones de Guided, 1 de Práctica libre, 1 Assessment
  reprobado y 1 aprobado en LoFlo, Then su curva de LoFlo muestra 3 puntos (Práctica libre y
  los 2 Assessment) en orden, con sus intentos fallidos, y dice "4 sesiones de práctica antes
  del primer aprobado".
- **AC-16:** Given el detalle de un operador, When elige la ficha imprimible, Then obtiene
  una ficha (impresa o en PDF, según DA-6) con estado y puntajes por guía, sesiones y pasos
  a reforzar, sin menú ni botones.

**Pasos**
- **AC-17:** Given el filtro Assessment, Then Pasos lista por guía los pasos ordenados por
  % de sesiones con fallo, con:
  - los intentos fallidos totales;
  - las veces que terminó sin acierto;
  - los motivos y acciones;
  - la mediana de duración;
  - el nombre de `StepNames.Display`.
- **AC-18:** Given el filtro en Práctica libre, Then aparece la columna de saltos, que no
  suma a los intentos fallidos ni al % de sesiones con fallo.

**Seguimiento**
- **AC-19:** Given un operador que entrenó Redefinity, no la aprobó y lleva 15 días sin
  sesiones de Redefinity (aunque practicó Moldeo ayer), Then aparece con S-2 en Redefinity.
- **AC-20:** Given un operador que terminó sin acierto el mismo paso de LoFlo en 2 Assessment
  y no aprobó LoFlo, Then aparece con S-3 y el nombre del paso. When después aprueba LoFlo,
  Then sale de Seguimiento.

**Exportar**
- **AC-21:** Given los filtros aplicados en Operadores, Pasos o Seguimiento, When presiona
  Exportar, Then descarga un `.xlsx` con esas filas y columnas, fechas en la hora de la planta
  y encabezados en español. Con 0 filas, solo los encabezados.
- **AC-22:** Given el período y la guía del Resumen, When descarga las sesiones, Then el
  `.xlsx` tiene las hojas Sesiones e Intentos por paso con esos filtros.

**Borrar usuario**
- **AC-23:** Given un operador con 7 sesiones, When el admin presiona Borrar, Then la
  confirmación avisa que se pierden 7 sesiones y que los indicadores del dashboard cambian.

**Rendimiento**
- **AC-24:** Given la BD de prueba con unos 1600 operadores y un año de datos generados, Then
  cada vista y cada descarga responden en menos de 3 s. *(Excepción: la descarga de sesiones
  del Resumen con "Todo", enmienda 2, D-148.)*

## Decisiones que propuso Arquitectura y Planeación (confirmadas por el usuario, 2026-10-22)

Las preguntó el usuario una por una antes de aprobar:

- **DA-1 ✔:** el estado usa todo el historial, no el período.
- **DA-2 ✔:** la duración del paso se mide desde el último intento del paso anterior, y en
  Pasos se muestra la mediana.
- **DA-3 ✔:** la lista de Seguimiento ignora el período (a), y S-3 se cuenta por paso de
  una misma guía (b).
- **DA-4 ✔:** el top 5 mezcla guías, con el nombre de cada una, y el filtro de guía muestra
  el de una sola. *(Superada por la enmienda 1, D-147: con "Todas", agrupa por tipo de paso.)*
- **DA-5 ✔:** Excel exporta lo filtrado.
- **DA-6 → diferida:** cómo se genera la ficha se decide al implementarla. *(Decidida el
  2026-10-23, D-150: impresión desde el navegador, con estilos de impresión; el PDF sale del
  "Guardar como PDF" del navegador.)*
- **DA-7 ✔:** índices nuevos y un script de datos de prueba para medir AC-24.
- **DA-8 ✔:** hora de la planta, formato `dd/MM/yyyy HH:mm`, y las semanas empiezan el
  lunes.
- **DA-9 ✔:** los administradores no aparecen como operadores.

En la revisión final (cuatro rondas más) el usuario decidió además:
- qué bloques del Resumen siguen el período;
- la tasa por operador y al primer intento;
- el orden de Pasos por % de sesiones con fallo;
- la curva sin Guided;
- que quien nunca entrenó aparezca como Sin iniciar;
- S-2 por guía;
- la salida automática de Seguimiento;
- que la última actividad sea la última sesión;
- que no hay restricción de RRHH y que no se registran las descargas;
- el aviso al borrar un usuario;
- los datos en vivo;
- el estado por guía y que las guías no publicadas no aparezcan;
- la descarga de sesiones desde el Resumen, con dos hojas;
- los supuestos de presentación: español, escritorio, el período por la fecha de inicio y
  `StepNames`;
- el filtro de modo de Pasos: solo Assessment o Práctica libre.

## Deferred Decisions

| Decisión | Fallback elegido ahora | Se revisa cuando |
|---|---|---|
| Área del operador (no de la guía) | Se agrupa por guía; quien nunca entrenó aparece solo como Sin iniciar general | Training pida ver quién de cada área falta por entrenar ("por guía ahora, área después") |
| Regla de "Aprobado" | Aprobó alguna vez (≥ 0.70), en `ReadinessPolicy` | Training o negocio cambie el criterio: se cambia solo la política |
| Rol Trainer (ve reportes, no gestiona usuarios) | Solo Administrator tiene `CanViewReports`; borrar avisa del impacto | Training pida acceso sin administrar cuentas |
| DA-6: cómo se genera la ficha imprimible | Ninguno: Desarrollo **consulta al usuario antes de implementarla**, con las dos opciones (impresión desde el navegador, sin dependencias; o "Descargar PDF" en el servidor, con una librería más en AUD-29) | Al llegar a la entrega de la ficha. Es lo primero que se recorta si falta tiempo |

## Recorte si no alcanza el tiempo

Primero la **curva de aprendizaje** y la **ficha imprimible**. Se conservan el Resumen,
Operadores, Operador (sin curva ni ficha), Pasos, Seguimiento, el aviso al borrar y Excel.

## Open Questions

Ninguna. DA-6 está diferida con su condición, y el resto está confirmado por el usuario.
