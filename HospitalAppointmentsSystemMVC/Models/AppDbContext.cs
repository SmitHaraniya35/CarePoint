using Microsoft.EntityFrameworkCore;

namespace HospitalAppointmentsSystemMVC.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {
            Database.EnsureCreated();
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Patient> Patients { get; set; }
        public DbSet<Doctor> Doctors { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<Prescription> Prescriptions { get; set; }
        public DbSet<Feedback> Feedbacks { get; set; }
        public DbSet<DoctorAvailability> DoctorAvailabilities { get; set; }

        public DbSet<DoctorUnavailability> DoctorUnavailabilities { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<Patient>()
                .HasIndex(p => p.ContactNumber)
                .IsUnique();

            modelBuilder.Entity<Doctor>()
                .HasIndex(d => d.ContactNumber)
                .IsUnique();

            // USER <--> PATIENT (One-to-One)
            modelBuilder.Entity<User>()
                .HasOne(u => u.PatientProfile)
                .WithOne(p => p.User)
                .HasForeignKey<Patient>(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict); // Deleting User deletes Patient

            // USER <--> DOCTOR (One-to-One)
            modelBuilder.Entity<User>()
                .HasOne(u => u.DoctorProfile)
                .WithOne(d => d.User)
                .HasForeignKey<Doctor>(d => d.UserId)
                .OnDelete(DeleteBehavior.Restrict); // Deleting User deletes Doctor

            // PATIENT ↔ APPOINTMENTS (Cascade OK)
            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Patient)
                .WithMany(p => p.Appointments)
                .HasForeignKey(a => a.PatientId)
                .OnDelete(DeleteBehavior.Restrict); // Keep cascade here

            // DOCTOR ↔ APPOINTMENTS (Restrict to prevent multiple cascade paths)
            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Doctor)
                .WithMany(d => d.Appointments)
                .HasForeignKey(a => a.DoctorId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent multiple cascade path error

            // APPOINTMENT <--> PRESCRIPTION (One-to-One)
            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Prescription)
                .WithOne(p => p.Appointment)
                .HasForeignKey<Prescription>(p => p.AppointmentId)
                .OnDelete(DeleteBehavior.Restrict); // Deleting Appointment deletes Prescription

            // DOCTOR <--> DOCTOR AVAILABILITY (One-to-Many)
            modelBuilder.Entity<DoctorAvailability>()
                .HasOne(da => da.Doctor)
                .WithMany()
                .HasForeignKey(da => da.DoctorId)
                .OnDelete(DeleteBehavior.Restrict); // Deleting Doctor deletes Availability
        }
    }
}
