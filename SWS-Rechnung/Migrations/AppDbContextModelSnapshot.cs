using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using SWSRechnung.Data;

#nullable disable
namespace SWSRechnung.Migrations
{
    [DbContext(typeof(AppDbContext))]
    partial class AppDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder b)
        {
#pragma warning disable 612, 618
            b.HasAnnotation("ProductVersion", "8.0.0");

            b.Entity("SWSRechnung.Models.Einstellung", e =>
            {
                e.HasKey("Id");
                e.ToTable("Einstellungen");
                e.Property<int>("Id").ValueGeneratedOnAdd()
                 .HasColumnType("INTEGER");
                e.Property<string>("Schluessel").IsRequired().HasMaxLength(100)
                 .HasColumnType("TEXT");
                e.Property<string>("Wert").HasColumnType("TEXT");
            });

            b.Entity("SWSRechnung.Models.Kunde", e =>
            {
                e.HasKey("Id");
                e.ToTable("Kunden");
                e.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("INTEGER");
                e.Property<bool>("Aktiv").HasColumnType("INTEGER").HasDefaultValue(true);
                e.Property<string>("Anrede").HasColumnType("TEXT");
                e.Property<string>("Ansprechpartner").HasMaxLength(200).HasColumnType("TEXT");
                e.Property<System.DateTime>("ErstelltAm").HasColumnType("TEXT");
                e.Property<string>("Email").HasMaxLength(200).HasColumnType("TEXT");
                e.Property<string>("Firmenname").IsRequired().HasMaxLength(200).HasColumnType("TEXT");
                e.Property<string>("Kundennummer").HasMaxLength(20).HasColumnType("TEXT");
                e.Property<string>("Land").IsRequired().HasMaxLength(100).HasColumnType("TEXT").HasDefaultValue("Deutschland");
                e.Property<string>("Notizen").HasColumnType("TEXT");
                e.Property<string>("Ort").HasMaxLength(100).HasColumnType("TEXT");
                e.Property<string>("PLZ").HasMaxLength(10).HasColumnType("TEXT");
                e.Property<string>("Strasse").HasMaxLength(200).HasColumnType("TEXT");
                e.Property<string>("Telefon").HasMaxLength(50).HasColumnType("TEXT");
                e.Property<string>("UstIdNr").HasMaxLength(50).HasColumnType("TEXT");
                e.Property<string>("Website").HasMaxLength(200).HasColumnType("TEXT");
            });

            b.Entity("SWSRechnung.Models.Angebot", e =>
            {
                e.HasKey("Id");
                e.ToTable("Angebote");
                e.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("INTEGER");
                e.Property<string>("Angebotsnummer").IsRequired().HasMaxLength(30).HasColumnType("TEXT");
                e.Property<string>("Betreff").HasMaxLength(300).HasColumnType("TEXT");
                e.Property<System.DateTime>("Angebotsdatum").HasColumnType("TEXT");
                e.Property<System.DateTime>("ErstelltAm").HasColumnType("TEXT");
                e.Property<System.DateTime?>("GueltigBis").HasColumnType("TEXT");
                e.Property<string>("Einleitung").HasColumnType("TEXT");
                e.Property<int>("KundeId").HasColumnType("INTEGER");
                e.Property<decimal>("MwStSatz").HasPrecision(5, 2).HasColumnType("TEXT");
                e.Property<string>("Notizen").HasColumnType("TEXT");
                e.Property<string>("Schlusstext").HasColumnType("TEXT");
                e.Property<int>("Status").HasColumnType("INTEGER");
                e.HasOne("SWSRechnung.Models.Kunde", "Kunde")
                 .WithMany("Angebote").HasForeignKey("KundeId")
                 .OnDelete(DeleteBehavior.Restrict).IsRequired();
                e.Navigation("Kunde");
            });

            b.Entity("SWSRechnung.Models.AngebotPosition", e =>
            {
                e.HasKey("Id");
                e.ToTable("AngebotPositionen");
                e.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("INTEGER");
                e.Property<int>("AngebotId").HasColumnType("INTEGER");
                e.Property<string>("Beschreibung").HasColumnType("TEXT");
                e.Property<string>("Bezeichnung").IsRequired().HasMaxLength(500).HasColumnType("TEXT");
                e.Property<string>("Einheit").IsRequired().HasMaxLength(20).HasColumnType("TEXT");
                e.Property<decimal>("Einzelpreis").HasPrecision(12, 4).HasColumnType("TEXT");
                e.Property<decimal>("Menge").HasPrecision(10, 3).HasColumnType("TEXT");
                e.Property<int>("Position").HasColumnType("INTEGER");
                e.Property<decimal>("Rabatt").HasPrecision(5, 2).HasColumnType("TEXT");
                e.HasOne("SWSRechnung.Models.Angebot", "Angebot")
                 .WithMany("Positionen").HasForeignKey("AngebotId")
                 .OnDelete(DeleteBehavior.Cascade).IsRequired();
                e.Navigation("Angebot");
            });

            b.Entity("SWSRechnung.Models.Rechnung", e =>
            {
                e.HasKey("Id");
                e.ToTable("Rechnungen");
                e.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("INTEGER");
                e.Property<int?>("AngebotId").HasColumnType("INTEGER");
                e.Property<string>("Betreff").HasMaxLength(300).HasColumnType("TEXT");
                e.Property<System.DateTime?>("BezahltAm").HasColumnType("TEXT");
                e.Property<string>("Einleitung").HasColumnType("TEXT");
                e.Property<System.DateTime>("ErstelltAm").HasColumnType("TEXT");
                e.Property<System.DateTime?>("FaelligAm").HasColumnType("TEXT");
                e.Property<int>("KundeId").HasColumnType("INTEGER");
                e.Property<System.DateTime?>("LeistungBis").HasColumnType("TEXT");
                e.Property<System.DateTime?>("LeistungVon").HasColumnType("TEXT");
                e.Property<decimal>("MwStSatz").HasPrecision(5, 2).HasColumnType("TEXT");
                e.Property<string>("Notizen").HasColumnType("TEXT");
                e.Property<string>("Rechnungsnummer").IsRequired().HasMaxLength(30).HasColumnType("TEXT");
                e.Property<string>("Schlusstext").HasColumnType("TEXT");
                e.Property<int>("Status").HasColumnType("INTEGER");
                e.Property<System.DateTime>("Rechnungsdatum").HasColumnType("TEXT");
                e.HasOne("SWSRechnung.Models.Angebot", "Angebot")
                 .WithMany().HasForeignKey("AngebotId");
                e.HasOne("SWSRechnung.Models.Kunde", "Kunde")
                 .WithMany("Rechnungen").HasForeignKey("KundeId")
                 .OnDelete(DeleteBehavior.Restrict).IsRequired();
                e.Navigation("Angebot");
                e.Navigation("Kunde");
            });

            b.Entity("SWSRechnung.Models.RechnungPosition", e =>
            {
                e.HasKey("Id");
                e.ToTable("RechnungPositionen");
                e.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("INTEGER");
                e.Property<string>("Beschreibung").HasColumnType("TEXT");
                e.Property<string>("Bezeichnung").IsRequired().HasMaxLength(500).HasColumnType("TEXT");
                e.Property<string>("Einheit").IsRequired().HasMaxLength(20).HasColumnType("TEXT");
                e.Property<decimal>("Einzelpreis").HasPrecision(12, 4).HasColumnType("TEXT");
                e.Property<decimal>("Menge").HasPrecision(10, 3).HasColumnType("TEXT");
                e.Property<int>("Position").HasColumnType("INTEGER");
                e.Property<decimal>("Rabatt").HasPrecision(5, 2).HasColumnType("TEXT");
                e.Property<int>("RechnungId").HasColumnType("INTEGER");
                e.HasOne("SWSRechnung.Models.Rechnung", "Rechnung")
                 .WithMany("Positionen").HasForeignKey("RechnungId")
                 .OnDelete(DeleteBehavior.Cascade).IsRequired();
                e.Navigation("Rechnung");
            });

            b.Entity("SWSRechnung.Models.Angebot", e => e.Navigation("Positionen"));
            b.Entity("SWSRechnung.Models.Rechnung", e => e.Navigation("Positionen"));
            b.Entity("SWSRechnung.Models.Kunde", e =>
            {
                e.Navigation("Angebote");
                e.Navigation("Rechnungen");
            });
#pragma warning restore 612, 618
        }
    }
}
