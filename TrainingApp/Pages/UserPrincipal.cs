using System.Security.Principal;
using TrainingApp.Domain;

namespace TrainingApp.Pages
{
    // Usuario autenticado con su rol (spec 013). Lo arma Global.asax en cada request con la
    // cuenta que da la BLL (RoleCache): con él funcionan los <location> con allow roles de
    // Web.config y SessionHelper pregunta por capacidades. El nombre del rol solo se usa en
    // Web.config. UserId es el de la cuenta actual: SessionHelper lo compara con la Session.
    public sealed class UserPrincipal : GenericPrincipal
    {
        public UserPrincipal(IIdentity identity, int userId, UserRole role)
            : base(identity, new[] { role.ToString() })
        {
            UserId = userId;
            Role   = role;
        }

        public int      UserId { get; }
        public UserRole Role   { get; }
    }
}
