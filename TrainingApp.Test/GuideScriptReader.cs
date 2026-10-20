using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TrainingApp.Domain;

namespace TrainingApp.Test
{
    // Lee los pasos de una guía de su script de Database/StoredProcedures: las filas
    // ([Order], StepType, Weight) del INSERT INTO @Steps que recibe spTrainingGuideSync.
    internal static class GuideScriptReader
    {
        private static readonly Regex StepRow =
            new Regex(@"^\s*\((\d+),\s*(\d+),\s*(\d+\.\d+)\)[,;]", RegexOptions.Multiline);

        public static List<TrainingStep> Read(string scriptName)
        {
            string sql = File.ReadAllText(Path.Combine(ScriptsFolder(), scriptName));
            int start = sql.IndexOf("INSERT INTO @Steps", StringComparison.Ordinal);
            Assert.That(start, Is.GreaterThanOrEqualTo(0), scriptName + " no tiene INSERT INTO @Steps.");
            int end = sql.IndexOf("spTrainingGuideSync", start, StringComparison.Ordinal);

            return StepRow.Matches(sql.Substring(start, end - start))
                .Cast<Match>()
                .Select(m => new TrainingStep
                {
                    Order    = int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture),
                    StepType = (StepType)int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture),
                    Weight   = decimal.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture)
                })
                .ToList();
        }

        // TrainingApp.Test/bin/Debug → raíz del repositorio → Database/StoredProcedures.
        private static string ScriptsFolder()
        {
            var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "Database", "StoredProcedures")))
                dir = dir.Parent;
            Assert.That(dir, Is.Not.Null, "No se encontró Database/StoredProcedures.");
            return Path.Combine(dir.FullName, "Database", "StoredProcedures");
        }
    }
}
