using System.Collections.Generic;
using System.Linq;
using TrainingApp.Domain;

namespace TrainingApp.BLL.Scoring
{
    // Cuándo una pieza completa Data Collection (spec 008, punto 8):
    //   - con grupos obligatorios en el área: al guardar todos ellos (los opcionales no cuentan);
    //   - sin grupos obligatorios: al guardar un grupo cualquiera.
    public static class DataCollectionCompletionRule
    {
        public static bool IsPieceComplete(ICollection<int> savedGroupIds, IList<DataCollectionGroup> areaGroups)
        {
            if (savedGroupIds == null || savedGroupIds.Count == 0 || areaGroups == null)
                return false;

            var required = areaGroups.Where(g => g.IsRequired).ToList();
            if (required.Count == 0)
                return areaGroups.Any(g => savedGroupIds.Contains(g.Id));

            return required.All(g => savedGroupIds.Contains(g.Id));
        }
    }
}
