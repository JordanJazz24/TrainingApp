---
name: revisar
description: Review en Opus de una entrega de Desarrollo en TrainingApp, calibrado según docs/process.md → 3. Usar siempre en lugar de llamar /code-review directo.
argument-hint: <low|medium|high> <rutas> — <foco>
model: opus
effort: high
---

Review pedido: **$ARGUMENTS**. Este turno corre en Opus (D-250); la sesión vuelve sola a su
modelo con el siguiente mensaje.

1. **Nivel, rutas y foco.** Si falta alguno, elígelo con la tabla de `docs/process.md` → "3.
   `/code-review` calibrado" y dile al usuario en una línea cuál y por qué. Si el cambio no
   lleva review según esa tabla, dilo y termina.
2. **Revisar.** Si `docs/process.md` → 3 → "Verificación única" dice que `/revisar` no usa
   `/code-review`, revisa tú las rutas con el foco. Si no, corre
   `/code-review <nivel> --max-findings 5 <rutas>` con el foco y el modelo de amenaza de esa
   sección ("Riesgo proporcional"), y espera a que termine: en la terminal corre en segundo
   plano.
3. **Verificar.** Cada hallazgo, contra el código. Descarta en una línea lo que la sección 3
   deja fuera de alcance; lo que queda, con su propuesta. Un bug que ya existía es B-n y lo
   decide el usuario (`docs/process.md` → 1).
4. **Reportar:** los archivos leídos (sin esa lista, el review no cuenta), los hallazgos
   verificados con su propuesta y los descartados en una línea cada uno. "Sin hallazgos" solo
   con la lista de lo leído.
