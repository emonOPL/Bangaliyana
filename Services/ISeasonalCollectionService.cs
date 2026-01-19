using Bangaliyana.Models;

namespace Bangaliyana.Services
{
    public interface ISeasonalCollectionService
    {
        Task<List<SeasonalCollection>> GetActiveCollectionsAsync();
        Task<List<SeasonalCollection>> GetFeaturedCollectionsAsync(int count = 4);
        Task<List<SeasonalCollection>> GetCurrentCollectionsAsync();
        Task<List<SeasonalCollection>> GetCollectionsBySeasonAsync(SeasonType season, int count = 10);
        Task<SeasonalCollection?> GetCollectionBySlugAsync(string slug);
        Task<SeasonalCollection?> GetCollectionByIdAsync(int id);
        Task<List<SeasonalCollection>> GetAllCollectionsAsync();
        Task CreateCollectionAsync(SeasonalCollection collection);
        Task UpdateCollectionAsync(SeasonalCollection collection);
        Task DeleteCollectionAsync(int id);
    }
}
