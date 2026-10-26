using System;
using System.Collections.Generic;
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

    // Configuración de las guías (spec 020): el tipo de Resource Setup de cada una. Cada método
    // recibe el userId del admin autenticado y verifica CanManageGuides sin confiar en la página.
    public class GuideAdminService
    {
        private readonly IUserRepository _users;
        private readonly Func<IList<GuideSummary>> _publishedGuides;
        private readonly Func<int, ResourceSetupType, bool> _setResourceSetupType;

        public GuideAdminService()
            : this(new UserRepository(),
                   () => new TrainingGuideRepository().GetPublishedForAdmin(),
                   (id, type) => new TrainingGuideRepository().SetResourceSetupType(id, type))
        {
        }

        // Para los tests: el repositorio de guías se reemplaza.
        public GuideAdminService(IUserRepository users, Func<IList<GuideSummary>> publishedGuides,
            Func<int, ResourceSetupType, bool> setResourceSetupType)
        {
            _users                = users ?? throw new ArgumentNullException(nameof(users));
            _publishedGuides      = publishedGuides ?? throw new ArgumentNullException(nameof(publishedGuides));
            _setResourceSetupType = setResourceSetupType ?? throw new ArgumentNullException(nameof(setResourceSetupType));
        }

        // Solo las publicadas (D-199).
        public IList<GuideSummary> GetPublishedGuides(int adminUserId)
        {
            RequireGuideManager(adminUserId);
            return _publishedGuides();
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
