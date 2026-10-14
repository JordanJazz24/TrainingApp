using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using TrainingApp.Domain;

namespace TrainingApp.BLL.Scoring
{
    // Spec 015 (C): nombre de la persona en entrenamiento del grupo de DC Trainer
    // (DCGTM_TRAINEE/A). Letras (con tildes y ñ) y espacios, de 2 a 60 caracteres tras recortar;
    // vacío es válido porque el grupo es opcional. Lo que no cumple es formato: lo bloquea el
    // control, sin intento. DataCollectionControl repite el mismo patrón en JS (ClientPattern).
    public static class PersonNameRule
    {
        // DataCollectionParameters.DataType del parámetro de nombre.
        public const string DataType = "PersonName";

        public const int MinLength = 2;
        public const int MaxLength = 60;

        // Sobre el valor recortado; también lo usa el cliente (con el flag "u" de JS). \p{M}: una
        // tilde escrita como carácter combinable (e + ◌́) también es letra.
        public const string ClientPattern = @"^[\p{L}\p{M} ]{2,60}$";

        private static readonly Regex Pattern = new Regex(ClientPattern);

        public static bool IsValid(string value)
        {
            string text = (value ?? string.Empty).Trim();
            return text.Length == 0 || Pattern.IsMatch(text);
        }

        // ¿Algún parámetro de nombre de las respuestas de un DC Group ({paramId: valor}) no
        // cumple? JSON ilegible: false (lo rechaza la calificación). Lo usan StepValidator y el
        // control, con el mismo parseo.
        public static bool HasInvalid(IEnumerable<DataCollectionParameter> groupParams, string answersJson)
        {
            Dictionary<string, string> answers;
            try
            {
                answers = string.IsNullOrWhiteSpace(answersJson)
                    ? null : JsonConvert.DeserializeObject<Dictionary<string, string>>(answersJson);
            }
            catch (JsonException)
            {
                return false;
            }
            if (answers == null) return false;

            return groupParams
                .Where(p => p.DataType == DataType)
                .Any(p =>
                {
                    string value;
                    return answers.TryGetValue(p.Id.ToString(CultureInfo.InvariantCulture), out value)
                        && !IsValid(value);
                });
        }
    }
}
