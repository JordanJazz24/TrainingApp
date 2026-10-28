# Briefs por pantalla para Claude Design (spec 022)

**Spec:** `022-mockups-por-pantalla.md` (D-202 a D-206). Lo aprobado va a `022-design/` con el
nombre de cada brief, pero como **`<archivo>.md` (lista de cambios)**, no como `.html`
(enmendado 2026-10-28, D-227). Las capturas PNG de la versión final las agrega Desarrollo en la
entrega (D-240).

Orden de diseño (P-1): Login → Cambio de contraseña → Guías → Pasos → Seguimiento →
Operadores → Operador → Usuarios → Inicio del operador. El Login fija los cambios al sistema
de diseño.

## Cómo usarlos (enmendado 2026-10-27: Claude Design con el repo conectado)

El usuario probó que Claude Design lee el repo (`JordanJazz24/TrainingApp`) y usa skills. Por
eso ya no se pegan los briefs ni se adjunta `admin.css`:

- **Rama a leer:** `TrainingApp` (cuando la sesión local integre la última versión de esta
  rama); mientras tanto, `cloud/022-mockups-por-pantalla`.
- **Solo lectura:** Claude Design no escribe en el repo. La exportación aprobada se la pasa el
  usuario a la sesión de la nube, o la sube él en local (spec → "Aprobación").
- **Prompt corto por pantalla** (cambiar N y el nombre):

```text
Lee del repo (rama TrainingApp, solo lectura) docs/specs/022-briefs.md: el "Contexto común"
y el brief N (<pantalla>). Lee también los archivos del repo que ese brief indica, más
TrainingApp/UI/master/admin.css y docs/specs/019-ui-ux-dashboard.md. Si ya hay exportaciones
aprobadas en docs/specs/022-design/, respétalas. Usa la skill frontend-design, siempre dentro
de las restricciones del brief. Entrégame primero la pantalla actual recreada (para comparar)
y después las 3 alternativas.
```

- **Sigue adjuntándose a mano** (no está en el repo): el mockup v1 (exportación o capturas del
  canvas "Mockup Fase 11 · Dashboard de Training",
  https://claude.ai/artifact/Uxqr9utFzd8w6uBgp3EYYd: Sistema de diseño, Resumen y Resumen en
  oscuro) y las **capturas actuales** de la pantalla que indica cada brief (lo que se ve con la
  app corriendo y datos reales).
- **Sin repo conectado:** pegar el contexto común y el brief, y adjuntar `admin.css`.

### Prompt de handoff (enmendado 2026-10-28, D-226)

Cuando el usuario aprueba una alternativa, Claude Design puede mandarla a una sesión de la nube
que la implementa. Pegar este prompt junto con el envío (cambiar `<pantalla>` y N):

```text
Contexto: proyecto MESTrainingSimulator (repo JordanJazz24/TrainingApp, ASP.NET Web Forms en
.NET Framework 4.7.2, Bootstrap 5.3.8 e íconos locales). Este diseño es la pantalla <pantalla>
de la spec 022 (docs/specs/022-mockups-por-pantalla.md, plan 022-plan.md, brief N en
022-briefs.md). Lee CLAUDE.md y esa spec antes de tocar nada.

Cómo trabajar:
1. Crea la rama cloud/022-<pantalla> desde origin/TrainingApp. Haz push SOLO a esa rama; nunca
   a TrainingApp ni a main.
2. Implementa el diseño en Web Forms con los tokens --mt-* de TrainingApp/UI/master/admin.css.
   Nada de React ni recursos externos; los mismos controles de servidor, ids, postbacks,
   anclas data-guide y confirmaciones; sin lógica en el code-behind. Si el diseño pide cambiar
   comportamiento (otro control, otro flujo de errores, datos nuevos), hazlo solo si es
   presentación y anótalo en la lista de cambios.
3. No puedes compilar ni probar: marca el commit como "no verificada". Lo verifica la sesión
   Desarrollo en local (Rebuild, aspnet_compiler, /code-review) y el usuario lo prueba.
4. Agrega en la misma rama docs/specs/022-design/<pantalla>.md con: la alternativa elegida,
   los cambios frente al v1 (tokens nuevos o con otro valor en claro y oscuro, componentes
   nuevos o modificados, estructura, comportamiento y textos) y lo que Desarrollo debe revisar
   porque no se compiló. No agregues capturas (las toma Desarrollo de la versión final) ni
   subas el HTML exportado.
5. No numeres decisiones (D-n) ni edites decisions.md, la spec, el plan, roadmap.md ni
   status.md: eso lo hace la sesión de planeación de la 022.
6. Al terminar, dile al usuario la rama, el commit y un resumen de la lista de cambios.
```

### Archivos del repo por pantalla (rutas bajo `TrainingApp/`)

Todas: `UI/master/admin.css`, `UI/master/theme.js` y el master de la pantalla
(`UI/master/Admin.Master` o `UI/master/Site.Master`, con su `.cs`).

| # | Pantalla | Archivos (cada `.aspx` con su `.aspx.cs`) |
|---|---|---|
| 1 | Login | `Pages/Auth/Login.aspx` |
| 2 | Cambio de contraseña | `Pages/Auth/ChangePassword.aspx` |
| 3 | Guías | `Pages/Admin/Guides.aspx`, `Pages/Admin/GuideListRow.cs` |
| 4 | Pasos | `Pages/Admin/Steps.aspx`, `Pages/Admin/StepRows.cs` |
| 5 | Seguimiento | `Pages/Admin/FollowUp.aspx`, `Pages/Admin/FollowUpListRow.cs`, `BLL/Reports/FollowUpRules.cs` |
| 6 | Operadores | `Pages/Admin/Operators.aspx`, `Pages/Admin/OperatorListRow.cs`, `Pages/Admin/ReportText.cs` |
| 7 | Operador | `Pages/Admin/Operator.aspx`, `Pages/Admin/OperatorDetailRows.cs`, `UI/master/admin-charts.js` |
| 8 | Usuarios | `Pages/Admin/Users.aspx`, `Pages/Admin/UserListRow.cs` |
| 9 | Inicio del operador | `Pages/Training/StartSession.aspx` |

---

## Contexto común (pegar primero)

```text
Contexto: MESTrainingSimulator, plataforma interna para entrenar y evaluar a operadores de
manufactura médica en flujos de SAP MES. Hay dos roles: Operator (entrena) y Administrator
(gerencia y trainers: ven el dashboard y administran usuarios y guías). Se usa en escritorio,
proyectado en reuniones y en laptop o tablet en planta (desde 768 px).

Te adjunto el sistema de diseño actual (admin.css con tokens --mt-* claro y oscuro) y el
mockup v1 aprobado (Resumen). Son el PUNTO DE PARTIDA, no un límite: puedes proponer cambios,
pero MÁRCALOS en cada alternativa (token nuevo, valor distinto, componente nuevo o modificado).

Restricciones fijas:
- La app es ASP.NET Web Forms con Bootstrap 5.3.8 y Bootstrap Icons 1.13.2 locales. Nada de
  React ni otros frameworks, CDN ni fuentes web: fuente del sistema (Segoe UI y su pila).
  Gráficas solo con Chart.js (en el mockup puedes dibujarlas con HTML/SVG para mostrar el
  estilo).
- Claro y oscuro con un interruptor de tema; contraste WCAG AA en los dos; los campos con
  borde de al menos 3:1; ningún estado se distingue solo por el color (siempre ícono o texto).
  Estados: Aprobado (verde), Reprobado (rojo), En entrenamiento (ámbar), Sin iniciar (gris).
  Modos (otra paleta): Guided, Práctica libre, Assessment.
- Solo los datos que te indico. Si propones una cifra nueva, que salga de esos mismos datos
  (contar, agrupar, comparar) y márcala como "métrica derivada". Si necesitaría datos que no
  están, anótala aparte como "necesitaría datos nuevos".
- El comportamiento no cambia: mismos filtros, botones y acciones. Lo que se resuelve en el
  cliente (desplegar, abrir un modal) no recarga la página. No quites datos que hoy se
  muestran sin marcarlo.
- Las tablas se desplazan dentro de su tarjeta en tablet; nunca scroll horizontal de página.

Lo que no me convence de las pantallas actuales: siguen siendo tablas planas (lo importante
no salta a la vista), no dicen qué hacer (muestran datos pero no guían la decisión) y el
login se ve de plantilla.

Pido 3 ALTERNATIVAS que difieran en la ESTRUCTURA (qué va arriba, cómo se agrupa, dónde está
la acción principal), no solo en el color. Cada una con: una nota corta de qué decide el
usuario con ella, la lista de cambios respecto al v1 y las métricas derivadas que proponga.
Entrega cada alternativa en claro y en oscuro, con datos de ejemplo realistas (nombres en
español, Code1 de 5 a 6 dígitos, guías de Moldeo, LoFlo, Subensamble 1 y Redefinity). La
exportación final: un HTML autocontenido (CSS y SVG inline, sin recursos externos) con el
interruptor de tema funcionando.

Si tienes la skill frontend-design, úsala, pero siempre dentro de estas restricciones: lo que
proponga fuera de ellas (otra librería, fuentes web, animaciones pesadas) márcalo como cambio.
```

---

## 1. Login → `022-design/login.html`

**Capturas:** el login actual en claro y oscuro, y un error (SweetAlert).

```text
Pantalla: LOGIN (único para los dos roles).
Quién: operadores (a veces en tablet en planta) y administradores.
Qué decide: entrar.
Qué ve primero: dónde poner su código y su contraseña.

Datos y acciones:
- Código de empleado y Contraseña; botón Entrar (Enter también entra);
- interruptor de tema;
- los errores llegan del servidor (credenciales, cuenta bloqueada 15 minutos) y hoy se
  muestran en un SweetAlert.
El primer acceso de un empleado se hace con la contraseña por defecto, que luego lo lleva a
cambiarla (no lo expliques con un enlace de "registrarse": no existe como pantalla).

Lo que pido: IDENTIDAD. Hoy es una tarjeta centrada que se ve de plantilla. Explora
estructuras distintas (por ejemplo, panel dividido con el nombre de la app y para qué sirve,
una tarjeta con encabezado de marca, u otra idea), sin imágenes externas: solo CSS, SVG inline
e íconos de Bootstrap Icons. Sin colores de marca corporativa (no hay guía de marca).

Queda fuera: "olvidé mi contraseña" (lo resuelve un admin con un reset), recordarme,
registro.

Esta es la PRIMERA pantalla que se aprueba: los cambios de tokens y componentes que traiga la
alternativa elegida valdrán para toda la app. Por eso, en cada alternativa, junto al login,
muestra una muestra chica del sistema con esos cambios aplicados (botones, campos, una
tarjeta, una tabla corta con insignias de estado), en claro y en oscuro.
```

## 2. Cambio de contraseña → `022-design/cambio-contrasena.md`

> **Enmendado 2026-10-28:** con el Login aprobado (3b, D-220 a D-225) y lo que hoy hace la
> página (`ChangePassword.aspx` y su `.cs`, verificado en el código).

**Capturas:** la pantalla actual en primer acceso (con el aviso), sin el aviso y con un error
(SweetAlert), en claro y oscuro.

```text
Pantalla: CAMBIO DE CONTRASEÑA (la misma para los dos roles).
Quién: un usuario en su primer acceso o tras un reset de un admin (obligatorio: no puede usar
la app hasta cambiarla). Hoy no hay ningún enlace a esta pantalla en la app: solo se llega
obligado (o escribiendo la URL). No agregues un enlace: eso sería comportamiento nuevo.
Qué decide: su nueva contraseña.
Qué ve primero: por qué está aquí y los tres campos.

Datos y acciones (hoy):
- aviso solo en primer acceso o tras un reset: "Por seguridad, debes establecer una
  contraseña personal antes de continuar. Como contraseña actual, usa la misma con la que
  entraste.";
- Contraseña actual, Nueva contraseña y Confirmar contraseña (las tres obligatorias);
- validación en la página: la nueva con al menos 8 caracteres y la confirmación igual a la
  nueva (mensajes bajo cada campo);
- errores del servidor: "La contraseña actual no es correcta." y "La nueva contraseña no
  puede ser la contraseña por defecto." (hoy en un SweetAlert);
- Guardar → va al inicio de su rol (un operador sin área pasa antes por "Elige tu área");
  Cancelar → cierra la sesión y vuelve al login. Enter = Guardar.

Referencia de SISTEMA (no de estructura): el Login ya aprobado, alternativa 3b. Léelo en
docs/specs/022-design/login.md (tokens, componentes y decisiones D-220 a D-225) y en la rama
cloud/022-login-3b (TrainingApp/Pages/Auth/Login.aspx y el bloque "Login: panel dividido" de
TrainingApp/UI/master/admin.css). Puedes reutilizar sus piezas: el modo de pantalla completa
(FullBleed), el panel de marca, los campos con ícono y la línea de estado (.mt-status-line)
para los errores del servidor. Propón 3 estructuras distintas; al menos una que NO sea el
panel dividido. Marca lo que reutilizas del Login y lo que es nuevo.

Queda fuera: medidor de fuerza, reglas nuevas de contraseña (más allá de 8 caracteres),
mostrar u ocultar la contraseña, preguntas de seguridad, un enlace de "cambiar contraseña"
en otras pantallas.
```

## 3. Guías → `022-design/guias.html`

**Capturas:** la lista de guías publicadas con una en Custom.

```text
Pantalla: GUÍAS (administración).
Quién: administradores.
Qué decide: qué tipo de Resource Setup usa cada guía para las sesiones nuevas.
Qué ve primero: las guías publicadas y su tipo.

Datos y acciones:
- lista de guías publicadas: producto, área, título y su tipo como insignia
  (Estándar / Custom);
- por guía, un selector Estándar / Custom y Guardar, con confirmación (el cambio vale para
  las sesiones nuevas; la que está en curso no cambia);
- estado vacío: "No hay guías publicadas."

Queda fuera: publicar o despublicar, editar pasos o pesos, crear guías.
Es una pantalla corta (hoy 4 guías): no la infles; que se lea en un vistazo.
```

## 4. Pasos → `022-design/pasos.html`

> **Enmendado 2026-10-28 (spec 023, D-217):** Pasos se diseña **cuando la 023 esté cerrada** y
> recibe su control de período y la comparación por paso, ya implementados. El texto de abajo
> ya lo refleja; ver `023-filtro-periodo.md` (puntos 1 a 6b, P-5, P-16).

**Capturas:** una guía con su ranking en Assessment y en Práctica libre, con "Ver todos"
abierto, con un período que tenga comparación y otro con "Todo"; y el control abierto en
Personalizado….

```text
Pantalla: PASOS (ranking de pasos difíciles).
Quién: trainers.
Qué decide: qué paso enseñar mejor, y por qué falla.
Qué ve primero: el ranking con el % de sesiones con fallo como barra visual.

Datos (agrupados por guía; un ranking por guía):
- número en el ranking, nombre del paso;
- % de sesiones con fallo (barra) y "N de M sesiones";
- intentos fallidos, veces sin acierto, mediana de duración;
- solo en Práctica libre: los saltos;
- los 2 motivos de fallo más frecuentes, y "Ver todos" que despliega el resto SIN recargar;
- por paso, el CAMBIO del % con fallo contra el período anterior de igual largo: flecha ▲/▼,
  valor en puntos, referencia y base de los dos períodos (ej. "▲ 6 pts · vs. 1–28 sep. · 12
  vs. 9 sesiones"). Bajar es mejora; el color dice si mejora, pero nunca solo el color. Sin
  datos en el anterior: "sin datos para comparar". Con "Todo" no hay comparación.
Filtros: período, guía, modo (Assessment, Práctica libre), Filtrar; "Descargar" (Excel).
El PERÍODO es un control de 7 opciones: Hoy, Esta semana, Este mes (el de entrada), Mes
anterior, Últimos 30 días, Todo y Personalizado…; solo con Personalizado… aparecen Inicio y
Fin (selector de fecha nativo) y Aplicar. Debajo, el rango aplicado en palabras ("Del 1 al 27
de oct. de 2026").
Puedes reacomodar el control, el rango y la comparación, pero NO cambiar su comportamiento
(opciones, reglas de comparación, textos de base): ya están implementados (spec 023).

Queda fuera: editar pasos o guías, otras comparaciones además de la del período anterior,
datos por operador (eso es Operador).
```

## 5. Seguimiento → `022-design/seguimiento.html`

**Capturas:** con varias filas (operadores que cumplen 1, 2 y 3 reglas) y vacía.

```text
Pantalla: SEGUIMIENTO.
Quién: trainers.
Qué decide: a quién llamar y por qué; ir a su detalle.
Qué ve primero: quién necesita atención y por qué regla.

Datos (una fila por operador y guía; quien ya aprobó la guía no aparece en ella; sin
período):
- nombre y Code1 (enlace al detalle del operador), guía (nombre y título), insignia de
  estado, última sesión;
- las reglas que cumple, como chips con texto:
  S-1 "Reprobó N Assessment" (2 o más sin aprobar ninguno),
  S-2 "N días sin practicar" (14 o más sin sesiones de esa guía),
  S-3 "Mismo paso sin acierto: <paso>" (el mismo paso sin acierto en 2 o más Assessment).
Orden: primero quien cumple más reglas.
Filtro: guía y Filtrar; "Descargar" (Excel).

Queda fuera: registrar llamadas o notas, marcar como atendido, cambiar las reglas o umbrales.

Ideas permitidas como métrica derivada: cuántos casos hay por regla, cuántos cumplen 2 o más.
```

## 6. Operadores → `022-design/operadores.html`

**Capturas a adjuntar:** la lista con filtros vacíos, filtrada por "Reprobado" y una guía, en
claro y oscuro; si se puede, a 768 px.

```text
Pantalla: OPERADORES (lista del dashboard).
Quién: gerencia y trainers, en el trabajo diario.
Qué decide: a quién abrir en detalle; quién está listo y quién no, por guía.
Qué ve primero: el estado por guía de cada operador, sin leer texto.

Datos (por operador, solo rol Operator):
- nombre y Code1 (enlace a su detalle);
- su ÁREA asignada como "Producto · Área" (ej. "Sensores · LoFlo"), o "Sin área" si aún no
  la eligió (hoy es la columna que sigue al nombre; puedes reubicarla);
- por cada guía publicada (columnas dinámicas, hoy 4): insignia de estado (Aprobado,
  Reprobado, En entrenamiento, Sin iniciar) y sus puntajes de Assessment como texto;
- su última actividad (fecha y hora de la planta).
Filtros (barra de arriba): búsqueda por nombre o Code1, estado (Todos, Aprobado, Reprobado,
En entrenamiento, Sin iniciar), guía, y el botón Filtrar; además "Descargar" (Excel).
Paginación de 50 al pie. Los filtros viven en la URL.

Queda fuera: filtrar por área, "sin iniciar por área", acciones sobre el operador (no hay:
es solo lectura), gráficas nuevas.

Ideas permitidas como métrica derivada (de esta misma lista): cuántos operadores de la vista
están en cada estado, quién está reprobado en alguna guía, quién no tiene actividad reciente.
```

## 7. Operador (detalle y ficha) → `022-design/operador.html`

**Capturas:** un operador con varias guías y sesiones, en claro y oscuro, y la vista de
impresión (ficha).

```text
Pantalla: OPERADOR (detalle de una persona).
Quién: trainers (y gerencia en reuniones); se imprime como ficha.
Qué decide: si está listo en cada guía y qué tiene que reforzar.
Qué ve primero: su estado por guía y los pasos a reforzar.

Datos:
- encabezado: nombre, Code1, su ÁREA ("Producto · Área" o "Sin área"), que también sale en
  la ficha impresa; botones Volver e Imprimir;
- Estado por guía (una tarjeta por guía): nombre y título de la guía, insignia de estado,
  mejor puntaje, último puntaje y última actividad;
- Pasos a reforzar, agrupados por guía: paso, veces sin acierto, intentos fallidos y el motivo
  principal;
- Curva de aprendizaje por guía (Chart.js): sus puntajes de Assessment en el tiempo, más un
  texto de su práctica; "sin puntos" si no tiene;
- Sesiones (tabla): fecha de inicio, guía, modo, estado, puntaje, duración e intentos
  fallidos.
La ficha impresa sale SIEMPRE en claro, sin menú, botones ni curva.

Queda fuera: editar al operador o cambiarle el área (eso es Usuarios), comparar con otros
operadores, datos nuevos.

Muestra también cómo se ve la ficha impresa de cada alternativa.
```

## 8. Usuarios → `022-design/usuarios.html`

> **Enmendado 2026-10-27** (enmienda 1 de la spec 021, D-208): ya no existe "Quitar área". El
> área se cambia en su propia columna, con un lápiz; la columna Acciones tiene solo Resetear,
> Desbloquear y Borrar. El texto de abajo ya lo refleja.

**Capturas:** la lista con operadores y admins (uno bloqueado, uno que debe cambiar la
contraseña), y el formulario de crear, más el selector de área abierto con el lápiz (la 021
ya está en la app, 2026-10-27).

```text
Pantalla: GESTIÓN DE USUARIOS (administración).
Quién: administradores.
Qué decide: dar de alta, resolver accesos (reset, desbloqueo, borrar) y corregir el área de
un operador.
Qué ve primero: quién tiene un problema de acceso (bloqueado, debe cambiar contraseña).

Datos y acciones:
- Crear usuario: Code1 y rol (Operador, Administrador), botón Crear (la contraseña inicial es
  la por defecto; no se escribe);
- Buscar por nombre o Code1;
- lista paginada (Anterior / Siguiente): nombre y Code1, ROL como insignia, ESTADO de la
  cuenta como insignia (Activa, Bloqueada, Debe cambiar la contraseña), ÁREA ("Producto ·
  Área", "Sin área" o "—" para un admin), fecha de alta y último acceso;
- cambiar el área (solo operadores): en la columna Área, junto al texto, un lápiz que abre
  AHÍ MISMO el selector de áreas AGRUPADO POR PRODUCTO, con Guardar (con confirmación) y
  Cancelar, sin recargar hasta Guardar. No hay "quitar área". Las filas de administrador
  muestran "—" y no tienen lápiz;
- columna Acciones, cada una con confirmación: Resetear contraseña, Desbloquear (solo si está
  bloqueada) y Borrar (no sobre uno mismo; si tiene historial se avisa).

Queda fuera: editar nombre o Code1, cambiar el rol, filtrar por área, elegir el área en el
alta, historial de cambios.
```

## 9. Inicio del operador → `022-design/inicio-operador.html`

**Capturas:** el inicio actual (con la 021: el área como texto, el selector de guía y las 3
tarjetas de modo), en claro y oscuro, y en tablet; si puedes, también el aviso sin área.

```text
Pantalla: INICIO DEL OPERADOR (antes de entrar al simulador).
Quién: operadores de planta, a veces en tablet.
Qué decide: qué guía entrenar y en qué modo.
Qué ve primero: su guía y el botón para empezar.

Datos y acciones (estado con área asignada):
- barra superior: nombre de la app, interruptor de tema, el usuario y Cerrar sesión;
- título y su último acceso;
- su ÁREA como texto: "Producto · Área" (no se puede cambiar aquí);
- selector de Guía (solo las guías publicadas de su área); si no hay: "No hay guías
  disponibles para esta área.";
- Modo, como 3 tarjetas grandes seleccionables, con ícono y una línea: Guided "Paso a paso
  con la guía", Práctica libre "Sin guía ni puntaje, con pistas", Assessment "Evaluación con
  puntaje";
- botón Iniciar; un mensaje de error del servidor arriba si algo falla.
Estado sin área (red de seguridad, casi no se ve): aviso "Aún no tienes un área asignada…",
sin selector ni Iniciar. Muéstralo también.

Queda fuera: el simulador (POD) en sí, historial o puntajes del operador, elegir otra área.
```
