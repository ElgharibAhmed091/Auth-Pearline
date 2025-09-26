namespace AuthAPI.Models
{
    public enum QuoteStatus
    {
        Pending,     // الطلب لسه جديد
        Completed,   // اتنفذ
        Shipped,     // اتشحن
        Cancelled    // اتلغى
    }
}
