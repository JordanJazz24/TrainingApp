using System.Security.Principal;
using TrainingApp.Domain;

namespace TrainingApp.Pages
{
    // Usuario autenticado con su rol (spec 013). Lo arma Global.asax en cada request con el rol
    // que da la BLL (RoleCache): con él funcionan los <location> con allow roles de Web.config
    // y SessionHelper pregunta por capacidades. El nombre del rol solo se usa en Web.config.
    public sealed class UserPrincipal : GenericPrincipal
    {
        public UserPrincipal(IIdentity identity, UserRole role)
            : base(identity, new[] { role.ToString() })
        {
            Role = role;
        }

        public UserRole Role { get; }
    }
}
