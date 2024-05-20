using System;
using System.Collections.Generic;
using FuckingDbIlay.Models;
using Microsoft.EntityFrameworkCore;

namespace FuckingDbIlay.Context {
    public partial class NotesDbContext : DbContext {
        public NotesDbContext() { }

        public NotesDbContext(DbContextOptions<NotesDbContext> options)
            : base(options) {
        }

        public virtual DbSet<Category> Categories { get; set; }
        public virtual DbSet<Note> Notes { get; set; }
        public virtual DbSet<NotesCategory> NotesCategories { get; set; }
        public virtual DbSet<Photo> Photos { get; set; }
        public virtual DbSet<ToDoList> ToDoLists { get; set; }
        public virtual DbSet<User> Users { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
            optionsBuilder.UseSqlServer("data source=ytin-pc;initial catalog=NotesDB;trusted_connection=true;Encrypt=False");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            modelBuilder.Entity<Category>(entity => {
                entity.HasKey(e => e.Id).HasName("PK__Categori__3214EC27AE2040ED");
                entity.Property(e => e.Id).HasColumnName("ID");
                entity.Property(e => e.Name).HasMaxLength(255);
            });

            modelBuilder.Entity<Note>(entity => {
                entity.HasKey(e => e.Id).HasName("PK__Notes__3214EC27CA8F7B19");
                entity.Property(e => e.Id).HasColumnName("ID").UseIdentityColumn();
                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("(getdate())")
                    .HasColumnType("datetime");
                entity.Property(e => e.Title).HasMaxLength(255);
                entity.Property(e => e.UserId).HasColumnName("UserID");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.Notes)
                    .HasForeignKey(d => d.UserId)
                    .HasConstraintName("FK__Notes__UserID__3A81B327");
            });

            modelBuilder.Entity<NotesCategory>(entity => {
                entity.HasKey(e => e.Id).HasName("PK__Notes_Ca__3214EC27CD8A9022");
                entity.ToTable("Notes_Categories");
                entity.Property(e => e.Id).HasColumnName("ID");
                entity.Property(e => e.CategoryId).HasColumnName("CategoryID");
                entity.Property(e => e.NoteId).HasColumnName("NoteID");

                entity.HasOne(d => d.Category)
                    .WithMany(p => p.NotesCategories)
                    .HasForeignKey(d => d.CategoryId)
                    .HasConstraintName("FK__Notes_Cat__Categ__403A8C7D");

                entity.HasOne(d => d.Note)
                    .WithMany(p => p.NotesCategories)
                    .HasForeignKey(d => d.NoteId)
                    .HasConstraintName("FK__Notes_Cat__NoteI__3F466844");
            });

            modelBuilder.Entity<Photo>(entity => {
                entity.HasKey(e => e.Id).HasName("PK__Photos__3214EC273CC02B22");
                entity.Property(e => e.Id).HasColumnName("ID");
                entity.Property(e => e.NoteId).HasColumnName("NoteID");
                entity.Property(e => e.Path).HasMaxLength(255);

                entity.HasOne(d => d.Note)
                    .WithMany(p => p.Photos)
                    .HasForeignKey(d => d.NoteId)
                    .HasConstraintName("FK__Photos__NoteID__4316F928");
            });

            modelBuilder.Entity<ToDoList>(entity => {
                entity.HasKey(e => e.Id).HasName("PK__ToDoList__3214EC27553487B0");
                entity.Property(e => e.Id).HasColumnName("ID");
                entity.Property(e => e.NoteId).HasColumnName("NoteID");
                entity.Property(e => e.Task).HasMaxLength(255);

                entity.HasOne(d => d.Note)
                    .WithMany(p => p.ToDoLists)
                    .HasForeignKey(d => d.NoteId)
                    .HasConstraintName("FK__ToDoLists__NoteI__45F365D3");
            });

            modelBuilder.Entity<User>(entity => {
                entity.HasKey(e => e.Id).HasName("PK__Users__3214EC276CAF44FD");
                entity.Property(e => e.Id).HasColumnName("ID");
                entity.Property(e => e.TelegramId).HasColumnName("TelegramID");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
