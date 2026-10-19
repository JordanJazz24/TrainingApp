# Plan — Spec 014: Tres tipos de scrap y pieza defectuosa indicada

**Spec:** `014-tres-scraps.md` (aprobada 2026-10-16, enmendada el mismo día: modal Add
Component) · **Estado:** **aprobado por el usuario (2026-10-16)**, con D-56 a D-60 y los
textos de la guía.
**Autoría:** sesión de Arquitectura y Planeación (verificado contra el código de
`TrainingApp`, `285d96e`). Lo ejecuta "desarrollo" (`docs/process.md` → "Tres sesiones de
trabajo").

Cuatro entregas en el orden del flujo. Cada una con el ciclo de `docs/process.md` §2:
rebuild, `aspnet_compiler`, tests, review calibrado, checklist en el navegador, "funciona" y
commit con el roadmap. **E4 (W13) es la última y la primera que se recorta** si no alcanza el
tiempo para noviembre; sin ella, la reparación rápida usa la Assembly Data de hoy.

## Lo que se verificó en el código antes de planear

- **`spTrainingGuideSync`** (`02`) identifica la guía por `AreaId` + `Title` y **aborta**
  (`THROW 50014`) si una guía existente cambia de forma; acepta `StepType` de 0 a 8. Las
  sesiones guardan `TrainingGuideId` y los intentos `StepId`, así que reescribir los pasos de
  una guía usada rompería el historial → D-56.
- **Selección de guía:** `spTrainingGuideGetByArea` lista solo `IsPublished = 1` (la más
  nueva primero) y `spTrainingGuideGetById` no filtra: una sesión en curso sigue cargando su
  guía aunque se despublique.
- **Log NC hoy:** `PodService.OpenLogNC` revisa secuencia y fila scrapeable (sin fila =
  intento fallido); `SubmitLogNC` califica solo el código (`StepValidator.ValidateLogNC`:
  cualquiera del catálogo) y scrapea las filas seleccionadas. En Serialized acepta varias
  piezas (`PieceSelectionRule`). La elección del código en NC Selection ya es un postback de
  navegación (`BtnLogNCCode_Click`); Add-Done es el envío.
- **`GuidedTest.aspx.cs`** tiene `switch` sobre `StepType.LogNC` en al menos 6 lugares
  (control del paso, envío, apertura, hint, botones habilitados, texto de ayuda). Los dos
  tipos nuevos tienen que pasar por todos → un helper `StepTypes.IsLogNC(type)` en la BLL.
- **`CheckSequence`** compara `step.StepType == clickedStepType`: con tres tipos de Log NC
  hay que comparar por "familia" (el botón [Log NC] vale en los tres).
- **Assembly:** `AssemblyProgress` guarda solo los Ids ensamblados y `PodState.Assembly`
  pasa a `null` al resolver el paso. El Browse y Remove component necesitan lo ensamblado
  **después** de Assembly → registro propio en `PodState` que sobrevive al paso.
- **Reconstrucción** (`RebuildState`): en Serialized no sabe qué piezas se scrapearon y las
  deja todas. Con hallazgos en `PodState` (que también se pierden), la reconstrucción
  regenera hallazgos sobre las piezas nuevas y aplica la disposición de los pasos de Log NC
  ya resueltos.
- **`StepValidationContext`** tiene `ValidNCCodes`; no tiene filas seleccionadas ni
  hallazgo.
- **`Operations`** existe en `00` (`Id`, `AreaId`, `Name`), sin uso; solo la referencia
  `TrainingGuides.OperationId` (nullable).
- **Tests:** solo `ScoreCalculatorTests` y `UserAdminServiceTests`; no hay archivo de tests
  de `StepValidator` (AC-8 los pide).

## Decisiones

**Del usuario (2026-10-16)**
- **D-56 · Versión automática de las guías:** si una guía cambia de forma,
  `spTrainingGuideSync` **despublica** la guía vieja y crea una nueva con el **mismo
  título** (reemplaza el `THROW 50014`). Las sesiones en curso y el historial siguen con la
  vieja. El usuario aclara que la BD es de pruebas y borrar datos no es problema: no hace
  falta más cuidado que eso.
- **D-57 · Pesos de transición solo entre E2 y E3:** en E2, las guías de LoFlo y Redefinity
  tienen desecho 0.05 + reparación rápida 0.05, con DC en 0.30; en E3, la tabla final (DC
  0.25). No se sube a `main` entre E2 y E3.

**De diseño (propuestas en este plan; se aprueban con él)**
- **D-58 · Operaciones simuladas en `Operations`** (P-14, D-52): se agregan las columnas
  `Sequence INT NULL` y `Description NVARCHAR(200) NULL`, y cada área con extendida carga 2
  filas `SIM` (p. ej. `010 O_LOFLO_SIM_PREP` y `020 O_LOFLO_SIM_ENSAMBLE`). La actual es la de
  mayor `Sequence`; la anterior, la que le sigue. Nada más usa `Operations` todavía.
- **D-59 · La respuesta de Log NC es un JSON** que arma el control
  (`{"code","componentId","removeAction","addComponent","disposition"}`, cada campo según el
  método) y viaja en `StepSubmitEventArgs.Answer`. Las filas seleccionadas ya llegan aparte
  (`SelectedIds()`). `StepValidator` lo compara con el `NcFinding` del paso.
- **D-60 · Un solo control:** `LogNCControl` sirve a los tres métodos (NC Selection y NC Data
  Entry son las mismas pantallas); los modales de la reparación rápida y la extendida son
  paneles del mismo control, manejados en el cliente.

## Entrega 1 — Hallazgo y desecho rápido calificado (AC-1 a AC-4, AC-6 a AC-8, AC-10 a AC-12, AC-23, AC-24)

Todas las áreas, sin cambiar la forma de ninguna guía.

- [x] **Catálogo:** `00` agrega `NCCodes.Method TINYINT NOT NULL` (0 `FastScrap`, 1
      `FastRepair`, 2 `ExtendedRepair`) y `NCCodes.RequiresComments BIT NOT NULL`
      (idempotente, con default para las filas existentes). `spNCCodeGetByArea` las devuelve.
      `Domain/NCCode` + enum `NCMethod`.
- [x] **Scripts:** `06` marca `F_FR_GENERAL` = `FastRepair` y `F_FX_GENERAL` =
      `ExtendedRepair` + `RequiresComments`; los `F_FS_…` = `FastScrap`. `08` y `05` agregan
      2-3 códigos `SIM_FR_…` y 2-3 `SIM_FX_…` (uno con `RequiresComments`); los existentes
      quedan como `FastScrap`.
- [x] **4-5 piezas:** `SerialSfcGenerator.MinPieces = 4`.
- [x] **`StepTypes.IsLogNC`** (y el método de cada tipo de Log NC) en la BLL; `GuidedTest`
      y `CheckSequence` pasan a usarlo (el botón [Log NC] vale en cualquier paso de Log NC).
- [x] **`NcFinding`** (`[Serializable]`) y **`NcFindingGenerator`**: un hallazgo por cada
      paso de Log NC de la guía, al dar Start (`StartPieces`) y al reconstruir. Serialized:
      piezas distintas al azar. NonSerialized: "N unidades" (N al azar, menor que la cantidad
      del lote). Código al azar del método del paso. `PodState.NcFindings`.
- [x] **Calificación (P-3, D-31, D-33, P-9):** `StepValidationContext` recibe el hallazgo del
      paso y las filas seleccionadas. Desecho correcto solo con exactamente la pieza del
      hallazgo (Serialized) o el SFC del split (NonSerialized) y el código exacto. Motivo
      `WrongDecision`, mensaje genérico. `OpenLogNC` no cambia (sin fila = intento fallido).
- [x] **Avance forzado (P-5):** aplica la disposición del hallazgo (desecha su pieza).
      **Reconstrucción:** aplica los pasos de Log NC ya resueltos sobre los hallazgos nuevos.
- [x] **Nota de hallazgo (D-49):** panel con el estilo de la guía, visible en Guided y
      Assessment mientras el paso actual es de Log NC: "Hallazgo n de N", SFC (o "N unidades
      del lote"), defecto (descripción del código) y disposición. Ancla `data-guide`.
- [x] **Guía (D-43 y textos de abajo):** alerta en rojo en el subpaso 1; la guía señala la
      pieza del hallazgo y `Reject` en la selección y el código (como D-25).
- [x] **Tests:** `StepValidatorLogNCTests` (pieza correcta, otra pieza, pieza de más, SFC del
      lote, código de otro método, código exacto) y `NcFindingGeneratorTests` (piezas
      distintas, código del método). `ScoreCalculatorTests` en verde.

> **Decisiones de implementación de E1** (desarrollo, 2026-10-16; no cambian la spec ni el
> plan):
>
> - Disposición en la nota: "desechar la pieza (scrap)" en Serialized y "desechar las
>   unidades (scrap)" en Moldeo.
> - Si el estado del POD tiene orden pero le faltan los hallazgos, se reconstruye (D-10).
> - El subpaso Component del desecho sigue siendo una captura hasta E2, que trae el Browse.
> - Códigos `SIM` nuevos. LoFlo: `SIM_FR_CONECTOR_FLOJO`, `SIM_FR_TUBO`, `SIM_FR_FILTRO`,
>   `SIM_FX_GENERAL` (con Comments), `SIM_FX_SELLO` y `SIM_FX_SOLDADURA`. Redefinity:
>   `SIM_FR_HUB`, `SIM_FR_LUER`, `SIM_FX_GENERAL` (con Comments) y `SIM_FX_PUNTA`.

> **Enmienda 2 de la spec (2026-10-16), dentro de la E1:** en Moldeo el hallazgo dicta la
> cantidad del split (nota visible también en el SFC Split; el split forzado y la
> reconstrucción usan N) y la guía de Guided abre con un subpaso Info que resalta la nota.
> Textos en la spec y en la tabla de abajo.
>
> **Enmienda 3 de la spec (2026-10-16), dentro de la E1:** la cantidad del split se
> califica contra N al presionar Split (Assessment: intento fallido, el split no se aplica y
> el 4.º intento fuerza un split de N; Guided: la guía no lo deja). Un segundo Split después
> del correcto es un aviso de formato. Test: N siempre de 1 a lote − 1. Al cerrar la spec,
> `docs/domain.md` → "Qué cuenta como intento" lo recoge.

## Entrega 2 — Reparación rápida (AC-5, AC-13 a AC-17)

- [ ] **`StepType.LogNCFastRepair = 9`**; `02`: `StepType` de 0 a 10. **D-56** en
      `spTrainingGuideSync` (despublicar + guía nueva con el mismo título si cambia la forma).
- [ ] **Guías `05` y `09`** con el paso nuevo después del desecho y los **pesos de transición
      de D-57**. `07` sin cambios.
- [ ] **Registro de lo ensamblado:** `PodState` guarda por componente su Assembly Data (hoy:
      Batch = Process Lot) al ensamblarlo y lo conserva después del paso. La reconstrucción
      lo llena con valores por defecto.
- [ ] **Browse de componentes (D-39):** el ⧉ de Component abre el Browse (Component |
      Description, OK / Cancel) con lo ensamblado, en **todos** los códigos. En el desecho
      sigue obligatorio si `RequiresNCComponent` y no se califica.
      *Enmienda 4 de la spec (2026-10-19): solo en los códigos de reparación; el desecho no
      muestra Component en ningún área.*
- [ ] **Flujo en el cliente (spec, punto 10):** Add-Done → Confirm remover → Remove component
      (datos de P-11) → Confirm agregar → **Add Component** (modal propio, enmienda
      2026-10-16 de la spec; captura `modal add componenet .png`; sin E4, Add Assembly
      Details con ★ Batch Number vacío) → Add. Un solo postback (punto 12). Cancel y ✕ de Add
      Component tampoco envían. No a remover, Cancel y
      ✕ vuelven a NC Data Entry sin enviar; Return, No a agregar y Send to Routing envían la
      decisión equivocada.
- [ ] **Calificación (D-34, D-35, D-48):** pieza, código, componente, `Scrap` y `Yes`.
      Formato sin intento: Qty to Remove y Qty to Assemble (numéricos, obligatorios),
      Component y Version vacíos y los datos de Add Assembly Details. La pieza sigue activa (D-47). Banner provisional "Component {componente}
      replaced on {SFC}" (D-55), anotado en `general-flow.md`.
- [ ] **`AttemptActions`** para Scrap, Return, Send to Routing y "No" a agregar (registro de
      actividad, spec 013).
- [ ] **Guía:** textos de abajo, notas D-44, anclas `data-guide` de cada modal nuevo.
- [ ] **Tests:** casos de la reparación rápida en `StepValidatorLogNCTests`.

## Entrega 3 — Reparación extendida (AC-18 a AC-22)

- [ ] **`StepType.LogNCExtendedRepair = 10`**.
- [ ] **Operaciones (D-58):** `00` (columnas), `08` y `05` cargan sus 2 operaciones `SIM`.
      `PodContext` las expone.
- [ ] **Guías `05` y `09`** con la extendida y los **pesos finales** (DC 0.25); D-56 crea la
      versión nueva.
- [ ] **Flujo en el cliente (punto 14):** Comments obligatorio si `RequiresComments`
      (formato); Add-Done → "NC logged" en NC Data Entry → "Select Disposition Function for
      {SFC}" → Select → banner. Cancel no envía (P-15).
- [ ] **Calificación (D-37b):** pieza, código y disposición. Return to any previous: banner
      con la operación anterior y la pieza sale del POD (D-54). Local Rework: banner
      provisional con la operación actual y la pieza sigue (V-5).
- [ ] **Component (enmienda 4):** NC Data Entry lo muestra también con los códigos de
      reparación extendida, **opcional** y sin calificarse (hoy solo aparece con la rápida).
- [ ] **Guía:** textos de abajo y nota de P-13 en Comments.
- [ ] **Tests:** casos de la extendida en `StepValidatorLogNCTests`.

## Entrega 4 — Assembly Data por tipo de material (W13; AC-25 a AC-29) · recortable

- [ ] `00`: `AssemblyComponents.MaterialControl TINYINT NOT NULL` (0 `Batch`, 1
      `InventorySfc`, 2 `MaterialDocument`); `spAssemblyComponentGetByArea` la devuelve;
      `Domain/AssemblyComponent` + enum.
- [ ] `05` y `08`: al menos un componente de cada tipo, con batch simulado del componente.
- [ ] `AssemblyControl` (Assemble Components) muestra los campos de P-17 y Add Component los
      de la enmienda 2026-10-16 de la spec (Batch Number lo escribe el operador). Formato en el cliente y en el servidor (letras y números, obligatorio);
      nada se califica.
- [ ] Remove component muestra la Assembly Data registrada.
- [ ] Guía de Assembly: subpasos para Inventory SFC y Material Document Number (textos al
      implementar, aprobados por el usuario).

## Textos de la guía de Guided (D-50, para aprobar con el plan)

Tratamiento de usted, como el resto de la guía. `{…}` lo llena el hallazgo.

**Los tres métodos, subpasos comunes**
| Subpaso | Texto |
|---|---|
| Info (primero; enmienda 2) | Serialized, en cada paso de Log NC: "Calidad reportó un hallazgo: revise el SFC, el defecto y la disposición. En este paso lo registra en el MES." · Moldeo, en el SFC Split (no se repite en Log NC): "Calidad reportó un hallazgo: revise el defecto, la cantidad y la disposición. Primero separe esas unidades con SFC Split; después las registra en Log NC." |
| Cantidad del split (Moldeo; enmienda 2) | "Ingrese la cantidad del hallazgo: **N**." |
| Seleccionar | "Seleccione la pieza del hallazgo: **{SFC}**." + alerta D-43 en rojo (Moldeo: "Seleccione el SFC creado por el split.") |
| Log NC | "Presione **Log NC** para registrar el defecto." |
| Código | Desecho: "Presione **Log NC** en el código del defecto del hallazgo: **{defecto}**." · Reparación rápida: "…en el código de reparación rápida del hallazgo: **{defecto}**." · Extendida: "…en el código de reparación extendida del hallazgo: **{defecto}**." |
| ~~Component (desecho, si el área lo pide)~~ | Sin subpaso: el desecho no muestra Component (enmienda 4, 2026-10-19). |
| Add-Done | "Presione **Add-Done** para registrar el NC." |

**Reparación rápida**
| Subpaso | Texto |
|---|---|
| Component | "Presione **⧉** en Component para buscar el componente a reemplazar." |
| Browse | "Seleccione el componente a reemplazar y dele **OK**." Resaltado en las filas y, al seleccionar la del hallazgo, en OK (enmienda 4, 2026-10-19; antes: "Seleccione **{componente}** y presione **OK**.") |
| Confirm remover | "Presione **Yes** para quitar el componente de la pieza." |
| Remove component | "Presione **Scrap**: el componente dañado se desecha." + nota D-44 (Return) |
| Confirm agregar | "Presione **Yes** para agregar el componente nuevo." + nota D-44 (Yes/No) |
| Add Component | "Complete el **Batch Number** del componente nuevo (o los datos que pida su material) y presione **Add**." (enmienda 2026-10-16) |
| Banner | "Verifique el mensaje: el componente se reemplazó y la pieza sigue en el POD." |

**Reparación extendida**
| Subpaso | Texto |
|---|---|
| Comments (si el código lo exige) | "Anote el MPI y su revisión, o el número de NCR: **{MPI del hallazgo}**." |
| Disposición | "Presione **Select** en **{Local Rework / Return to any previous}**, la disposición del hallazgo." |
| Banner | Return to any previous: "Verifique el mensaje: la pieza se procesará en **{operación anterior}** y salió del POD." · Local Rework: "Verifique el mensaje: la pieza se retrabaja en esta operación y sigue en el POD." |

## Al cerrar la spec

- [ ] `docs/domain.md`: flujo estándar (paso 8 con los tres métodos, 4-5 piezas), "Qué
      cuenta como intento" (pieza + código en Add-Done; Return, No a agregar y Send to
      Routing fallan; No a remover, Cancel y Return to POD no cuentan), tabla de pesos y
      catálogos (`Method`, `RequiresComments`, `MaterialControl`, `Operations` `SIM`).
- [ ] `docs/architecture.md`: `NcFinding` y registro de lo ensamblado en `PodState`,
      versión automática de las guías (D-56), un control para los tres métodos (D-60).
- [ ] `docs/ui-reference/general-flow.md` (Pasos 4 y 8: banners provisionales, Browse en los
      códigos de reparación, enmienda 4), `inventario-pantallas.md` (#17-#23 a ✅), y si entró E4: `glossary.md`
      (Batch Number), `general-flow.md` → Paso 4 y el ejemplo de `docs/process.md` §8, que
      dicen "Batch Number = Process Lot".
- [ ] Roadmap: fila 6 de la Fase 8.5, "Estado actual", "Historial" y "BD de desarrollo".
- [ ] Roadmap → "Deuda y riesgos conocidos": `RequiresNCComponent` sin uso desde la enmienda 4
      (columna `Areas.RequiresNCComponent` en `00`, `05`, `06` y `08`, `spAreaGet*`,
      `AreaRepository` y `Area.RequiresNCComponent`); quitarla no entra en esta spec.

## Review

- E1: `high` (cambia la calificación de Log NC en todas las áreas, `CheckSequence` y la
  reconstrucción).
- E2 y E3: `medium`, foco en `StepValidator`, el JSON del control y el postback único.
- E4: `low` (no cambia el puntaje).
