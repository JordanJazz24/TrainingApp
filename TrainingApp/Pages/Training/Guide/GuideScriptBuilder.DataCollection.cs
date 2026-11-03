using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TrainingApp.BLL.Scoring;
using TrainingApp.BLL.Training;
using TrainingApp.Domain;

namespace TrainingApp.Pages.Training.Guide
{
    public sealed partial class GuideScriptBuilder
    {
        // Data Collection: cada pieza (Serialized; NonSerialized, el lote) por cada grupo
        // obligatorio (R4: si el área no tiene, el primero). Lo guardado antes de este request
        // no se muestra; del grupo recién guardado queda su banner. El último Save completa el
        // paso sin mensaje, así que no hay cola.
        private static StepScript DataCollection(PodContext ctx, TrainingStep step, GuideMoment moment)
        {
            var s = new StepScript(step, "Data Collection");
            AddPodSelection(s, ctx, AllPiecesNote);
            s.Add("open", GuideMode.Click, Anchor("pod-dc-collect"), postback: true,
                html: H("Presione ", B("DC Collect"), " para abrir la Data Collection List."),
                note: H("La cantidad de datos a ingresar, dependerá de la operación."));

            var groups = ctx.DataCollectionGroups;
            var required = groups.Where(g => g.IsRequired).ToList();
            if (required.Count == 0 && groups.Count > 0) required.Add(groups[0]);

            var progress = ctx.State.DataCollection;
            var pieces = DataCollectionPieces(ctx);
            int currentPiece = progress?.CurrentIndex ?? 0;
            string justSaved = s.DetailOf(moment, GuideMomentKind.Progress);

            AddTrainerNote(s, ctx, groups, pieces, progress, currentPiece);

            for (int pi = 0; pi < pieces.Count; pi++)
            {
                var piece = pieces[pi];
                for (int gi = 0; gi < required.Count; gi++)
                {
                    var g = required[gi];
                    bool lastPair = pi == pieces.Count - 1 && gi == required.Count - 1;

                    AddGroupSubsteps(s, ctx, piece, g, lastPair, completesPiece: gi == required.Count - 1);
                    ApplyGroupProgress(s, progress, piece, g, pi, currentPiece, lastPair, justSaved);
                }
            }
            return s;
        }

        // Grupo Trainer (spec 015, D-24): la guía no pasa por él; una nota sobre su fila, una
        // sola vez, al abrir la lista con la primera pieza y antes del primer Collect.
        private static void AddTrainerNote(StepScript s, PodContext ctx, IList<DataCollectionGroup> groups,
            IList<DataCollectionPiece> pieces, DataCollectionProgress progress, int currentPiece)
        {
            var trainer = groups.FirstOrDefault(g => ctx.DataCollectionParameters.Any(p =>
                p.DataCollectionGroupId == g.Id && p.DataType == PersonNameRule.DataType));
            if (trainer != null)
            {
                s.Add("trainer", GuideMode.Info, Anchor("dc-collect") + Attr("data-group", trainer.Code),
                    html: H(B("NOTA: "), "Complete ", B(trainer.Code), " únicamente cuando esté realizando un entrenamiento e ingrese su nombre."));
                bool started = pieces.Count > 0 && progress != null &&
                    (currentPiece > 0 || progress.SavedGroupIds(pieces[0].RowId).Count > 0);
                if (started) s.Hide("trainer");
                else s.OpenedName = "trainer";
            }
        }

        // Los subpasos de un grupo en una pieza: Collect, el bloque de parámetros, Save y, si no
        // es el último par, el banner (con su entrada por detalle).
        private static void AddGroupSubsteps(StepScript s, PodContext ctx, DataCollectionPiece piece,
            DataCollectionGroup g, bool lastPair, bool completesPiece)
        {
            bool serialized = ctx.Area.SerializationType == SerializationType.Serialized;
            string p = GroupKey(piece, g);
            string detail = piece.RowId + "|" + g.Code;

            s.Add(p + ".collect", GuideMode.Click, Anchor("dc-collect") + Attr("data-group", g.Code), postback: true,
                html: serialized
                    ? H("Presione ", B("Collect"), " en el grupo ", B(g.Code), " para la pieza ", B(piece.SFCCode), ".")
                    : H("Presione ", B("Collect"), " en el grupo ", B(g.Code), "."));
            var block = DataCollectionBlock(ctx, g);
            s.Add(p + ".block", GuideMode.Capture, Anchor("dc-params"), fields: block.Item1, html: block.Item2);
            s.Add(p + ".save", GuideMode.Click, Anchor("dc-save"), postback: true, retry: p + ".block",
                html: H("Presione ", B("Save"), " para guardar el grupo."));
            if (!lastPair)
            {
                s.Add(p + ".banner", GuideMode.Info, Anchor("pod-banner"), hide: true,
                    html: completesPiece
                        ? H("Verifique el mensaje: la pieza ", B(piece.SFCCode), " completó Data Collection y sigue la siguiente.")
                        : H("Verifique el mensaje ", B("All data for DC group " + g.Code + " has been collected"), "."));
                s.ProgressByDetail[detail] = p + ".banner";
            }
        }

        // Lo ya guardado no se muestra; lo que falta fija dónde abre el control y, en la pieza
        // actual, a qué bloque vuelven Collect y un intento incorrecto.
        private static void ApplyGroupProgress(StepScript s, DataCollectionProgress progress,
            DataCollectionPiece piece, DataCollectionGroup g, int pi, int currentPiece, bool lastPair, string justSaved)
        {
            string p = GroupKey(piece, g);
            string detail = piece.RowId + "|" + g.Code;

            bool saved = progress != null && (pi < currentPiece ||
                (pi == currentPiece && progress.SavedGroupIds(piece.RowId).Contains(g.Id)));
            if (saved)
            {
                s.Hide(p + ".collect", p + ".block", p + ".save");
                if (!lastPair && detail != justSaved) s.Hide(p + ".banner");
            }
            else
            {
                if (s.OpenedName == null) s.OpenedName = p + ".collect";
                // Collect abrió el grupo (o un intento incorrecto lo reabrió): su bloque.
                if (pi == currentPiece)
                {
                    s.OpenedByDetail[g.Code]    = p + ".block";
                    s.IncorrectByDetail[g.Code] = p + ".block";
                }
            }
        }

        private static string GroupKey(DataCollectionPiece piece, DataCollectionGroup g)
            => "dc." + piece.RowId + "." + g.Id.ToString(CultureInfo.InvariantCulture);

        // Piezas de Data Collection: las que fijó la BLL al abrirlo o, antes de abrirlo, las
        // filas activas de la orden (las mismas que tomará: todas en Serialized, el lote en
        // NonSerialized).
        private static IList<DataCollectionPiece> DataCollectionPieces(PodContext ctx)
        {
            var order = ctx.State.Order;
            if (ctx.State.DataCollection != null)
                return ctx.State.DataCollection.Pieces
                    .Select(p => new DataCollectionPiece { RowId = p.RowId, SFCCode = p.SFCCode ?? order?.SFCCode })
                    .ToList();
            if (order == null) return new List<DataCollectionPiece>();
            return order.ActiveRows()
                .Select(r => new DataCollectionPiece { RowId = r.RowId, SFCCode = r.SFCCode ?? order.SFCCode })
                .ToList();
        }

        // Bloque de un grupo de DC: las reglas de cada parámetro (las de StepValidator) y el
        // texto que las lista.
        private static Tuple<List<GuideField>, string> DataCollectionBlock(PodContext ctx, DataCollectionGroup group)
        {
            var fields = new List<GuideField>();
            var lines = new List<string> { H("Ingrese los valores:") };
            var parameters = ctx.DataCollectionParameters
                .Where(p => p.DataCollectionGroupId == group.Id)
                .OrderBy(p => p.DisplayOrder);

            foreach (var prm in parameters)
            {
                string selector = Attr("data-paramid", prm.Id.ToString(CultureInfo.InvariantCulture));
                string name = (prm.Name ?? string.Empty).Trim().TrimEnd(':');
                switch (prm.DataType)
                {
                    case "Boolean":
                        fields.Add(new GuideField { Selector = selector, Expect = AnswerYes });
                        lines.Add(H("• ", B(name), ": ", B("Si")));
                        break;
                    case "Date":
                        fields.Add(new GuideField { Selector = selector, Rule = "date" });
                        lines.Add(H("• ", B(name), ": una fecha válida, por ejemplo ",
                            B(DateTime.Today.AddYears(1).ToString("d 'de' MMMM 'de' yyyy", Spanish))));
                        break;
                    case "Numeric":
                        fields.Add(new GuideField { Selector = selector, Rule = "number", Min = prm.MinValue, Max = prm.MaxValue });
                        lines.Add(H("• ", B(name), ": ") + NumericRange(prm));
                        break;
                    default:
                        fields.Add(new GuideField { Selector = selector, Rule = "required" });
                        lines.Add(H("• ", B(name)));
                        break;
                }
            }
            return Tuple.Create(fields, string.Join("<br>", lines));
        }

        private static string NumericRange(DataCollectionParameter prm)
        {
            string unit = string.IsNullOrWhiteSpace(prm.Unit) ? string.Empty : " " + prm.Unit.Trim();
            if (prm.MinValue.HasValue && prm.MaxValue.HasValue)
                return H("entre ", B(Num(prm.MinValue.Value)), " y ", B(Num(prm.MaxValue.Value)), unit);
            if (prm.MinValue.HasValue)
                return H("mayor o igual a ", B(Num(prm.MinValue.Value)), unit);
            if (prm.MaxValue.HasValue)
                return H("menor o igual a ", B(Num(prm.MaxValue.Value)), unit);
            return H("un valor numérico");
        }

        private static string Num(decimal value) => value.ToString("0.######", CultureInfo.InvariantCulture);
    }
}
