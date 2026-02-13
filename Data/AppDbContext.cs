using AfghanPay.API.Models;
using AfghanPay.Models;
using Microsoft.EntityFrameworkCore;

namespace AfghanPay.API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Agent> Agents { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<Fee> Fees { get; set; }
        public DbSet<Commission> Commissions { get; set; }
        public DbSet<AdminUser> AdminUsers { get; set; }
        public DbSet<CashoutRequest> cashoutrequest { get; set; }
        public DbSet<AdminEvents> AdminEvents { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure table names to match PostgreSQL
            modelBuilder.Entity<User>().ToTable("users");
            modelBuilder.Entity<Agent>().ToTable("agents");
            modelBuilder.Entity<Transaction>().ToTable("transactions");
            modelBuilder.Entity<Fee>().ToTable("fees");
            modelBuilder.Entity<Commission>().ToTable("commissions");
            modelBuilder.Entity<AdminUser>().ToTable("admin_users");

            // Configure column names to match snake_case in PostgreSQL
            modelBuilder.Entity<User>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.PhoneNumber).HasColumnName("phone_number");
                entity.Property(e => e.FullName).HasColumnName("full_name");
                entity.Property(e => e.PinHash).HasColumnName("pin_hash");
                entity.Property(e => e.Balance).HasColumnName("balance");
                entity.Property(e => e.Status).HasColumnName("status");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            });

            modelBuilder.Entity<Agent>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.UserId).HasColumnName("user_id");
                entity.Property(e => e.AgentCode).HasColumnName("agent_code");
                entity.Property(e => e.FloatBalance).HasColumnName("float_balance");
                entity.Property(e => e.CommissionBalance).HasColumnName("commission_balance");
                entity.Property(e => e.Status).HasColumnName("status");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId);
            });

            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.TransactionRef).HasColumnName("transaction_ref");
                entity.Property(e => e.Type).HasColumnName("type");
                entity.Property(e => e.SenderId).HasColumnName("sender_id");
                entity.Property(e => e.ReceiverId).HasColumnName("receiver_id");
                entity.Property(e => e.AgentId).HasColumnName("agent_id");
                entity.Property(e => e.Amount).HasColumnName("amount");
                entity.Property(e => e.Fee).HasColumnName("fee");
                entity.Property(e => e.Status).HasColumnName("status");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");

                entity.HasOne(e => e.Sender)
                    .WithMany()
                    .HasForeignKey(e => e.SenderId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Receiver)
                    .WithMany()
                    .HasForeignKey(e => e.ReceiverId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Agent)
                    .WithMany()
                    .HasForeignKey(e => e.AgentId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Fee>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.TransactionType).HasColumnName("transaction_type");
                entity.Property(e => e.FeePercentage).HasColumnName("fee_percentage");
                entity.Property(e => e.MinFee).HasColumnName("min_fee");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            });

            modelBuilder.Entity<Commission>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.AgentId).HasColumnName("agent_id");
                entity.Property(e => e.TransactionId).HasColumnName("transaction_id");
                entity.Property(e => e.Amount).HasColumnName("amount");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");

                entity.HasOne(e => e.Agent)
                    .WithMany()
                    .HasForeignKey(e => e.AgentId);

                entity.HasOne(e => e.Transaction)
                    .WithMany()
                    .HasForeignKey(e => e.TransactionId);
            });

            modelBuilder.Entity<AdminUser>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Username).HasColumnName("username");
                entity.Property(e => e.PasswordHash).HasColumnName("password_hash");
                entity.Property(e => e.FullName).HasColumnName("full_name");
                entity.Property(e => e.Role).HasColumnName("role");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            });
        }
    }
}
