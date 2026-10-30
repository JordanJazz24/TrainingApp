---
name: cerrar-entrega
description: Cierre de una entrega de Desarrollo en TrainingApp, solo después del "funciona" del usuario. Commit de sus rutas, casilla y hash en el plan y estado.
argument-hint: <plan> <entrega>, p. ej. 022-plan E6
---

Entrega a cerrar: **$ARGUMENTS**. Si el usuario todavía no dijo "funciona" sobre esta
entrega, para y pídeselo (`docs/process.md` → "2. Cada entrega").

1. **Verificación al día.** Si algo cambió desde la última corrida, repite lo que corresponda
   (`CLAUDE.md` → "Correr local, compilar y probar"): Rebuild; `aspnet_compiler` si se tocó
   `.aspx`, `.ascx` o `.master`; tests si cambió la BLL o el Domain.
2. **Checklist de Web Forms** sobre lo que tocó la entrega (P-3 de
   `docs/specs/proceso-eficiente-plan.md`):
   - ningún `id` de control o botón coincide con el nombre de una función JS de la página;
   - ningún texto se codifica dos veces (`CLAUDE.md` → "Sesión y seguridad": `<%#: %>` en el
     HTML, `<%# %>` en atributos de controles de servidor).
3. **Git.** `git branch --show-current` (nunca `main`) y `git status` (sin carpetas de
   compilación ni basura). Commit solo de tus rutas: `git commit -m "…" -- <rutas>`.
4. **Plan:** marca las casillas de la entrega y agrega la nota de cierre con el hash.
5. **Estado (D-244):** `docs/status.md` → "📍 Estado actual" solo si cambió lo que está en
   curso; se reescribe, 40 líneas como máximo. `docs/history.md` solo si la entrega cierra la
   spec, la fase o el plan directo; en ese caso, avisa a Planeación por nombre para que la
   marque en el roadmap. Una entrega sola no se avisa.
6. Commit de esos docs, solo sus rutas. Responde en 3 líneas como máximo: hashes y lo que
   sigue.
