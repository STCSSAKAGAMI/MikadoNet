using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MikadoNet.Models;

namespace MikadoNet.Data;

public class MikadoNetDbContext : IdentityDbContext<AppUser>
{
    public MikadoNetDbContext(DbContextOptions<MikadoNetDbContext> options) : base(options)
    {
    }

    public DbSet<DatHaiin> HaiinRecords => Set<DatHaiin>();
    public DbSet<DatHaiinLog> HaiinLogs => Set<DatHaiinLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<DatHaiin>()
            .HasKey(entity => new { entity.WorkDate, entity.HaiinCode });

        modelBuilder.Entity<DatHaiin>()
            .Property(entity => entity.HaiinCode)
            .ValueGeneratedNever();

        modelBuilder.Entity<DatHaiinLog>()
            .HasKey(entity => entity.Sequence);

        modelBuilder.Entity<DatHaiinLog>()
            .Property(entity => entity.Sequence)
            .ValueGeneratedNever();
    }
}
