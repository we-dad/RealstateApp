using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
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
   
   public void GenerateCustomerFinancialRecordPdf(
    CustomerInstallment customer,
    List<CustomerInstallment> rows,
    string path)
{
    QuestPDF.Settings.License = LicenseType.Community;

    var contracts = rows
        .Where(x => x.ContractId > 0)
        .GroupBy(x => x.ContractId)
        .Select(x => x.First())
        .ToList();

    var receipts = rows
        .Where(x => x.ReceiptId > 0)
        .OrderBy(x => x.ReceiptDate)
        .ThenBy(x => x.ReceiptId)
        .ToList();

    var totalAmount = contracts.Sum(x => x.ContractAmount);
    var paidAmount = receipts.Sum(x => x.ReceiptAmount);
    var leftAmount = totalAmount - paidAmount;

    Document.Create(container =>
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(30);

            page.DefaultTextStyle(x =>
                x.FontFamily("Arial")
                 .FontSize(11));

            page.Header()
                .AlignRight()
                .Text("السجل المالي للعميل")
                .FontSize(22)
                .Bold();

            page.Content().PaddingTop(15).Column(col =>
            {
                col.Spacing(12);

                col.Item().Border(1).Padding(10).Column(info =>
                {
                    info.Item().AlignRight().Text($"اسم العميل : {customer.Name}");
                    info.Item().AlignRight().Text($"رقم الهوية : {customer.IdentityNumber}");
                    info.Item().AlignRight().Text($"الجوال : {customer.Phone}");
                    info.Item().AlignRight().Text($"العنوان : {customer.Address}");
                    info.Item().AlignRight().Text($"الوظيفة : {customer.Job}");
                });

                col.Item().Border(1).Padding(10).Column(summary =>
                {
                    summary.Item().AlignRight().Text($"إجمالي مبلغ العقد : {totalAmount:N2}").Bold();
                    summary.Item().AlignRight().Text($"إجمالي المدفوع : {paidAmount:N2}").Bold();
                    summary.Item().AlignRight().Text($"المتبقي : {leftAmount:N2}").Bold();

                    if (leftAmount <= 0)
                    {
                        summary.Item()
                            .AlignRight()
                            .Text("تم اكمال الدفع")
                            .FontSize(16)
                            .Bold();
                    }
                });

                col.Item().AlignRight().Text("بيانات العقد").FontSize(16).Bold();

                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                    });

                    table.Header(header =>
                    {
                        header.Cell().Border(1).Padding(5).AlignCenter().Text("الحالة").Bold();
                        header.Cell().Border(1).Padding(5).AlignCenter().Text("تاريخ الانتهاء").Bold();
                        header.Cell().Border(1).Padding(5).AlignCenter().Text("تاريخ البدء").Bold();
                        header.Cell().Border(1).Padding(5).AlignCenter().Text("رقم العقد").Bold();
                    });

                    foreach (var c in contracts)
                    {
                        table.Cell().Border(1).Padding(5).AlignCenter().Text(c.ContractState);
                        table.Cell().Border(1).Padding(5).AlignCenter().Text(c.ContractEndDate.ToString("yyyy-MM-dd"));
                        table.Cell().Border(1).Padding(5).AlignCenter().Text(c.ContractStartDate.ToString("yyyy-MM-dd"));
                        table.Cell().Border(1).Padding(5).AlignCenter().Text(c.ContractNumber);
                    }
                });

                col.Item().AlignRight().Text("سندات القبض").FontSize(16).Bold();

                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.ConstantColumn(40);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Border(1).Padding(5).AlignCenter().Text("المبلغ").Bold();
                        header.Cell().Border(1).Padding(5).AlignCenter().Text("طريقة الدفع").Bold();
                        header.Cell().Border(1).Padding(5).AlignCenter().Text("التاريخ").Bold();
                        header.Cell().Border(1).Padding(5).AlignCenter().Text("رقم السند").Bold();
                        header.Cell().Border(1).Padding(5).AlignCenter().Text("#").Bold();
                    });

                    var i = 1;

                    foreach (var r in receipts)
                    {
                        table.Cell().Border(1).Padding(5).AlignCenter().Text(r.ReceiptAmount.ToString("N2"));
                        table.Cell().Border(1).Padding(5).AlignCenter().Text(r.PaymentMethod);
                        table.Cell().Border(1).Padding(5).AlignCenter().Text(r.ReceiptDate.ToString("yyyy-MM-dd"));
                        table.Cell().Border(1).Padding(5).AlignCenter().Text(r.ReceiptNumber);
                        table.Cell().Border(1).Padding(5).AlignCenter().Text(i++.ToString());
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
    }).GeneratePdf(path);
}
private static string N(decimal v) => v.ToString("N2", CultureInfo.InvariantCulture);
    private static string N(double v) => v.ToString("N2", CultureInfo.InvariantCulture);
 
    public void ExportDashboard(string path, string yearLabel, DashboardStatsInstallment s)
    {
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
                    col.Item().Text($"تقرير التقسيط — {yearLabel}").FontSize(18).Bold();
                    col.Item().Text($"تاريخ الإصدار: {DateTime.Now:yyyy-MM-dd HH:mm}")
                        .FontSize(9).FontColor(Colors.Grey.Medium);
                    col.Item().PaddingTop(6).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                });
 
                page.Content().PaddingVertical(8).Column(col =>
                {
                    col.Spacing(14);
 
                    // ---- Records counts ----
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
 
                        Count("الملاك", s.OwnersCount.ToString());
                        Count("العملاء", s.CustomersCount.ToString());
                        Count("المنتجات", s.ProductsCount.ToString());
                        Count("العقود", s.ContractsCount.ToString());
                        Count("سندات القبض", s.ReceiptsCount.ToString());
                        Count("سندات الصرف", s.ExpensesCount.ToString());
                    });
 
                    // ---- KPIs ----
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
 
                        Kpi("إجمالي المحصّل", N(s.TotalCollected));
                        Kpi("إجمالي المصروفات", N(s.TotalExpenses));
                        Kpi("صافي الدخل", N(s.NetIncome));
                        Kpi("المتبقي على العملاء", N(s.OutstandingBalance));
                    });
 
                    // ---- Portfolio status ----
                    col.Item().PaddingTop(4).Text("حالة المحفظة").FontSize(13).Bold();
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
 
                        Kpi("عقود جارية", s.ActiveContracts.ToString());
                        Kpi("عقود متعثرة", s.LateContracts.ToString());
                        Kpi("عقود منتهية", s.FinishedContracts.ToString());
                    });
 
                    // ---- Monthly income vs expenses ----
                    if (s.MonthlyFlows.Count > 0)
                    {
                        col.Item().PaddingTop(4).Text("الدخل مقابل المصروفات (شهريًا)").FontSize(13).Bold();
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(2); c.RelativeColumn(2);
                                c.RelativeColumn(2); c.RelativeColumn(2);
                            });
 
                            void H(string t) => table.Cell().Background(Colors.Grey.Lighten3)
                                .Padding(4).Text(t).Bold().FontSize(9);
 
                            H("الشهر"); H("الدخل"); H("المصروفات"); H("الصافي");
 
                            foreach (var f in s.MonthlyFlows)
                            {
                                var net = f.Collected - f.Spent;
                                table.Cell().Padding(4).Text(f.Month).FontSize(9);
                                table.Cell().Padding(4).Text(N(f.Collected)).FontSize(9);
                                table.Cell().Padding(4).Text(N(f.Spent)).FontSize(9);
                                table.Cell().Padding(4).Text(N(net)).FontSize(9)
                                     .FontColor(net < 0 ? Colors.Red.Medium : Colors.Black);
                            }
                        });
                    }
 
                    // ---- Top products ----
                    col.Item().PaddingTop(6).Text("الأكثر مبيعاً").FontSize(13).Bold();
                    if (s.TopProducts.Count == 0)
                        col.Item().Text("لا يوجد.").FontSize(10).FontColor(Colors.Grey.Medium);
                    else
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c => { c.RelativeColumn(4); c.RelativeColumn(1); });
                            table.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("المنتج").Bold().FontSize(9);
                            table.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("عدد العقود").Bold().FontSize(9);
                            foreach (var p in s.TopProducts)
                            {
                                table.Cell().Padding(4).Text(p.ProductName).FontSize(9);
                                table.Cell().Padding(4).Text(p.ContractCount.ToString()).FontSize(9);
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
 
    // ---- dedicated report: late payers only ----
    public void ExportLatePayers(string path, string yearLabel, DashboardStatsInstallment s)
    {
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
                    col.Item().Text($"تقرير المتأخرين عن السداد للأقساط — {yearLabel}").FontSize(18).Bold();
                    col.Item().Text($"تاريخ الإصدار: {DateTime.Now:yyyy-MM-dd HH:mm}")
                        .FontSize(9).FontColor(Colors.Grey.Medium);
                    col.Item().PaddingTop(6).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                });
 
                page.Content().PaddingVertical(8).Column(col =>
                {
                    var totalOutstanding = s.LatePayers.Sum(x => x.ShortfallAmount);
                    col.Item().PaddingBottom(8)
                        .Text($"عدد المتأخرين: {s.LatePayers.Count}    |    إجمالي المتأخرات: {N(totalOutstanding)}")
                        .FontSize(11).Bold();
 
                    if (s.LatePayers.Count == 0)
                        col.Item().Text("لا يوجد متأخرات.").FontSize(11).FontColor(Colors.Grey.Medium);
                    else
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(3); c.RelativeColumn(2);
                                c.RelativeColumn(2); c.RelativeColumn(2);
                                c.RelativeColumn(2); c.RelativeColumn(2);
                                c.RelativeColumn(2);
                            });
 
                            void H(string t) => table.Cell().Background(Colors.Grey.Lighten3)
                                .Padding(4).Text(t).Bold().FontSize(9);
 
                            H("العميل"); H("رقم العقد"); H("القسط الشهري");
                            H("المدفوع"); H("المتأخر"); H("الحالة"); H("آخر دفعة");
 
                            foreach (var l in s.LatePayers)
                            {
                                table.Cell().Padding(4).Text(l.CustomerName).FontSize(9);
                                table.Cell().Padding(4).Text(l.ContractNumber).FontSize(9);
                                table.Cell().Padding(4).Text(N(l.MonthlyInstallment)).FontSize(9);
                                table.Cell().Padding(4).Text(l.ProgressText).FontSize(9);
                                table.Cell().Padding(4).Text(N(l.ShortfallAmount)).FontSize(9)
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
 
    // ---- dedicated report: upcoming installments only ----
    public void ExportUpcoming(string path, string yearLabel, DashboardStatsInstallment s)
    {
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
                    col.Item().Text($"تقرير الأقساط المستحقة خلال 7 أيام — {yearLabel}").FontSize(18).Bold();
                    col.Item().Text($"تاريخ الإصدار: {DateTime.Now:yyyy-MM-dd HH:mm}")
                        .FontSize(9).FontColor(Colors.Grey.Medium);
                    col.Item().PaddingTop(6).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                });
 
                page.Content().PaddingVertical(8).Column(col =>
                {
                    var totalDue = s.Upcoming.Sum(x => x.Amount);
                    col.Item().PaddingBottom(8)
                        .Text($"عدد الأقساط: {s.Upcoming.Count}    |    إجمالي المبالغ: {N(totalDue)}")
                        .FontSize(11).Bold();
 
                    if (s.Upcoming.Count == 0)
                        col.Item().Text("لا توجد أقساط قريبة.").FontSize(11).FontColor(Colors.Grey.Medium);
                    else
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(3); c.RelativeColumn(2);
                                c.RelativeColumn(2); c.RelativeColumn(2); c.RelativeColumn(2);
                            });
 
                            void H(string t) => table.Cell().Background(Colors.Grey.Lighten3)
                                .Padding(4).Text(t).Bold().FontSize(9);
 
                            H("العميل"); H("رقم العقد"); H("التاريخ"); H("المبلغ"); H("متبقٍ (أيام)");
 
                            foreach (var u in s.Upcoming)
                            {
                                table.Cell().Padding(4).Text(u.CustomerName).FontSize(9);
                                table.Cell().Padding(4).Text(u.ContractNumber).FontSize(9);
                                table.Cell().Padding(4).Text(u.DueDate.ToString("yyyy-MM-dd")).FontSize(9);
                                table.Cell().Padding(4).Text(N(u.Amount)).FontSize(9);
                                table.Cell().Padding(4).Text(u.DaysUntilDue.ToString()).FontSize(9);
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