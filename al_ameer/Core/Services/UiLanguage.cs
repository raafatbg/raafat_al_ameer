using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace al_ameer.Services;

public static class UiLanguage
{
    private static readonly Dictionary<string, string> Arabic = new(StringComparer.Ordinal)
    {
        ["MANAGEMENT SYSTEM"] = "نظام الإدارة", ["DASHBOARD"] = "لوحة التحكم",
        ["Overview"] = "نظرة عامة", ["Inventory"] = "المخزون", ["Sales & POS"] = "المبيعات ونقطة البيع",
        ["Customers"] = "الزبائن", ["Customer Accounts"] = "حسابات الزبائن", ["Suppliers"] = "المورّدون",
        ["Expenses"] = "المصاريف", ["ADMINISTRATION"] = "الإدارة", ["Reports"] = "التقارير",
        ["Employees"] = "الموظفون", ["Sign Out"] = "تسجيل الخروج", ["Control Center"] = "مركز التحكم",
        ["Inventory Management"] = "إدارة المخزون", ["Customer Registry"] = "سجل الزبائن",
        ["Employees & Salary"] = "الموظفون والرواتب", ["Supplier Directory"] = "دليل المورّدين",
        ["Financial Analytics"] = "التقارير المالية", ["Search categories or products..."] = "ابحث عن فئة أو منتج...",
        ["+ ADD PRODUCT"] = "+ إضافة منتج", ["+ ADD CATEGORY"] = "+ إضافة فئة",
        ["Inventory categories"] = "فئات المخزون", ["Open a folder to view its products."] = "افتح مجلداً لعرض منتجاته.",
        ["← All categories"] = "← كل الفئات", ["Customer debit & credit"] = "حساب الزبون مدين ودائن",
        ["Invoices are debits; recorded payments are credits. Amounts are in LBP."] = "الفواتير مدين والدفعات دائن. المبالغ بالليرة اللبنانية.",
        ["Refresh"] = "تحديث", ["CUSTOMERS"] = "الزبائن", ["Customer"] = "الزبون",
        ["Balance"] = "الرصيد", ["Select a customer"] = "اختر زبوناً", ["Date"] = "التاريخ",
        ["Invoice"] = "الفاتورة", ["Debit"] = "مدين", ["Paid"] = "المدفوع", ["Due"] = "المستحق",
        ["PAYMENT HISTORY"] = "سجل الدفعات", ["Invoice ID"] = "رقم الفاتورة", ["Credit"] = "دائن",
        ["Method"] = "الطريقة", ["Notes"] = "ملاحظات", ["Record payment"] = "تسجيل دفعة",
        ["Reverse"] = "عكس القيد", ["Reversed"] = "تاريخ العكس", ["Reason"] = "السبب",
        ["Employees & salary ledger"] = "الموظفون وسجل الرواتب",
        ["Post salary due and payments with dates. Monthly salary is a reference amount."] = "سجّل المستحقات والدفعات بالتاريخ. الراتب الشهري قيمة مرجعية.",
        ["+ Add employee"] = "+ إضافة موظف", ["Employee"] = "الموظف", ["Salary due"] = "الراتب المستحق",
        ["Select an employee"] = "اختر موظفاً", ["Edit employee"] = "تعديل الموظف",
        ["SALARY HISTORY"] = "سجل الرواتب", ["Type"] = "النوع", ["Amount (LBP)"] = "المبلغ (ل.ل.)",
        ["Post entry"] = "تسجيل قيد", ["Financial reports"] = "التقارير المالية",
        ["From"] = "من", ["Through"] = "إلى", ["Cash flow"] = "التدفق النقدي",
        ["Customer aging"] = "أعمار ديون الزبائن", ["Salary balances"] = "أرصدة الرواتب",
        ["Receipts LBP"] = "المقبوضات ل.ل.", ["Expenses LBP"] = "المصاريف ل.ل.",
        ["Salary paid LBP"] = "الرواتب المدفوعة ل.ل.", ["Net LBP"] = "الصافي ل.ل.",
        ["0–30 days"] = "٠–٣٠ يوماً", ["31–60 days"] = "٣١–٦٠ يوماً",
        ["61–90 days"] = "٦١–٩٠ يوماً", ["90+ days"] = "أكثر من ٩٠ يوماً",
        ["Total LBP"] = "المجموع ل.ل.", ["Salary due LBP"] = "الراتب المستحق ل.ل.",
        ["Paid LBP"] = "المدفوع ل.ل.", ["Balance LBP"] = "الرصيد ل.ل.",
        ["Sales / New Transaction"] = "المبيعات / معاملة جديدة", ["Rate: "] = "سعر الصرف: ",
        ["ITEM"] = "الصنف", ["PRICE"] = "السعر", ["QTY"] = "الكمية", ["TOTAL"] = "المجموع",
        ["Walk-in Customer"] = "زبون نقدي", ["Discount (LBP)"] = "الحسم (ل.ل.)",
        ["Tax (%)"] = "الضريبة (%)", ["TOTAL AMOUNT"] = "المبلغ الإجمالي",
        ["Payment"] = "الدفع", ["Paid Amount"] = "المبلغ المدفوع",
        ["Print Receipt after saving"] = "طباعة الإيصال بعد الحفظ", ["Complete Sale"] = "إتمام البيع",
        ["Name"] = "الاسم", ["Barcode"] = "الباركود", ["Category"] = "الفئة",
        ["Stock"] = "المخزون", ["Price"] = "السعر", ["Actions"] = "إجراءات",
        ["Edit"] = "تعديل", ["Delete"] = "حذف", ["Save"] = "حفظ", ["Cancel"] = "إلغاء",
        ["Add"] = "إضافة", ["Phone"] = "الهاتف", ["Description"] = "الوصف"
        , ["Settings / الإعدادات"] = "الإعدادات", ["Al Ameer Fleet Operations"] = "عمليات الأمير",
        ["Administrator"] = "المدير", ["Premium Admin"] = "إدارة النظام",
        ["PREMIUM TIRE SERVICES"] = "خدمات الإطارات الممتازة", ["Staff Login"] = "دخول الموظفين",
        ["Enter your credentials to continue"] = "أدخل بيانات الدخول للمتابعة",
        ["USERNAME"] = "اسم المستخدم", ["PASSWORD"] = "كلمة المرور", ["SIGN IN"] = "تسجيل الدخول",
        ["Exit Application"] = "إغلاق البرنامج", ["Select Customer"] = "اختيار الزبون",
        ["SELECT CUSTOMER"] = "اختيار الزبون", ["Select"] = "اختيار",
        ["ADD EMPLOYEE"] = "إضافة موظف", ["Full name"] = "الاسم الكامل",
        ["Job title"] = "المسمى الوظيفي", ["Hire date"] = "تاريخ التوظيف",
        ["Monthly salary (LBP)"] = "الراتب الشهري (ل.ل.)", ["Active employee"] = "موظف نشط",
        ["Save Employee"] = "حفظ الموظف", ["Salary due: 0 LBP"] = "الراتب المستحق: ٠ ل.ل.",
        ["Outstanding: 0 LBP"] = "الرصيد المستحق: ٠ ل.ل.",
        ["Operating cash flow: sale receipts minus expenses and salary payments. Purchases are not included."] =
            "التدفق التشغيلي: المقبوضات ناقص المصاريف والرواتب. المشتريات غير مشمولة.",
        ["Aging uses invoice date; no contractual due date is stored. Balances shown as of today."] =
            "تصنيف الديون حسب تاريخ الفاتورة؛ لا يوجد تاريخ استحقاق محفوظ. الأرصدة لليوم.",
        ["Add Product"] = "إضافة منتج", ["ADD NEW PRODUCT"] = "إضافة منتج جديد",
        ["Product Name"] = "اسم المنتج", ["Supplier"] = "المورّد", ["Price Currency"] = "عملة السعر",
        ["LBP per USD"] = "ليرة لكل دولار", ["Cost Price"] = "سعر التكلفة",
        ["Selling Price"] = "سعر البيع", ["Stock Quantity"] = "كمية المخزون",
        ["This item is a Tire"] = "هذا المنتج إطار", ["Width"] = "العرض", ["Ratio"] = "النسبة",
        ["Diam."] = "القطر", ["Save to Inventory"] = "حفظ في المخزون",
        ["Search by category or description..."] = "ابحث بالفئة أو الوصف...",
        ["+ ADD EXPENSE"] = "+ إضافة مصروف", ["DATE"] = "التاريخ", ["CATEGORY"] = "الفئة",
        ["DESCRIPTION"] = "الوصف", ["AMOUNT"] = "المبلغ", ["ACTIONS"] = "إجراءات",
        ["Search suppliers..."] = "ابحث عن مورّد...", ["+ ADD SUPPLIER"] = "+ إضافة مورّد",
        ["COMPANY NAME"] = "اسم الشركة", ["CONTACT PERSON"] = "جهة الاتصال", ["PHONE"] = "الهاتف",
        ["Details"] = "التفاصيل", ["Search by name or phone..."] = "ابحث بالاسم أو الهاتف...",
        ["+ NEW CUSTOMER"] = "+ زبون جديد", ["NAME"] = "الاسم", ["REG. DATE"] = "تاريخ التسجيل",
        ["OVERVIEW"] = "نظرة عامة", ["Business Dashboard"] = "لوحة الأعمال",
        ["TOTAL SALES"] = "إجمالي المبيعات", ["Today's Revenue"] = "إيرادات اليوم",
        ["ITEMS IN STOCK"] = "قطع في المخزون", ["Total Quantity Available"] = "الكمية المتاحة",
        ["STOCK VALUE"] = "قيمة المخزون", ["Capital at Cost"] = "رأس المال بسعر التكلفة",
        ["LOW STOCK ALERT"] = "تنبيه نقص المخزون", ["Products below 5 units"] = "منتجات أقل من ٥ وحدات",
        ["QUICK ACTIONS"] = "إجراءات سريعة", ["+ NEW SALE (POS)"] = "+ بيع جديد",
        [" REFRESH DATA"] = "تحديث البيانات", ["FINANCIAL REPORTS"] = "التقارير المالية",
        ["INVENTORY"] = "المخزون", [" Refresh"] = "تحديث", ["Dismiss"] = "إغلاق"
        , ["Search name or phone..."] = "ابحث بالاسم أو الهاتف...",
        ["Amount LBP"] = "المبلغ ل.ل.", ["Notes (optional)"] = "ملاحظات (اختياري)"
        , ["Services"] = "الخدمات", ["BROWSE ITEMS"] = "تصفح الأصناف",
        ["Open a folder; double-click an item to add it."] = "افتح مجلداً وانقر مرتين على الصنف لإضافته.",
        ["Refresh list"] = "تحديث القائمة", ["TYPE"] = "النوع",
        ["Maintain service names and prices for the Sales browser. Inactive services remain on old invoices."] =
            "إدارة أسماء الخدمات وأسعارها للمبيعات. تبقى الخدمات غير النشطة في الفواتير القديمة.",
        ["Service"] = "الخدمة", ["Price LBP"] = "السعر ل.ل.", ["Active"] = "نشط",
        ["Add service"] = "إضافة خدمة", ["Edit service"] = "تعديل الخدمة",
        ["Service name"] = "اسم الخدمة", ["Price (LBP)"] = "السعر (ل.ل.)",
        ["Available in Sales"] = "متاحة في المبيعات", ["New"] = "جديد",
        ["Save service"] = "حفظ الخدمة"
        , ["Scan barcode or search product..."] = "امسح الباركود أو ابحث عن منتج..."
    };

    public static void Apply(DependencyObject root)
    {
        if (AppSettings.Current.Language != "ar") return;
        if (root is FrameworkElement element) element.FlowDirection = FlowDirection.RightToLeft;
        if (root is TextBlock text) text.Text = Translate(text.Text);
        // ComboBox values remain canonical English identifiers used by transaction logic.
        if (root is ContentControl content && root is not ComboBoxItem && content.Content is string label)
            content.Content = Translate(label);
        if (root is DataGrid grid)
            foreach (var column in grid.Columns)
                if (column.Header is string header) column.Header = Translate(header);
        int count = VisualTreeHelper.GetChildrenCount(root);
        for (int i = 0; i < count; i++) Apply(VisualTreeHelper.GetChild(root, i));
    }

    public static string Translate(string value) => AppSettings.Current.Language == "ar" &&
        Arabic.TryGetValue(value, out string? result) ? result : value;
}
