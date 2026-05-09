using System;
using System.Globalization;
using System.IO;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using RealEstateInstallmentsManager.Models;

namespace RealEstateInstallmentsManager.Services;

public class PdfServiceInstallment
{
    public PdfServiceInstallment()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public void GenerateContractInstallmentPdf(ContractInstallment c, string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);
        

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.PageColor(Colors.White);

                page.DefaultTextStyle(x => x
                    .FontSize(13)
                    .FontFamily("Arial")); 

                page.ContentFromRightToLeft();

                page.Content().Column(col =>
                {
                    // ===== Header =====
                    col.Item().AlignCenter()
                        .Text("FALAH")
                        .FontSize(40)
                        .FontColor("#8FA8D8")
                        .Bold();

                    col.Item().PaddingTop(10).LineHorizontal(1);

                    if (c.ProductType == "سيارات")
                {
                col.Item().PaddingTop(18)
                        .AlignCenter()
                        .Text("عقد بيع سيارات بالاجل")
                        .FontSize(18)
                        .Bold();
                }
                    else
                    {
                        col.Item().PaddingTop(18)
                            .AlignCenter()
                            .Text("عقد بيع أجهزة بالاجل")
                            .FontSize(18)
                            .Bold();
                    }

                        // التاريخ والموافق
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().AlignRight().Column(right =>
                            {
                                right.Item().Text($"التاريخ : {c.ContractStartDate:yyyy/MM/dd}");
                                right.Item().Text($"الموافق : {ToSaudiHijri(c.ContractStartDate)}");
                            });

                            row.ConstantItem(120).AlignLeft().Text($"No {c.ContractNumber}");
                        });

                        col.Item().PaddingTop(15);

                        // نص العقد الرئيسي
                        col.Item().Text(text =>
                        {
                            text.Span("أقر أنا الموقع أدناه ");
                            text.Span($"{c.CustomerName} ").Bold();
                            text.Span("الحامل لحفيظة النفوس رقم ");
                            text.Span($"{c.CustomerIdentityNumber} ").Bold();
                            if (c.ProductType == "سيارات")
                            {
                                text.Span("بأني اشتريت السيارة التالية بياناتها:");
                            }
                            else
                            {
                                text.Span("بأني اشتريت الجهاز التالية بياناته:");
                            }
                        });

                        if (c.ProductType == "سيارات")
                        {
                            col.Item().PaddingTop(10).Border(1).Padding(10).Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn(); 
                                });

                                // Header row
                                table.Cell().Border(1).Padding(5).Text("اسم السيارة").Bold(); 
                                table.Cell().Border(1).Padding(5).Text("رقم اللوحة").Bold();
                                table.Cell().Border(1).Padding(5).Text("رقم الهيكل").Bold();
                                table.Cell().Border(1).Padding(5).Text("الموديل").Bold();
                                table.Cell().Border(1).Padding(5).Text("اللون").Bold();

                                // Data row
                                table.Cell().Border(1).Padding(5).Text(c.ProductName ?? ""); 
                                table.Cell().Border(1).Padding(5).Text(c.ProductCarPlateNumber ?? ""); 
                                table.Cell().Border(1).Padding(5).Text(c.ProductCarVIN ?? "");
                                table.Cell().Border(1).Padding(5).Text(c.ProductCarModel ?? "");
                                table.Cell().Border(1).Padding(5).Text(c.ProductCarColor ?? "");
                            });
                        }
                        else
                        {
                            col.Item().PaddingTop(10).Border(1).Padding(10).Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                });

                                // Header row
                                table.Cell().Border(1).Padding(5).Text("اسم الجهاز").Bold(); 
                                table.Cell().Border(1).Padding(5).Text("الحجم").Bold();
                                table.Cell().Border(1).Padding(5).Text("اللون").Bold();


                                // Data row
                                table.Cell().Border(1).Padding(5).Text(c.ProductName ?? ""); 
                                table.Cell().Border(1).Padding(5).Text(c.ProductMobileStorage ?? "");    
                                table.Cell().Border(1).Padding(5).Text(c.ProductMobileColor ?? "");      
                            });
                        }
                       
                        
                        col.Item().PaddingTop(10).Text(text =>
                        {
                            text.Span("ملك ");
                            text.Span("لمؤسسة فلاح حماد العوفي التجارية").Bold();
                            text.Span("رقم السجل التجاري ");
                            text.Span("7051422512 ").Bold();
                            text.Span("العنوان ");
                            text.Span("محافظة بدر").Bold();
                        });

                        col.Item().Text(text =>
                        {
                            text.Span("وذلك بمبلغ وقدره ");
                            text.Span($"{c.MainTotalAmount:0.##} ريال").Bold();
                            if (c.BoolDownPayment)
                            {
                                text.Span("، دفعت مبلغ وقدره ");
                                text.Span($"{c.DownPayment:0.##} ريال").Bold();
                                text.Span("، والباقي مبلغ وقدره ");
                                text.Span($"{c.CurrentTotalAmount:0.##} ريال").Bold();
                                text.Span(".");
                            }
                        });

                        col.Item().Text(text =>
                        {
                            text.Span("مقسطة على ");
                            text.Span($"{c.ContractPeriod:0} ").Bold();
                            text.Span("قسط، قيمة كل قسط ");
                            text.Span($"{c.MonthlyInstallment:0.##} ريال").Bold();
                            text.Span(" اعتباراً من تاريخ ");
                            text.Span($"{c.ContractStartDate:yyyy/MM/dd}").Bold();
                            text.Span(" حتى نهاية ");
                            text.Span($"{c.ContractEndDate:yyyy/MM/dd}").Bold();
                            text.Span(".");
                        });
                        
                        if (c.ProductType == "سيارات")
                        {
                            col.Item().Text(
                                "وأني قد قبلت السيارة بحالتها الحاضرة بعد الكشف الفني الدقيق عليها، " +
                                "ولا يحق لي الرجوع على المؤسسة أو البائع بأي حق كان ومن أي ناحية كانت إطلاقاً، " +
                                "وأتعهد بسداد الأقساط حال حلول تواريخها ومواعيدها بدون أي تأخير ولا مماطلة."
                            );
                        }
                        else
                        { col.Item().Text(
                                "وأني قد قبلت الجهاز بحالته الحاضرة بعد الكشف الفني الدقيق عليه، " +
                                "ولا يحق لي الرجوع على المؤسسة أو البائع بأي حق كان ومن أي ناحية كانت إطلاقاً، " +
                                "وأتعهد بسداد الأقساط حال حلول تواريخها ومواعيدها بدون أي تأخير ولا مماطلة.");
                        }

                        col.Item().PaddingTop(10);

                        if (!string.IsNullOrWhiteSpace(c.CustomerSponserName))
                        {
                            col.Item().Text(text =>
                            {
                                text.Span("كما أقر أنا الكفيل ");
                                text.Span($"{c.CustomerSponserName} ").Bold();
                                text.Span("الحامل لحفيظة النفوس رقم ");
                                text.Span($"{c.CustomerSponserIdentityNumber} ").Bold();
                                text.Span("بكفالتي الغرمية على ما ذكر بعاليه بعد قراءته حرفياً، وأتعهد بموجب توقيعي أدناه " +
                                          "بسداد أي قسط يتأخر مكفولي المذكور عن السداد بمجرد أول طلب من البائع أو المؤسسة، " +
                                          "وهذا إقرار مني عن كفالة غرم وأداء وتسليم لا أخلي محل المكفول في كل ما نجم من إجراءات الكفالة الغرمية، " +
                                          "كما نقرر جميعاً أن كافة إجراءات نقل الملكية هي على المشتري، وهذا إقرار صحيح شرعاً بصحة البيع والله خير الشاهدين.");
                            });
                        }

                        col.Item().PaddingTop(15);

                        col.Item().Text("ملاحظات :").Bold().Underline();
                        if (c.ProductType == "سيارات")
                        {
                            col.Item().Text(
                                "كما أقر أنا المشتري بأنني استلمت السيارة والأوراق الخاصة بها وأنها سليمة وخالية من الملاحظات.");
                        }
                        else
                        {
                            col.Item().Text(
                                "كما أقر أنا المشتري بأنني استلمت الجهاز والأوراق الخاصة بها وأنها سليمة وخالية من الملاحظات.");
                        }

                        col.Item().PaddingTop(25);

                        if (!string.IsNullOrWhiteSpace(c.CustomerSponserName))
                        {
                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                });

                                table.Cell().Padding(5).AlignCenter().Text("المشتري").Bold().Underline();
                                table.Cell().Padding(5).AlignCenter().Text("الكفيل").Bold().Underline();
                                table.Cell().Padding(5).AlignCenter().Text("البائع").Bold().Underline();

                                table.Cell().PaddingTop(10).Text($"الاسم : {c.CustomerName}");
                                table.Cell().PaddingTop(10).Text($"الاسم : {c.CustomerSponserName}");
                                table.Cell().PaddingTop(10).Text("الاسم : مؤسسة فلاح حماد العوفي التجارية");

                                table.Cell().PaddingTop(10).Text("التوقيع : ...............................");
                                table.Cell().PaddingTop(10).Text("التوقيع : ...............................");
                                table.Cell().PaddingTop(10).Text("التوقيع : ...............................");

                                table.Cell().PaddingTop(10).Text("التاريخ : ................................");
                                table.Cell().PaddingTop(10).Text("التاريخ : ................................");
                                table.Cell().PaddingTop(10).Text("التاريخ : ................................");
                            });
                        }
                        else
                        {
                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                });

                                table.Cell().Padding(5).AlignCenter().Text("المشتري").Bold().Underline();
                                table.Cell().Padding(5).AlignCenter().Text("البائع").Bold().Underline();

                                table.Cell().PaddingTop(10).Text($"الاسم : {c.CustomerName}");
                                table.Cell().PaddingTop(10).Text("الاسم : مؤسسة فلاح حماد العوفي التجارية");

                                table.Cell().PaddingTop(10).Text("التوقيع : ...............................");
                                table.Cell().PaddingTop(10).Text("التوقيع : ...............................");

                                table.Cell().PaddingTop(10).Text("التاريخ : ................................");
                                table.Cell().PaddingTop(10).Text("التاريخ : ................................");
                            });
                        }
                        

                        col.Item().PaddingTop(20);

                        // أرقام التواصل
                        col.Item().AlignRight().Column(x =>
                        {
                            x.Item().Text($"رقم المشتري : {c.CustomerPhone}");
                            if (!string.IsNullOrWhiteSpace(c.CustomerSponserName))
                            {
                                x.Item().Text($"رقم الكفيل : {c.CustomerSponserPhone}");
                            }
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
    public static string ToSaudiHijri(DateTime date)
    {
        var cal = new UmAlQuraCalendar();

        int day = cal.GetDayOfMonth(date);
        int month = cal.GetMonth(date);
        int year = cal.GetYear(date);

        return $"{day:00}/{month:00}/{year}";
    }
  public void GenerateSanadLeAmrPdf(ContractInstallment c, string filePath)
{
    var directory = Path.GetDirectoryName(filePath);
    if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
        Directory.CreateDirectory(directory);

    QuestPDF.Settings.License = LicenseType.Community;

    Document.Create(container =>
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());
            page.Margin(20);
            page.PageColor(Colors.White);
            page.ContentFromRightToLeft();

            page.DefaultTextStyle(x => x
                .FontFamily("Arial")
                .FontSize(14)
                .LineHeight(1.5f));

            // Center small note in page
            page.Content()
                .AlignCenter()
                .AlignMiddle()
                .Width(620)
                .Height(400)
                .Border(1)
                .Padding(15)
                .Column(col =>
                {
                    col.Spacing(4);

                    // ===== Title =====
                    col.Item()
                        .AlignCenter()
                        .Text("سند لأمر")
                        .Bold()
                        .FontSize(16);

                    // ===== Header =====
                    col.Item().Row(row =>
                    {
                        row.RelativeItem()
                            .AlignRight()
                            .Text($"التاريخ : {DateTime.Now:dd/MM/yy}");

                        row.RelativeItem()
                            .AlignLeft()
                            .Text($"رقم السند : {c.ContractNumber}");
                    });
                    
                    // ===== Body =====
                    col.Item().PaddingTop(4).AlignRight()
                        .Text("حرر في مدينة :                                                 بدر");

                    col.Item().AlignRight()
                        .Text("أتعهد أنا الموقع أدناه أن أدفع بموجب هذا السند بدون قيد أو شرط لأمر:");

                    col.Item().PaddingRight(15).AlignRight()
                        .Text("اسم المستفيد : فلاح حماد حامد العوفي");
                    
                    col.Item().AlignRight().PaddingRight(18)
                        .Text($"مبلغ وقدره : {c.MainTotalAmount:0.##}");

                    col.Item().AlignRight().PaddingRight(18)
                        .Text($"تاريخ الاستحقاق : {DateTime.Now.AddMonths(5):dd/MM/yy}");
                    
                    col.Item().PaddingTop(15).Row(row =>
                    {
                        row.RelativeItem().AlignRight()
                            .Text($"اسم المدين : {c.CustomerName}");

                        row.RelativeItem()
                            .AlignCenter()
                            .Text("توقيع المدين : ");
                    });
                    if (!string.IsNullOrWhiteSpace(c.CustomerSponserName))
                    {
                        col.Item().PaddingTop(15).Row(row =>
                        {
                            row.RelativeItem().AlignRight()
                                .Text($"اسم الكفيل : {c.CustomerSponserName}");

                            row.RelativeItem()
                                .AlignCenter()
                                .Text("توقيع الكفيل : ");
                        });
                    }
                    
                    // ===== Footer =====
                    col.Item().PaddingTop(15)
                        .AlignRight()
                        .Text(
                            "هذا السند محرر وفق نظام الأوراق التجارية ويعد سنداً تنفيذياً واجب النفاذ وفق الأنظمة المعمول بها في المملكة العربية السعودية.");
                });
        });
    })
    .GeneratePdf(filePath);
}
  public void GenerateReceiptPdf(ReceiptInstallment r, string filePath)
{
    Document.Create(container =>
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());
            page.Margin(30);
            page.DefaultTextStyle(x =>
                x.FontFamily("Arial")
                    .FontSize(12)
            );

            page.Content()
                .AlignCenter()
                .AlignMiddle()
                .MaxWidth(650)
                .Column(col =>
                {
                col.Spacing(10);

                col.Item().Element(section =>
                {
                    section.Border(1).Padding(10).Column(s =>
                    {
                        s.Spacing(6);

                        var date = r.ReceiptDate;

                        s.Item().Row(row =>
                        {
                            row.AutoItem().Text($"تاريخ السند: {date:yyyy/MM/dd}").AlignLeft().Bold();
                            row.RelativeItem();
                            row.AutoItem().Text($"{r.ReceiptNumber} : رقم السند").AlignRight().Bold();
                        });

                        s.Item().Text("سند قبض").FontSize(18).Bold().AlignCenter();
                        
                        s.Item().ContentFromRightToLeft().Row(row =>
                        {
                            row.RelativeItem()
                                .AlignRight()
                                .Text("استلمنا من السيد / ")
                                .Bold();

                            row.RelativeItem()
                                .AlignRight()
                                .Text(r.CustomerName);

                            row.RelativeItem(4);
                        });

                        s.Item().Row(row =>
                        {
                            row.RelativeItem();

                            row.RelativeItem()
                                .Border(1)
                                .PaddingVertical(4)
                                .PaddingHorizontal(8)
                                .AlignCenter()
                                .Row(x =>
                                {
                                    x.AutoItem()
                                        .Text("﷼")
                                        .Bold();

                                    x.AutoItem()
                                        .PaddingLeft(4)
                                        .Text(r.Amount.ToString("N2"))
                                        .Bold();
                                });

                            row.RelativeItem()
                                .AlignRight()
                                .Text(": مبلغ وقدره")
                                .Bold();
                        });

                        s.Item().Row(row =>
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

                        s.Item().Row(row =>
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
                        
                        s.Item().Row(row =>
                        {
                            row.RelativeItem();
                            row.RelativeItem();
                            row.RelativeItem();
                            row.RelativeItem();

                            row.RelativeItem()
                                .AlignRight()
                                .Row(x =>
                                {
                                    x.AutoItem().Text("﷼").Bold();
                                    x.AutoItem().PaddingLeft(3)
                                        .Text(r.CurrentTotalAmount.ToString("N2"));
                                });

                            row.RelativeItem()
                                .AlignRight()
                                .Text(": المتبقي")
                                .Bold();
                        });

                        s.Item().Row(row =>
                        {
                            row.AutoItem()
                                .PaddingLeft(20)
                                .Column(c =>
                                {
                                    c.Spacing(6);
                                    c.Item().AlignCenter().Text(": المستلم").Bold();
                                    c.Item().AlignCenter().Text("فلاح العوفي").Bold();
                                    c.Item().PaddingTop(10).Height(24);
                                });
                        });
                    });
                });
            });
        });
    })
    .GeneratePdf(filePath);
}
   public void GenerateExpensesPdf(ExpensesInstallment e, string filePath)
    {
        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
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

}