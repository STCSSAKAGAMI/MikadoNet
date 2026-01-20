using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using MikadoNet.Data;
using MikadoNet.Models;

namespace MikadoNet.Migrations;

[DbContext(typeof(MikadoNetDbContext))]
partial class MikadoNetDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasAnnotation("ProductVersion", "8.0.0");

        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.Property<string>("Id").HasColumnType("nvarchar(450)");
            entity.Property<short?>("TantoCode").HasColumnType("smallint");
            entity.HasKey("Id");
            entity.ToTable("AspNetUsers");
        });

        modelBuilder.Entity<DatHaiin>(entity =>
        {
            entity.HasKey(e => new { e.WorkDate, e.HaiinCode });
            entity.ToTable("dat_配員");
        });

        modelBuilder.Entity<DatHaiinLog>(entity =>
        {
            entity.HasKey(e => e.Sequence);
            entity.ToTable("dat_配員ログ");
        });
    }
}
