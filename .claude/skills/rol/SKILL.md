---
name: rol
description: Arranca esta sesión de TrainingApp en su rol (planeacion, desarrollo o reuniones). Usar al iniciar una sesión o después de /clear.
argument-hint: planeacion | desarrollo | reuniones
---

Tu rol es: **$ARGUMENTS**. Si no viene o no es `planeacion`, `desarrollo` ni `reuniones`,
pregúntale al usuario cuál es y no sigas.

1. Lee `docs/process.md` → "Tres sesiones de trabajo": qué hace y qué no hace tu rol, el
   modelo y el esfuerzo que le tocan (D-246), la coordinación (D-247) y la higiene (D-248).
   Si eres `desarrollo`, lee también `docs/process.md` → "2. Cada entrega".
2. Lee solo el estado de tu rol:
   - `planeacion`: `docs/roadmap.md` → la tabla de fases y "Siguiente".
   - `desarrollo`: `docs/status.md` → "📍 Estado actual" y el plan de la entrega en curso.
   - `reuniones`: `docs/reuniones.md`.
3. Corre `ListAgents`. Si dos sesiones vivas comparten nombre, o si la tuya no se llama como su
   rol ("Planeación", "Desarrollo", "Reuniones"), díselo al usuario en una línea. Nunca crees
   otra sesión.
4. Si el modelo o el esfuerzo de esta sesión no son los de tu rol en D-246, díselo al usuario
   en una línea. No los cambies tú.
5. Responde en 5 líneas como máximo: tu rol, dónde quedamos y qué sigue. Después espera
   instrucciones.
