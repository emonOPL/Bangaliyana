using Bangaliyana.Utilities;

namespace Bangaliyana.Services
{
    public class CompareService : ICompareService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private const string CompareSessionKey = "CompareProducts";
        private const int MaxCompareItems = 4;

        public CompareService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        private ISession? Session => _httpContextAccessor.HttpContext?.Session;

        public List<int> GetCompareList()
        {
            if (Session == null) return new List<int>();
            return Session.Get<List<int>>(CompareSessionKey) ?? new List<int>();
        }

        public bool AddToCompare(int productId)
        {
            if (Session == null) return false;

            var compareList = GetCompareList();

            // Check if already in compare
            if (compareList.Contains(productId))
            {
                return false;
            }

            // Check max limit
            if (compareList.Count >= MaxCompareItems)
            {
                return false;
            }

            compareList.Add(productId);
            Session.Set(CompareSessionKey, compareList);
            return true;
        }

        public bool RemoveFromCompare(int productId)
        {
            if (Session == null) return false;

            var compareList = GetCompareList();

            if (!compareList.Contains(productId))
            {
                return false;
            }

            compareList.Remove(productId);
            Session.Set(CompareSessionKey, compareList);
            return true;
        }

        public void ClearCompare()
        {
            Session?.Remove(CompareSessionKey);
        }

        public bool IsInCompare(int productId)
        {
            return GetCompareList().Contains(productId);
        }

        public int GetCompareCount()
        {
            return GetCompareList().Count;
        }
    }
}
