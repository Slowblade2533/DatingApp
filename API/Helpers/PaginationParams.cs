namespace API.Helpers;

public class PaginationParams
{
    private const int MaxPageSize = 50; // ป้องกันคนใส่ PageSize มา 1 ล้านแล้วดึงทำเซิร์ฟเวอร์พัง
    public int PageNumber { get; set; } = 1;
    private int _pageSize = 10; // ค่าเริ่มต้นดึงทีละ 10 คน
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = (value > MaxPageSize) ? MaxPageSize : value;
    }
}
