# Roadmap — MESTrainingSimulator

> **Fases y planes.** Lo escribe Arquitectura y Planeación (`docs/process.md` → "Tres
> sesiones de trabajo"): fases, su alcance ("No incluye"), planes, revisiones con negocio y
> orden de tareas. Dónde quedamos, deuda, BD del usuario y auditorías: `docs/status.md`; lo
> cerrado: `docs/history.md`, y las secciones cerradas del roadmap, en `docs/archivo/` (D-245).
> Lo que sigue, en "Siguiente" (máx. 15 líneas, D-244). Se separaron el 2026-10-21: las referencias `roadmap →
> "Estado actual"`, "Deuda y riesgos conocidos", "BD de desarrollo del usuario",
> "Auditorías" o "Historial" de specs y auditorías apuntan ahí. Desde el 2026-10-30, las referencias a
> secciones cerradas del roadmap (Fases 7 a 11, Fase 8.5 y sus filas, specs 023 y 024, "Después de
> la Fase 10", las revisiones con negocio, "Para retomar") apuntan a
> `docs/archivo/roadmap-hasta-2026-10-30.md`. Las reglas vigentes están en
> `CLAUDE.md`, el proceso en `docs/process.md` y los mecanismos en `docs/architecture.md`. Las
> secciones de cada fase describen lo que se hizo **en su momento**: si contradicen a
> `CLAUDE.md`, manda `CLAUDE.md`.

---

## Fases

| Fase | Estado | Cierre | Spec / plan |
|---|---|---|---|
| 0 · Setup | ✅ | — | — |
| 1 · Auth | ✅ | — | — |
| 2 · Dominio y catálogo de R&C | ✅ | — | `002-rc-moldeo-catalog.md` |
| 3 · Motor de TrainingGuide | ✅ | — | — |
| 4 · TrainingSession y modos | ✅ | — | `003-training-session-y-modos.md` |
| 5 · ScoreCalculator: cobertura de tests | ✅ | 2026-10-07 | `fase-5-plan.md` |
| 6 · UserControls por tipo de paso | ✅ | 2026-09-24 | `004`, `005`, `006`, `007` |
| — · Ajustes tras la 6 (S1/S2) y spec 008 (Serialized) | ✅ | 2026-09-28 | `008-*.md` |
| 6.5 · Fidelidad de UI/UX con el SAP MES real | ✅ | 2026-10-02 | `fase-6.5-plan.md` |
| — · Auditoría + spec 009 (motor del POD) + auditoría final | ✅ | 2026-10-06 | `009-*.md`, `docs/audits/` |
| 6.6 · Overlays guiados | ✅ | 2026-10-07 | `010-*.md` |
| 7 · Catálogo de LoFlo | ✅ | 2026-10-07 | plan directo |
| 8 · Validación interna | ✅ | 2026-10-12 | revisiones y validación con técnicos |
| 8.5 · Entrega de noviembre | ✅ | 2026-10-22 | specs 011-017, filas 1-13 |
| 9 · Deploy del MVP | ✅ (en el servidor faltan la publicación, la prueba de humo, D-19, y el lanzamiento, a cargo del usuario y TI) | 2026-10-22 | `fase-9-plan.md` |
| 10 · Dashboard e historial por operador (T8) | ✅ (E1–E8; D-136 a D-150) | 2026-10-23 | `018-dashboard-training.md`, `018-plan.md` |
| — · Guía Wires / Subensamble 1 para el MVP | ✅ (`0f01f11`; scripts `11` y `12`, sin C#) | 2026-10-26 | plan directo (D-161) |
| 11 · UI/UX del dashboard y la administración | ✅ (E1–E6 y AC-7; D-153 a D-164) | 2026-10-27 | `019-ui-ux-dashboard.md` (brief de diseño), `019-plan.md` |
| — · Área asignada por operador | ✅ (E1–E5: `425e944`, `d6af08d`, `a483731`, `03cd8df`, `b06a993`; D-165 a D-179 y D-208; cierra B-20, B-3 y B-4) | 2026-10-27 | `021-area-operador.md`, `021-plan.md` |
| — · R3 Custom Resource Setup | ✅ (E1–E5: `144d845`, `a0c7c77`, `3a18960` + `82d000d`, `5d12e80`, `3336221`; D-180 a D-201) | 2026-10-27 | `020-custom-resource-setup.md`, `020-plan.md` |
| — · Filtro de período en Pasos y Resumen | ✅ (E1–E4: `2ffb614`, `869f99a`, `5359a5a`, `9c318df`, en `spec-022`, integrada en `TrainingApp` en `f8aab65`; D-209 a D-218) | 2026-10-28 | `023-filtro-periodo.md`, `023-plan.md` |
| — · Dashboard por área (Operadores y Operador) | ✅ (E1–E4 en la nube, revisada en local y verificada por Desarrollo en `spec-024`, cierre `ab81231`, integrada en `e86a9f3`; D-260 a D-274) | 2026-10-29 | `024-dashboard-por-area.md`, `024-plan.md` |
| **12 · Mockups por pantalla** | ▶️ brief y plan aprobados el 2026-10-27 (en la nube, D-202 a D-207); Login (3b, D-220 a D-227) y Cambio de contraseña (2a, D-228 a D-232) aprobados el 2026-10-28; E1 ✅ (`642cacd`, integrada en `f8aab65`); E2a ✅ Login (`53a9d52`) y E2b ✅ Cambio de contraseña (`8ccda11`), integradas en `TrainingApp` (`07363e3`); E3 ✅ Guías (`bb3c444`), E4 ✅ Pasos (`5a2ad7a`) y E1b ✅ barra superior (`feb1f4f`), integradas (`3544e69`); E5 ✅ Seguimiento (`11d4173`, cierre `2f29a62`) y E6 parcial: Operadores ✅ (`1afe5ed`, cierre `866d80a`), integradas en `TrainingApp` (`4f59cbd`); faltan Operador y su ficha (E6), E7 y E8 según sus mockups; sin capturas desde el 2026-10-30 (D-242) | | `022-mockups-por-pantalla.md` (brief de diseño), `022-plan.md`, `022-briefs.md` |
| — · Flujo de trabajo eficiente | ✅ (E1 `88b1ca3`, E2 `5ff7e13` en `spec-022`, E3 `4bdb449` + `cb103ff`; D-243 a D-249) | 2026-10-30 | `proceso-eficiente-plan.md` |

### ▶️ Siguiente (Planeación, 2026-10-30)

Qué sigue y en qué orden (máx. 15 líneas, D-244). Lo que está en curso: `docs/status.md`; lo
cerrado: las casillas de cada plan, `docs/history.md` y `docs/archivo/`.

1. **022 (Fase 12):** E5 y la lista de Operadores de la E6 integradas en `TrainingApp` (`4f59cbd`).
   Siguen Operador y su ficha (resto de la E6), E7 Usuarios y
   E8 Inicio, cada una al aprobarse su mockup; Desarrollo en Sonnet desde la siguiente (D-246). Al
   cerrar la E8 termina la variante de la nube (D-249).
2. **Negocio:** validar los textos de "qué hacer" de Operadores (D-298) antes de cerrar la E6.
   Diseño UI/UX (nube) sigue con D-299 y D-300 a D-319; Planeación integra su rama.
3. **Usuario:** antes de publicar en producción, los scripts que lista `docs/status.md` y avisar a
   los trainers que los operadores eligen su área (D-170); subir `TrainingApp` a `main` cuando lo
   decida (D-5).

### Abiertos de secciones archivadas

Pendientes que quedaron sin cerrar en secciones movidas a
`docs/archivo/roadmap-hasta-2026-10-30.md`. Antes de retomarlos, confirmar si una spec posterior
ya los resolvió.

- **Fase 8 → Revisión con SAP MES (2026-10-09):** variantes del flujo que entran (decidir con
  negocio), piezas de otra orden en la cola y escanear la Shop Order, mensajes sin captura (ambiente
  de calidad) y las preguntas "sin respuesta".
- **Fase 8 → Retroalimentación de la trainer:** W15 (ícono ✔ del grupo de DC, captura pendiente).
- **Después de la Fase 10 → Revisión con Ernesto (2026-10-22):** la contraseña del login de MES; la
  reparación rápida que pierde el campo del componente al forzar el avance desde la BD (sin
  confirmar; si se confirma, B-n con plan directo).

### Fase 9 — Deploy del MVP
**Plan aprobado el 2026-10-22: `docs/specs/fase-9-plan.md`** (D-129 a D-134). El MVP se
publica el 2026-10-22 en el IIS interno para recoger retroalimentación (E1: la parte de
Desarrollo está cerrada en `584cdf2`; faltan la publicación, la prueba de humo y el
lanzamiento, que hace el usuario). AUD-28 (lo barato) y AUD-29 (parches) se suman en una
2.ª publicación (E2), que se hace **junto con la Fase 10** (D-135). Mientras tanto avanzan la
Fase 10 y las mejoras, y la **versión final se entrega en la 2.ª semana de noviembre**. Los
datos del MVP se descartan (D-130). Lo que sigue es el texto original de la fase.

**Objetivo:** deploy en servidor interno con IIS: connection string de producción, App Pool,
permisos, prueba de humo. Incluye AUD-28/29.
- **Arranque en frío en IIS** (medido 2026-10-07 en desarrollo): tras reciclar el App Pool,
  el primer request compila el markup (3-10 s por carpeta; ~18 s todo el sitio). Resolver al
  publicar: precompilar el sitio y configurar el App Pool con `startMode="AlwaysRunning"`,
  `preloadEnabled="true"` (Application Initialization) e idle timeout 0.
**No incluye:** CI/CD automatizado, monitoreo avanzado.

### Fase 12 — Mockups por pantalla (spec 022)

**Brief de diseño aprobado el 2026-10-27: `docs/specs/022-mockups-por-pantalla.md`**, con su
plan `022-plan.md` (9 entregas) y los briefs para Claude Design en `022-briefs.md`. Se hizo en la
nube (`cloud/022-mockups-por-pantalla`, D-202 a D-207) y se integró el 2026-10-27. Nace de que
al usuario no le gustaron las pantallas sin mockup de la Fase 11: reemplaza D-160 y el "sin
mockup" de D-163.

**Objetivo:** que cada pantalla tenga su propio mockup aprobado en Claude Design (3 alternativas
que difieren en la estructura) y se implemente contra la exportación guardada en
`docs/specs/022-design/<pantalla>.html`, que es su criterio de aceptación.

**Incluye:** Login, Cambio de contraseña, Guías, Pasos, Seguimiento, Operadores, Operador (con la
ficha impresa), Usuarios e Inicio del operador; cambios al sistema de diseño (cada uno una D-n,
para toda la app; el Login fija el sistema); métricas derivadas en la BLL con los datos que la
vista ya carga, cada una con su D-n (D-205).

**No incluye:**
- el Resumen (más allá de heredar tokens), "Elige tu área", el POD y los Excel;
- datos, SPs o columnas nuevos; cambios de reglas, filtros, estados o cálculos;
- acciones, filtros o descargas que hoy no existen;
- React u otro framework, fuentes web, CDN y colores de marca corporativa.

**Orden, en paralelo con la 021 (D-207, P-7):** cada entrega espera su mockup aprobado. E1
(sistema de diseño), E2 (Login y Cambio de contraseña), E3 (Guías), E4 (Pasos) y E5
(Seguimiento) son libres y se intercalan con la 021 cuando el usuario se lo pida a Desarrollo,
nunca a la mitad de una entrega de la 021. E6 (Operadores y Operador) va después de la E5 de la
021; E7 (Usuarios), después de su E4, y E8 (Inicio del operador), después de su E3. **La 021
se cerró el 2026-10-27:** desde entonces ninguna entrega de la 022 espera a la 021, solo su mockup.
- **Coordinación:** la 021 y la 022 tocan `admin.css` y `Users.aspx`. La 022 se implementa en su
  propia rama (`spec-022`), y la E1 se coordina con Desarrollo para no pisar la 021.
- **Enmiendas de la nube integradas (2026-10-28, `863f925`):** la 021 cerrada (ninguna entrega
  la espera, solo la E1 y su mockup); Usuarios sin "Quitar área" (D-208), con el lápiz en la
  columna Área; rango D-220 a D-239 y B-30 a B-39; los briefs se usan con Claude Design leyendo
  el repo (rama `TrainingApp`) con un prompt corto por pantalla (`022-briefs.md` → "Cómo
  usarlos").
- **Login aprobado (2026-10-28, `f197a85`):** alternativa 3b, panel dividido (D-220 a D-225):
  tokens `--mt-brand-*` y `--mt-on-brand*` (D-221), `.auth-*` y `.mt-status-line` (D-222),
  `Admin.Master` con `FullBleed` solo en el login (D-223), errores en la línea de estado en vez
  de SweetAlert (D-224) y textos nuevos (D-225). El criterio pasa a ser capturas PNG y
  `022-design/<pantalla>.md`, no el HTML exportado (D-227, excepción a P-3). Flujo con handoff
  de Claude Design (D-226, `process.md` → "Variante temporal"): la rama `cloud/022-login-3b`
  (`c7294ee`, "no verificada") es el punto de partida de la E1 y la E2a; no se integra tal cual.
- **Orden con la 023 (D-219):** ver "Spec 023 → Orden". La E2 se parte en E2a (Login) y E2b
  (Cambio de contraseña).
- **Cambio de contraseña aprobado (2026-10-28, `2fd0e8b`):** alternativa 2a (D-228): franja de
  marca y una tarjeta montada, 3 pasos en primer acceso, Nueva y Confirmar lado a lado y los
  requisitos fijos; `.pw-*` y `.mt-steps` reutilizable, sin tokens nuevos (D-229); pantalla
  completa siempre, también en el cambio voluntario (D-230); errores en `.mt-status-line` y
  botones `LinkButton` (D-231); textos nuevos (D-232). "Cancelar y cerrar sesión" solo nombra lo
  que ya hacía Cancelar (`SessionHelper.Logout`). Rama de partida `cloud/022-cambio-contrasena`
  (`221297f`, sale de la del Login): la E2b va después de la E2a.
