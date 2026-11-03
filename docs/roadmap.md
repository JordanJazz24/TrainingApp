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
| 9 · Deploy del MVP | ✅ E1 (MVP publicado y funcionando en IIS 10, por `http://`; D-19 cerrada), E2, E3 (D-144, `d98563a`) E4 (AUD-75 a AUD-78, `7a13625`) y E5 (AUD-73 y AUD-79, `53df030`); falta la E6, del usuario, la 2.ª publicación (D-256, D-257) | 2026-10-22 | `fase-9-plan.md` |
| 10 · Dashboard e historial por operador (T8) | ✅ (E1–E8; D-136 a D-150) | 2026-10-23 | `018-dashboard-training.md`, `018-plan.md` |
| — · Guía Wires / Subensamble 1 para el MVP | ✅ (`0f01f11`; scripts `11` y `12`, sin C#) | 2026-10-26 | plan directo (D-161) |
| 11 · UI/UX del dashboard y la administración | ✅ (E1–E6 y AC-7; D-153 a D-164) | 2026-10-27 | `019-ui-ux-dashboard.md` (brief de diseño), `019-plan.md` |
| — · Área asignada por operador | ✅ (E1–E5: `425e944`, `d6af08d`, `a483731`, `03cd8df`, `b06a993`; D-165 a D-179 y D-208; cierra B-20, B-3 y B-4) | 2026-10-27 | `021-area-operador.md`, `021-plan.md` |
| — · R3 Custom Resource Setup | ✅ (E1–E5: `144d845`, `a0c7c77`, `3a18960` + `82d000d`, `5d12e80`, `3336221`; D-180 a D-201) | 2026-10-27 | `020-custom-resource-setup.md`, `020-plan.md` |
| — · Filtro de período en Pasos y Resumen | ✅ (E1–E4: `2ffb614`, `869f99a`, `5359a5a`, `9c318df`, en `spec-022`, integrada en `TrainingApp` en `f8aab65`; D-209 a D-218) | 2026-10-28 | `023-filtro-periodo.md`, `023-plan.md` |
| — · Dashboard por área (Operadores y Operador) | ✅ (E1–E4 en la nube, revisada en local y verificada por Desarrollo en `spec-024`, cierre `ab81231`, integrada en `e86a9f3`; D-260 a D-274) | 2026-10-29 | `024-dashboard-por-area.md`, `024-plan.md` |
| **12 · Mockups por pantalla** | ✅ (E1 a E9, 2026-11-02) brief y plan aprobados el 2026-10-27 (en la nube, D-202 a D-207); Login (3b, D-220 a D-227) y Cambio de contraseña (2a, D-228 a D-232) aprobados el 2026-10-28; E1 ✅ (`642cacd`, integrada en `f8aab65`); E2a ✅ Login (`53a9d52`) y E2b ✅ Cambio de contraseña (`8ccda11`), integradas en `TrainingApp` (`07363e3`); E3 ✅ Guías (`bb3c444`), E4 ✅ Pasos (`5a2ad7a`) y E1b ✅ barra superior (`feb1f4f`), integradas (`3544e69`); E5 ✅ Seguimiento (`11d4173`, cierre `2f29a62`) y Operadores (`1afe5ed`, cierre `866d80a`), integradas en `TrainingApp` (`4f59cbd`); E6 ✅ con Operador y su ficha (H, D-299 a D-305; `b34e721`, cierre `55931dd`, integrada en `c3f58d5`); E7 ✅ Usuarios (CD-3, handoff local D-251; `8a9664c`, cierre `733f70d`, integrada en `a9ce811`, D-306 a D-314); E8 ✅ Inicio (`3817e50`, review `90efca9`, integrada en `22bed84`); E8b ✅ Resumen «Sala» (D-252; `c9d2c6c`, cierre `c08b31f`, integrada en `fff2614`; D-320 a D-327); E9 ✅ (AC-7 medido, `e1f6665`); el fin de sesión va a la spec 025 (D-253); sin capturas desde el 2026-10-30 (D-242) | 2026-11-02 | `022-mockups-por-pantalla.md` (brief de diseño), `022-plan.md`, `022-briefs.md` |
| — · Flujo de trabajo eficiente | ✅ (E1 `88b1ca3`, E2 `5ff7e13` en `spec-022`, E3 `4bdb449` + `cb103ff`; D-243 a D-249) | 2026-10-30 | `proceso-eficiente-plan.md` |
| — · Fin de sesión del POD | ✅ (E1 `fc5630f`, E2 diseño v3, E3 `03419d9`, E4 `87c8bb2`, E5 `e322e4e`, review `157287b`, E6 `e7d3b58`; D-330 a D-345; D-21 y D-86 sin efecto) | 2026-11-02 | `025-fin-de-sesion.md`, `025-plan.md`, `025-design/` |

### ▶️ Siguiente (Planeación, 2026-11-02)

Qué sigue y en qué orden (máx. 15 líneas, D-244). Lo que está en curso: `docs/status.md`; lo
cerrado: las casillas de cada plan, `docs/history.md` y `docs/archivo/`.

1. **Fase 9 · 2.ª publicación (versión final, 2.ª semana de noviembre; D-256):** la auditoría
   pre-deploy (`docs/audits/2026-11-02-auditoria-pre-deploy.md`) dejó AUD-71 a AUD-80. Desarrollo
   cerró la E3 a la E5. En la E6, TI confirmó HTTPS con redirección y los scripts están
   corridos (2026-11-03); faltan: Desarrollo activa `requireSSL` y HSTS, el usuario rota `sa`
   (AUD-71, D-8), republica con `docs/publicacion.md` y manda el mensaje de lanzamiento. AUD-74 (clases grandes), spec después de publicar.
2. **Sin spec nueva por ahora (usuario, 2026-11-02).** Candidatas de la 025: el resultado visible
   para el admin desde el dashboard e historial y métricas del operador. Siguiente D-n: D-257.
3. **Usuario:** avisar a los trainers que cada operador elige su área (D-170); la guía piloto de
   Custom Resource Setup (D-194); las capturas de los 4 modales del Start de Moldeo; subir
   `TrainingApp` a `main` cuando lo decida (D-5).

### Abiertos de secciones archivadas

Pendientes que quedaron sin cerrar en secciones movidas a
`docs/archivo/` (la Fase 12, archivada el 2026-11-02, no dejó ninguno). Antes de retomarlos, confirmar si una spec posterior
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
**2026-11-02 (usuario):** el MVP se publicó sin configuración especial del servidor (IIS 10, en
`http://crapps.philips.com/`; HTTPS sin confirmar, D-257) y funcionó; D-19 cerrada. Falta la E2: republicar con D-144 y el mensaje de lanzamiento.

**Objetivo:** deploy en servidor interno con IIS: connection string de producción, App Pool,
permisos, prueba de humo. Incluye AUD-28/29.
- **Arranque en frío en IIS** (medido 2026-10-07 en desarrollo): tras reciclar el App Pool,
  el primer request compila el markup (3-10 s por carpeta; ~18 s todo el sitio). Resolver al
  publicar: precompilar el sitio y configurar el App Pool con `startMode="AlwaysRunning"`,
  `preloadEnabled="true"` (Application Initialization) e idle timeout 0.
**No incluye:** CI/CD automatizado, monitoreo avanzado.
