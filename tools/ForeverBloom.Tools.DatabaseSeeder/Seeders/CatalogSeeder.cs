using Dapper;
using ForeverBloom.Application.Abstractions.Data;
using ForeverBloom.Application.Abstractions.Data.Repositories;
using ForeverBloom.Application.Categories.Commands.CreateCategory;
using ForeverBloom.Application.Categories.Commands.UpdateCategory;
using ForeverBloom.Application.Products.Commands.CreateProduct;
using ForeverBloom.Application.Products.Commands.UpdateProduct;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.SharedKernel.Optional;
using MediatR;

namespace ForeverBloom.Tools.DatabaseSeeder.Seeders;

/// <summary>
/// Orchestrates the seeding of catalog data (categories and products) using application use cases.
/// </summary>
public sealed class CatalogSeeder
{
    private const string SentinelCategorySlug = "obrazy-botaniczne";

    private readonly ISender _sender;
    private readonly IDbConnectionFactory _dbConnectionFactory;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IProductRepository _productRepository;
    private readonly ILogger<CatalogSeeder> _logger;

    public CatalogSeeder(
        ISender sender,
        IDbConnectionFactory dbConnectionFactory,
        ICategoryRepository categoryRepository,
        IProductRepository productRepository,
        ILogger<CatalogSeeder> logger)
    {
        _sender = sender;
        _dbConnectionFactory = dbConnectionFactory;
        _categoryRepository = categoryRepository;
        _productRepository = productRepository;
        _logger = logger;
    }

    /// <summary>
    /// Seeds the catalog with categories and products.
    /// </summary>
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting catalog seeding workflow");

        if (await HasExistingSeedDataAsync(cancellationToken))
        {
            _logger.LogInformation(
                "Catalog data already exists (found sentinel category with slug '{SentinelSlug}'). Skipping seeding.",
                SentinelCategorySlug);
            return;
        }

        _logger.LogInformation("No existing seed data found. Proceeding with seeding.");

        // ------------
        // Categories
        // ------------

        // Obrazy botaniczne
        var obrazyBotaniczneId = await CreateCategoryAsync(
            new CreateCategoryCommand(
                Name: "Obrazy botaniczne",
                Description: "Moje obrazy powstają pod wpływem chwili, emocji, wdzięczności, głosu serca lub potrzeby wyciszenia. To dar natury dla Twojego domu.",
                Slug: "obrazy-botaniczne",
                ImagePath: "/images/uploads/categories/obrazy-botaniczne/banner.avif",
                ImageAltText: "Obrazy botaniczne",
                ParentCategoryId: null,
                DisplayOrder: 1),
            cancellationToken);

        await PublishCategoryAsync(obrazyBotaniczneId, cancellationToken);

        // Obrazy płaskie
        var obrazyPlaskieId = await CreateCategoryAsync(
            new CreateCategoryCommand(
                Name: "Obrazy płaskie",
                Description: "Moje obrazy powstają pod wpływem chwili, emocji, wdzięczności, głosu serca lub potrzeby wyciszenia. To dar natury dla Twojego domu.",
                Slug: "obrazy-plaskie",
                ImagePath: "/images/uploads/categories/obrazy-botaniczne/banner.avif",
                ImageAltText: "Obrazy płaskie",
                ParentCategoryId: obrazyBotaniczneId,
                DisplayOrder: 1),
            cancellationToken);

        await PublishCategoryAsync(obrazyPlaskieId, cancellationToken);

        // Obrazy przestrzenne
        var obrazyPrzestrzenneId = await CreateCategoryAsync(
            new CreateCategoryCommand(
                Name: "Obrazy przestrzenne",
                Description: "Moje obrazy powstają pod wpływem chwili, emocji, wdzięczności, głosu serca lub potrzeby wyciszenia. To dar natury dla Twojego domu.",
                Slug: "obrazy-przestrzenne",
                ImagePath: "/images/uploads/categories/obrazy-botaniczne/banner.avif",
                ImageAltText: "Obrazy przestrzenne",
                ParentCategoryId: obrazyBotaniczneId,
                DisplayOrder: 2),
            cancellationToken);

        await PublishCategoryAsync(obrazyPrzestrzenneId, cancellationToken);

        // Suszone kwiaty
        var suszoneKwiatyId = await CreateCategoryAsync(
            new CreateCategoryCommand(
                Name: "Suszone kwiaty",
                Description: "Najpiękniejsze zestawy kwiatów do Twojego rękodzieła.",
                Slug: "suszone-kwiaty",
                ImagePath: "/images/uploads/categories/suszone-kwiaty/banner.avif",
                ImageAltText: "Suszone kwiaty",
                ParentCategoryId: null,
                DisplayOrder: 2),
            cancellationToken);

        await PublishCategoryAsync(suszoneKwiatyId, cancellationToken);

        // Zestawy DIY
        var zestawyDiyId = await CreateCategoryAsync(
            new CreateCategoryCommand(
                Name: "Zestawy \"Zrób sobie obraz\"",
                Description: "Zestawy DIY dla Ciebie do samodzielnego stworzenia kwiatowego obrazu.",
                Slug: "zestawy-diy-zrob-sobie-obraz",
                ImagePath: "/images/uploads/categories/zestawy-diy-zrob-sobie-obraz/banner.avif",
                ImageAltText: "Zestawy \"Zrób sobie obraz\"",
                ParentCategoryId: null,
                DisplayOrder: 3),
            cancellationToken);

        await PublishCategoryAsync(zestawyDiyId, cancellationToken);

        // ------------
        // Products
        // ------------

        // Obrazy płaskie — Obraz 2
        var obraz2Id = await CreateProductAsync(
            new CreateProductCommand(
                Name: "Obraz 2",
                SeoTitle: "Obraz 2",
                FullDescription: """
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
                MetaDescription: "Wymiary: 21 × 29,5 × 4,5 cm. Kompozycja z suszonych kwiatów na szybie, w ramie z drewna sosnowego.",
                Slug: "obraz-2",
                CategoryId: obrazyPlaskieId,
                Price: 200m,
                DisplayOrder: 2,
                IsFeatured: false,
                AvailabilityStatus: ProductAvailabilityStatus.MadeToOrder,
                Images:
                [
                    new CreateProductCommandImage(
                        Source: "/images/uploads/products/obrazy-botaniczne/obrazy-plaskie/obraz-2/thumbnail.avif",
                        AltText: "Obraz 2",
                        IsPrimary: true,
                        DisplayOrder: 1),
                    new CreateProductCommandImage(
                        Source: "/images/uploads/products/obrazy-botaniczne/obrazy-plaskie/obraz-2/gallery-1.avif",
                        AltText: "Obraz 2",
                        IsPrimary: false,
                        DisplayOrder: 2)
                ]),
            cancellationToken);

        await PublishProductAsync(obraz2Id, cancellationToken);

        // Obrazy płaskie — Obraz 3
        var obraz3Id = await CreateProductAsync(
            new CreateProductCommand(
                Name: "Obraz 3",
                SeoTitle: "Obraz 3",
                FullDescription: """
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
                MetaDescription: "Wymiary: 23 × 27 cm. Kompozycja z suszonych kwiatów na szybie, w białej ramie przestrzennej z szybą.",
                Slug: "obraz-3",
                CategoryId: obrazyPlaskieId,
                Price: null,
                DisplayOrder: 3,
                IsFeatured: true,
                AvailabilityStatus: ProductAvailabilityStatus.MadeToOrder,
                Images:
                [
                    new CreateProductCommandImage(
                        Source: "/images/uploads/products/obrazy-botaniczne/obrazy-plaskie/obraz-3/thumbnail.avif",
                        AltText: "Obraz 3",
                        IsPrimary: true,
                        DisplayOrder: 1),
                    new CreateProductCommandImage(
                        Source: "/images/uploads/products/obrazy-botaniczne/obrazy-plaskie/obraz-3/gallery-1.avif",
                        AltText: "Obraz 3",
                        IsPrimary: false,
                        DisplayOrder: 2)
                ]),
            cancellationToken);

        await PublishProductAsync(obraz3Id, cancellationToken);

        // Obrazy płaskie — Obraz 4
        var obraz4Id = await CreateProductAsync(
            new CreateProductCommand(
                Name: "Obraz 4",
                SeoTitle: "Obraz 4",
                FullDescription: """
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
                MetaDescription: "Wymiary: 30 × 42 × 2 cm. Kompozycja z suszonych kwiatów na szybie, w ramie z drewna sosnowego.",
                Slug: "obraz-4",
                CategoryId: obrazyPlaskieId,
                Price: 300m,
                DisplayOrder: 4,
                IsFeatured: true,
                AvailabilityStatus: ProductAvailabilityStatus.MadeToOrder,
                Images:
                [
                    new CreateProductCommandImage(
                        Source: "/images/uploads/products/obrazy-botaniczne/obrazy-plaskie/obraz-4/thumbnail.avif",
                        AltText: "Obraz 4",
                        IsPrimary: true,
                        DisplayOrder: 1)
                ]),
            cancellationToken);

        await PublishProductAsync(obraz4Id, cancellationToken);

        // Obrazy przestrzenne — Obraz 1
        var obraz1Id = await CreateProductAsync(
            new CreateProductCommand(
                Name: "Obraz 1",
                SeoTitle: "Obraz 1",
                FullDescription: """
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
                MetaDescription: "Wymiary: 50 × 50 × 4,5 cm. Kompozycja z suszonych kwiatów na płótnie lub papierze, w białej ramie przestrzennej z szybą.",
                Slug: "obraz-1",
                CategoryId: obrazyPrzestrzenneId,
                Price: 450m,
                DisplayOrder: 1,
                IsFeatured: true,
                AvailabilityStatus: ProductAvailabilityStatus.MadeToOrder,
                Images:
                [
                    new CreateProductCommandImage(
                        Source: "/images/uploads/products/obrazy-botaniczne/obrazy-przestrzenne/obraz-1/thumbnail.avif",
                        AltText: "Obraz 1",
                        IsPrimary: true,
                        DisplayOrder: 1),
                    new CreateProductCommandImage(
                        Source: "/images/uploads/products/obrazy-botaniczne/obrazy-przestrzenne/obraz-1/gallery-1.avif",
                        AltText: "Obraz 1",
                        IsPrimary: false,
                        DisplayOrder: 2),
                    new CreateProductCommandImage(
                        Source: "/images/uploads/products/obrazy-botaniczne/obrazy-przestrzenne/obraz-1/gallery-2.avif",
                        AltText: "Obraz 1",
                        IsPrimary: false,
                        DisplayOrder: 3)
                ]),
            cancellationToken);

        await PublishProductAsync(obraz1Id, cancellationToken);

        // Obrazy przestrzenne — Obraz 5
        var obraz5Id = await CreateProductAsync(
            new CreateProductCommand(
                Name: "Obraz 5",
                SeoTitle: "Obraz 5",
                FullDescription: """
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
                MetaDescription: "Wymiary: 25 × 25 × 4,5 cm. Kompozycja z suszonych kwiatów na płótnie lub papierze, w białej ramie przestrzennej z szybą.",
                Slug: "obraz-5",
                CategoryId: obrazyPrzestrzenneId,
                Price: 300m,
                DisplayOrder: 5,
                IsFeatured: true,
                AvailabilityStatus: ProductAvailabilityStatus.MadeToOrder,
                Images:
                [
                    new CreateProductCommandImage(
                        Source: "/images/uploads/products/obrazy-botaniczne/obrazy-przestrzenne/obraz-5/thumbnail.avif",
                        AltText: "Obraz 5",
                        IsPrimary: true,
                        DisplayOrder: 1),
                    new CreateProductCommandImage(
                        Source: "/images/uploads/products/obrazy-botaniczne/obrazy-przestrzenne/obraz-5/gallery-1.avif",
                        AltText: "Obraz 5",
                        IsPrimary: false,
                        DisplayOrder: 2)
                ]),
            cancellationToken);

        await PublishProductAsync(obraz5Id, cancellationToken);

        // Obrazy przestrzenne — Obraz 6
        var obraz6Id = await CreateProductAsync(
            new CreateProductCommand(
                Name: "Obraz 6",
                SeoTitle: "Obraz 6",
                FullDescription: """
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
                MetaDescription: "Wymiary: 25 × 25 × 4,5 cm. Kompozycja z suszonych kwiatów na płótnie lub papierze, w białej ramie przestrzennej z szybą.",
                Slug: "obraz-6",
                CategoryId: obrazyPrzestrzenneId,
                Price: 300m,
                DisplayOrder: 6,
                IsFeatured: false,
                AvailabilityStatus: ProductAvailabilityStatus.MadeToOrder,
                Images:
                [
                    new CreateProductCommandImage(
                        Source: "/images/uploads/products/obrazy-botaniczne/obrazy-przestrzenne/obraz-6/thumbnail.avif",
                        AltText: "Obraz 6",
                        IsPrimary: true,
                        DisplayOrder: 1),
                    new CreateProductCommandImage(
                        Source: "/images/uploads/products/obrazy-botaniczne/obrazy-przestrzenne/obraz-6/gallery-1.avif",
                        AltText: "Obraz 6",
                        IsPrimary: false,
                        DisplayOrder: 2),
                    new CreateProductCommandImage(
                        Source: "/images/uploads/products/obrazy-botaniczne/obrazy-przestrzenne/obraz-6/gallery-2.avif",
                        AltText: "Obraz 6",
                        IsPrimary: false,
                        DisplayOrder: 3),
                    new CreateProductCommandImage(
                        Source: "/images/uploads/products/obrazy-botaniczne/obrazy-przestrzenne/obraz-6/gallery-3.avif",
                        AltText: "Obraz 6",
                        IsPrimary: false,
                        DisplayOrder: 4)
                ]),
            cancellationToken);

        await PublishProductAsync(obraz6Id, cancellationToken);

        // Obrazy przestrzenne — Obraz 7
        var obraz7Id = await CreateProductAsync(
            new CreateProductCommand(
                Name: "Obraz 7",
                SeoTitle: "Obraz 7",
                FullDescription: """
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
                MetaDescription: "Wymiary: 32 × 42 × 3 cm. Kompozycja z suszonych kwiatów na płótnie lub papierze, w białej ramie przestrzennej z szybą.",
                Slug: "obraz-7",
                CategoryId: obrazyPrzestrzenneId,
                Price: 350m,
                DisplayOrder: 7,
                IsFeatured: true,
                AvailabilityStatus: ProductAvailabilityStatus.MadeToOrder,
                Images:
                [
                    new CreateProductCommandImage(
                        Source: "/images/uploads/products/obrazy-botaniczne/obrazy-przestrzenne/obraz-7/thumbnail.avif",
                        AltText: "Obraz 7",
                        IsPrimary: true,
                        DisplayOrder: 1),
                    new CreateProductCommandImage(
                        Source: "/images/uploads/products/obrazy-botaniczne/obrazy-przestrzenne/obraz-7/gallery-1.avif",
                        AltText: "Obraz 7",
                        IsPrimary: false,
                        DisplayOrder: 2),
                    new CreateProductCommandImage(
                        Source: "/images/uploads/products/obrazy-botaniczne/obrazy-przestrzenne/obraz-7/gallery-2.avif",
                        AltText: "Obraz 7",
                        IsPrimary: false,
                        DisplayOrder: 3),
                    new CreateProductCommandImage(
                        Source: "/images/uploads/products/obrazy-botaniczne/obrazy-przestrzenne/obraz-7/gallery-3.avif",
                        AltText: "Obraz 7",
                        IsPrimary: false,
                        DisplayOrder: 4)
                ]),
            cancellationToken);
        await PublishProductAsync(obraz7Id, cancellationToken);

        // Obrazy przestrzenne — Obraz 8
        var obraz8Id = await CreateProductAsync(
            new CreateProductCommand(
                Name: "Obraz 8",
                SeoTitle: "Obraz 8",
                FullDescription: """
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
                MetaDescription: "Wymiary: 25 × 25 × 4,5 cm. Kompozycja z suszonych kwiatów na płótnie lub papierze, w białej ramie przestrzennej z szybą.",
                Slug: "obraz-8",
                CategoryId: obrazyPrzestrzenneId,
                Price: 300m,
                DisplayOrder: 8,
                IsFeatured: true,
                AvailabilityStatus: ProductAvailabilityStatus.MadeToOrder,
                Images:
                [
                    new CreateProductCommandImage(
                        Source: "/images/uploads/products/obrazy-botaniczne/obrazy-przestrzenne/obraz-8/thumbnail.avif",
                        AltText: "Obraz 8",
                        IsPrimary: true,
                        DisplayOrder: 1),
                    new CreateProductCommandImage(
                        Source: "/images/uploads/products/obrazy-botaniczne/obrazy-przestrzenne/obraz-8/gallery-1.avif",
                        AltText: "Obraz 8",
                        IsPrimary: false,
                        DisplayOrder: 2),
                    new CreateProductCommandImage(
                        Source: "/images/uploads/products/obrazy-botaniczne/obrazy-przestrzenne/obraz-8/gallery-2.avif",
                        AltText: "Obraz 8",
                        IsPrimary: false,
                        DisplayOrder: 3)
                ]),
            cancellationToken);
        await PublishProductAsync(obraz8Id, cancellationToken);

        // Obrazy przestrzenne — Obraz 9
        var obraz9Id = await CreateProductAsync(
            new CreateProductCommand(
                Name: "Obraz 9",
                SeoTitle: "Obraz 9",
                FullDescription: """
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
                MetaDescription: "Wymiary: 25 × 25 × 4,5 cm. Kompozycja z suszonych kwiatów na płótnie lub papierze, w białej ramie przestrzennej z szybą.",
                Slug: "obraz-9",
                CategoryId: obrazyPrzestrzenneId,
                Price: 300m,
                DisplayOrder: 9,
                IsFeatured: true,
                AvailabilityStatus: ProductAvailabilityStatus.MadeToOrder,
                Images:
                [
                    new CreateProductCommandImage(
                        Source: "/images/uploads/products/obrazy-botaniczne/obrazy-przestrzenne/obraz-9/thumbnail.avif",
                        AltText: "Obraz 9",
                        IsPrimary: true,
                        DisplayOrder: 1),
                    new CreateProductCommandImage(
                        Source: "/images/uploads/products/obrazy-botaniczne/obrazy-przestrzenne/obraz-9/gallery-1.avif",
                        AltText: "Obraz 9",
                        IsPrimary: false,
                        DisplayOrder: 2)
                ]),
            cancellationToken);
        await PublishProductAsync(obraz9Id, cancellationToken);

        // Obrazy przestrzenne — Obraz 10
        var obraz10Id = await CreateProductAsync(
            new CreateProductCommand(
                Name: "Obraz 10",
                SeoTitle: "Obraz 10",
                FullDescription: """
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
                MetaDescription: "Wymiary: 25 × 25 × 4,5 cm. Kompozycja z suszonych kwiatów na płótnie lub papierze, w białej ramie przestrzennej z szybą.",
                Slug: "obraz-10",
                CategoryId: obrazyPrzestrzenneId,
                Price: 300m,
                DisplayOrder: 10,
                IsFeatured: false,
                AvailabilityStatus: ProductAvailabilityStatus.MadeToOrder,
                Images:
                [
                    new CreateProductCommandImage(
                        Source: "/images/uploads/products/obrazy-botaniczne/obrazy-przestrzenne/obraz-10/thumbnail.avif",
                        AltText: "Obraz 10",
                        IsPrimary: true,
                        DisplayOrder: 1)
                ]),
            cancellationToken);
        await PublishProductAsync(obraz10Id, cancellationToken);

        // Obrazy przestrzenne — Obraz 11
        var obraz11Id = await CreateProductAsync(
            new CreateProductCommand(
                Name: "Obraz 11",
                SeoTitle: "Obraz 11",
                FullDescription: """
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
                MetaDescription: "Wymiary: 32 × 42 × 3 cm. Kompozycja z suszonych kwiatów na płótnie lub papierze, w białej ramie przestrzennej z szybą.",
                Slug: "obraz-11",
                CategoryId: obrazyPrzestrzenneId,
                Price: 350m,
                DisplayOrder: 11,
                IsFeatured: false,
                AvailabilityStatus: ProductAvailabilityStatus.MadeToOrder,
                Images:
                [
                    new CreateProductCommandImage(
                        Source: "/images/uploads/products/obrazy-botaniczne/obrazy-przestrzenne/obraz-11/thumbnail.avif",
                        AltText: "Obraz 11",
                        IsPrimary: true,
                        DisplayOrder: 1),
                    new CreateProductCommandImage(
                        Source: "/images/uploads/products/obrazy-botaniczne/obrazy-przestrzenne/obraz-11/gallery-1.avif",
                        AltText: "Obraz 11",
                        IsPrimary: false,
                        DisplayOrder: 2)
                ]),
            cancellationToken);
        await PublishProductAsync(obraz11Id, cancellationToken);

        // Obrazy przestrzenne — Obraz 12
        var obraz12Id = await CreateProductAsync(
            new CreateProductCommand(
                Name: "Obraz 12",
                SeoTitle: "Obraz 12",
                FullDescription: """
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
                MetaDescription: "Wymiary: 25 × 25 × 4,5 cm. Kompozycja z suszonych kwiatów na płótnie lub papierze, w białej ramie przestrzennej z szybą.",
                Slug: "obraz-12",
                CategoryId: obrazyPrzestrzenneId,
                Price: 300m,
                DisplayOrder: 12,
                IsFeatured: true,
                AvailabilityStatus: ProductAvailabilityStatus.MadeToOrder,
                Images:
                [
                    new CreateProductCommandImage(
                        Source: "/images/uploads/products/obrazy-botaniczne/obrazy-przestrzenne/obraz-12/thumbnail.avif",
                        AltText: "Obraz 12",
                        IsPrimary: true,
                        DisplayOrder: 1),
                    new CreateProductCommandImage(
                        Source: "/images/uploads/products/obrazy-botaniczne/obrazy-przestrzenne/obraz-12/gallery-1.avif",
                        AltText: "Obraz 12",
                        IsPrimary: false,
                        DisplayOrder: 2)
                ]),
            cancellationToken);
        await PublishProductAsync(obraz12Id, cancellationToken);

        // Obrazy przestrzenne — Obraz 13
        var obraz13Id = await CreateProductAsync(
            new CreateProductCommand(
                Name: "Obraz 13",
                SeoTitle: "Obraz 13",
                FullDescription: """
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
                MetaDescription: "Wymiary: 25 × 25 × 4,5 cm. Kompozycja z suszonych kwiatów na płótnie lub papierze, w białej ramie przestrzennej z szybą.",
                Slug: "obraz-13",
                CategoryId: obrazyPrzestrzenneId,
                Price: 300m,
                DisplayOrder: 13,
                IsFeatured: false,
                AvailabilityStatus: ProductAvailabilityStatus.MadeToOrder,
                Images:
                [
                    new CreateProductCommandImage(
                        Source: "/images/uploads/products/obrazy-botaniczne/obrazy-przestrzenne/obraz-13/thumbnail.avif",
                        AltText: "Obraz 13",
                        IsPrimary: true,
                        DisplayOrder: 1)
                ]),
            cancellationToken);
        await PublishProductAsync(obraz13Id, cancellationToken);

        // Obrazy przestrzenne — Obraz 14
        var obraz14Id = await CreateProductAsync(
            new CreateProductCommand(
                Name: "Obraz 14",
                SeoTitle: "Obraz 14",
                FullDescription: """
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
                MetaDescription: "Wymiary: 32 × 42 × 3 cm. Kompozycja z suszonych kwiatów na płótnie lub papierze, w białej ramie przestrzennej z szybą.",
                Slug: "obraz-14",
                CategoryId: obrazyPrzestrzenneId,
                Price: 350m,
                DisplayOrder: 14,
                IsFeatured: true,
                AvailabilityStatus: ProductAvailabilityStatus.MadeToOrder,
                Images:
                [
                    new CreateProductCommandImage(
                        Source: "/images/uploads/products/obrazy-botaniczne/obrazy-przestrzenne/obraz-14/thumbnail.avif",
                        AltText: "Obraz 14",
                        IsPrimary: true,
                        DisplayOrder: 1),
                    new CreateProductCommandImage(
                        Source: "/images/uploads/products/obrazy-botaniczne/obrazy-przestrzenne/obraz-14/gallery-1.avif",
                        AltText: "Obraz 14",
                        IsPrimary: false,
                        DisplayOrder: 2),
                    new CreateProductCommandImage(
                        Source: "/images/uploads/products/obrazy-botaniczne/obrazy-przestrzenne/obraz-14/gallery-2.avif",
                        AltText: "Obraz 14",
                        IsPrimary: false,
                        DisplayOrder: 3)
                ]),
            cancellationToken);
        await PublishProductAsync(obraz14Id, cancellationToken);

        // Suszone kwiaty - Zestaw suszonych kwiatów
        var zestawSuszonychKwiatowId = await CreateProductAsync(
            new CreateProductCommand(
                Name: "Zestaw suszonych kwiatów",
                SeoTitle: "Zestaw suszonych kwiatów",
                FullDescription: """
                                  <p>
                                      Wszystkie kwiaty wykorzystywane w naszych obrazach suszymy samodzielnie w naszej pracowni.
                                      Pochodzą one z naszego ogrodu działkowego oraz od okolicznych kwiaciarni.
                                      W sezonie zbieramy też rośliny na łąkach i w lasach.
                                  </p>

                                  <p>
                                      Jeżeli interesuje Cię zakup suszonych kwiatów, a nie ma ich aktualnie w ofercie, to skontaktuj się z nami.
                                  </p>
                                  """,
                MetaDescription: "Wszystkie kwiaty wykorzystywane w naszych obrazach suszymy samodzielnie w naszej pracowni.",
                Slug: "zestaw-suszonych-kwiatow",
                CategoryId: suszoneKwiatyId,
                Price: null,
                DisplayOrder: 1,
                IsFeatured: false,
                AvailabilityStatus: ProductAvailabilityStatus.ComingSoon,
                Images:
                [
                    new CreateProductCommandImage(
                        Source: "/images/uploads/products/suszone-kwiaty/zestaw-suszonych-kwiatow/thumbnail.avif",
                        AltText: "Zestaw suszonych kwiatów",
                        IsPrimary: true,
                        DisplayOrder: 1)
                ]),
            cancellationToken);
        await PublishProductAsync(zestawSuszonychKwiatowId, cancellationToken);

        // Zestawy DIY - Zestaw „Zrób sobie obraz”
        var zestawZrobSobieObrazId = await CreateProductAsync(
            new CreateProductCommand(
                Name: "Zestaw \"Zrób sobie obraz\"",
                SeoTitle: "Zestaw \"Zrób sobie obraz\"",
                FullDescription: """
                                  <p>
                                    Zestaw „Zrób sobie obraz” do samodzielnej pracy kreatywnej. Idealny na prezent dla Ciebie
                                    lub bliskiej osoby z artystyczną duszą. Praca z kwiatami to kontakt z naturą, a jej
                                    uzdrawiająca moc znana jest od wieków. Autorska kompozycja kwiatowa wprowadzi do Twojego domu
                                    spokój, radość i kawałek nieprzemijającej natury.
                                  </p>

                                  <p>Zestaw składa się z:</p>
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

                                  <p>
                                    Pamiętaj, że obraz powstanie z materiału roślinnego i organicznego, dlatego z czasem może ulegać zmianom, a jego kolory mogą blaknąć. To naturalny, nieunikniony proces i nie stanowi podstawy do reklamacji. Kupując ten zestaw, potwierdzasz, że jesteś tego świadomy. Aby obraz jak najdłużej zachował naturalny wygląd, eksponuj go z dala od bezpośredniego światła i wilgoci.
                                  </p>
                                  """,
                MetaDescription: "Zestaw DIY do samodzielnej pracy kreatywnej. Idealny na prezent dla Ciebie lub bliskiej osoby z artystyczną duszą.",
                Slug: "zestaw-zrob-sobie-obraz",
                CategoryId: zestawyDiyId,
                Price: null,
                DisplayOrder: 1,
                IsFeatured: false,
                AvailabilityStatus: ProductAvailabilityStatus.ComingSoon,
                Images:
                [
                    new CreateProductCommandImage(
                        Source: "/images/uploads/products/zestawy-diy-zrob-sobie-obraz/zestaw-zrob-sobie-obraz/thumbnail.avif",
                        AltText: "Zestaw \"Zrób sobie obraz\"",
                        IsPrimary: true,
                        DisplayOrder: 1)
                ]),
            cancellationToken);
        await PublishProductAsync(zestawZrobSobieObrazId, cancellationToken);

        _logger.LogInformation("Catalog seeding workflow completed successfully");
    }

    /// <summary>
    /// Checks if seed data already exists in the database by looking for a sentinel category.
    /// </summary>
    private async Task<bool> HasExistingSeedDataAsync(CancellationToken cancellationToken)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken);

        const string sql = "SELECT EXISTS(SELECT 1 FROM categories WHERE current_slug = @Slug AND deleted_at IS NULL)";

        var exists = await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(sql, new { Slug = SentinelCategorySlug }, cancellationToken: cancellationToken));

        return exists;
    }

    private async Task<long> CreateCategoryAsync(
        CreateCategoryCommand command,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating category '{Name}' with slug '{Slug}'", command.Name, command.Slug);

        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            throw new InvalidOperationException(
                $"Failed to create category '{command.Name}': {result.Error.Message}");
        }

        var categoryId = result.Value.CategoryId;
        _logger.LogInformation("Created category '{Name}' with ID {CategoryId}", command.Name, categoryId);

        return categoryId;
    }

    private async Task PublishCategoryAsync(long categoryId, CancellationToken cancellationToken)
    {
        var category = await _categoryRepository.GetByIdAsync(categoryId, cancellationToken);
        if (category is null)
        {
            throw new InvalidOperationException($"Category with ID {categoryId} not found after creation");
        }

        var command = new UpdateCategoryCommand(
            CategoryId: categoryId,
            RowVersion: category.RowVersion,
            Name: Optional<string>.Unset,
            Description: Optional<string?>.Unset,
            ImagePath: Optional<string?>.Unset,
            ImageAltText: Optional<string?>.Unset,
            DisplayOrder: Optional<int>.Unset,
            PublishStatus: Optional<PublishStatus>.FromValue(PublishStatus.Published));

        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            throw new InvalidOperationException(
                $"Failed to publish category {categoryId}: {result.Error.Message}");
        }

        _logger.LogInformation("Published category ID {CategoryId}", categoryId);
    }

    private async Task<long> CreateProductAsync(
        CreateProductCommand command,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating product '{Name}' with slug '{Slug}'", command.Name, command.Slug);

        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            throw new InvalidOperationException(
                $"Failed to create product '{command.Name}': {result.Error.Message}");
        }

        var productId = result.Value.Id;
        _logger.LogInformation("Created product '{Name}' with ID {ProductId}", command.Name, productId);

        return productId;
    }

    private async Task PublishProductAsync(long productId, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(productId, cancellationToken);
        if (product is null)
        {
            throw new InvalidOperationException($"Product with ID {productId} not found after creation");
        }

        var command = new UpdateProductCommand(
            ProductId: productId,
            RowVersion: product.RowVersion,
            Name: Optional<string>.Unset,
            SeoTitle: Optional<string?>.Unset,
            FullDescription: Optional<string?>.Unset,
            MetaDescription: Optional<string?>.Unset,
            CategoryId: Optional<long>.Unset,
            Price: Optional<decimal?>.Unset,
            DisplayOrder: Optional<int>.Unset,
            IsFeatured: Optional<bool>.Unset,
            Availability: Optional<ProductAvailabilityStatus>.Unset,
            PublishStatus: Optional<PublishStatus>.FromValue(PublishStatus.Published));

        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            throw new InvalidOperationException(
                $"Failed to publish product {productId}: {result.Error.Message}");
        }

        _logger.LogInformation("Published product ID {ProductId}", productId);
    }
}
