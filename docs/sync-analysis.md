# ميزة الاسترجاع من Supabase (فحص فقط، بدون تعديل)

## تصحيح لجوابي السابق
قلت سابقًا إن المزامنة رفع فقط ولا يوجد سحب. هذا كان **خطأ**. البحث عن `Pull` لم يجد شيئًا لأن الاسم الفعلي هو `Sync...FromCloudAsync` و`UpsertFromCloud`.

لا توجد ميزة باسم "استرجاع" أو "استعادة". لا يوجد زر بهذا الاسم في أي ملف axaml. نتائج البحث عن `Restore` و`Download` و`Import` و`تحميل` هي فقط تحديث البرنامج نفسه في `Main/Program.cs` (الأسطر 92 و137 و139) وتعليقات عادية.
الموجود فعليًا هو **سحب تلقائي من السحابة داخل كل شاشة قائمة**.

## 1. أين الميزة، ومن أي زر أو شاشة؟

لكل جدول دالة في شاشته اسمها `Sync<...>FromCloudAsync`. تستدعي `Cloud<...>Service.Get<...>Async()` ثم تنادي `UpsertFromCloud` لكل صف.

**العقارات (`RealEstate/Views`)**

| الشاشة | الدالة |
|---|---|
| `OwnersViewRealEstate.axaml.cs` | `SyncOwnersFromCloudAsync` |
| `TenantsViewRealEstate.axaml.cs` | `SyncTenantsFromCloudAsync` |
| `UnitsViewRealEstate.axaml.cs` | `SyncUnitsFromCloudAsync` |
| `ContractsViewRealEstate.axaml.cs` | `SyncContractsFromCloudAsync` |
| `ReceiptsViewRealEstate.axaml.cs` | `SyncReceiptsFromCloudAsync` |
| `ExpensesViewRealEstate.axaml.cs` | `SyncExpensesFromCloudAsync` |

**التقسيط (`Installment/Views`)**

| الشاشة | الدالة |
|---|---|
| `OwnersViewInstallment.axaml.cs` | `SyncOwnersFromCloudAsync` |
| `CustomerViewInstallment.axaml.cs` | `SyncCustomersFromCloudAsync` |
| `ProductViewInstallment.axaml.cs` | سحب المنتجات (تم التأكد من `GetProductsAsync` في السطر 130) |
| `ContractViewInstallment.axaml.cs` | `SyncContractsFromCloudAsync` |
| `ReceiptViewInstallment.axaml.cs` | `SyncReceiptsFromCloudAsync` |
| `ExpensesViewInstallment.axaml.cs` | سحب المصاريف (`GetExpensesAsync` في السطر 126) |

**متى تشتغل؟**
- **عند فتح الشاشة**: كل مُنشئ (constructor) يستدعيها بدون انتظار، مثل `_ = SyncOwnersFromCloudAsync();`.
- **عند زر "تحديث"** (`Content="تحديث" Click="Refresh_Click"`)، ويوجد في شاشات القوائم. الزر يستدعي `PushAllDirtyAsync()` ثم `Sync...FromCloudAsync()`.

لا يوجد زر مستقل، ولا شاشة خاصة بالاسترجاع. الشرط الوحيد أن `AppSession.CanReadOnline` تكون true، أي الدور admin أو editor أو viewer. الدور الافتراضي `tester` لا يسحب شيئًا.

## 2. كيف تتعامل مع البيانات المحلية الموجودة؟

**دمج (upsert) بالمفتاح `CloudId`.** لا مسح ولا تجاهل عام. المرجع: `OwnerServiceRealEstate.UpsertFromCloud`، والبقية بنفس النمط (تم التأكد أن كل خدمة لها `UpsertFromCloud`).

- إذا **لم يوجد** صف محلي بنفس `CloudId`: يُدخل صف جديد بـ `IsDirty = 0` و`SyncAction = ''`.
- إذا **وُجد**: يُحدَّث الصف بقيم السحابة، **بشرط `IsDirty = 0`**.
- **لا يوجد أي حذف** لصف محلي غير موجود في السحابة. أوامر `DELETE FROM` في الخدمات المحلية كلها للحذف المحلي بعد الرفع (`DeleteLocalPermanent`)، وليست من مسار السحب.
- مطابقة الصفوف تعتمد على `CloudId` فقط، وليس على الاسم أو رقم الهوية. لو أُدخل صف محلي جديد ولم يُرفع بعد (`CloudId` فارغ) ثم نزل نفس السجل من السحابة، يظهر مكررًا.

## 3. الصفوف `IsDirty` و`SyncAction = delete`

- **`IsDirty = 1`** (تعديل محلي غير مرفوع): السحب **لا يكتب فوقه**، بسبب الشرط `AND IsDirty = 0`. تعديلاتك المحلية محمية.
- **`SyncAction = 'delete'`**: هذا الصف يبقى محليًا وله `CloudId` ولا يزال `IsDirty = 1`. السحب يجده كأنه موجود، فلا يُعاد إدخاله ولا يُحدَّث. **لا يعود للحياة** ما دام لم يُرفع حذفه. بعد الرفع يُحذف من السحابة ومن المحلي.
- **الرفع مقابل السحب**: الرفع (`PushAllDirtyAsync`) هو الذي ينفذ الحذف في السحابة، وليس السحب.
- **خطر سباق**: في شاشات مثل `OwnersViewRealEstate` الرفع والسحب يعملان معًا بدون انتظار (`_ = _sync.PushAllDirtyAsync(); _ = SyncOwnersFromCloudAsync();`). تعليق في `UnitsViewRealEstate` (السطران 38 و39) يقول إن الرفع يجب أن ينتهي قبل السحب وإلا تتكرر الصفوف. وشاشة الوحدات وحدها تنتظر الرفع أولًا (`SyncAsync`). هذا احتمال تكرار في بقية الشاشات. **لم أختبره فعليًا**، هو استنتاج من الكود والتعليق.

## 4. كل الجداول أو بعضها؟

الـ12 جدولًا كلها لها سحب، لكن **كل جدول يُسحب فقط عند فتح شاشته أو الضغط على "تحديث" فيها**. لا يوجد "استرجاع شامل" بضغطة واحدة. ولا أعرف هل شاشات لوحة المعلومات (dashboard) تسحب شيئًا، لم أفحصها.

**ترتيب مهم**: العقود والإيصالات والمصاريف تعتمد على جداول أب. مثال في `SyncContractsFromCloudAsync`: إذا لم توجد الوحدة أو المستأجر محليًا (`GetLocalIdByCloudId` يرجع 0) يُتخطى العقد بصمت (`continue`). فعلى قاعدة فاضية تحتاج فتح شاشات الأب أولًا (المُلّاك والوحدات والمستأجرون) ثم العقود، ثم الإيصالات والمصاريف.

## 5. هل يشمل بيانات الدخول والصلاحيات؟

**لا**، حسب ما وجدته.
- لا يوجد جدول محلي للمستخدمين أو الصلاحيات. الجداول الـ12 كلها بيانات عمل (ملاك وعملاء ومنتجات وعقود وإيصالات ومصاريف ووحدات ومستأجرين).
- الدور يُقرأ من Supabase عند كل دخول (`LoginView.axaml.cs` السطران 43 و44: `RoleService.GetMyRoleAsync()`) ويُحفظ في الذاكرة فقط (`AppSession.Role`)، وإذا لم يوجد يصبح `tester`.
- لم أجد في `AuthService` و`RoleService` و`LoginView` أي كود يحفظ كلمة المرور أو الدور محليًا. لم أفحص هل مكتبة Supabase نفسها تحفظ جلسة أو توكن في مكان ما.

## ملخص الأثر على قاعدة فاضية
عند فتح البرنامج بقاعدة SQLite فاضية وبدور admin أو editor أو viewer، تُسحب البيانات تلقائيًا شاشةً شاشة وتُدمج. لا يُحذف شيء من السحابة (لا صفوف dirty). وبدور `tester` لا يُسحب شيء.
