using Bangaliyana.Models;

namespace Bangaliyana.Services
{
    public interface IBlogService
    {
        // Blog Posts
        Task<List<BlogPost>> GetPublishedPostsAsync(int page = 1, int pageSize = 10);
        Task<List<BlogPost>> GetFeaturedPostsAsync(int count = 5);
        Task<BlogPost?> GetPostBySlugAsync(string slug);
        Task<BlogPost?> GetPostByIdAsync(int id);
        Task<List<BlogPost>> GetAllPostsAsync();
        Task<List<BlogPost>> GetPostsByCategoryAsync(int categoryId, int page = 1, int pageSize = 10);
        Task<List<BlogPost>> SearchPostsAsync(string query, int count = 10);
        Task<List<BlogPost>> GetRelatedPostsAsync(int postId, int count = 4);
        Task<int> GetTotalPostCountAsync(int? categoryId = null);
        Task CreatePostAsync(BlogPost post);
        Task UpdatePostAsync(BlogPost post);
        Task DeletePostAsync(int id);
        Task IncrementViewCountAsync(int id);

        // Blog Categories
        Task<List<BlogCategory>> GetActiveCategoriesAsync();
        Task<BlogCategory?> GetCategoryBySlugAsync(string slug);
        Task<BlogCategory?> GetCategoryByIdAsync(int id);
        Task<List<BlogCategory>> GetAllCategoriesAsync();
        Task CreateCategoryAsync(BlogCategory category);
        Task UpdateCategoryAsync(BlogCategory category);
        Task DeleteCategoryAsync(int id);
    }
}
