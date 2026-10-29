using System;
using System.Collections.Generic;
using System.Linq;
using TrainingApp.BLL.Auth;
using TrainingApp.DAL.Repositories;
using TrainingApp.Domain;

namespace TrainingApp.BLL.Admin
{
    public enum GuideAdminResult
    {
        Ok,
        GuideNotFound
    }

    // Las guías publicadas para la página Guías, con las Custom primero y el conteo por tipo
    // (spec 022, D-233, D-235). Todo tipo que no sea Custom cuenta como Estándar.
    public class PublishedGuideList
    {
        public IList<PublishedGuide> Guides { get; set; } = new List<PublishedGuide>();
        public int StandardCount { get; set; }
        public int CustomCount { get; set; }
    }

    // Configuración de las guías (spec 020): el tipo de Resource Setup de cada una. Cada método
    // recibe el userId del admin autenticado y verifica CanManageGuides sin confiar en la página.
    public class GuideAdminService
    {
        private readonly IUserRepository _users;
        private readonly Func<IList<PublishedGuide>> _publishedGuides;
        private readonly Func<int, ResourceSetupType, bool> _setResourceSetupType;

        public GuideAdminService()
            : this(new UserRepository(),
                   () => new TrainingGuideRepository().GetPublishedForAdmin(),
                   (id, type) => new TrainingGuideRepository().SetResourceSetupType(id, type))
        {
        }

        // Para los tests: el repositorio de guías se reemplaza.
        public GuideAdminService(IUserRepository users, Func<IList<PublishedGuide>> publishedGuides,
            Func<int, ResourceSetupType, bool> setResourceSetupType)
        {
            _users                = users ?? throw new ArgumentNullException(nameof(users));
            _publishedGuides      = publishedGuides ?? throw new ArgumentNullException(nameof(publishedGuides));
            _setResourceSetupType = setResourceSetupType ?? throw new ArgumentNullException(nameof(setResourceSetupType));
        }

        // Solo las publicadas (D-199), las Custom primero; OrderBy es estable, así que dentro de
        // cada tipo queda el orden del repositorio (D-233).
        public PublishedGuideList GetPublishedGuides(int adminUserId)
        {
            RequireGuideManager(adminUserId);
            var guides = _publishedGuides()
                .OrderBy(g => g.ResourceSetupType == ResourceSetupType.Custom ? 0 : 1)
                .ToList();
            int custom = guides.Count(g => g.ResourceSetupType == ResourceSetupType.Custom);
            return new PublishedGuideList
            {
                Guides        = guides,
                CustomCount   = custom,
                StandardCount = guides.Count - custom
            };
        }

        // Vale para las sesiones que empiecen después; las que están en curso conservan el suyo (D-188).
        public GuideAdminResult SetResourceSetupType(int adminUserId, int guideId, ResourceSetupType type)
        {
            RequireGuideManager(adminUserId);
            if (!Enum.IsDefined(typeof(ResourceSetupType), type))
                throw new ArgumentOutOfRangeException(nameof(type), type, "Tipo de Resource Setup inválido.");

            return _setResourceSetupType(guideId, type) ? GuideAdminResult.Ok : GuideAdminResult.GuideNotFound;
        }

        private void RequireGuideManager(int adminUserId)
        {
            var admin = _users.GetById(adminUserId);
            if (admin == null || !AccessPolicy.CanManageGuides(admin.Role))
                throw new UnauthorizedAccessException("El usuario no puede configurar las guías.");
        }
    }
}
