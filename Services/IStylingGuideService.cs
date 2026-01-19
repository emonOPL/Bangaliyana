using Bangaliyana.Models;

namespace Bangaliyana.Services
{
    public interface IStylingGuideService
    {
        Task<List<StylingGuide>> GetActiveGuidesAsync(int page = 1, int pageSize = 10);
        Task<List<StylingGuide>> GetFeaturedGuidesAsync(int count = 4);
        Task<List<StylingGuide>> GetGuidesByTypeAsync(StylingGuideType type, int count = 10);
        Task<StylingGuide?> GetGuideBySlugAsync(string slug);
        Task<StylingGuide?> GetGuideByIdAsync(int id);
        Task<List<StylingGuide>> GetAllGuidesAsync();
        Task<List<StylingGuide>> SearchGuidesAsync(string query, int count = 10);
        Task<int> GetTotalGuideCountAsync(StylingGuideType? type = null);
        Task CreateGuideAsync(StylingGuide guide);
        Task UpdateGuideAsync(StylingGuide guide);
        Task DeleteGuideAsync(int id);
        Task IncrementViewCountAsync(int id);
    }
}
