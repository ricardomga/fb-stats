using System.Data.Entity;

namespace Primavera.eCommerce.Statistics.Models
{
    public class StatisticsContext : DbContext
    {
        public StatisticsContext() : base("eCommerceStatistics")
        {
            // Database.SetInitializer(new DropCreateDatabaseAlways<StatisticsContext>());
        }

        public virtual DbSet<Item> Items { get; set; }
        public virtual DbSet<FbUser> FbUsers { get; set; }
        public virtual DbSet<Like> Likes { get; set; }
        public virtual DbSet<Comment> Comments { get; set; }
    }
}
