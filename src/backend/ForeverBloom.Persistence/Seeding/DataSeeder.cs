using ForeverBloom.Domain.Shared.Models;
using ForeverBloom.Persistence.Context;
using ForeverBloom.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace ForeverBloom.Persistence.Seeding;

public sealed class DataSeeder
{
    private readonly ApplicationDbContext _dbContext;

    public DataSeeder(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedCatalogAsync(cancellationToken);

        if (_dbContext.ChangeTracker.HasChanges())
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task SeedCatalogAsync(CancellationToken cancellationToken = default)
    {
        var hasSeedData =
            await _dbContext.Categories.AnyAsync(c => c.CurrentSlug == "obrazy-botaniczne", cancellationToken);

        if (hasSeedData)
        {
            return;
        }

        var obrazyBotaniczne = new Category
        {
            Name = "Obrazy botaniczne",
            Description =
                "Moje obrazy powstają pod wpływem chwili, emocji, wdzięczności, głosu serca lub potrzeby wyciszenia. To dar natury dla Twojego domu.",
            CurrentSlug = "obrazy-botaniczne",
            ImagePath = "/images/uploads/categories/obrazy-botaniczne/banner.avif",
            Path = new LTree("obrazy-botaniczne"),
            ParentCategory = null,
            DisplayOrder = 1,
            IsActive = true
        };
        var obrazyPlaskie = new Category
        {
            Name = "Obrazy płaskie",
            Description =
                "Moje obrazy powstają pod wpływem chwili, emocji, wdzięczności, głosu serca lub potrzeby wyciszenia. To dar natury dla Twojego domu.",
            CurrentSlug = "obrazy-plaskie",
            ImagePath = "/images/uploads/categories/obrazy-botaniczne/banner.avif",
            Path = new LTree("obrazy-botaniczne.obrazy-plaskie"),
            ParentCategory = obrazyBotaniczne,
            DisplayOrder = 1,
            IsActive = true
        };
        var obrazyPrzestrzenne = new Category
        {
            Name = "Obrazy przestrzenne",
            Description =
                "Moje obrazy powstają pod wpływem chwili, emocji, wdzięczności, głosu serca lub potrzeby wyciszenia. To dar natury dla Twojego domu.",
            CurrentSlug = "obrazy-przestrzenne",
            ImagePath = "/images/uploads/categories/obrazy-botaniczne/banner.avif",
            Path = new LTree("obrazy-botaniczne.obrazy-przestrzenne"),
            ParentCategory = obrazyBotaniczne,
            DisplayOrder = 2,
            IsActive = true
        };
        var suszoneKwiaty = new Category
        {
            Name = "Suszone kwiaty",
            Description = "Najpiękniejsze zestawy kwiatów do Twojego rękodzieła.",
            CurrentSlug = "suszone-kwiaty",
            ImagePath = "/images/uploads/categories/suszone-kwiaty/banner.avif",
            Path = new LTree("suszone-kwiaty"),
            ParentCategory = null,
            DisplayOrder = 2,
            IsActive = true
        };
        var zestawyZrobSobieObraz = new Category
        {
            Name = "Zestawy \"Zrób sobie obraz\"",
            Description = "Zestawy DIY dla Ciebie do samodzielnego stworzenia kwiatowego obrazu.",
            CurrentSlug = "zestawy-diy-zrob-sobie-obraz",
            ImagePath = "/images/uploads/categories/zestawy-diy-zrob-sobie-obraz/banner.avif",
            Path = new LTree("zestawy-diy-zrob-sobie-obraz"),
            ParentCategory = null,
            DisplayOrder = 3,
            IsActive = true
        };

        // Add all categories to context
        var allCategories = new[]
        {
            obrazyBotaniczne, obrazyPlaskie, obrazyPrzestrzenne, suszoneKwiaty, zestawyZrobSobieObraz
        };
        _dbContext.Categories.AddRange(allCategories);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Create SlugRegistry entries for categories
        var categorySlugEntries = allCategories.Select(c => new SlugRegistryEntry
        {
            Slug = c.CurrentSlug, EntityType = EntityType.Category, EntityId = c.Id, IsActive = true
        });
        _dbContext.SlugRegistry.AddRange(categorySlugEntries);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Obrazy płaskie
        var obraz2 = new Product
        {
            Name = "Obraz 2",
            SeoTitle = "Obraz 2",
            FullDescription = """
                              <p>
                                Zdjęcia przedstawiają kompozycję suszonych kwiatów wykonaną w naszej pracowni.<br>
                                Oferujemy wykonanie kompozycji inspirowanej pokazanym wzorem — w tej samej ramie, ze wspólnie ustalonym doborem kwiatów w ramach dostępności.
                              </p>

                              <p>
                                Kwiaty na zdjęciach: gerbera, astrantia, hortensja, eukaliptus, chaber.<br>
                                Oprawa: kwiaty przyklejane są na szybę i oprawiane w wysokiej jakości ramę z drewna sosnowego z szybą.<br>
                                Wymiary ramy: 21 × 29,5 × 4,5 cm.
                              </p>

                              <p>
                                Czas realizacji: 2–3 tygodnie, w zależności od dostępności wybranych kwiatów.
                              </p>

                              <p>
                                Wszystkie kwiaty w obrazie są suszone, projektowane i składane ręcznie w naszej pracowni. Ze względu na organiczny charakter suszenia kwiatów, nie ma dwóch identycznych obrazów.
                              </p>

                              <p>
                                Pamiętaj, że obraz powstał z materiału roślinnego i organicznego, dlatego z czasem może ulegać zmianom, a jego kolory mogą blaknąć. To naturalny, nieunikniony proces i nie stanowi podstawy do reklamacji. Kupując ten obraz, potwierdzasz, że jesteś tego świadomy. Aby jak najdłużej zachował naturalny wygląd, eksponuj go z dala od bezpośredniego światła i wilgoci.
                              </p>
                              """,
            MetaDescription =
                "Wymiary: 21 × 29,5 × 4,5 cm. Kompozycja z suszonych kwiatów na szybie, w ramie z drewna sosnowego.",
            CurrentSlug = "obraz-2",
            Price = 200,
            DisplayOrder = 2,
            IsFeatured = false,
            PublishStatus = PublishStatus.Published,
            Availability = ProductAvailabilityStatus.MadeToOrder,
            Category = obrazyPlaskie,
            Images = new List<ProductImage>
            {
                new()
                {
                    ImagePath =
                        "/images/uploads/products/obrazy-botaniczne/obrazy-plaskie/obraz-2/thumbnail.avif",
                    AltText = "Obraz 2",
                    IsPrimary = true,
                    DisplayOrder = 1
                },
                new()
                {
                    ImagePath =
                        "/images/uploads/products/obrazy-botaniczne/obrazy-plaskie/obraz-2/gallery-1.avif",
                    AltText = "Obraz 2",
                    IsPrimary = false,
                    DisplayOrder = 2
                }
            }
        };

        var obraz3 = new Product
        {
            Name = "Obraz 3",
            SeoTitle = "Obraz 3",
            FullDescription = """
                              <p>
                                Zdjęcia przedstawiają kompozycję suszonych kwiatów wykonaną w naszej pracowni.<br>
                                Oferujemy wykonanie kompozycji inspirowanej pokazanym wzorem — w tej samej ramie, ze wspólnie ustalonym doborem kwiatów w ramach dostępności.
                              </p>

                              <p>
                                Kwiaty na zdjęciach: astrantia, hibiskus, mak, hortensja, margerytka, tobołek.<br>
                                Oprawa: kwiaty przyklejane są na szybę i oprawiane w wysokiej jakości białą ramę przestrzenną z szybą.<br>
                                Wymiary ramy: 23 × 27 cm.
                              </p>

                              <p>
                                Czas realizacji: 2–3 tygodnie, w zależności od dostępności wybranych kwiatów.
                              </p>

                              <p>
                                Wszystkie kwiaty w obrazie są suszone, projektowane i składane ręcznie w naszej pracowni. Ze względu na organiczny charakter suszenia kwiatów, nie ma dwóch identycznych obrazów.
                              </p>

                              <p>
                                Pamiętaj, że obraz powstał z materiału roślinnego i organicznego, dlatego z czasem może ulegać zmianom, a jego kolory mogą blaknąć. To naturalny, nieunikniony proces i nie stanowi podstawy do reklamacji. Kupując ten obraz, potwierdzasz, że jesteś tego świadomy. Aby jak najdłużej zachował naturalny wygląd, eksponuj go z dala od bezpośredniego światła i wilgoci.
                              </p>
                              """,
            MetaDescription =
                "Wymiary: 23 × 27 cm. Kompozycja z suszonych kwiatów na szybie, w białej ramie przestrzennej z szybą.",
            CurrentSlug = "obraz-3",
            Price = null,
            DisplayOrder = 3,
            IsFeatured = true,
            PublishStatus = PublishStatus.Published,
            Availability = ProductAvailabilityStatus.MadeToOrder,
            Category = obrazyPlaskie,
            Images = new List<ProductImage>
            {
                new()
                {
                    ImagePath =
                        "/images/uploads/products/obrazy-botaniczne/obrazy-plaskie/obraz-3/thumbnail.avif",
                    AltText = "Obraz 3",
                    IsPrimary = true,
                    DisplayOrder = 1
                },
                new()
                {
                    ImagePath =
                        "/images/uploads/products/obrazy-botaniczne/obrazy-plaskie/obraz-3/gallery-1.avif",
                    AltText = "Obraz 3",
                    IsPrimary = false,
                    DisplayOrder = 2
                }
            }
        };

        var obraz4 = new Product
        {
            Name = "Obraz 4",
            SeoTitle = "Obraz 4",
            FullDescription = """
                              <p>
                                Zdjęcia przedstawiają kompozycję suszonych kwiatów wykonaną w naszej pracowni.<br>
                                Oferujemy wykonanie kompozycji inspirowanej pokazanym wzorem — w tej samej ramie, ze wspólnie ustalonym doborem kwiatów w ramach dostępności.
                              </p>

                              <p>
                                Kwiaty na zdjęciach: paproć, mak, eukaliptus, koronka Królowej Anny, krwawnik, chmiel, chaber.<br>
                                Oprawa: kwiaty przyklejane są na szybę i oprawiane w wysokiej jakości ramę z drewna sosnowego z szybą.<br>
                                Wymiary ramy: 30 × 42 × 2 cm.
                              </p>

                              <p>
                                Czas realizacji: 2–3 tygodnie, w zależności od dostępności wybranych kwiatów.
                              </p>

                              <p>
                                Wszystkie kwiaty w obrazie są suszone, projektowane i składane ręcznie w naszej pracowni. Ze względu na organiczny charakter suszenia kwiatów, nie ma dwóch identycznych obrazów.
                              </p>

                              <p>
                                Pamiętaj, że obraz powstał z materiału roślinnego i organicznego, dlatego z czasem może ulegać zmianom, a jego kolory mogą blaknąć. To naturalny, nieunikniony proces i nie stanowi podstawy do reklamacji. Kupując ten obraz, potwierdzasz, że jesteś tego świadomy. Aby jak najdłużej zachował naturalny wygląd, eksponuj go z dala od bezpośredniego światła i wilgoci.
                              </p>
                              """,
            MetaDescription =
                "Wymiary: 30 × 42 × 2 cm. Kompozycja z suszonych kwiatów na szybie, w ramie z drewna sosnowego.",
            CurrentSlug = "obraz-4",
            Price = 300,
            DisplayOrder = 4,
            IsFeatured = true,
            PublishStatus = PublishStatus.Published,
            Availability = ProductAvailabilityStatus.MadeToOrder,
            Category = obrazyPlaskie,
            Images = new List<ProductImage>
            {
                new()
                {
                    ImagePath =
                        "/images/uploads/products/obrazy-botaniczne/obrazy-plaskie/obraz-4/thumbnail.avif",
                    AltText = "Obraz 4",
                    IsPrimary = true,
                    DisplayOrder = 1
                }
            }
        };

        // Obrazy przestrzenne
        var obraz1 = new Product
        {
            Name = "Obraz 1",
            SeoTitle = "Obraz 1",
            FullDescription = """
                              <p>
                                Zdjęcia przedstawiają kompozycję suszonych kwiatów wykonaną w naszej pracowni.<br>
                                Oferujemy wykonanie kompozycji inspirowanej pokazanym wzorem — w tej samej ramie, ze wspólnie ustalonym doborem kwiatów w ramach dostępności.
                              </p>

                              <p>
                                Kwiaty na zdjęciach: słonecznik, gerber, chryzantema, goździk, nawłoć, palma.<br>
                                Oprawa: kwiaty przyklejane są na zagruntowane płótno bawełniane lub papier bawełniany najwyzszej jakości i oprawiane w wysokiej jakości białą ramę przestrzenną z szybą (shadow box).<br>
                                Wymiary ramy: 50 × 50 × 4,5 cm.
                              </p>

                              <p>
                                Czas realizacji: 2–3 tygodnie, w zależności od dostępności wybranych kwiatów.
                              </p>

                              <p>
                                Wszystkie kwiaty w obrazie są suszone, projektowane i składane ręcznie w naszej pracowni. Ze względu na organiczny charakter suszenia kwiatów, nie ma dwóch identycznych obrazów.
                              </p>

                              <p>
                                Pamiętaj, że obraz powstał z materiału roślinnego i organicznego, dlatego z czasem może ulegać zmianom, a jego kolory mogą blaknąć. To naturalny, nieunikniony proces i nie stanowi podstawy do reklamacji. Kupując ten obraz, potwierdzasz, że jesteś tego świadomy. Aby jak najdłużej zachował naturalny wygląd, eksponuj go z dala od bezpośredniego światła i wilgoci.
                              </p>
                              """,
            MetaDescription =
                "Wymiary: 50 × 50 × 4,5 cm. Kompozycja z suszonych kwiatów na płótnie lub papierze, w białej ramie przestrzennej z szybą.",
            CurrentSlug = "obraz-1",
            Price = 450,
            DisplayOrder = 1,
            IsFeatured = true,
            PublishStatus = PublishStatus.Published,
            Availability = ProductAvailabilityStatus.MadeToOrder,
            Category = obrazyPrzestrzenne,
            Images = new List<ProductImage>
            {
                new()
                {
                    ImagePath =
                        "/images/uploads/products/obrazy-botaniczne/obrazy-przestrzenne/obraz-1/thumbnail.avif",
                    AltText = "Obraz 1",
                    IsPrimary = true,
                    DisplayOrder = 1
                },
                new()
                {
                    ImagePath =
                        "/images/uploads/products/obrazy-botaniczne/obrazy-przestrzenne/obraz-1/gallery-1.avif",
                    AltText = "Obraz 1",
                    IsPrimary = false,
                    DisplayOrder = 2
                },
                new()
                {
                    ImagePath =
                        "/images/uploads/products/obrazy-botaniczne/obrazy-przestrzenne/obraz-1/gallery-2.avif",
                    AltText = "Obraz 1",
                    IsPrimary = false,
                    DisplayOrder = 3
                }
            }
        };

        var obraz5 = new Product
        {
            Name = "Obraz 5",
            SeoTitle = "Obraz 5",
            FullDescription = """
                              <p>
                                Zdjęcia przedstawiają kompozycję suszonych kwiatów wykonaną w naszej pracowni.<br>
                                Oferujemy wykonanie kompozycji inspirowanej pokazanym wzorem — w tej samej ramie, ze wspólnie ustalonym doborem kwiatów w ramach dostępności.
                              </p>

                              <p>
                                Kwiaty na zdjęciach: róża.<br>
                                Oprawa: kwiaty przyklejane są na zagruntowane płótno bawełniane lub papier bawełniany najwyzszej jakości i oprawiane w wysokiej jakości białą ramę przestrzenną z szybą (shadow box).<br>
                                Wymiary ramy: 25 × 25 × 4,5 cm.
                              </p>

                              <p>
                                Czas realizacji: 2–3 tygodnie, w zależności od dostępności wybranych kwiatów.
                              </p>

                              <p>
                                Wszystkie kwiaty w obrazie są suszone, projektowane i składane ręcznie w naszej pracowni. Ze względu na organiczny charakter suszenia kwiatów, nie ma dwóch identycznych obrazów.
                              </p>

                              <p>
                                Pamiętaj, że obraz powstał z materiału roślinnego i organicznego, dlatego z czasem może ulegać zmianom, a jego kolory mogą blaknąć. To naturalny, nieunikniony proces i nie stanowi podstawy do reklamacji. Kupując ten obraz, potwierdzasz, że jesteś tego świadomy. Aby jak najdłużej zachował naturalny wygląd, eksponuj go z dala od bezpośredniego światła i wilgoci.
                              </p>
                              """,
            MetaDescription =
                "Wymiary: 25 × 25 × 4,5 cm. Kompozycja z suszonych kwiatów na płótnie lub papierze, w białej ramie przestrzennej z szybą.",
            CurrentSlug = "obraz-5",
            Price = 300,
            DisplayOrder = 5,
            IsFeatured = true,
            PublishStatus = PublishStatus.Published,
            Availability = ProductAvailabilityStatus.MadeToOrder,
            Category = obrazyPrzestrzenne,
            Images = new List<ProductImage>
            {
                new()
                {
                    ImagePath =
                        "/images/uploads/products/obrazy-botaniczne/obrazy-przestrzenne/obraz-5/thumbnail.avif",
                    AltText = "Obraz 5",
                    IsPrimary = true,
                    DisplayOrder = 1
                },
                new()
                {
                    ImagePath =
                        "/images/uploads/products/obrazy-botaniczne/obrazy-przestrzenne/obraz-5/gallery-1.avif",
                    AltText = "Obraz 5",
                    IsPrimary = false,
                    DisplayOrder = 2
                }
            }
        };

        var obraz6 = new Product
        {
            Name = "Obraz 6",
            SeoTitle = "Obraz 6",
            FullDescription = """
                              <p>
                                Zdjęcia przedstawiają kompozycję suszonych kwiatów wykonaną w naszej pracowni.<br>
                                Oferujemy wykonanie kompozycji inspirowanej pokazanym wzorem — w tej samej ramie, ze wspólnie ustalonym doborem kwiatów w ramach dostępności.
                              </p>

                              <p>
                                Kwiaty na zdjęciach: hortensja.<br>
                                Oprawa: kwiaty przyklejane są na zagruntowane płótno bawełniane lub papier bawełniany najwyzszej jakości i oprawiane w wysokiej jakości białą ramę przestrzenną z szybą (shadow box).<br>
                                Wymiary ramy: 25 × 25 × 4,5 cm.
                              </p>

                              <p>
                                Czas realizacji: 2–3 tygodnie, w zależności od dostępności wybranych kwiatów.
                              </p>

                              <p>
                                Wszystkie kwiaty w obrazie są suszone, projektowane i składane ręcznie w naszej pracowni. Ze względu na organiczny charakter suszenia kwiatów, nie ma dwóch identycznych obrazów.
                              </p>

                              <p>
                                Pamiętaj, że obraz powstał z materiału roślinnego i organicznego, dlatego z czasem może ulegać zmianom, a jego kolory mogą blaknąć. To naturalny, nieunikniony proces i nie stanowi podstawy do reklamacji. Kupując ten obraz, potwierdzasz, że jesteś tego świadomy. Aby jak najdłużej zachował naturalny wygląd, eksponuj go z dala od bezpośredniego światła i wilgoci.
                              </p>
                              """,
            MetaDescription =
                "Wymiary: 25 × 25 × 4,5 cm. Kompozycja z suszonych kwiatów na płótnie lub papierze, w białej ramie przestrzennej z szybą.",
            CurrentSlug = "obraz-6",
            Price = 300,
            DisplayOrder = 6,
            IsFeatured = false,
            PublishStatus = PublishStatus.Published,
            Availability = ProductAvailabilityStatus.MadeToOrder,
            Category = obrazyPrzestrzenne,
            Images = new List<ProductImage>
            {
                new()
                {
                    ImagePath =
                        "/images/uploads/products/obrazy-botaniczne/obrazy-przestrzenne/obraz-6/thumbnail.avif",
                    AltText = "Obraz 6",
                    IsPrimary = true,
                    DisplayOrder = 1
                },
                new()
                {
                    ImagePath =
                        "/images/uploads/products/obrazy-botaniczne/obrazy-przestrzenne/obraz-6/gallery-1.avif",
                    AltText = "Obraz 6",
                    IsPrimary = false,
                    DisplayOrder = 2
                },
                new()
                {
                    ImagePath =
                        "/images/uploads/products/obrazy-botaniczne/obrazy-przestrzenne/obraz-6/gallery-2.avif",
                    AltText = "Obraz 6",
                    IsPrimary = false,
                    DisplayOrder = 3
                },
                new()
                {
                    ImagePath =
                        "/images/uploads/products/obrazy-botaniczne/obrazy-przestrzenne/obraz-6/gallery-3.avif",
                    AltText = "Obraz 6",
                    IsPrimary = false,
                    DisplayOrder = 4
                }
            }
        };

        var obraz7 = new Product
        {
            Name = "Obraz 7",
            SeoTitle = "Obraz 7",
            FullDescription = """
                              <p>
                                Zdjęcia przedstawiają kompozycję suszonych kwiatów wykonaną w naszej pracowni.<br>
                                Oferujemy wykonanie kompozycji inspirowanej pokazanym wzorem — w tej samej ramie, ze wspólnie ustalonym doborem kwiatów w ramach dostępności.
                              </p>

                              <p>
                                Kwiaty na zdjęciach: róża, hibiskus, mak, gerbera, astrantia, jeżówka, storczyk, anemon, hortensja, eukaliptus.<br>
                                Oprawa: kwiaty przyklejane są na zagruntowane płótno bawełniane lub papier bawełniany najwyzszej jakości i oprawiane w wysokiej jakości białą ramę przestrzenną z szybą (shadow box).<br>
                                Wymiary ramy: 32 × 42 × 3 cm.
                              </p>

                              <p>
                                Czas realizacji: 2–3 tygodnie, w zależności od dostępności wybranych kwiatów.
                              </p>

                              <p>
                                Wszystkie kwiaty w obrazie są suszone, projektowane i składane ręcznie w naszej pracowni. Ze względu na organiczny charakter suszenia kwiatów, nie ma dwóch identycznych obrazów.
                              </p>

                              <p>
                                Pamiętaj, że obraz powstał z materiału roślinnego i organicznego, dlatego z czasem może ulegać zmianom, a jego kolory mogą blaknąć. To naturalny, nieunikniony proces i nie stanowi podstawy do reklamacji. Kupując ten obraz, potwierdzasz, że jesteś tego świadomy. Aby jak najdłużej zachował naturalny wygląd, eksponuj go z dala od bezpośredniego światła i wilgoci.
                              </p>
                              """,
            MetaDescription =
                "Wymiary: 32 × 42 × 3 cm. Kompozycja z suszonych kwiatów na płótnie lub papierze, w białej ramie przestrzennej z szybą.",
            CurrentSlug = "obraz-7",
            Price = 350,
            DisplayOrder = 7,
            IsFeatured = true,
            PublishStatus = PublishStatus.Published,
            Availability = ProductAvailabilityStatus.MadeToOrder,
            Category = obrazyPrzestrzenne,
            Images = new List<ProductImage>
            {
                new()
                {
                    ImagePath =
                        "/images/uploads/products/obrazy-botaniczne/obrazy-przestrzenne/obraz-7/thumbnail.avif",
                    AltText = "Obraz 7",
                    IsPrimary = true,
                    DisplayOrder = 1
                },
                new()
                {
                    ImagePath =
                        "/images/uploads/products/obrazy-botaniczne/obrazy-przestrzenne/obraz-7/gallery-1.avif",
                    AltText = "Obraz 7",
                    IsPrimary = false,
                    DisplayOrder = 2
                },
                new()
                {
                    ImagePath =
                        "/images/uploads/products/obrazy-botaniczne/obrazy-przestrzenne/obraz-7/gallery-2.avif",
                    AltText = "Obraz 7",
                    IsPrimary = false,
                    DisplayOrder = 3
                },
                new()
                {
                    ImagePath =
                        "/images/uploads/products/obrazy-botaniczne/obrazy-przestrzenne/obraz-7/gallery-3.avif",
                    AltText = "Obraz 7",
                    IsPrimary = false,
                    DisplayOrder = 4
                }
            }
        };

        var obraz8 = new Product
        {
            Name = "Obraz 8",
            SeoTitle = "Obraz 8",
            FullDescription = """
                              <p>
                                Zdjęcia przedstawiają kompozycję suszonych kwiatów wykonaną w naszej pracowni.<br>
                                Oferujemy wykonanie kompozycji inspirowanej pokazanym wzorem — w tej samej ramie, ze wspólnie ustalonym doborem kwiatów w ramach dostępności.
                              </p>

                              <p>
                                Kwiaty na zdjęciach: mak, astrantia, anemon, tobołek, lawenda, kraspedia, dmuszek, eukaliptus.<br>
                                Oprawa: kwiaty przyklejane są na zagruntowane płótno bawełniane lub papier bawełniany najwyzszej jakości i oprawiane w wysokiej jakości białą ramę przestrzenną z szybą (shadow box).<br>
                                Wymiary ramy: 25 × 25 × 4,5 cm.
                              </p>

                              <p>
                                Czas realizacji: 2–3 tygodnie, w zależności od dostępności wybranych kwiatów.
                              </p>

                              <p>
                                Wszystkie kwiaty w obrazie są suszone, projektowane i składane ręcznie w naszej pracowni. Ze względu na organiczny charakter suszenia kwiatów, nie ma dwóch identycznych obrazów.
                              </p>

                              <p>
                                Pamiętaj, że obraz powstał z materiału roślinnego i organicznego, dlatego z czasem może ulegać zmianom, a jego kolory mogą blaknąć. To naturalny, nieunikniony proces i nie stanowi podstawy do reklamacji. Kupując ten obraz, potwierdzasz, że jesteś tego świadomy. Aby jak najdłużej zachował naturalny wygląd, eksponuj go z dala od bezpośredniego światła i wilgoci.
                              </p>
                              """,
            MetaDescription =
                "Wymiary: 25 × 25 × 4,5 cm. Kompozycja z suszonych kwiatów na płótnie lub papierze, w białej ramie przestrzennej z szybą.",
            CurrentSlug = "obraz-8",
            Price = 300,
            DisplayOrder = 8,
            IsFeatured = true,
            PublishStatus = PublishStatus.Published,
            Availability = ProductAvailabilityStatus.MadeToOrder,
            Category = obrazyPrzestrzenne,
            Images = new List<ProductImage>
            {
                new()
                {
                    ImagePath =
                        "/images/uploads/products/obrazy-botaniczne/obrazy-przestrzenne/obraz-8/thumbnail.avif",
                    AltText = "Obraz 8",
                    IsPrimary = true,
                    DisplayOrder = 1
                },
                new()
                {
                    ImagePath =
                        "/images/uploads/products/obrazy-botaniczne/obrazy-przestrzenne/obraz-8/gallery-1.avif",
                    AltText = "Obraz 8",
                    IsPrimary = false,
                    DisplayOrder = 2
                },
                new()
                {
                    ImagePath =
                        "/images/uploads/products/obrazy-botaniczne/obrazy-przestrzenne/obraz-8/gallery-2.avif",
                    AltText = "Obraz 8",
                    IsPrimary = false,
                    DisplayOrder = 3
                }
            }
        };

        var obraz9 = new Product
        {
            Name = "Obraz 9",
            SeoTitle = "Obraz 9",
            FullDescription = """
                              <p>
                                Zdjęcia przedstawiają kompozycję suszonych kwiatów wykonaną w naszej pracowni.<br>
                                Oferujemy wykonanie kompozycji inspirowanej pokazanym wzorem — w tej samej ramie, ze wspólnie ustalonym doborem kwiatów w ramach dostępności.
                              </p>

                              <p>
                                Kwiaty na zdjęciach: jaskier, róża, storczyk, astrantia, tulipan kraspedia, tobołek, lawenda.<br>
                                Oprawa: kwiaty przyklejane są na zagruntowane płótno bawełniane lub papier bawełniany najwyzszej jakości i oprawiane w wysokiej jakości białą ramę przestrzenną z szybą (shadow box).<br>
                                Wymiary ramy: 25 × 25 × 4,5 cm.
                              </p>

                              <p>
                                Czas realizacji: 2–3 tygodnie, w zależności od dostępności wybranych kwiatów.
                              </p>

                              <p>
                                Wszystkie kwiaty w obrazie są suszone, projektowane i składane ręcznie w naszej pracowni. Ze względu na organiczny charakter suszenia kwiatów, nie ma dwóch identycznych obrazów.
                              </p>

                              <p>
                                Pamiętaj, że obraz powstał z materiału roślinnego i organicznego, dlatego z czasem może ulegać zmianom, a jego kolory mogą blaknąć. To naturalny, nieunikniony proces i nie stanowi podstawy do reklamacji. Kupując ten obraz, potwierdzasz, że jesteś tego świadomy. Aby jak najdłużej zachował naturalny wygląd, eksponuj go z dala od bezpośredniego światła i wilgoci.
                              </p>
                              """,
            MetaDescription =
                "Wymiary: 25 × 25 × 4,5 cm. Kompozycja z suszonych kwiatów na płótnie lub papierze, w białej ramie przestrzennej z szybą.",
            CurrentSlug = "obraz-9",
            Price = 300,
            DisplayOrder = 9,
            IsFeatured = true,
            PublishStatus = PublishStatus.Published,
            Availability = ProductAvailabilityStatus.MadeToOrder,
            Category = obrazyPrzestrzenne,
            Images = new List<ProductImage>
            {
                new()
                {
                    ImagePath =
                        "/images/uploads/products/obrazy-botaniczne/obrazy-przestrzenne/obraz-9/thumbnail.avif",
                    AltText = "Obraz 9",
                    IsPrimary = true,
                    DisplayOrder = 1
                },
                new()
                {
                    ImagePath =
                        "/images/uploads/products/obrazy-botaniczne/obrazy-przestrzenne/obraz-9/gallery-1.avif",
                    AltText = "Obraz 9",
                    IsPrimary = false,
                    DisplayOrder = 2
                }
            }
        };

        var obraz10 = new Product
        {
            Name = "Obraz 10",
            SeoTitle = "Obraz 10",
            FullDescription = """
                              <p>
                                Zdjęcia przedstawiają kompozycję suszonych kwiatów wykonaną w naszej pracowni.<br>
                                Oferujemy wykonanie kompozycji inspirowanej pokazanym wzorem — w tej samej ramie, ze wspólnie ustalonym doborem kwiatów w ramach dostępności.
                              </p>

                              <p>
                                Kwiaty na zdjęciach: lawenda, mak, astrantia, margerytka, róża, kraspedia, dmuszek, koronka Królowej Anny.<br>
                                Oprawa: kwiaty przyklejane są na zagruntowane płótno bawełniane lub papier bawełniany najwyzszej jakości i oprawiane w wysokiej jakości białą ramę przestrzenną z szybą (shadow box).<br>
                                Wymiary ramy: 25 × 25 × 4,5 cm.
                              </p>

                              <p>
                                Czas realizacji: 2–3 tygodnie, w zależności od dostępności wybranych kwiatów.
                              </p>

                              <p>
                                Wszystkie kwiaty w obrazie są suszone, projektowane i składane ręcznie w naszej pracowni. Ze względu na organiczny charakter suszenia kwiatów, nie ma dwóch identycznych obrazów.
                              </p>

                              <p>
                                Pamiętaj, że obraz powstał z materiału roślinnego i organicznego, dlatego z czasem może ulegać zmianom, a jego kolory mogą blaknąć. To naturalny, nieunikniony proces i nie stanowi podstawy do reklamacji. Kupując ten obraz, potwierdzasz, że jesteś tego świadomy. Aby jak najdłużej zachował naturalny wygląd, eksponuj go z dala od bezpośredniego światła i wilgoci.
                              </p>
                              """,
            MetaDescription =
                "Wymiary: 25 × 25 × 4,5 cm. Kompozycja z suszonych kwiatów na płótnie lub papierze, w białej ramie przestrzennej z szybą.",
            CurrentSlug = "obraz-10",
            Price = 300,
            DisplayOrder = 10,
            IsFeatured = false,
            PublishStatus = PublishStatus.Published,
            Availability = ProductAvailabilityStatus.MadeToOrder,
            Category = obrazyPrzestrzenne,
            Images = new List<ProductImage>
            {
                new()
                {
                    ImagePath =
                        "/images/uploads/products/obrazy-botaniczne/obrazy-przestrzenne/obraz-10/thumbnail.avif",
                    AltText = "Obraz 10",
                    IsPrimary = true,
                    DisplayOrder = 1
                }
            }
        };

        var obraz11 = new Product
        {
            Name = "Obraz 11",
            SeoTitle = "Obraz 11",
            FullDescription = """
                              <p>
                                Zdjęcia przedstawiają kompozycję suszonych kwiatów wykonaną w naszej pracowni.<br>
                                Oferujemy wykonanie kompozycji inspirowanej pokazanym wzorem — w tej samej ramie, ze wspólnie ustalonym doborem kwiatów w ramach dostępności.
                              </p>

                              <p>
                                Kwiaty na zdjęciach: storczyk.<br>
                                Oprawa: kwiaty przyklejane są na zagruntowane płótno bawełniane lub papier bawełniany najwyzszej jakości i oprawiane w wysokiej jakości białą ramę przestrzenną z szybą (shadow box).<br>
                                Wymiary ramy: 32 × 42 × 3 cm.
                              </p>

                              <p>
                                Czas realizacji: 2–3 tygodnie, w zależności od dostępności wybranych kwiatów.
                              </p>

                              <p>
                                Wszystkie kwiaty w obrazie są suszone, projektowane i składane ręcznie w naszej pracowni. Ze względu na organiczny charakter suszenia kwiatów, nie ma dwóch identycznych obrazów.
                              </p>

                              <p>
                                Pamiętaj, że obraz powstał z materiału roślinnego i organicznego, dlatego z czasem może ulegać zmianom, a jego kolory mogą blaknąć. To naturalny, nieunikniony proces i nie stanowi podstawy do reklamacji. Kupując ten obraz, potwierdzasz, że jesteś tego świadomy. Aby jak najdłużej zachował naturalny wygląd, eksponuj go z dala od bezpośredniego światła i wilgoci.
                              </p>
                              """,
            MetaDescription =
                "Wymiary: 32 × 42 × 3 cm. Kompozycja z suszonych kwiatów na płótnie lub papierze, w białej ramie przestrzennej z szybą.",
            CurrentSlug = "obraz-11",
            Price = 350,
            DisplayOrder = 11,
            IsFeatured = false,
            PublishStatus = PublishStatus.Published,
            Availability = ProductAvailabilityStatus.MadeToOrder,
            Category = obrazyPrzestrzenne,
            Images = new List<ProductImage>
            {
                new()
                {
                    ImagePath =
                        "/images/uploads/products/obrazy-botaniczne/obrazy-przestrzenne/obraz-11/thumbnail.avif",
                    AltText = "Obraz 11",
                    IsPrimary = true,
                    DisplayOrder = 1
                },
                new()
                {
                    ImagePath =
                        "/images/uploads/products/obrazy-botaniczne/obrazy-przestrzenne/obraz-11/gallery-1.avif",
                    AltText = "Obraz 11",
                    IsPrimary = false,
                    DisplayOrder = 2
                }
            }
        };

        var obraz12 = new Product
        {
            Name = "Obraz 12",
            SeoTitle = "Obraz 12",
            FullDescription = """
                              <p>
                                Zdjęcia przedstawiają kompozycję suszonych kwiatów wykonaną w naszej pracowni.<br>
                                Oferujemy wykonanie kompozycji inspirowanej pokazanym wzorem — w tej samej ramie, ze wspólnie ustalonym doborem kwiatów w ramach dostępności.
                              </p>

                              <p>
                                Kwiaty na zdjęciach: anemon, róża, stokrotka afrykańska, narcyz, lawenda, tobołek, dmuszek, koronka Królowej Anny.<br>
                                Oprawa: kwiaty przyklejane są na zagruntowane płótno bawełniane lub papier bawełniany najwyzszej jakości i oprawiane w wysokiej jakości białą ramę przestrzenną z szybą (shadow box).<br>
                                Wymiary ramy: 25 × 25 × 4,5 cm.
                              </p>

                              <p>
                                Czas realizacji: 2–3 tygodnie, w zależności od dostępności wybranych kwiatów.
                              </p>

                              <p>
                                Wszystkie kwiaty w obrazie są suszone, projektowane i składane ręcznie w naszej pracowni. Ze względu na organiczny charakter suszenia kwiatów, nie ma dwóch identycznych obrazów.
                              </p>

                              <p>
                                Pamiętaj, że obraz powstał z materiału roślinnego i organicznego, dlatego z czasem może ulegać zmianom, a jego kolory mogą blaknąć. To naturalny, nieunikniony proces i nie stanowi podstawy do reklamacji. Kupując ten obraz, potwierdzasz, że jesteś tego świadomy. Aby jak najdłużej zachował naturalny wygląd, eksponuj go z dala od bezpośredniego światła i wilgoci.
                              </p>
                              """,
            MetaDescription =
                "Wymiary: 25 × 25 × 4,5 cm. Kompozycja z suszonych kwiatów na płótnie lub papierze, w białej ramie przestrzennej z szybą.",
            CurrentSlug = "obraz-12",
            Price = 300,
            DisplayOrder = 12,
            IsFeatured = true,
            PublishStatus = PublishStatus.Published,
            Availability = ProductAvailabilityStatus.MadeToOrder,
            Category = obrazyPrzestrzenne,
            Images = new List<ProductImage>
            {
                new()
                {
                    ImagePath =
                        "/images/uploads/products/obrazy-botaniczne/obrazy-przestrzenne/obraz-12/thumbnail.avif",
                    AltText = "Obraz 12",
                    IsPrimary = true,
                    DisplayOrder = 1
                },
                new()
                {
                    ImagePath =
                        "/images/uploads/products/obrazy-botaniczne/obrazy-przestrzenne/obraz-12/gallery-1.avif",
                    AltText = "Obraz 12",
                    IsPrimary = false,
                    DisplayOrder = 2
                }
            }
        };

        var obraz13 = new Product
        {
            Name = "Obraz 13",
            SeoTitle = "Obraz 13",
            FullDescription = """
                              <p>
                                Zdjęcia przedstawiają kompozycję suszonych kwiatów wykonaną w naszej pracowni.<br>
                                Oferujemy wykonanie kompozycji inspirowanej pokazanym wzorem — w tej samej ramie, ze wspólnie ustalonym doborem kwiatów w ramach dostępności.
                              </p>

                              <p>
                                Kwiaty na zdjęciach: anemon, róża, stokrotka afrykańska, narcyz, jaskier, lawenda, tobołek, dmuszek, koronka Królowej Anny.<br>
                                Oprawa: kwiaty przyklejane są na zagruntowane płótno bawełniane lub papier bawełniany najwyzszej jakości i oprawiane w wysokiej jakości białą ramę przestrzenną z szybą (shadow box).<br>
                                Wymiary ramy: 25 × 25 × 4,5 cm.
                              </p>

                              <p>
                                Czas realizacji: 2–3 tygodnie, w zależności od dostępności wybranych kwiatów.
                              </p>

                              <p>
                                Wszystkie kwiaty w obrazie są suszone, projektowane i składane ręcznie w naszej pracowni. Ze względu na organiczny charakter suszenia kwiatów, nie ma dwóch identycznych obrazów.
                              </p>

                              <p>
                                Pamiętaj, że obraz powstał z materiału roślinnego i organicznego, dlatego z czasem może ulegać zmianom, a jego kolory mogą blaknąć. To naturalny, nieunikniony proces i nie stanowi podstawy do reklamacji. Kupując ten obraz, potwierdzasz, że jesteś tego świadomy. Aby jak najdłużej zachował naturalny wygląd, eksponuj go z dala od bezpośredniego światła i wilgoci.
                              </p>
                              """,
            MetaDescription =
                "Wymiary: 25 × 25 × 4,5 cm. Kompozycja z suszonych kwiatów na płótnie lub papierze, w białej ramie przestrzennej z szybą.",
            CurrentSlug = "obraz-13",
            Price = 300,
            DisplayOrder = 13,
            IsFeatured = false,
            PublishStatus = PublishStatus.Published,
            Availability = ProductAvailabilityStatus.MadeToOrder,
            Category = obrazyPrzestrzenne,
            Images = new List<ProductImage>
            {
                new()
                {
                    ImagePath =
                        "/images/uploads/products/obrazy-botaniczne/obrazy-przestrzenne/obraz-13/thumbnail.avif",
                    AltText = "Obraz 13",
                    IsPrimary = true,
                    DisplayOrder = 1
                }
            }
        };

        var obraz14 = new Product
        {
            Name = "Obraz 14",
            SeoTitle = "Obraz 14",
            FullDescription = """
                              <p>
                                Zdjęcia przedstawiają kompozycję suszonych kwiatów wykonaną w naszej pracowni.<br>
                                Oferujemy wykonanie kompozycji inspirowanej pokazanym wzorem — w tej samej ramie, ze wspólnie ustalonym doborem kwiatów w ramach dostępności.
                              </p>

                              <p>
                                Kwiaty na zdjęciach: storczyk.<br>
                                Oprawa: kwiaty przyklejane są na zagruntowane płótno bawełniane lub papier bawełniany najwyzszej jakości i oprawiane w wysokiej jakości białą ramę przestrzenną z szybą (shadow box).<br>
                                Wymiary ramy: 32 × 42 × 3 cm.
                              </p>

                              <p>
                                Czas realizacji: 2–3 tygodnie, w zależności od dostępności wybranych kwiatów.
                              </p>

                              <p>
                                Wszystkie kwiaty w obrazie są suszone, projektowane i składane ręcznie w naszej pracowni. Ze względu na organiczny charakter suszenia kwiatów, nie ma dwóch identycznych obrazów.
                              </p>

                              <p>
                                Pamiętaj, że obraz powstał z materiału roślinnego i organicznego, dlatego z czasem może ulegać zmianom, a jego kolory mogą blaknąć. To naturalny, nieunikniony proces i nie stanowi podstawy do reklamacji. Kupując ten obraz, potwierdzasz, że jesteś tego świadomy. Aby jak najdłużej zachował naturalny wygląd, eksponuj go z dala od bezpośredniego światła i wilgoci.
                              </p>
                              """,
            MetaDescription =
                "Wymiary: 32 × 42 × 3 cm. Kompozycja z suszonych kwiatów na płótnie lub papierze, w białej ramie przestrzennej z szybą.",
            CurrentSlug = "obraz-14",
            Price = 350,
            DisplayOrder = 14,
            IsFeatured = true,
            PublishStatus = PublishStatus.Published,
            Availability = ProductAvailabilityStatus.MadeToOrder,
            Category = obrazyPrzestrzenne,
            Images = new List<ProductImage>
            {
                new()
                {
                    ImagePath =
                        "/images/uploads/products/obrazy-botaniczne/obrazy-przestrzenne/obraz-14/thumbnail.avif",
                    AltText = "Obraz 14",
                    IsPrimary = true,
                    DisplayOrder = 1
                },
                new()
                {
                    ImagePath =
                        "/images/uploads/products/obrazy-botaniczne/obrazy-przestrzenne/obraz-14/gallery-1.avif",
                    AltText = "Obraz 14",
                    IsPrimary = false,
                    DisplayOrder = 2
                },
                new()
                {
                    ImagePath =
                        "/images/uploads/products/obrazy-botaniczne/obrazy-przestrzenne/obraz-14/gallery-2.avif",
                    AltText = "Obraz 14",
                    IsPrimary = false,
                    DisplayOrder = 3
                }
            }
        };

        var zestawSuszonychKwiatow = new Product
        {
            Name = "Zestaw suszonych kwiatów",
            SeoTitle = "Zestaw suszonych kwiatów",
            FullDescription = """
                              <p>
                                  Wszystkie kwiaty wykorzystywane w naszych obrazach suszymy samodzielnie w naszej pracowni.
                                  Pochodzą one z naszego ogrodu działkowego oraz od okolicznych kwiaciarni.
                                  W sezonie zbieramy też rośliny na łąkach i w lasach.
                              </p>

                              <p>
                                  Jeżeli interesuje Cię zakup suszonych kwiatów, a nie ma ich aktualnie w ofercie, to skontaktuj się z nami.
                              </p>
                              """,
            MetaDescription =
                "Wszystkie kwiaty wykorzystywane w naszych obrazach suszymy samodzielnie w naszej pracowni.",
            CurrentSlug = "zestaw-suszonych-kwiatow",
            Price = null,
            DisplayOrder = 1,
            IsFeatured = false,
            PublishStatus = PublishStatus.Published,
            Availability = ProductAvailabilityStatus.ComingSoon,
            Category = suszoneKwiaty,
            Images = new List<ProductImage>
            {
                new()
                {
                    ImagePath =
                        "/images/uploads/products/suszone-kwiaty/zestaw-suszonych-kwiatow/thumbnail.avif",
                    AltText = "Zestaw suszonych kwiatów",
                    IsPrimary = true,
                    DisplayOrder = 1
                }
            }
        };

        var zestawZrobSobieObraz = new Product
        {
            Name = "Zestaw \"Zrób sobie obraz\"",
            SeoTitle = "Zestaw \"Zrób sobie obraz\"",
            FullDescription = """
                              <p>
                                Zestaw „Zrób sobie obraz” do samodzielnej pracy kreatywnej. Idealny na prezent dla Ciebie
                                lub bliskiej osoby z artystyczną duszą. Praca z kwiatami to kontakt z naturą, a jej
                                uzdrawiająca moc znana jest od wieków. Autorska kompozycja kwiatowa wprowadzi do Twojego domu
                                spokój, radość i kawałek nieprzemijającej natury.
                              </p>

                              <p>
                                Zestaw składa się z:
                                <ul>
                                  <li>
                                    ramy drewnianej z drewna sosnowego — do wyboru (w ramach dostępności):
                                    <ul>
                                      <li>
                                        rama z dwiema szybami<br>
                                        <small>Wymiary: 21 × 29,5 × 4 cm lub 30 × 42 × 2 cm</small>
                                      </li>
                                      <li>
                                        rama z przygotowanym podobraziem z naklejonym papierem bawełnianym najwyższej jakości<br>
                                        <small>Wymiary: 32 × 42 × 3 cm</small>
                                      </li>
                                      <li>
                                        rama z przygotowanym podobraziem z naklejonym zagruntowanym płótnem bawełnianym<br>
                                        <small>Wymiary: 32 × 42 × 3 cm</small>
                                      </li>
                                    </ul>
                                  </li>
                                  <li>precyzyjnego aplikatora z klejem</li>
                                  <li>zestawu suszonych kwiatów</li>
                                  <li>pęsety</li>
                                  <li>instrukcji klejenia i oprawiania</li>
                                </ul>
                              </p>

                              <p>
                                Pamiętaj, że obraz powstanie z materiału roślinnego i organicznego, dlatego z czasem może ulegać zmianom, a jego kolory mogą blaknąć. To naturalny, nieunikniony proces i nie stanowi podstawy do reklamacji. Kupując ten zestaw, potwierdzasz, że jesteś tego świadomy. Aby obraz jak najdłużej zachował naturalny wygląd, eksponuj go z dala od bezpośredniego światła i wilgoci.
                              </p>
                              """,
            MetaDescription =
                "Zestaw DIY do samodzielnej pracy kreatywnej. Idealny na prezent dla Ciebie lub bliskiej osoby z artystyczną duszą.",
            CurrentSlug = "zestaw-zrob-sobie-obraz",
            Price = null,
            DisplayOrder = 1,
            IsFeatured = false,
            PublishStatus = PublishStatus.Published,
            Availability = ProductAvailabilityStatus.ComingSoon,
            Category = zestawyZrobSobieObraz,
            Images = new List<ProductImage>
            {
                new()
                {
                    ImagePath =
                        "/images/uploads/products/zestawy-diy-zrob-sobie-obraz/zestaw-zrob-sobie-obraz/thumbnail.avif",
                    AltText = "Zestaw \"Zrób sobie obraz\"",
                    IsPrimary = true,
                    DisplayOrder = 1
                }
            }
        };

        // Add all products to context
        var allProducts = new[]
        {
            obraz1, obraz2, obraz3, obraz4, obraz5, obraz6, obraz7, obraz8, obraz9, obraz10, obraz11, obraz12,
            obraz13, obraz14, zestawSuszonychKwiatow, zestawZrobSobieObraz
        };
        _dbContext.Products.AddRange(allProducts);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Create SlugRegistry entries for products
        var productSlugEntries = allProducts.Select(p => new SlugRegistryEntry
        {
            Slug = p.CurrentSlug, EntityType = EntityType.Product, EntityId = p.Id, IsActive = true
        });
        _dbContext.SlugRegistry.AddRange(productSlugEntries);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
