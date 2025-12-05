using System.Data.Entity;

namespace Quixo.Web.Models
{
    public class QuixoDbInitializer : CreateDatabaseIfNotExists<QuixoDbContext>
    {
        protected override void Seed(QuixoDbContext context)
        {
            base.Seed(context);
        }
    }
}
