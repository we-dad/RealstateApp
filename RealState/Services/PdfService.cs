using System;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using RealEstateApp.Models;

namespace RealEstateApp.Services;

public class PdfService
{
    public PdfService()
    {
        // مهم لـ QuestPDF (Community)
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public void GenerateContractPdf(Contract c, string filePath)
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
                        Clause("بند 3:", $"قيمة الإيجار الشهري: {c.RentAmount} ⃁ ، طريقة الدفع: {c.ContractPayMethod}.");
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

    public void GenerateReceiptPdf(Receipt r, string filePath)
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

                page.Content().PaddingTop(250).Column(col =>
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
                            s.Item().ExtendHorizontal().Row(row =>
                            {
                                row.RelativeItem();

                                row.RelativeItem()
                                   .AlignCenter()
                                   .Text(r.TenantName);

                                row.RelativeItem()
                                   .AlignRight()
                                   .Text("/ استلمنا من السيد")
                                   .Bold();
                            });

                            s.Item().ExtendHorizontal().Row(row =>
                             {
                                 row.RelativeItem();

                                 row.RelativeItem()
                                 .Border(1)
                                 .PaddingVertical(4)
                                 .PaddingHorizontal(8)
                                 .AlignCenter()
                                 .Text($"⃁ {r.Amount:N0}")
                                 .Bold();

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
                                    .Text("ايجار شقة");

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
                        });
                    });
                });
            });
        })
        .GeneratePdf(filePath);
    }

    public void GenerateExpensesPdf(Expenses e, string filePath)
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

                page.Content().PaddingTop(250).Column(col =>
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
                                 .Text($"⃁ {e.ExpensesAmount:N0}")
                                 .Bold();

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

}
