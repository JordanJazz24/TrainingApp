# Plan directo R1 y R2 — Ajustes de la revisión con Ernesto (2026-10-22)

**Status:** **aprobado por el usuario (2026-10-26)**, con la enmienda 1 de la spec 016 · Origen: roadmap →
"Revisión con Ernesto, MES (2026-10-22)", R1 y R2; orden en roadmap → "Después de la Fase 10".

- **R1** cambia una regla del Ingreso a MES: va como **enmienda 1 de la spec 016** (D-151).
- **R2** es solo texto de la guía: plan directo, sin spec (D-152).

Van en **un plan y una entrega**, porque las dos tocan la guía (`GuideScriptBuilder`), son
chicas y se prueban juntas (usuario, 2026-10-26).

## Decisiones del usuario (2026-10-26)

| D | Decisión |
|---|---|
| D-151 | **Hora a Hora sale del Ingreso a MES** (spec 016 → enmienda 1): el flujo es script → Repositorio → Recursos MES → login. La tarjeta Hora-Hora (3.0) se ve y no hace nada, como la (1.0), sin intento ni aviso en ningún modo. La guía no menciona Hora a Hora. La pantalla, su pestaña, el aviso provisional (D-72) y su registro se borran del código. Log On exige solo Recursos MES. |
| D-152 | **La guía no menciona acciones disciplinarias** ("no nos corresponde", Ernesto): las alertas de Resource Setup y de Log NC se cortan antes de esa frase y queda "CRÍTICO". Enmienda el texto de D-43 (spec 014). |

## E1 · R1 y R2

**R2, textos** (`Pages/Training/Guide/GuideScriptBuilder.cs`):
- [ ] **Resource Setup, alerta de Remove All:** termina en "…sin ese registro se pierde la
  trazabilidad de la operación." Se quita ", lo que puede conllevar acciones disciplinarias".
- [ ] **Log NC, alerta del subpaso 1 (`WrongUnitAlert`):** "Revise que solo esté seleccionada la
  pieza/SFC defectuosa. Scrapear la unidad equivocada es CRÍTICO: MES no lo detecta." Se quita
  "y puede haber acciones disciplinarias". Actualizar el comentario que cita D-43.
- [ ] Buscar en la app (`.cs`, `.ascx`, `.aspx`, `.js`) que no quede "disciplin" en un texto
  visible.

**R1, Hora a Hora fuera del Ingreso** (spec 016 → enmienda 1):
- [ ] **BLL** (`BLL/Training/MesLogin.cs`): quitar la pantalla `HoraHora`, la acción
  `HoraHoraStart` y `HoraHoraDone` del avance. Log On exige solo el registro de Recursos MES:
  sin él, intento fallido y vuelve al Repositorio (P-4). Actualizar los comentarios (D-67).
- [ ] **`AttemptActions`:** quitar el mapeo de la acción. Los intentos ya guardados con
  `MES.HoraHora.Iniciar` se siguen viendo en el dashboard con ese código: revisar que ninguna
  vista ni descarga dependa de la constante.
- [ ] **Una acción `HoraHoraStart` que llegue de una página vieja** (abierta antes de
  publicar): se trata como cualquier acción desconocida del Ingreso hoy, sin intento y sin
  error 500.
- [ ] **Control** (`MesLoginControl.ascx` y `.ascx.cs`):
  - quitar la pantalla de Hora a Hora 3.0, su pestaña en Edge, el aviso provisional y su JS;
  - la tarjeta Hora-Hora (3.0) queda **visible e inerte**, como la (1.0): no abre nada, no
    envía nada;
  - el cálculo de pestañas abiertas sin `hh`.
- [ ] **Estilos e imágenes:** quitar los `.mes-hh-*` de `presap.css` y `horahora-photo.jpg` (y
  su `Content` en el `.csproj`) si nadie más los usa. `card-hh3.jpg` se queda (la tarjeta se ve).
- [ ] **Guía de Guided** (`GuideScriptBuilder`, Ingreso a MES): quitar los subpasos `horahora`,
  `hhCode1` y `hhStart` y el `IncorrectByDetail` de Hora a Hora. Del Repositorio va directo a la
  tarjeta de Recursos MES, sin mencionar Hora a Hora. El ancla `mes-card-hh3` se queda si la
  tarjeta la usa para otra cosa; si no, se quita.
- [ ] **Práctica libre:** quitar la pista de Hora a Hora (`StepHints`), y `HoraHoraDone` de
  `StepViewModel` y de `GuidedTest.aspx.cs`.
- [ ] **Domain:** el comentario de `StepType.MesLogin` (`TrainingStep.cs`) sin Hora a Hora.
- [ ] **Tests:** los de `MesLogin` y `StepValidator` que cubren Hora a Hora se ajustan:
  - Log On sin Recursos MES: intento fallido y vuelve al Repositorio;
  - Log On con Recursos MES: completa;
  - la acción de Hora a Hora ya no existe.

  `ScoreCalculatorTests` sigue en verde.

**Sin cambios de BD:** ni scripts, ni pesos, ni versiones de guía.

**Verificación de Desarrollo:**
- rebuild y tests;
- `aspnet_compiler`, porque cambia el `.ascx`;
- `/code-review low` sobre `MesLogin.cs`, `MesLoginControl` y el Ingreso de `GuideScriptBuilder`,
  con foco en que Log On siga exigiendo Recursos MES y que el avance forzado (P-5) no cambie.

**Checklist del usuario:**
- **Guided:** el Ingreso va script → Repositorio → Recursos MES → login, sin mencionar Hora a
  Hora. La alerta de Remove All en Resource Setup y la del subpaso 1 de Log NC terminan sin la
  frase disciplinaria.
- **Práctica libre:**
  - clic en Hora-Hora (3.0) y en (1.0): no pasa nada, sin aviso;
  - Log On antes de Recursos MES: intento fallido y vuelve al Repositorio;
  - las pistas no mencionan Hora a Hora.
- **Assessment:** el Ingreso completo sin Hora a Hora llega al POD, y la sesión termina con su
  resumen (Logout con la limpieza, sin cambios).
- **F5** a mitad del Ingreso: sigue donde iba.

## No incluye

- Hora a Hora en cualquier otra forma (tablero, registro de unidades, nota en la guía).
- Cambiar los pesos, los pasos de las guías o la limpieza de Logout.
- Otras alertas o textos de la guía.
- R3 (Custom Resource Setup) y la Fase 11.

## Docs al cerrar

- **Desarrollo:**
  - `docs/domain.md`: el Ingreso a MES en "Flujo estándar" y en "Qué cuenta como intento", sin
    Hora a Hora;
  - `docs/architecture.md`: si menciona Hora a Hora en el Ingreso;
  - `docs/ui-reference/pre-sap-flow.md`: una nota con fecha de que el simulador ya no incluye
    Hora a Hora (D-151);
  - `status.md` (incluida la deuda de la spec 016: el aviso de D-72 deja de ser provisional
    porque se quita) y `history.md`.
- **Arquitectura y Planeación:**
  - roadmap: R1 y R2 ✅;
  - `decisions.md`: D-72 sin efecto y D-43, D-64 y D-67 enmendadas, ya hecho al aprobar;
    D-151 y D-152 quedan vigentes;
  - `reuniones.md` lo actualiza Reuniones y dudas (⏳ → ✅).
