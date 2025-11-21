using Dapper;
using ForeverBloom.Application.Abstractions.Data;
using ForeverBloom.Application.Abstractions.Data.Repositories;
using ForeverBloom.Application.Categories.Commands.CreateCategory;
using ForeverBloom.Application.Categories.Commands.UpdateCategory;
using ForeverBloom.Application.Products.Commands.CreateProduct;
using ForeverBloom.Application.Products.Commands.UpdateProduct;
using ForeverBloom.Domain.Catalog;
using ForeverBloom.SharedKernel.Optional;
using ForeverBloom.Tools.DatabaseSeeder.Extensions;
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
            CreateCategoryCommand.Create(
                name: "Obrazy botaniczne",
                slug: "obrazy-botaniczne",
                description: "Moje obrazy powstają pod wpływem chwili, emocji, wdzięczności, głosu serca lub potrzeby wyciszenia. To dar natury dla Twojego domu.",
                imagePath: "/images/uploads/categories/obrazy-botaniczne/banner.avif",
                imageAltText: "Obrazy botaniczne",
                parentCategoryId: null,
                displayOrder: 1).ValueOrThrow("Category 'Obrazy botaniczne' creation"),
            cancellationToken);

        await PublishCategoryAsync(obrazyBotaniczneId, cancellationToken);

        // Obrazy płaskie
        var obrazyPlaskieId = await CreateCategoryAsync(
            CreateCategoryCommand.Create(
                name: "Obrazy płaskie",
                slug: "obrazy-plaskie",
                description: "Moje obrazy powstają pod wpływem chwili, emocji, wdzięczności, głosu serca lub potrzeby wyciszenia. To dar natury dla Twojego domu.",
                imagePath: "/images/uploads/categories/obrazy-botaniczne/banner.avif",
                imageAltText: "Obrazy płaskie",
                parentCategoryId: obrazyBotaniczneId,
                displayOrder: 1).ValueOrThrow("Category 'Obrazy płaskie' creation"),
            cancellationToken);

        await PublishCategoryAsync(obrazyPlaskieId, cancellationToken);

        // Obrazy przestrzenne
        var obrazyPrzestrzenneId = await CreateCategoryAsync(
            CreateCategoryCommand.Create(
                name: "Obrazy przestrzenne",
                slug: "obrazy-przestrzenne",
                description: "Moje obrazy powstają pod wpływem chwili, emocji, wdzięczności, głosu serca lub potrzeby wyciszenia. To dar natury dla Twojego domu.",
                imagePath: "/images/uploads/categories/obrazy-botaniczne/banner.avif",
                imageAltText: "Obrazy przestrzenne",
                parentCategoryId: obrazyBotaniczneId,
                displayOrder: 2).ValueOrThrow("Category 'Obrazy przestrzenne' creation"),
            cancellationToken);

        await PublishCategoryAsync(obrazyPrzestrzenneId, cancellationToken);

        // Suszone kwiaty
        var suszoneKwiatyId = await CreateCategoryAsync(
            CreateCategoryCommand.Create(
                name: "Suszone kwiaty",
                slug: "suszone-kwiaty",
                description: "Najpiękniejsze zestawy kwiatów do Twojego rękodzieła.",
                imagePath: "/images/uploads/categories/suszone-kwiaty/banner.avif",
                imageAltText: "Suszone kwiaty",
                parentCategoryId: null,
                displayOrder: 2).ValueOrThrow("Category 'Suszone kwiaty' creation"),
            cancellationToken);

        await PublishCategoryAsync(suszoneKwiatyId, cancellationToken);

        // Zestawy DIY
        var zestawyDiyId = await CreateCategoryAsync(
            CreateCategoryCommand.Create(
                name: "Zestawy \"Zrób sobie obraz\"",
                slug: "zestawy-diy-zrob-sobie-obraz",
                description: "Zestawy DIY dla Ciebie do samodzielnego stworzenia kwiatowego obrazu.",
                imagePath: "/images/uploads/categories/zestawy-diy-zrob-sobie-obraz/banner.avif",
                imageAltText: "Zestawy \"Zrób sobie obraz\"",
                parentCategoryId: null,
                displayOrder: 3).ValueOrThrow("Category 'Zestawy DIY' creation"),
            cancellationToken);

        await PublishCategoryAsync(zestawyDiyId, cancellationToken);

        // ------------
        // Products
        // ------------

        // Obrazy płaskie — Obraz 2
        var obraz2Id = await CreateProductAsync(
            CreateProductCommand.Create(
                name: "Obraz 2",
                slug: "obraz-2",
                categoryId: obrazyPlaskieId,
                availabilityStatus: ProductAvailabilityStatus.MadeToOrder.Name,
                isFeatured: false,
                seoTitle: "Obraz 2",
                fullDescription: """
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
                metaDescription: "Wymiary: 21 × 29,5 × 4,5 cm. Kompozycja z suszonych kwiatów na szybie, w ramie z drewna sosnowego.",
                price: 200m,
                images:
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
                ]).ValueOrThrow("Product 'Obraz 2' creation"),
            cancellationToken);

        await PublishProductAsync(obraz2Id, cancellationToken);

        // Obrazy płaskie — Obraz 3
        var obraz3Id = await CreateProductAsync(
            CreateProductCommand.Create(
                name: "Obraz 3",
                seoTitle: "Obraz 3",
                fullDescription: """
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
                metaDescription: "Wymiary: 23 × 27 cm. Kompozycja z suszonych kwiatów na szybie, w białej ramie przestrzennej z szybą.",
                slug: "obraz-3",
                categoryId: obrazyPlaskieId,
                price: null,
                isFeatured: true,
                availabilityStatus: ProductAvailabilityStatus.MadeToOrder.Name,
                images:
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
                ]).ValueOrThrow("Product 'Obraz 3' creation"),
            cancellationToken);

        await PublishProductAsync(obraz3Id, cancellationToken);

        // Obrazy płaskie — Obraz 4
        var obraz4Id = await CreateProductAsync(
            CreateProductCommand.Create(
                name: "Obraz 4",
                seoTitle: "Obraz 4",
                fullDescription: """
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
                metaDescription: "Wymiary: 30 × 42 × 2 cm. Kompozycja z suszonych kwiatów na szybie, w ramie z drewna sosnowego.",
                slug: "obraz-4",
                categoryId: obrazyPlaskieId,
                price: 300m,
                isFeatured: true,
                availabilityStatus: ProductAvailabilityStatus.MadeToOrder.Name,
                images:
                [
                    new CreateProductCommandImage(
                        Source: "/images/uploads/products/obrazy-botaniczne/obrazy-plaskie/obraz-4/thumbnail.avif",
                        AltText: "Obraz 4",
                        IsPrimary: true,
                        DisplayOrder: 1)
                ]).ValueOrThrow("Product 'Obraz 4' creation"),
            cancellationToken);

        await PublishProductAsync(obraz4Id, cancellationToken);

        // Obrazy przestrzenne — Obraz 1
        var obraz1Id = await CreateProductAsync(
            CreateProductCommand.Create(
                name: "Obraz 1",
                seoTitle: "Obraz 1",
                fullDescription: """
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
                metaDescription: "Wymiary: 50 × 50 × 4,5 cm. Kompozycja z suszonych kwiatów na płótnie lub papierze, w białej ramie przestrzennej z szybą.",
                slug: "obraz-1",
                categoryId: obrazyPrzestrzenneId,
                price: 450m,
                isFeatured: true,
                availabilityStatus: ProductAvailabilityStatus.MadeToOrder.Name,
                images:
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
                ]).ValueOrThrow("Product 'Obraz 1' creation"),
            cancellationToken);

        await PublishProductAsync(obraz1Id, cancellationToken);

        // Obrazy przestrzenne — Obraz 5
        var obraz5Id = await CreateProductAsync(
            CreateProductCommand.Create(
                name: "Obraz 5",
                seoTitle: "Obraz 5",
                fullDescription: """
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
                metaDescription: "Wymiary: 25 × 25 × 4,5 cm. Kompozycja z suszonych kwiatów na płótnie lub papierze, w białej ramie przestrzennej z szybą.",
                slug: "obraz-5",
                categoryId: obrazyPrzestrzenneId,
                price: 300m,
                isFeatured: true,
                availabilityStatus: ProductAvailabilityStatus.MadeToOrder.Name,
                images:
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
                ]).ValueOrThrow("Product 'Obraz 5' creation"),
            cancellationToken);

        await PublishProductAsync(obraz5Id, cancellationToken);

        // Obrazy przestrzenne — Obraz 6
        var obraz6Id = await CreateProductAsync(
            CreateProductCommand.Create(
                name: "Obraz 6",
                seoTitle: "Obraz 6",
                fullDescription: """
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
                metaDescription: "Wymiary: 25 × 25 × 4,5 cm. Kompozycja z suszonych kwiatów na płótnie lub papierze, w białej ramie przestrzennej z szybą.",
                slug: "obraz-6",
                categoryId: obrazyPrzestrzenneId,
                price: 300m,
                isFeatured: false,
                availabilityStatus: ProductAvailabilityStatus.MadeToOrder.Name,
                images:
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
                ]).ValueOrThrow("Product 'Obraz 6' creation"),
            cancellationToken);

        await PublishProductAsync(obraz6Id, cancellationToken);

        // Obrazy przestrzenne — Obraz 7
        var obraz7Id = await CreateProductAsync(
            CreateProductCommand.Create(
                name: "Obraz 7",
                seoTitle: "Obraz 7",
                fullDescription: """
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
                metaDescription: "Wymiary: 32 × 42 × 3 cm. Kompozycja z suszonych kwiatów na płótnie lub papierze, w białej ramie przestrzennej z szybą.",
                slug: "obraz-7",
                categoryId: obrazyPrzestrzenneId,
                price: 350m,
                isFeatured: true,
                availabilityStatus: ProductAvailabilityStatus.MadeToOrder.Name,
                images:
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
                ]).ValueOrThrow("Product 'Obraz 7' creation"),
            cancellationToken);
        await PublishProductAsync(obraz7Id, cancellationToken);

        // Obrazy przestrzenne — Obraz 8
        var obraz8Id = await CreateProductAsync(
            CreateProductCommand.Create(
                name: "Obraz 8",
                seoTitle: "Obraz 8",
                fullDescription: """
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
                metaDescription: "Wymiary: 25 × 25 × 4,5 cm. Kompozycja z suszonych kwiatów na płótnie lub papierze, w białej ramie przestrzennej z szybą.",
                slug: "obraz-8",
                categoryId: obrazyPrzestrzenneId,
                price: 300m,
                isFeatured: true,
                availabilityStatus: ProductAvailabilityStatus.MadeToOrder.Name,
                images:
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
                ]).ValueOrThrow("Product 'Obraz 8' creation"),
            cancellationToken);
        await PublishProductAsync(obraz8Id, cancellationToken);

        // Obrazy przestrzenne — Obraz 9
        var obraz9Id = await CreateProductAsync(
            CreateProductCommand.Create(
                name: "Obraz 9",
                seoTitle: "Obraz 9",
                fullDescription: """
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
                metaDescription: "Wymiary: 25 × 25 × 4,5 cm. Kompozycja z suszonych kwiatów na płótnie lub papierze, w białej ramie przestrzennej z szybą.",
                slug: "obraz-9",
                categoryId: obrazyPrzestrzenneId,
                price: 300m,
                isFeatured: true,
                availabilityStatus: ProductAvailabilityStatus.MadeToOrder.Name,
                images:
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
                ]).ValueOrThrow("Product 'Obraz 9' creation"),
            cancellationToken);
        await PublishProductAsync(obraz9Id, cancellationToken);

        // Obrazy przestrzenne — Obraz 10
        var obraz10Id = await CreateProductAsync(
            CreateProductCommand.Create(
                name: "Obraz 10",
                seoTitle: "Obraz 10",
                fullDescription: """
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
                metaDescription: "Wymiary: 25 × 25 × 4,5 cm. Kompozycja z suszonych kwiatów na płótnie lub papierze, w białej ramie przestrzennej z szybą.",
                slug: "obraz-10",
                categoryId: obrazyPrzestrzenneId,
                price: 300m,
                isFeatured: false,
                availabilityStatus: ProductAvailabilityStatus.MadeToOrder.Name,
                images:
                [
                    new CreateProductCommandImage(
                        Source: "/images/uploads/products/obrazy-botaniczne/obrazy-przestrzenne/obraz-10/thumbnail.avif",
                        AltText: "Obraz 10",
                        IsPrimary: true,
                        DisplayOrder: 1)
                ]).ValueOrThrow("Product 'Obraz 10' creation"),
            cancellationToken);
        await PublishProductAsync(obraz10Id, cancellationToken);

        // Obrazy przestrzenne — Obraz 11
        var obraz11Id = await CreateProductAsync(
            CreateProductCommand.Create(
                name: "Obraz 11",
                seoTitle: "Obraz 11",
                fullDescription: """
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
                metaDescription: "Wymiary: 32 × 42 × 3 cm. Kompozycja z suszonych kwiatów na płótnie lub papierze, w białej ramie przestrzennej z szybą.",
                slug: "obraz-11",
                categoryId: obrazyPrzestrzenneId,
                price: 350m,
                isFeatured: false,
                availabilityStatus: ProductAvailabilityStatus.MadeToOrder.Name,
                images:
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
                ]).ValueOrThrow("Product 'Obraz 11' creation"),
            cancellationToken);
        await PublishProductAsync(obraz11Id, cancellationToken);

        // Obrazy przestrzenne — Obraz 12
        var obraz12Id = await CreateProductAsync(
            CreateProductCommand.Create(
                name: "Obraz 12",
                seoTitle: "Obraz 12",
                fullDescription: """
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
                metaDescription: "Wymiary: 25 × 25 × 4,5 cm. Kompozycja z suszonych kwiatów na płótnie lub papierze, w białej ramie przestrzennej z szybą.",
                slug: "obraz-12",
                categoryId: obrazyPrzestrzenneId,
                price: 300m,
                isFeatured: true,
                availabilityStatus: ProductAvailabilityStatus.MadeToOrder.Name,
                images:
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
                ]).ValueOrThrow("Product 'Obraz 12' creation"),
            cancellationToken);
        await PublishProductAsync(obraz12Id, cancellationToken);

        // Obrazy przestrzenne — Obraz 13
        var obraz13Id = await CreateProductAsync(
            CreateProductCommand.Create(
                name: "Obraz 13",
                seoTitle: "Obraz 13",
                fullDescription: """
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
                metaDescription: "Wymiary: 25 × 25 × 4,5 cm. Kompozycja z suszonych kwiatów na płótnie lub papierze, w białej ramie przestrzennej z szybą.",
                slug: "obraz-13",
                categoryId: obrazyPrzestrzenneId,
                price: 300m,
                isFeatured: false,
                availabilityStatus: ProductAvailabilityStatus.MadeToOrder.Name,
                images:
                [
                    new CreateProductCommandImage(
                        Source: "/images/uploads/products/obrazy-botaniczne/obrazy-przestrzenne/obraz-13/thumbnail.avif",
                        AltText: "Obraz 13",
                        IsPrimary: true,
                        DisplayOrder: 1)
                ]).ValueOrThrow("Product 'Obraz 13' creation"),
            cancellationToken);
        await PublishProductAsync(obraz13Id, cancellationToken);

        // Obrazy przestrzenne — Obraz 14
        var obraz14Id = await CreateProductAsync(
            CreateProductCommand.Create(
                name: "Obraz 14",
                seoTitle: "Obraz 14",
                fullDescription: """
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
                metaDescription: "Wymiary: 32 × 42 × 3 cm. Kompozycja z suszonych kwiatów na płótnie lub papierze, w białej ramie przestrzennej z szybą.",
                slug: "obraz-14",
                categoryId: obrazyPrzestrzenneId,
                price: 350m,
                isFeatured: true,
                availabilityStatus: ProductAvailabilityStatus.MadeToOrder.Name,
                images:
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
                ]).ValueOrThrow("Product 'Obraz 14' creation"),
            cancellationToken);
        await PublishProductAsync(obraz14Id, cancellationToken);

        // Suszone kwiaty - Zestaw suszonych kwiatów
        var zestawSuszonychKwiatowId = await CreateProductAsync(
            CreateProductCommand.Create(
                name: "Zestaw suszonych kwiatów",
                seoTitle: "Zestaw suszonych kwiatów",
                fullDescription: """
                                  <p>
                                      Wszystkie kwiaty wykorzystywane w naszych obrazach suszymy samodzielnie w naszej pracowni.
                                      Pochodzą one z naszego ogrodu działkowego oraz od okolicznych kwiaciarni.
                                      W sezonie zbieramy też rośliny na łąkach i w lasach.
                                  </p>

                                  <p>
                                      Jeżeli interesuje Cię zakup suszonych kwiatów, a nie ma ich aktualnie w ofercie, to skontaktuj się z nami.
                                  </p>
                                  """,
                metaDescription: "Wszystkie kwiaty wykorzystywane w naszych obrazach suszymy samodzielnie w naszej pracowni.",
                slug: "zestaw-suszonych-kwiatow",
                categoryId: suszoneKwiatyId,
                price: null,
                isFeatured: false,
                availabilityStatus: ProductAvailabilityStatus.ComingSoon.Name,
                images:
                [
                    new CreateProductCommandImage(
                        Source: "/images/uploads/products/suszone-kwiaty/zestaw-suszonych-kwiatow/thumbnail.avif",
                        AltText: "Zestaw suszonych kwiatów",
                        IsPrimary: true,
                        DisplayOrder: 1)
                ]).ValueOrThrow("Product 'Zestaw suszonych kwiatów' creation"),
            cancellationToken);
        await PublishProductAsync(zestawSuszonychKwiatowId, cancellationToken);

        // Zestawy DIY - Zestaw „Zrób sobie obraz”
        var zestawZrobSobieObrazId = await CreateProductAsync(
            CreateProductCommand.Create(
                name: "Zestaw \"Zrób sobie obraz\"",
                seoTitle: "Zestaw \"Zrób sobie obraz\"",
                fullDescription: """
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
                metaDescription: "Zestaw DIY do samodzielnej pracy kreatywnej. Idealny na prezent dla Ciebie lub bliskiej osoby z artystyczną duszą.",
                slug: "zestaw-zrob-sobie-obraz",
                categoryId: zestawyDiyId,
                price: null,
                isFeatured: false,
                availabilityStatus: ProductAvailabilityStatus.ComingSoon.Name,
                images:
                [
                    new CreateProductCommandImage(
                        Source: "/images/uploads/products/zestawy-diy-zrob-sobie-obraz/zestaw-zrob-sobie-obraz/thumbnail.avif",
                        AltText: "Zestaw \"Zrób sobie obraz\"",
                        IsPrimary: true,
                        DisplayOrder: 1)
                ]).ValueOrThrow("Product 'Zestaw \"Zrób sobie obraz\"' creation"),
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
        var categoryId = result.ValueOrThrow($"Category '{command.Name}' creation").CategoryId;

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

        var command = UpdateCategoryCommand.Create(
            categoryId: categoryId,
            rowVersion: category.RowVersion,
            publishStatus: Optional<string>.FromValue("published"))
            .ValueOrThrow($"Category '{category.Name}' publish");

        (await _sender.Send(command, cancellationToken)).ValueOrThrow($"Category '{category.Name}' publish");

        _logger.LogInformation("Published category ID {CategoryId}", categoryId);
    }

    private async Task<long> CreateProductAsync(
        CreateProductCommand command,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating product '{Name}' with slug '{Slug}'", command.Name, command.Slug);

        var result = await _sender.Send(command, cancellationToken);
        var productId = result.ValueOrThrow($"Product '{command.Name}' creation").Id;

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

        var command = UpdateProductCommand.Create(
                productId: productId,
                rowVersion: product.RowVersion,
                publishStatus: Optional<string>.FromValue(PublishStatus.Published.Name))
            .ValueOrThrow($"Product '{product.Name}' publish");

        (await _sender.Send(command, cancellationToken)).ValueOrThrow($"Product '{product.Name}' publish");

        _logger.LogInformation("Published product ID {ProductId}", productId);
    }
}
