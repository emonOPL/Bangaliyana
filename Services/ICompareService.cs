namespace Bangaliyana.Services
{
    public interface ICompareService
    {
        List<int> GetCompareList();
        bool AddToCompare(int productId);
        bool RemoveFromCompare(int productId);
        void ClearCompare();
        bool IsInCompare(int productId);
        int GetCompareCount();
    }
}
