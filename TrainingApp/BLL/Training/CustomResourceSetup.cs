using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TrainingApp.Domain;

namespace TrainingApp.BLL.Training
{
    // Lo que muestra Custom Resource Setup (spec 020): campos de solo lectura y los Tool Groups.
    public class CustomResourceSetupData
    {
        public string Site { get; set; }
        public string Resource { get; set; }
        public string Operation { get; set; }
        public IList<ToolGroup> ToolGroups { get; set; }
    }

    // Único punto de acceso a los datos de Custom RS (D-184). Resource y Operation son los
    // simulados del POD; los Tool Groups salen de un pool simulado. Con las Operations reales,
    // solo cambia de dónde lee este proveedor.
    public static class CustomResourceSetupProvider
    {
        public const int MinToolGroups = 2;
        public const int MaxToolGroups = 4;

        // Los de la captura del MES real (Custom Resourse Setup/tabal de heraemintas cargada).
        public static readonly IReadOnlyList<ToolGroup> Pool = new List<ToolGroup>
        {
            new ToolGroup { Code = "TG_FLUID_DISPENSER",   Description = "Dispensador de fluido" },
            new ToolGroup { Code = "TG_HEATER_BLOCK",      Description = "Bloque de calentamiento" },
            new ToolGroup { Code = "TG_HOLE_GAUGE",        Description = "Hole Gauge" },
            new ToolGroup { Code = "TG_RULER",             Description = "Regla" },
            new ToolGroup { Code = "TG_SOLDERING_STATION", Description = "Estacion de Soldadura" },
            new ToolGroup { Code = "TG_TIMER",             Description = "Cronómetro" },
            new ToolGroup { Code = "TG_UV_LIGHT",          Description = "Fuente de luz ultravioleta" },
            new ToolGroup { Code = "TG_WELLER_HOT_AIR",    Description = "Estación de aire caliente" }
        };

        public static CustomResourceSetupData For(PodContext ctx) => new CustomResourceSetupData
        {
            Site       = PodDefaults.Site,
            Resource   = ctx.ResourceName,
            Operation  = ctx.CurrentOperationName,
            ToolGroups = ToolGroupsFor(ctx.Session.Id)
        };

        // De 2 a 4 grupos sorteados con el Id de la sesión como semilla: el mismo conjunto en
        // cada request de la sesión sin guardarlo (D-184); otra sesión, otro sorteo (D-192).
        // Orden alfabético por código, como la captura.
        public static IList<ToolGroup> ToolGroupsFor(int sessionId)
        {
            var random = new Random(sessionId);
            int count  = random.Next(MinToolGroups, MaxToolGroups + 1);
            // ToList antes de ordenar: cada grupo saca su clave una sola vez, en el orden del pool.
            return Pool.Select(g => new { Group = g, Key = random.Next() })
                       .ToList()
                       .OrderBy(x => x.Key)
                       .Take(count)
                       .Select(x => new ToolGroup { Code = x.Group.Code, Description = x.Group.Description })
                       .OrderBy(g => g.Code, StringComparer.Ordinal)
                       .ToList();
        }
    }

    public class CustomToolEntry
    {
        [JsonProperty("group")] public string Group { get; set; }
        [JsonProperty("value")] public string Value { get; set; }
    }

    // Save de Custom RS: {"kind":"custom","setupState","shopOrderAssociated","shopOrder","tools"}.
    // "kind" lo distingue del Save de la estándar ({"setupState","rows"}).
    public class CustomResourceSetupAnswer
    {
        public const string Kind = "custom";

        [JsonProperty("kind")]                public string KindValue { get; set; }
        [JsonProperty("setupState")]          public string SetupState { get; set; }
        [JsonProperty("shopOrderAssociated")] public bool ShopOrderAssociated { get; set; }
        [JsonProperty("shopOrder")]           public string ShopOrder { get; set; }
        [JsonProperty("tools")]               public List<CustomToolEntry> Tools { get; set; }

        public bool HasToolNumber => Tools != null && Tools.Any(t => !string.IsNullOrWhiteSpace(t?.Value));

        // Invariantes del Save antes de calificarlo; violarlos es una manipulación, no un intento:
        // - el Save tiene que ser de la pantalla de la sesión (D-196);
        // - con la orden asociada, Shop Order numérica y al menos un Tool Number, que el control
        //   ya avisa como formato (D-191).
        // Devuelve el Save de Custom, o null si la sesión es Standard (lo sigue SubmitResourceSetup).
        public static CustomResourceSetupAnswer RequireForSession(ResourceSetupType sessionType, string json)
        {
            bool custom = IsCustomShape(json);
            if (custom != (sessionType == ResourceSetupType.Custom))
                throw new ArgumentException("El Save no corresponde al Resource Setup de la sesión.", nameof(json));
            if (!custom) return null;

            var answer = TryParse(json)
                ?? throw new ArgumentException("El Save de Custom Resource Setup está mal formado.", nameof(json));
            if (answer.ShopOrderAssociated &&
                (!SimulatedOrder.IsValidShopOrder(answer.ShopOrder?.Trim()) || !answer.HasToolNumber))
                throw new ArgumentException("La orden asociada debe ser numérica y llevar al menos un Tool Number.", nameof(json));
            return answer;
        }

        // Shop Order con la que se crea la orden tras calificar el Save: la por defecto si el paso
        // avanzó forzado; la asociada, aunque sea "Open" (D-193); null sin asociar (no hay orden).
        public static string ShopOrderToCreate(StepSubmitOutcome outcome, CustomResourceSetupAnswer answer)
        {
            if (outcome == StepSubmitOutcome.FailedForced) return PodDefaults.DefaultShopOrder;
            return answer.ShopOrderAssociated ? answer.ShopOrder.Trim() : null;
        }

        // null si el JSON no es un Save de Custom RS.
        public static CustomResourceSetupAnswer TryParse(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try
            {
                var answer = JsonConvert.DeserializeObject<CustomResourceSetupAnswer>(json);
                return answer?.KindValue == Kind ? answer : null;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        public static bool IsCustomShape(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return false;
            try
            {
                return JToken.Parse(json) is JObject obj && obj["kind"]?.Type == JTokenType.String &&
                       obj.Value<string>("kind") == Kind;
            }
            catch (JsonException)
            {
                return false;
            }
        }
    }
}
