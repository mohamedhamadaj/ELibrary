using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ELibrary.DataAccess.EntityConfigurations
{
    public class FavoritEntityTypeConfiguration : IEntityTypeConfiguration<Favorite>
    {
        public void Configure(EntityTypeBuilder<Favorite> builder)
        {
            builder.HasKey(e => new { e.BookId, e.ApplicationUserId });
        }
    }
}
