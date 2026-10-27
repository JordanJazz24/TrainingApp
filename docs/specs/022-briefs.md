# Briefs por pantalla para Claude Design (spec 022)

**Spec:** `022-mockups-por-pantalla.md` (D-202 a D-206). **Uso:** por cada pantalla, pegar en
Claude Design el **contexto común** y después el **brief de la pantalla**, con sus adjuntos.
La exportación aprobada va a `022-design/<archivo>.html` (nombre en cada brief).

Orden de diseño (P-1): Login → Cambio de contraseña → Guías → Pasos → Seguimiento →
Operadores → Operador → Usuarios → Inicio del operador. El Login fija los cambios al sistema
de diseño. Desde la segunda pantalla, se
adjuntan también las exportaciones ya aprobadas.

## Adjuntos comunes (todas las pantallas)

- `TrainingApp/UI/master/admin.css` (tokens `--mt-*` en claro y oscuro, y componentes).
- El mockup v1: exportación o capturas del canvas "Mockup Fase 11 · Dashboard de Training"
  (https://claude.ai/artifact/Uxqr9utFzd8w6uBgp3EYYd): tableros Sistema de diseño, Resumen y
  Resumen en oscuro.
- Las exportaciones ya aprobadas de `022-design/`.
- Capturas actuales de la pantalla en claro y en oscuro, que toma el usuario (las indica cada
  brief).

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

## 2. Cambio de contraseña → `022-design/cambio-contrasena.html`

**Capturas:** la pantalla actual (primer acceso y cambio voluntario) y un error.

```text
Pantalla: CAMBIO DE CONTRASEÑA (la misma para los dos roles).
Quién: un usuario en su primer acceso o tras un reset (obligatorio), o por gusto.
Qué decide: su nueva contraseña.
Qué ve primero: los tres campos y por qué está aquí (primer acceso o cambio).

Datos y acciones:
- Contraseña actual (siempre se pide; en el primer acceso es la por defecto), Nueva
  contraseña y Confirmar; botones Guardar y Cancelar;
- reglas que valida el servidor: la nueva no puede ser la por defecto y debe coincidir con la
  confirmación; los errores llegan del servidor.
Propón tus propias 3 estructuras: no tiene que copiar la del login. La coherencia viene de
los tokens y componentes ya aprobados (te adjunto el login aprobado como referencia de
sistema, no de estructura).

Queda fuera: medidor de fuerza o reglas nuevas de contraseña, preguntas de seguridad.
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

**Capturas:** una guía con su ranking en Assessment y en Práctica libre, con "Ver todos"
abierto.

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
- los 2 motivos de fallo más frecuentes, y "Ver todos" que despliega el resto SIN recargar.
Filtros: período (Últimos 30 días, Todo), guía, modo (Assessment, Práctica libre), Filtrar;
"Descargar" (Excel).

Queda fuera: editar pasos o guías, comparar periodos, datos por operador (eso es Operador).
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
- su ÁREA asignada como "Producto · Área" (ej. "Sensores · LoFlo"), o "Sin área";
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
- encabezado: nombre, Code1, su ÁREA ("Producto · Área" o "Sin área"); botones Volver e
  Imprimir;
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

**Capturas:** la lista con operadores y admins (uno bloqueado, uno que debe cambiar la
contraseña), y el formulario de crear. Como el Área llega con la 021, si aún no está en la
app, se diseña igual con los datos de abajo.

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
- acciones por fila, cada una con confirmación: Resetear contraseña, Desbloquear (solo si
  está bloqueada), Eliminar (no sobre uno mismo; si tiene historial se avisa), y para
  operadores: Cambiar área (selector de áreas AGRUPADO POR PRODUCTO, se abre en la fila o en
  un modal sin recargar, y Guardar) y Quitar área.

Queda fuera: editar nombre o Code1, cambiar el rol, filtrar por área, elegir el área en el
alta, historial de cambios.
```

## 9. Inicio del operador → `022-design/inicio-operador.html`

**Capturas:** el inicio actual (con la cascada Producto, Área y Guía y las 3 tarjetas de
modo), en claro y oscuro, y en tablet.

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
