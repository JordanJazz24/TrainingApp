# Spec 021: Área asignada por operador

**Status:** Draft (2026-10-27)
**Date:** 2026-10-27 · **Origen:** negocio, 2026-10-27 (roadmap → "Después de la Fase 10") ·
**Sesión:** Arquitectura y Planeación en la nube (`docs/process.md` → "Variante temporal: specs
y planes en la nube"), rama `cloud/021-area-operador`
**Decisiones:** D-165 a D-178, del usuario. Las propuestas de Claude (P-1 a P-12) esperan
confirmación antes de aprobar; al confirmarlas se agrupan en **D-179**. Cierra **B-20** y
**B-3**.

## Problem

Negocio confirmó que cada operador trabaja en **un área** y solo debe entrenar las guías de esa
área. Hoy el inicio del operador (`StartSession.aspx`) deja elegir cualquier Producto, Área y
guía publicada, y nada en la app registra a qué área pertenece cada operador: la API de
empleados no la da (roadmap → decisiones de la spec 013). La Fase 10 lo dejó fuera ("el área
del operador, diferida"); esta spec lo trae.

Además, `PodService.StartSession` no revisa en la BLL que la guía exista y esté publicada
(B-20): solo la página lo impide. La validación del área va en el mismo punto, así que se
cierran juntas.

## Scope

### In

**A. Datos** (D-165)
1. Un área por operador: `Users.AreaId` (`INT NULL`, FK a `Areas`). Sin área = `NULL`. Solo
   el rol Operator la usa (P-2).

**B. El operador elige su área una vez** (D-166, D-169, D-170, D-171, D-176, D-177)
2. Pantalla nueva **"Elige tu área"** (Operator, `Site.Master`): **un solo selector** con las
   áreas **agrupadas por producto** (ej. Sensores › LoFlo), sin postback intermedio (D-169), y el
   botón Continuar. Solo aparecen las **áreas con al menos una guía publicada** (D-176).
3. Continuar abre una confirmación en el cliente: "¿Tu área es *Producto · Área*? Después solo
   tu trainer podrá cambiarla." (Sí / No). Con Sí, un postback guarda el área (D-177).
4. Flujo: login → cambio de contraseña (si toca) → **Elige tu área** (si no tiene) → inicio.
   Toda página de Training redirige a "Elige tu área" mientras el operador no tenga área, igual
   que hoy redirige al cambio de contraseña (D-169).
5. Pasa por esta pantalla todo operador sin área: los que se autorregistran, los que crea un
   admin y los que ya existen (D-170). El alta de usuarios en Usuarios no cambia.
6. Después, el operador **no** puede cambiarla: la BLL rechaza un segundo guardado (D-171).

**C. El admin la corrige en Usuarios** (D-172, D-176)
7. Columna **Área** en la lista de Usuarios ("Producto · Área", "Sin área" o "—" para un
   administrador).
8. Por fila de operador, la acción **Cambiar área**: el mismo selector agrupado y las mismas
   áreas elegibles del punto 2, y Guardar, con confirmación. También **Quitar área**: el operador la vuelve a
   elegir en su próximo ingreso.

**D. El inicio del operador** (D-167, D-168)
9. Con área: desaparecen los selectores de Producto y Área. Se ve "*Producto · Área*" como
   texto y solo el selector de guía de esa área (y el modo, como hoy).
10. Sin área: aviso "Aún no tienes un área asignada…" y sin selector ni Iniciar (D-168). Con el
    punto 4 casi no debería verse: es la red de seguridad si el admin quita el área durante la
    Session del operador.

**E. La BLL valida al iniciar** (D-173, D-174, B-20)
11. `PodService.StartSession` rechaza con **excepción** (no es un intento ni cambia el puntaje):
    guía inexistente, guía no publicada (B-20), guía de otra área u operador sin área.
    Valida **antes** de abandonar la sesión en curso (D-9): un inicio rechazado no la toca.
12. El área solo decide qué guías se pueden **iniciar**. Una sesión en curso de otra área (porque
    el admin cambió o quitó el área) se puede terminar y cuenta como siempre (D-174).

**E2. Usuarios: B-3**
12b. `Users.aspx` manda al **inicio del rol** (`SessionHelper.RedirectHome`), no al login,
    cuando la BLL niega el acceso (`UnauthorizedAccessException`), también en Cambiar y Quitar
    área (B-3, CLAUDE.md → "Auth").

**F. Dashboard: solo mostrar el área** (D-175)
13. "Área" del operador en la lista de Operadores, en su Excel y en el encabezado del Operador
    (y su ficha). Filtros, estados y cálculos siguen por la guía, sin cambios.

**G. Datos de ejemplo** (D-178)
14. Redefinity se ofrece como cualquier área con guía publicada. Se quita el aviso "NO USAR EN
    PRODUCCIÓN" del encabezado de `05_SeedData.sql` y de la tabla de áreas de
    `docs/domain.md`.

### Out (no incluye)

- Más de un área por operador. El **entrenamiento cruzado** se hace cambiando el área en
  Usuarios y regresándola después; el historial de las dos áreas queda en el dashboard.
- Filtrar el dashboard por el área del operador, "Sin iniciar" por área o "quién de cada área
  falta por entrenar" (el "por área después" de la spec 018).
- Historial de cambios de área (solo se guarda la actual).
- Alcance por área para los administradores, el rol Trainer y las Operations.
- Buscar o filtrar Usuarios por área.
- Elegir el área en el alta de Usuarios (D-170: el operador la elige al entrar).
- Cambios en el motor del POD, el puntaje, los modos o la guía visual.

## Decisiones del usuario (2026-10-27)

| ID | Decisión |
|---|---|
| D-165 | **Una sola área** por operador (`Users.AreaId` nullable). Entrenamiento cruzado: el admin le cambia el área y la regresa (usuario, 2.ª ronda) |
| D-166 | El **operador** elige su área al autorregistrarse; el admin la corrige en Usuarios |
| D-167 | Inicio del operador: **área fija como texto** y solo el selector de guía |
| D-168 | Operador sin área: **aviso y no entrena** |
| D-169 | La elige en una **pantalla propia tras el cambio de contraseña**; las páginas de Training redirigen ahí mientras no tenga área. Un **solo selector agrupado por producto** (usuario, 2.ª ronda) |
| D-170 | **Todo operador sin área** (autorregistro, alta por admin y existentes) la elige en su próximo ingreso |
| D-171 | Una vez elegida, **solo un admin** la cambia |
| D-172 | Usuarios: **columna Área** y acciones **Cambiar área** y **Quitar área** por fila, con el mismo selector agrupado |
| D-173 | La **BLL valida** el área en `StartSession` (excepción, no intento) |
| D-174 | Una sesión en curso de otra área **se puede terminar** |
| D-175 | Dashboard: **solo mostrar** el área del operador (Operadores y Operador); filtros y cálculos sin cambios |
| D-176 | Solo son elegibles las **áreas con guía publicada** |
| D-177 | El operador **confirma** antes de guardar (aviso de que no podrá cambiarla) |
| D-178 | Redefinity es elegible como las demás; se quita el aviso "NO USAR EN PRODUCCIÓN" (`05_SeedData.sql` y `docs/domain.md`) |
| B-20 | **Entra** en esta spec: `StartSession` rechaza una guía inexistente o no publicada (punto 11) |
| B-3 | **Entra** en la entrega de Usuarios (punto 12b) |

## Propuestas de Claude (a confirmar antes de aprobar)

No las preguntó Claude; el usuario las confirma o corrige. Al confirmarlas se registran juntas
como **D-179** (usuario, 2.ª ronda). Si el plan necesita decisiones nuevas, se pide el siguiente
rango libre.

| # | Propuesta |
|---|---|
| P-1 | **Esquema:** `Users.AreaId` en `00_Schema.sql`, con su `IF COL_LENGTH … ALTER TABLE` como la spec 020. La BD del usuario y la del MVP son de pruebas (`docs/process.md` → 6): basta correr los scripts o recrearla. `spUserSearch`, `spUserGetBy*` y el SP de operadores del dashboard devuelven el área. Un área nunca se borra (los scripts no borran filas), así que la FK no necesita `ON DELETE` |
| P-2 | **Solo Operators tienen área.** La BLL rechaza (excepción) asignar área a un administrador. Sin capacidad nueva en `AccessPolicy`: elegir la propia es `CanTrain`; cambiarla o quitarla, `CanManageUsers` |
| P-3 | **Redirección:** `SessionHelper.MustChooseArea()` con una clave de Session escrita **solo** en `SetUserSession` (CLAUDE.md → "Sesión y seguridad"); después de elegir, la página refresca la Session con el usuario de la BD y `SetUserSession`. Orden en `Page_Load` de Training: `EnsureUserSession` → `MustChangePassword` → `MustChooseArea`. La Session solo decide la redirección: el inicio lee el área de la BLL en cada carga y la BLL valida contra la BD, así que un cambio del admin vale al instante para iniciar guías |
| P-4 | **Elegir la propia** (`ChooseOwnArea(userId, areaId)` en la BLL): solo si el usuario es Operator, no tiene área y el área es elegible. El SP actualiza solo `WHERE AreaId IS NULL`: dos pestañas no pisan una elección hecha (la segunda es excepción) |
| P-5 | **Cambiar o quitar** (`UserAdminService.SetArea(adminId, userId, areaId?)`): verifica que quien llama es admin (como el resto de `UserAdminService`), que la cuenta es Operator y que el área es elegible; `null` la quita. Sin registro de actividad del cambio |
| P-6 | **Área elegible = con al menos una guía publicada**, calculado en un SP (no en la página). Si después un área se queda sin guías publicadas, sus operadores la conservan y el inicio muestra el mensaje actual "no hay guías" |
| P-7 | **Errores de `StartSession`:** son envíos manipulados o una lista vieja; la excepción va a la página de error de la app, sin mensaje propio. Excepción: operador sin área → la página no llega a llamar (punto 10) |
| P-8 | **Tests:** las reglas nuevas en clases puras de la BLL con datos en memoria (`PodService` no admite repositorios inyectados, D-15): quién puede iniciar qué guía (inexistente, no publicada, otra área, sin área, válida), quién puede elegir o cambiar área y qué áreas son elegibles |
| P-9 | **Texto del área:** "Producto · Área" en todas partes (inicio, Usuarios, dashboard y Excel). "Sin área" para un operador sin área; "—" para un administrador |
| P-10 | **Pantalla "Elige tu área":** `Pages/Training/ChooseArea.aspx` con `Site.Master` (el sistema de diseño del inicio del operador, Fase 11). No muestra el último acceso (lo muestra el inicio después) |
| P-11 | **Cambiar área en Usuarios:** el selector agrupado se abre en la fila (o un modal de la página) sin postback hasta Guardar, con la confirmación `adminConfirm` que ya usan las demás acciones |
| P-12 | **Docs al cerrar** (Desarrollo): la regla del área en `docs/domain.md` (sección nueva "Área del operador"), el flujo en `docs/architecture.md` → "Roles, login y gestión de usuarios", B-20 corregido en `docs/decisions.md` |

## Acceptance criteria

- **AC-1** Given un operador sin área (nuevo o existente), When entra y ya cambió su contraseña,
  Then va a "Elige tu área". Intentar abrir otra página de Training lo devuelve ahí.
- **AC-2** Given "Elige tu área", Then solo aparecen las áreas con guía publicada (hoy Moldeo,
  LoFlo, Subensamble 1 y Redefinity) bajo su producto.
- **AC-3** Given un área elegida, When pulsa Continuar y responde No, Then no hay postback ni
  cambio. When responde Sí, Then se guarda y llega a su inicio.
- **AC-4** Given un operador con área, Then su inicio muestra "Producto · Área" sin selectores de
  Producto ni Área, y solo las guías publicadas de su área.
- **AC-5** Given un operador con área, When abre "Elige tu área" a mano o la reenvía, Then no
  puede cambiarla (redirige al inicio; la BLL rechaza el guardado).
- **AC-6** Given Usuarios, Then cada fila muestra el área ("Sin área" o "—" para un admin). When
  el admin cambia el área de un operador, Then su próximo inicio muestra la nueva área y sus
  guías. When la quita, Then el operador la vuelve a elegir en su próximo ingreso.
- **AC-7** Given un operador con una sesión en curso de otra área (el admin le cambió el área),
  Then puede terminarla y cuenta en el dashboard.
- **AC-8** Given un envío a `StartSession` con una guía inexistente, no publicada o de otra área,
  o un operador sin área, Then la BLL lanza una excepción, no se crea la sesión y la sesión en
  curso **no** se abandona (tests de la BLL).
- **AC-9** Given la lista de Operadores del dashboard, su Excel y el detalle del Operador, Then
  se ve el área del operador. Los estados, filtros y números no cambian.
- **AC-6b** Given un admin cuya cuenta perdió el rol o fue borrada, When envía una acción de
  Usuarios, Then va a su inicio (o al login si ya no tiene rol), no al login con sesión (B-3).
- **AC-10** Given un administrador, Then no tiene área, no ve "Elige tu área" ni la acción
  Cambiar área sobre su propia fila o la de otro admin.
- **AC-11** El puntaje y los tests existentes siguen en verde; `05_SeedData.sql` y
  `docs/domain.md` ya no dicen "NO USAR EN PRODUCCIÓN".

## Riesgos y coordinación

- **Alcance:** levanta el "área del operador (diferida)" del "No incluye" de la Fase 10. La sesión
  local de Arquitectura y Planeación lo registra en el roadmap.
- **B-3** se cierra aquí (punto 12b). Si al implementar ya se cerró por la E2 de la 018, el
  punto queda sin efecto.
- **Operadores ya existentes:** al publicar, todos pasan por "Elige tu área" en su próximo
  ingreso (D-170). Avisar a los trainers antes.
- **Datos de prueba:** el dashboard de volumen (Fase 11, AC-7) tiene operadores sin área; se
  verán como "Sin área".
