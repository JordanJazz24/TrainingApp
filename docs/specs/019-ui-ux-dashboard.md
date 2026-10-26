# Brief de diseño 019: UI/UX del dashboard y de la administración (Fase 11)

**Status:** **aprobado por el usuario (2026-10-26)**, con P-1 a P-5 · D-153 a D-159 · sigue el
mockup, que se aprueba antes del plan · **Tipo:** brief de diseño
(`docs/process.md` → "1. Clasificar el pedido", fila 3): solo presentación, sin reglas ni datos
nuevos.
**Date:** 2026-10-26 · tres rondas de preguntas con el usuario (intención de UX, sistema visual
y alcance, detalles).

## Problema

Al aprobar la E6 de la spec 018 (2026-10-23), el usuario dijo que la funcionalidad del
dashboard es correcta, pero que los diseños "son muy básicos y feos", y pidió "actualizar los
ui/ux de todo este dashboard y todos sus módulos, también incluido el diseño del excel".

Lo que más molesta hoy (usuario, 2026-10-26):
- **se ve como tablas planas:** falta jerarquía, todo pesa igual;
- **colores y estados:** Aprobado, Reprobado y Seguimiento no se distinguen de un vistazo;
- **gráficas pobres:** Chart.js con su estilo por defecto.

## Quién lo usa y dónde (usuario, 2026-10-26)

- **Gerencia y trainers, por igual.** El Resumen se piensa para la mirada rápida (gerencia);
  Operadores, Operador, Pasos y Seguimiento, para el trabajo diario (trainers).
- **Dónde:** escritorio en oficina, **proyectado en reuniones** y **laptop o tablet en planta**.
  - Proyectado: textos, números y gráficas legibles a distancia.
  - Tablet: usable desde 768 px de ancho, sin perder información.

## Intención por vista

| Vista | Qué tiene que ver primero | Qué decide con ella |
|---|---|---|
| **Resumen** | **Quién está listo** (usuario): operadores por estado (Aprobado, Reprobado, En entrenamiento) y la tasa de aprobación por guía, como tarjetas grandes arriba, sin scroll | Cómo va el entrenamiento; dónde mirar después (el top 5 lleva a Pasos y el estado a Operadores) |
| **Operadores** | El estado por guía de cada operador, visible sin leer texto | A quién abrir en detalle |
| **Operador** | Su estado por guía y sus pasos a reforzar | Si está listo y qué reforzar; la ficha impresa |
| **Pasos** | El ranking con el % de sesiones con fallo como barra visual | Qué paso enseñar mejor |
| **Seguimiento** | Quién necesita atención y por qué regla | A quién llamar; ir a su detalle |
| **Usuarios, login y cambio de contraseña** | El mismo sistema visual, para que la administración no quede a medias | — |

## Sistema de diseño (primero, antes de las pantallas)

Línea **moderna y neutra** (usuario): tarjetas con sombra suave, mucho espacio, un color de
acento y colores de estado. Todo en un CSS común con **tokens**, que usan todas las vistas.

- **Tokens:** colores (acento, neutrales, superficies y los de estado), tipografía, escala de
  espaciado, radios y sombras. **Cada token tiene su valor claro y su valor oscuro.**
- **Tipografía del sistema** (usuario): Segoe UI en Windows, con su pila de respaldo. Sin fuentes
  descargadas.
- **Íconos: Bootstrap Icons, local** (usuario), sin CDN, como Chart.js. Entra al inventario de
  paquetes de AUD-29.
- **Componentes compartidos:**
  - tarjeta de indicador (número grande, etiqueta y período);
  - tarjeta de sección;
  - tabla (encabezado fijo, filas con hover, columnas numéricas alineadas a la derecha);
  - **insignia de estado**;
  - chips de reglas (Seguimiento);
  - barra de filtros;
  - botones;
  - paginación;
  - estado vacío ("Sin datos para este período");
  - barra de % en tablas.
- **Estados, siempre con texto o ícono además del color** (accesibilidad): Aprobado, Reprobado,
  En entrenamiento, Sin iniciar; y los modos Guided, Práctica libre y Assessment, con una paleta
  distinta de la de estados para no confundirlos.
- **Gráficas:** Chart.js con los tokens (colores, fuente, grilla suave, tooltips), en claro y en
  oscuro.
- **Modo oscuro** (usuario): los mismos tokens con valores oscuros.
- **Borde de los campos** (enmienda del 2026-10-26, usuario, con el aviso de Desarrollo en la E2;
  D-162): el borde del mockup (`#D6DCE4` en claro y `#2C3746` en oscuro) da 1.4:1 y no alcanza
  el contraste de los controles (WCAG 1.4.11, 3:1). Sirve para tablas, paneles y divisores, que
  son decorativos. Los **campos** (`form-control`, `form-select`, `input-group`) usan un token
  propio: **`#808B9B` en claro y `#627083` en oscuro** (3.2 a 3.7:1). El borde del mockup no
  cambia.
- **Contraste AA** en los dos modos.

## Bootstrap 5.3 (usuario: "actualizar, pero primero verificar que no rompa la aplicación")

- Hoy la app usa **Bootstrap 5.2.3** local. La actualización a 5.3.x estaba pendiente (D-131).
  5.3 trae variables CSS y modos de color (`data-bs-theme`), que sirven para los tokens y el modo
  oscuro.
- **Primero se verifica** en una entrega propia, antes de cualquier diseño:
  - actualizar a la última 5.3.x;
  - regresión completa: el POD en los tres modos (incluidos el Ingreso a MES y la limpieza), el
    login, Usuarios y el dashboard.
  - **Si algo se rompe y no se arregla sin tocar comportamiento, se para y decide el usuario.**
- Toca toda la app, también el POD, aunque su diseño no cambia.

## Excel profesional (usuario)

- Fila de título con la vista, los filtros aplicados y la fecha de descarga.
- Encabezados con color, filtros de Excel activos y la primera fila fija.
- Anchos ajustados y formatos: %, fechas en la hora de la planta y duraciones.
- Estados con color, igual que en pantalla.
- Las mismas columnas y datos de hoy (spec 018, D-147, D-149). La descarga de sesiones sigue
  escribiéndose en streaming (D-149), y su estilo se agrega ahí.

## Mockup antes del código (usuario)

Una página estática navegable con:
- el sistema de diseño: colores, tipografía, tarjetas, tabla, insignias, chips, filtros y
  gráficas;
- el **Resumen** con datos de ejemplo;
- en claro y en oscuro.

La arma Arquitectura y Planeación como artifact (no es código de la app). **El usuario la
aprueba antes del plan**, y queda como criterio de aceptación de todas las pantallas.

**Mockup v1, aprobado por el usuario como base (2026-10-26):**
[canvas "Mockup Fase 11 · Dashboard de Training"](https://claude.ai/artifact/Uxqr9utFzd8w6uBgp3EYYd).
Es privado y solo lo abre el usuario. Tiene tres tableros:
- **Sistema de diseño:** tokens claro y oscuro, tipografía, estados, modos, seguimiento,
  controles, indicador, estado vacío, tabla y Excel.
- **Resumen:** interactivo, con el interruptor de tema.
- **Resumen en oscuro.**

Los datos son de ejemplo. Las gráficas se dibujan con HTML solo para mostrar el estilo; en la
app son de Chart.js con los mismos tokens.

## Pantallas sin mockup (usuario, 2026-10-26, D-160)

Se construyen con los componentes del mockup. El usuario eligió estas disposiciones entre dos
opciones por pantalla:

- **Operadores:** tabla.
  - Arriba, la barra de filtros: búsqueda por nombre o Code1, estado y guía, más Descargar.
  - Columnas: el operador (nombre y Code1, enlace al detalle), **una columna por guía con su
    insignia de estado**, el mejor puntaje y la última actividad.
  - Paginación de 50 al pie. Descartado: tarjetas por operador.
- **Pasos:** tabla.
  - Columnas: el número en el ranking, el paso, **la barra de % de sesiones con fallo** (como el
    top 5), los intentos fallidos, las veces sin acierto, la mediana y, en Práctica libre, los
    saltos.
  - Debajo de cada paso, los **2 motivos más frecuentes**. **"Ver todos"** despliega el resto en
    el cliente, sin postback.
  - Descartado: lista grande con panel lateral.
- **Seguimiento:** tabla, **una fila por operador y guía**.
  - Las reglas van como chips **con texto** ("Reprobó 2 Assessment", "14 días sin practicar",
    "Mismo paso sin acierto: Log NC"), no solo S-1, S-2 y S-3.
  - Columnas además de las reglas: la última sesión y el enlace al detalle.
  - Primero quien cumple más reglas. Descartado: agrupado por guía.
- **Usuarios:** tabla.
  - Columnas: el **rol** y el **estado de la cuenta** como insignias (Activa, Bloqueada, Debe
    cambiar la contraseña), el último acceso y las **acciones de hoy** como botones de la fila.
  - **Mismo comportamiento que hoy** (spec 013): solo cambia el diseño.
  - Descartado: dejar la disposición actual con el estilo nuevo.
- **Operador (detalle):** como dice "Intención por vista":
  - arriba, el estado por guía como insignias grandes y los pasos a reforzar;
  - después la curva y las sesiones;
  - la ficha impresa en claro (D-157).
- **Login y cambio de contraseña:** una tarjeta centrada con el sistema de diseño y el
  interruptor de tema.

> **Enmienda 2026-10-26 (usuario, D-163): la página de inicio del operador entra en la
> Fase 11.** `StartSession.aspx` y su master, `Site.Master`, que no usa ninguna otra página,
> pasan a usar el sistema de diseño. Antes eran parte del "área del operador" del Out.
> - **Barra superior:** el nombre de la app, el interruptor de tema, el usuario y **Cerrar
>   sesión** (mismo comportamiento: D-13).
> - **Contenido:**
>   - el título y el último acceso (spec 013);
>   - Producto, Área y Guía siguen como listas, con la misma cascada;
>   - los **3 modos pasan a ser tarjetas grandes seleccionables**, con su ícono y una línea de
>     qué es cada uno: Guided "paso a paso con la guía", Práctica libre "sin guía ni puntaje, con
>     pistas", Assessment "evaluación con puntaje";
>   - el botón Iniciar.
> - **Mismos datos y mismo flujo:** mismos controles y postbacks; solo cambia cómo se ven.
> - **Modo oscuro** con el interruptor, como el login (D-157). **El POD (`Training.Master`) no
>   recibe el tema** y sigue con el aspecto del MES en claro.
> - **Sin mockup** (usuario): se revisa en la app. Va **en la E2**, con el login y el cambio de
>   contraseña.

## Skill de frontend-design (usuario, al aprobar)

El mockup y la implementación usan la skill `frontend-design` (plugin `frontend-design`).
**Siempre dentro de lo que fija este brief:**
- la línea moderna y neutra;
- los tokens en claro y en oscuro;
- Bootstrap 5.3 y Bootstrap Icons locales, sin CDN ni fuentes web;
- contraste AA.

La skill propone cómo se ve; el brief y el mockup aprobado mandan. Si propone algo fuera del
brief (otra librería, fuentes, animaciones pesadas), se pregunta antes.

## Propuestas (de Claude; confirmadas por el usuario el 2026-10-26)

- **P-1 · Cómo se elige el modo oscuro:** sigue al sistema operativo (`prefers-color-scheme`)
  y hay un interruptor en la barra superior que lo cambia. La elección se recuerda en ese
  navegador (`localStorage`: es una preferencia de presentación, no estado de negocio). **La
  ficha impresa sale siempre en claro.**
- **P-2 · Colores de estado:** Aprobado verde, Reprobado rojo, En entrenamiento ámbar, Sin
  iniciar gris. Seguimiento usa el rojo con un ícono de alerta.
- **P-3 · Orden del Resumen:**
  1. arriba, las tarjetas de estado y de tasa por guía;
  2. después la actividad por semana;
  3. al final el top 5 de pasos.
  El período de cada bloque va en su encabezado.
- **P-4 · Tablas en tablet:** se ocultan las columnas secundarias y la tabla se desplaza en
  horizontal dentro de su tarjeta, nunca la página entera.
- **P-5 · Entregas** (las define el plan; propuesta):
  1. Bootstrap 5.3 con la verificación;
  2. el sistema de diseño en `Admin.Master`, con login, cambio de contraseña y Usuarios;
  3. el Resumen;
  4. Operadores y Operador (con la ficha);
  5. Pasos y Seguimiento;
  6. los Excel.

## Scope

### In
- Las cinco vistas del dashboard (Resumen, Operadores, Operador, Pasos y Seguimiento), con sus
  filtros, tablas, gráficas, la curva y la ficha imprimible.
- Gestión de usuarios, el login y el cambio de contraseña (todo lo que usa `Admin.Master`).
- El sistema de diseño (CSS común con tokens), el modo oscuro y Bootstrap Icons.
- Bootstrap 5.3, con la verificación previa.
- El diseño de los `.xlsx`.

### Out
- **El POD** (`Training.Master`): no cambia su diseño. Solo se verifica que siga funcionando con
  Bootstrap 5.3. La página de inicio del operador (`StartSession.aspx`, `Site.Master`) sí entra
  (enmienda, D-163).
- Cambios en cálculos, reglas, datos, filtros o columnas del dashboard (spec 018, D-136 a D-150).
- Vistas, descargas o gráficas nuevas.
- Colores de marca corporativa: se usa un acento neutro. Si después hay guía de marca, se
  cambian los tokens.
- Fuentes web.

## Criterios de aceptación

- **AC-1:** cada vista, el login y Usuarios coinciden con el mockup aprobado, en claro y en
  oscuro (capturas de antes y después en el plan).
- **AC-2:** contraste AA en los dos modos. Ningún estado se distingue solo por el color.
- **AC-3:** en un proyector y en una tablet de 768 px, el Resumen se lee sin scroll horizontal y
  sus tarjetas de estado se ven sin bajar.
- **AC-4:** después de Bootstrap 5.3, una sesión completa de Guided, Práctica libre y Assessment
  en las tres áreas funciona igual (Ingreso a MES, POD, Logout y la limpieza), con la guía de
  Guided y sus anclas `data-guide` intactas.
- **AC-5:** los filtros siguen en la URL, F5 conserva la vista y la ficha se imprime en claro sin
  menú ni botones.
- **AC-6:** cada `.xlsx` abre en Excel sin el aviso de "reparar", con título, encabezados con
  color, filtros, primera fila fija y formatos. La descarga de sesiones sigue validándose contra
  el esquema (D-149).
- **AC-7:** el rendimiento no empeora: las vistas siguen en menos de 3 s con la BD de volumen
  (AC-24 de la spec 018, con su excepción de D-148).

## Decisiones del usuario (2026-10-26)

| D | Decisión |
|---|---|
| D-153 | El dashboard es para **gerencia y trainers por igual**; se usa en escritorio, proyectado y en laptop o tablet en planta. El Resumen muestra primero **quién está listo**. |
| D-154 | **Alcance:** las cinco vistas del dashboard, los Excel y además el login, el cambio de contraseña y Usuarios (todo `Admin.Master`). El POD y el área del operador no cambian de diseño. |
| D-155 | **Bootstrap 5.3.x**, en una entrega propia y **primero verificado**: regresión completa de la app. Si algo se rompe y no se arregla sin tocar comportamiento, se para y decide el usuario. |
| D-156 | **Estilo moderno neutro** con tokens en un CSS común, **la fuente del sistema** y **Bootstrap Icons local**. Estados con texto o ícono además del color (P-2). |
| D-157 | **Modo oscuro:** sigue al sistema operativo, con un interruptor en la barra que se recuerda en el navegador; la ficha impresa sale siempre en claro (P-1). |
| D-158 | **Excel profesional:** título con la vista, los filtros y la fecha; encabezados con color, filtros activos, primera fila fija, formatos y estados con color; mismas columnas y datos. |
| D-159 | **Mockup navegable aprobado antes del plan**, como criterio de aceptación. El mockup y la implementación usan la skill `frontend-design`, dentro del brief. |
| D-160 | **Pantallas sin mockup** (Operadores, Pasos, Seguimiento y Usuarios) como tablas con los componentes del mockup, con las disposiciones de "Pantallas sin mockup". |

## Open Questions

_Ninguna._
