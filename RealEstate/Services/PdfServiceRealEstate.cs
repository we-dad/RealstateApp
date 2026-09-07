using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using RealEstateInstallmentsManager.Models;

namespace RealEstateInstallmentsManager.Services;

public class PdfServiceRealEstate
{
    public PdfServiceRealEstate()
    {
        // مهم لـ QuestPDF (Community)
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public void GenerateContractPdf(ContractRealEstate c, string filePath)
    {
        var ContractPeriod = CalculateContractPeriod(c.ContractStartDate, c.ContractEndDate);

        QuestPDF.Settings.License = LicenseType.Community;

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);

                page.DefaultTextStyle(x => x
                    .FontFamily("Arial")
                    .FontSize(12));

                page.Content().Column(col =>
                {
                    // ===== Header =====
                    col.Item().AlignCenter()
                        .Text("FALAH")
                        .FontSize(40)
                        .FontColor("#8FA8D8")
                        .Bold();

                    col.Item().PaddingTop(10).LineHorizontal(1);

                    col.Item().PaddingTop(18)
                        .AlignCenter()
                        .Text("عقد إيجار")
                        .FontSize(18)
                        .Bold();


                    // ===== Two parties blocks (يمين/يسار) =====
                    col.Item().PaddingTop(12).Row(r =>
                   {
                       // يمين: الطرف الثاني (المستأجر)
                       r.RelativeItem().Element(e => PartyBox(
                           e,
                           "الطرف الثاني (المستأجر)",
                           name: c.TenantName,
                           id: c.TenantIdentityNumber,
                           address: c.TenantAddress
                       ));

                       r.ConstantItem(100); // مسافة وسط

                       // يسار: الطرف الأول (المؤجر)
                       r.RelativeItem().Element(e => PartyBox(
                           e,
                           "الطرف الأول (المؤجر)",
                           name: c.OwnerName,
                           id: c.OwnerIdentityNumber,
                           address: c.OwnerAddress
                       ));
                   });

                    // ===== Agreement line =====
                    col.Item().PaddingTop(10).AlignRight()
                        .Text(": بموجب هذا العقد اتفق الطرفان و هما بكامل الأهلية القانونية على البنود الآتية")
                        .Bold();
                    col.Item().LineHorizontal(1);

                    col.Item().AlignRight().PaddingTop(6).DefaultTextStyle(x => x.FontSize(10).LineHeight(1.25f)).Text(text =>
                    {
                        text.AlignRight();

                        void Clause(string title, string body)
                        {
                            text.Span(title).Bold().Underline();
                            text.Span(" " + FixRtlPunctuation(body));
                            text.Line("");
                            text.Line("");
                        }

                        Clause("بند 1:", $"يؤجر الطرف الأول للطرف الثاني {c.ContractApartmentType} الواقعة في: ({c.City} - {c.District}) والمكونة من: {c.ContractUnitRoomsNum} غرف، {c.ContractUnitFloorNum} دور .");
                        Clause("بند 2:", $"مدة هذا العقد ({ContractPeriod}) تبدأ من تاريخ: {c.ContractStartDate:dd/MM/yyyy} وتنتهي بتاريخ: {c.ContractEndDate:dd/MM/yyyy} قابلة للتجديد بموافقة الطرفين.");
                        Clause("بند 3:", $"قيمة الإيجار الشهري: {c.RentAmount} ريال ، طريقة الدفع: {c.ContractPayMethod}.");
                        Clause("بند 4:", $"يلتزم المستأجر بالمحافظة على الشقة وعدم استخدامها في أي نشاط غير مشروع، ولا يحق له التنازل أو التأجير من الباطن إلا بموافقة خطية من المؤجر، {c.ContractOpligation}.");
                        Clause("بند 5:", "في حال رغبة أحد الطرفين إنهاء العقد قبل المدة، يجب إشعار الطرف الآخر قبل (شهر واحد) على الأقل.");
                        Clause("بند 6:", "يعتبر هذا العقد ساري المفعول بين الطرفين فور التوقيع عليه، وتحكمه أنظمة المملكة العربية السعودية.");
                    });
                    // ===== Signatures (يمين/يسار) =====
                    col.Item().PaddingTop(10).Row(r =>
                    {
                        // يمين: المستأجر
                        r.RelativeItem().Element(e => SignatureBox(e, "الطرف الثاني (المستأجر)", c.TenantName));

                        r.ConstantItem(100);

                        // يسار: المؤجر
                        r.RelativeItem().Element(e => SignatureBox(e, "الطرف الأول (المؤجر)", c.OwnerName));
                    });
                });

                // ===== Footer line + org info =====
                page.Footer().Element(footer =>
                {
                    footer.Column(col =>
                    {
                        col.Item().LineHorizontal(1);

                        col.Item()
                           .PaddingTop(8)
                           .AlignCenter()
                           .Text("مؤسسة فلاح حماد العوفي    سجل تجاري رقم 7051422512    جوال رقم 0554607289    ص ب 42393")
                           .FontSize(10);
                    });
                });
            });
        })
        .GeneratePdf(filePath);
    }

    static string FixRtlPunctuation(string s)//for arabic marker
    {
        const string RLM = "\u200F";
        return s.Replace(".", "." + RLM)
                .Replace("،", "،" + RLM)
                .Replace(",", "،" + RLM)
                .Replace(":", ":" + RLM);
    }

    static void PartyBox(IContainer container,
                         string title,
                         string? name,
                         string? id,
                         string? address)
    {
        container.Padding(10).Column(col =>
        {
            col.Spacing(6);

            col.Item().AlignRight().Text(title).Bold();
            col.Item().AlignRight().Text($"الاسم : {name ?? ""}");
            col.Item().AlignRight().Text($"رقم الھویة / الإقامة : {id ?? ""}");
            col.Item().AlignRight().Text($"العنوان : {address ?? ""}");
        });
    }

    static void SignatureBox(IContainer container, string title, string? name)
    {
        container.Column(col =>
        {
            col.Spacing(6);

            col.Item().AlignRight().Text(title).Bold();
            col.Item().AlignRight().Text($"الاسم : {name ?? ""}");
            col.Item().AlignRight().Text(" : التوقیع");
            col.Item().PaddingTop(16);
        });
    }

    public string CalculateContractPeriod(DateTime start, DateTime end)
    {
        if (end < start) (start, end) = (end, start);

        int totalDays = (end - start).Days;

        int totalMonths = totalDays / 30;
        int remainingDays = totalDays % 30;

        if (remainingDays >= 15)
            totalMonths++;

        int years = totalMonths / 12;
        int months = totalMonths % 12;

        string Y(int n) => n switch
        {
            0 => "",
            1 => "سنة",
            2 => "سنتين",
            _ => $"{n} سنوات"
        };

        string M(int n) => n switch
        {
            0 => "",
            1 => "شهر",
            2 => "شهرين",
            _ => $"{n} أشهر"
        };

        if (years > 0 && months > 0) return $"{Y(years)} و {M(months)}";
        if (years > 0) return Y(years);
        return M(months);
    }

    public void GenerateReceiptPdf(ReceiptRealEstate r, string filePath)
    {
        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x =>
    x.FontFamily("Arial")
     .FontSize(12)
);

                page.Content()
                    .AlignCenter()
                    .AlignMiddle()
                    .Width(650)
                    .Column(col =>
                {
                    col.Spacing(10);

                    col.Item().Element(section =>
                    {

                        section.Border(1).Padding(10).Column(s =>
                        {
                            s.Spacing(6);

                            var date = r.ReceiptDate;
                            var formatted = date.ToString("yyyy/MM/dd");

                            s.Item().ExtendHorizontal().Row(row =>
                            {

                                row.AutoItem().Text($"تاريخ السند: {date:yyyy/MM/dd}").AlignLeft().Bold();

                                row.RelativeItem();

                                row.AutoItem().Text($"{r.ReceiptNumber} : رقم السند").AlignRight().Bold();
                            });


                            s.Item().Text("سند قبض").FontSize(18).Bold().AlignCenter();

                            s.Item().ExtendHorizontal();

                            s.Item().ContentFromRightToLeft().Row(row =>
                            {
                                row.RelativeItem()
                                    .AlignRight()
                                    .Text("استلمنا من السيد / ")
                                    .Bold();

                                row.RelativeItem()
                                    .AlignRight()
                                    .Text(r.TenantName);

                                row.RelativeItem(4);
                            });

                            s.Item().ExtendHorizontal().Row(row =>
                             {
                                 row.RelativeItem();

                                 row.RelativeItem()
                                     .Border(1)
                                     .PaddingVertical(4)
                                     .PaddingHorizontal(8)
                                     .AlignCenter()
                                     .Row(amountRow =>
                                     {
                                         amountRow.AutoItem()
                                             .Text("ريال")
                                             .Bold();

                                         amountRow.ConstantItem(8);

                                         amountRow.AutoItem()
                                             .Text(r.Amount.ToString("N0"))
                                             .Bold();
                                     });

                                 row.RelativeItem()
                                    .AlignRight()
                                    .Text(": مبلغ وقدره")
                                    .Bold();
                             });
                            s.Item().ExtendHorizontal().Row(row =>
                             {
                                 row.RelativeItem();
                                 row.RelativeItem();
                                 row.RelativeItem();
                                 row.RelativeItem();

                                 row.RelativeItem()
                                     .AlignRight()
                                    .Text("مستحقات مالية");

                                 row.RelativeItem()
                                    .AlignRight()
                                    .Text(": وذلك بمقابل")
                                    .Bold();
                             });

                            s.Item().ExtendHorizontal().Row(row =>
                           {
                               row.RelativeItem();
                               row.RelativeItem();
                               row.RelativeItem();
                               row.RelativeItem();

                               row.RelativeItem()
                                   .AlignRight()
                                  .Text(r.PaymentMethod);

                               row.RelativeItem()
                                  .AlignRight()
                                  .Text(": طريقة الدفع")
                                  .Bold();
                           });
                            s.Item().ExtendHorizontal()
                            .Row(row =>
                            {
                                row.AutoItem()
                            .PaddingLeft(20)
                            .Column(col =>
                            {
                                col.Spacing(6);
                                col.Item()
                                .AlignCenter()
                                .Text(": المستلم")
                                .Bold();
                                col.Item()
                                .AlignCenter()
                                .Text("فلاح العوفي")
                                .Bold();
                                col.Item()
                                .PaddingTop(10)
                                .Height(24);
                            });
                            });

                            s.Item()
                                .AlignRight()
                                .PaddingTop(10)
                                .Text($"السند رقم {r.ReceiptsCount}")
                                .Bold();
                        });
                    });
                });
            });
        })
        .GeneratePdf(filePath);
    }

    public void GenerateExpensesPdf(ExpensesRealEstate e, string filePath)
    {
        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x =>
                    x.FontFamily("Arial")
                        .FontSize(12)
                );

                page.Content()
                    .AlignCenter()
                    .AlignMiddle()
                    .Width(500)
                    .Column(col =>
                    {
                    col.Spacing(10);

                    col.Item().Element(section =>
                    {

                        section.Border(1).Padding(10).Column(s =>
                        {
                            s.Spacing(6);

                            var date = e.ExpensesDate;
                            var formatted = date.ToString("yyyy/MM/dd");

                            s.Item().ExtendHorizontal().Row(row =>
                            {

                                row.AutoItem().Text($"تاريخ السند: {date:yyyy/MM/dd}").AlignLeft().Bold();

                                row.RelativeItem();

                                row.AutoItem().Text($"{e.ExpensesNumber} : رقم السند").AlignRight().Bold();
                            });


                            s.Item().Text("سند صرف").FontSize(18).Bold().AlignCenter();

                            s.Item().ExtendHorizontal();

                            s.Item().ExtendHorizontal().Row(row =>
                             {
                                 row.RelativeItem();

                                 row.RelativeItem()
                                     .Border(1)
                                     .PaddingVertical(4)
                                     .PaddingHorizontal(8)
                                     .AlignCenter()
                                     .Row(amountRow =>
                                     {
                                         amountRow.AutoItem()
                                             .Text("ريال")
                                             .Bold();

                                         amountRow.ConstantItem(8);

                                         amountRow.AutoItem()
                                             .Text(e.ExpensesAmount.ToString("N0"))
                                             .Bold();
                                     });

                                 row.RelativeItem()
                                    .AlignRight()
                                    .Text(": تم صرف مبلغ وقدره")
                                    .Bold();
                             });
                            s.Item().ExtendHorizontal().Row(row =>
                             {
                                 row.RelativeItem();
                                 row.RelativeItem();
                                 row.RelativeItem();
                                 row.RelativeItem();

                                 row.RelativeItem()
                                     .AlignRight()
                                    .Text(e.ExpensesService);

                                 row.RelativeItem()
                                    .AlignRight()
                                    .Text(": وذلك بمقابل خدمة")
                                    .Bold();
                             });

                            s.Item().ExtendHorizontal()
                            .Row(row =>
                            {
                                row.AutoItem()
                            .PaddingLeft(20)
                            .Column(col =>
                            {
                                col.Spacing(6);
                                col.Item()
                                .AlignCenter()
                                .Text(": المستلم")
                                .Bold();
                                col.Item()
                                .AlignCenter()
                                .Text("فلاح العوفي")
                                .Bold();
                                col.Item()
                                .PaddingTop(10)
                                .Height(24);
                            });
                            });
                        });
                    });
                });
            });
        })
        .GeneratePdf(filePath);
    }

    public void GenerateTenantReceiptsRecordPdf(
        TenantRealEstate tenant,
        List<TenantRealEstate> receipts,
        string path)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var orderedReceipts = receipts
            .OrderBy(x => x.ReceiptDate)
            .ThenBy(x => x.ReceiptId)
            .ToList();

        var totalPaid = orderedReceipts.Sum(x => x.ReceiptAmount);

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);

                page.DefaultTextStyle(x =>
                    x.FontFamily("Arial")
                     .FontSize(11));

                page.Header().Column(col =>
                {
                    col.Item()
                        .AlignRight()
                        .Text("السجل المالي للمستأجر")
                        .FontSize(22)
                        .Bold();

                    col.Item()
                        .PaddingTop(10)
                        .Border(1)
                        .Padding(10)
                        .Column(info =>
                        {
                            info.Item().AlignRight().Text($"اسم المستأجر : {tenant.Name}");
                            info.Item().AlignRight().Text($"رقم الهوية : {tenant.IdentityNumber}");
                            info.Item().AlignRight().Text($"الجوال : {tenant.Phone}");
                            info.Item().AlignRight().Text($"العنوان : {tenant.Address}");
                        });
                });

                page.Content().PaddingTop(15).Column(col =>
                {
                    col.Item()
                        .AlignRight()
                        .Text($"إجمالي المدفوع : {totalPaid:N2}")
                        .FontSize(14)
                        .Bold();

                    col.Item().PaddingTop(10).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn();      // المبلغ
                            columns.RelativeColumn();      // رقم العقد
                            columns.RelativeColumn();      // التاريخ
                            columns.RelativeColumn();      // رقم السند
                            columns.ConstantColumn(45);    // الترتيب
                        });

                        table.Header(header =>
                        {
                            header.Cell().Border(1).Padding(5).AlignCenter().Text("المبلغ").Bold();
                            header.Cell().Border(1).Padding(5).AlignCenter().Text("رقم العقد").Bold();
                            header.Cell().Border(1).Padding(5).AlignCenter().Text("التاريخ").Bold();
                            header.Cell().Border(1).Padding(5).AlignCenter().Text("رقم السند").Bold();
                            header.Cell().Border(1).Padding(5).AlignCenter().Text("#").Bold();
                        });

                        var order = 1;

                        foreach (var r in orderedReceipts)
                        {
                            table.Cell().Border(1).Padding(5).AlignCenter()
                                .Text(r.ReceiptAmount.ToString("N2"));

                            table.Cell().Border(1).Padding(5).AlignCenter()
                                .Text(r.ContractNumber);

                            table.Cell().Border(1).Padding(5).AlignCenter()
                                .Text(r.ReceiptDate.ToString("yyyy-MM-dd"));

                            table.Cell().Border(1).Padding(5).AlignCenter()
                                .Text(r.ReceiptNumber);

                            table.Cell().Border(1).Padding(5).AlignCenter()
                                .Text(order++.ToString());
                        }
                    });
                });

                page.Footer()
                    .AlignCenter()
                    .Text(x =>
                    {
                        x.Span("صفحة ");
                        x.CurrentPageNumber();
                    });
            });
        })
        .GeneratePdf(path);
    }

    // =====================================================================
    // Dashboard analysis report. A normal method of this class now,
    // not a nested class. Call it as:
    //   new PdfServiceRealEstate().ExportDashboard(path, year, ...);
    // =====================================================================
    private static string N(decimal v) => v.ToString("N2", CultureInfo.InvariantCulture);

    public void ExportDashboard(
        string path,
        int year,
        decimal totalReceipts,
        decimal totalExpenses,
        int rented,
        int vacant,
        List<UnitYearStatRowRealEstate> buildings,
        List<(string month, decimal receipts, decimal expenses)> monthly,
        string ownersCount,
        string unitsCount,
        string tenantsCount,
        string contractsCount,
        string receiptsCount)
    {
        var net = totalReceipts - totalExpenses;
        var totalUnits = rented + vacant;
        var occupancy = totalUnits == 0 ? 0 : (rented * 100.0 / totalUnits);

        Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(28);
                page.DefaultTextStyle(t => t.FontFamily("Arial").FontSize(10));
                page.ContentFromRightToLeft();

                page.Header().Column(col =>
                {
                    col.Item().Text(year == 0
                            ? "تقرير التحليل العقاري — كل السنوات"
                            : $"تقرير التحليل العقاري — سنة {year}")
                        .FontSize(18).Bold();
                    col.Item().Text($"تاريخ الإصدار: {DateTime.Now:yyyy-MM-dd HH:mm}")
                        .FontSize(9).FontColor(Colors.Grey.Medium);
                    col.Item().PaddingTop(6).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                });

                page.Content().PaddingVertical(8).Column(col =>
                {
                    col.Spacing(14);

                    // ---- Records ----
                    col.Item().Text("السجلات").FontSize(13).Bold();
                    col.Item().Row(row =>
                    {
                        void Count(string label, string value)
                        {
                            row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2)
                               .Padding(6).Column(c =>
                               {
                                   c.Item().Text(label).FontSize(8).FontColor(Colors.Grey.Darken1);
                                   c.Item().Text(value).FontSize(12).Bold();
                               });
                        }

                        Count("الملاك", ownersCount);
                        Count("الوحدات", unitsCount);
                        Count("المستأجرين", tenantsCount);
                        Count("العقود", contractsCount);
                        Count("سندات القبض", receiptsCount);
                    });

                    // ---- KPI summary ----
                    col.Item().PaddingTop(4).Text("الملخص المالي").FontSize(13).Bold();
                    col.Item().Row(row =>
                    {
                        void Kpi(string label, string value)
                        {
                            row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2)
                               .Padding(8).Column(c =>
                               {
                                   c.Item().Text(label).FontSize(9).FontColor(Colors.Grey.Darken1);
                                   c.Item().Text(value).FontSize(13).Bold();
                               });
                        }

                        Kpi("إجمالي القبض", N(totalReceipts));
                        Kpi("إجمالي الصرف", N(totalExpenses));
                        Kpi("الصافي", N(net));
                        Kpi("نسبة الإشغال", $"{occupancy:0.#}%");
                    });

                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text($"عدد الوحدات: {totalUnits}   |   مؤجرة: {rented}   |   شاغرة: {vacant}")
                           .FontSize(10);
                    });

                    // ---- Monthly income vs expenses ----
                    if (monthly.Count > 0)
                    {
                        col.Item().PaddingTop(6).Text("القبض والصرف (شهريًا)").FontSize(13).Bold();
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(2); c.RelativeColumn(2);
                                c.RelativeColumn(2); c.RelativeColumn(2);
                            });

                            void H(string t) => table.Cell().Background(Colors.Grey.Lighten3)
                                .Padding(4).Text(t).Bold().FontSize(9);

                            H("الشهر"); H("القبض"); H("الصرف"); H("الصافي");

                            foreach (var m in monthly)
                            {
                                var mNet = m.receipts - m.expenses;
                                table.Cell().Padding(4).Text(m.month).FontSize(9);
                                table.Cell().Padding(4).Text(N(m.receipts)).FontSize(9);
                                table.Cell().Padding(4).Text(N(m.expenses)).FontSize(9);
                                table.Cell().Padding(4).Text(N(mNet)).FontSize(9)
                                     .FontColor(mNet < 0 ? Colors.Red.Medium : Colors.Black);
                            }
                        });
                    }

                    // ---- Buildings ranking ----
                    col.Item().PaddingTop(6).Text("ترتيب العمارات حسب الدخل").FontSize(13).Bold();
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(3);
                            c.RelativeColumn(2);
                            c.RelativeColumn(2);
                            c.RelativeColumn(2);
                            c.RelativeColumn(2);
                            c.RelativeColumn(2);
                        });

                        void H(string t) => table.Cell().Background(Colors.Grey.Lighten3)
                            .Padding(4).Text(t).Bold().FontSize(9);

                        H("العمارة"); H("الإشغال"); H("عقود السنة");
                        H("القبض"); H("الصرف"); H("الصافي");

                        foreach (var b in buildings)
                        {
                            table.Cell().Padding(4).Text(b.UnitName).FontSize(9);
                            table.Cell().Padding(4).Text(b.Occupancy).FontSize(9);
                            table.Cell().Padding(4).Text(b.ContractsStartedThisYear.ToString()).FontSize(9);
                            table.Cell().Padding(4).Text(N(b.ReceiptsTotalThisYear)).FontSize(9);
                            table.Cell().Padding(4).Text(N(b.ExpensesTotalThisYear)).FontSize(9);
                            table.Cell().Padding(4).Text(N(b.NetThisYear)).FontSize(9)
                                 .FontColor(b.NetThisYear < 0 ? Colors.Red.Medium : Colors.Black);
                        }
                    });
                });

                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("صفحة ");
                    t.CurrentPageNumber();
                    t.Span(" من ");
                    t.TotalPages();
                });
            });
        })
        .GeneratePdf(path);
    }

    // ---- dedicated report: late tenants only ----
    public void ExportLateTenants(string path, string yearLabel, List<LateTenantRowRealEstate> late)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(28);
                page.DefaultTextStyle(t => t.FontFamily("Arial").FontSize(10));
                page.ContentFromRightToLeft();

                page.Header().Column(col =>
                {
                    col.Item().Text($"تقرير المستأجرين المتأخرين للعقار — {yearLabel}").FontSize(18).Bold();
                    col.Item().Text($"تاريخ الإصدار: {DateTime.Now:yyyy-MM-dd HH:mm}")
                        .FontSize(9).FontColor(Colors.Grey.Medium);
                    col.Item().PaddingTop(6).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                });

                page.Content().PaddingVertical(8).Column(col =>
                {
                    var totalOutstanding = late.Sum(x => x.Outstanding);
                    col.Item().PaddingBottom(8)
                        .Text($"عدد المتأخرين: {late.Count}    |    إجمالي المتأخرات: {N(totalOutstanding)}")
                        .FontSize(11).Bold();

                    if (late.Count == 0)
                        col.Item().Text("لا يوجد متأخرات.").FontSize(11).FontColor(Colors.Grey.Medium);
                    else
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(3); c.RelativeColumn(2); c.RelativeColumn(3);
                                c.RelativeColumn(2); c.RelativeColumn(2); c.RelativeColumn(2);
                                c.RelativeColumn(2); c.RelativeColumn(2); c.RelativeColumn(2);
                            });

                            void H(string t) => table.Cell().Background(Colors.Grey.Lighten3)
                                .Padding(4).Text(t).Bold().FontSize(9);

                            H("المستأجر"); H("الجوال"); H("الوحدة"); H("طريقة الدفع");
                            H("المستحق"); H("المدفوع"); H("المتبقي"); H("التأخر"); H("آخر دفعة");

                            foreach (var l in late)
                            {
                                table.Cell().Padding(4).Text(l.TenantName).FontSize(9);
                                table.Cell().Padding(4).Text(l.TenantPhone).FontSize(9);
                                table.Cell().Padding(4).Text(l.UnitName).FontSize(9);
                                table.Cell().Padding(4).Text(l.PayMethod).FontSize(9);
                                table.Cell().Padding(4).Text(N(l.ExpectedToDate)).FontSize(9);
                                table.Cell().Padding(4).Text(N(l.PaidToDate)).FontSize(9);
                                table.Cell().Padding(4).Text(N(l.Outstanding)).FontSize(9)
                                     .FontColor(Colors.Red.Medium).Bold();
                                table.Cell().Padding(4).Text(l.BehindText).FontSize(9);
                                table.Cell().Padding(4).Text(l.LastPaymentText).FontSize(9);
                            }
                        });
                });

                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("صفحة ");
                    t.CurrentPageNumber();
                    t.Span(" من ");
                    t.TotalPages();
                });
            });
        })
        .GeneratePdf(path);
    }

    // ---- dedicated report: upcoming payments only ----
    public void ExportUpcoming(string path, string yearLabel, List<UpcomingPaymentRowRealEstate> upcoming)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(28);
                page.DefaultTextStyle(t => t.FontFamily("Arial").FontSize(10));
                page.ContentFromRightToLeft();

                page.Header().Column(col =>
                {
                    col.Item().Text($"تقرير الدفعات المستحقة للعقار هذا الشهر — {yearLabel}").FontSize(18).Bold();
                    col.Item().Text($"تاريخ الإصدار: {DateTime.Now:yyyy-MM-dd HH:mm}")
                        .FontSize(9).FontColor(Colors.Grey.Medium);
                    col.Item().PaddingTop(6).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                });

                page.Content().PaddingVertical(8).Column(col =>
                {
                    var totalDue = upcoming.Sum(x => x.Amount);
                    col.Item().PaddingBottom(8)
                        .Text($"عدد الدفعات: {upcoming.Count}    |    إجمالي المبالغ: {N(totalDue)}")
                        .FontSize(11).Bold();

                    if (upcoming.Count == 0)
                        col.Item().Text("لا توجد دفعات مستحقة هذا الشهر.").FontSize(11).FontColor(Colors.Grey.Medium);
                    else
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(3); c.RelativeColumn(3);
                                c.RelativeColumn(3); c.RelativeColumn(2); c.RelativeColumn(2);
                            });

                            void H(string t) => table.Cell().Background(Colors.Grey.Lighten3)
                                .Padding(4).Text(t).Bold().FontSize(9);

                            H("المستأجر"); H("الوحدة"); H("العمارة"); H("تاريخ الاستحقاق"); H("المبلغ");

                            foreach (var u in upcoming)
                            {
                                table.Cell().Padding(4).Text(u.TenantName).FontSize(9);
                                table.Cell().Padding(4).Text(u.UnitName).FontSize(9);
                                table.Cell().Padding(4).Text(u.BuildingName).FontSize(9);
                                table.Cell().Padding(4).Text(u.DueDate.ToString("yyyy-MM-dd")).FontSize(9);
                                table.Cell().Padding(4).Text(N(u.Amount)).FontSize(9);
                            }
                        });
                });

                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("صفحة ");
                    t.CurrentPageNumber();
                    t.Span(" من ");
                    t.TotalPages();
                });
            });
        })
        .GeneratePdf(path);
    }
}