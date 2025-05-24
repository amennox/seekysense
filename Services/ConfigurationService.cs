using McpServer.Data;
using McpServer.Models;
using Microsoft.EntityFrameworkCore;

public class ConfigurationService
{
    private readonly McpDbContext _dbContext;
    private readonly EmbeddingService _embeddingService;
    private readonly ElasticsearchService _elasticService;

    public ConfigurationService(
        McpDbContext dbContext,
        EmbeddingService embeddingService,
        ElasticsearchService elasticService)
    {
        _dbContext = dbContext;
        _embeddingService = embeddingService;
        _elasticService = elasticService;
    }

    // --------------------
    // SCOPES
    // --------------------

    public async Task<bool> InsertScopeAsync(Scope scope)
    {
        var embedding = await _embeddingService.GetEmbeddingFromOllama(scope.DescriptionFullText);
        if (embedding == null || embedding.Length == 0)
            throw new Exception("Embedding failed");

        _dbContext.Scopes.Add(scope);
        await _dbContext.SaveChangesAsync();

        var scopeDoc = new ScopeDocument
        {
            ScopeId = scope.ScopeId,
            Name = scope.Name,
            DescriptionFullText = scope.DescriptionFullText,
            DescriptionVector = embedding
        };
        await _elasticService.InsertScopeDocumentAsync(scopeDoc);

        return true;
    }

    public async Task<List<Scope>> GetAllScopesAsync() =>
        await _dbContext.Scopes.ToListAsync();

    public async Task<Scope?> GetScopeByIdAsync(string id) =>
        await _dbContext.Scopes.FindAsync(id);

    public async Task<bool> UpdateScopeAsync(Scope updatedScope)
    {
        var existing = await _dbContext.Scopes.FindAsync(updatedScope.ScopeId);
        if (existing == null) return false;

        existing.ScopeType = updatedScope.ScopeType;
        existing.ScopeDataLiveAuthType = updatedScope.ScopeDataLiveAuthType;
        existing.ScopeDataLiveAuthMethod = updatedScope.ScopeDataLiveAuthMethod;
        existing.Name = updatedScope.Name;
        existing.DescriptionFullText = updatedScope.DescriptionFullText;

        var embedding = await _embeddingService.GetEmbeddingFromOllama(updatedScope.DescriptionFullText);
        if (embedding == null || embedding.Length == 0)
            throw new Exception("Embedding failed");

        var scopeDoc = new ScopeDocument
        {
            ScopeId = updatedScope.ScopeId,
            Name = updatedScope.Name,
            DescriptionFullText = updatedScope.DescriptionFullText,
            DescriptionVector = embedding
        };
        await _elasticService.InsertScopeDocumentAsync(scopeDoc);

        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteScopeAsync(string id)
    {
        var scope = await _dbContext.Scopes.FindAsync(id);
        if (scope == null) return false;

        _dbContext.Scopes.Remove(scope);
        await _dbContext.SaveChangesAsync();

        await _elasticService.DeleteScopeDocumentAsync(id);
        return true;
    }

    // --------------------
    // BUSINESSAUTHS
    // --------------------

    public async Task InsertBusinessAuthAsync(BusinessAuth auth)
    {
        _dbContext.BusinessAuths.Add(auth);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<List<BusinessAuth>> GetAllBusinessAuthsAsync() =>
        await _dbContext.BusinessAuths.ToListAsync();

    public async Task<BusinessAuth?> GetBusinessAuthByIdAsync(Guid id) =>
        await _dbContext.BusinessAuths.FindAsync(id);

    public async Task<bool> UpdateBusinessAuthAsync(BusinessAuth updatedAuth)
    {
        var existing = await _dbContext.BusinessAuths.FindAsync(updatedAuth.Id);
        if (existing == null) return false;

        existing.BusinessId = updatedAuth.BusinessId;
        existing.ScopeId = updatedAuth.ScopeId;
        existing.ApiKey = updatedAuth.ApiKey;

        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteBusinessAuthAsync(Guid id)
    {
        var auth = await _dbContext.BusinessAuths.FindAsync(id);
        if (auth == null) return false;

        _dbContext.BusinessAuths.Remove(auth);
        await _dbContext.SaveChangesAsync();
        return true;
    }

    // --------------------
    // USERAUTHS
    // --------------------

    public async Task InsertUserAuthAsync(UserAuth auth)
    {
        _dbContext.UserAuths.Add(auth);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<List<UserAuth>> GetAllUserAuthsAsync() =>
        await _dbContext.UserAuths.ToListAsync();

    public async Task<UserAuth?> GetUserAuthByIdAsync(Guid id) =>
        await _dbContext.UserAuths.FindAsync(id);

    public async Task<bool> UpdateUserAuthAsync(UserAuth updatedAuth)
    {
        var existing = await _dbContext.UserAuths.FindAsync(updatedAuth.Id);
        if (existing == null) return false;

        existing.UserId = updatedAuth.UserId;
        existing.BusinessId = updatedAuth.BusinessId;
        existing.ScopeId = updatedAuth.ScopeId;
        existing.ApiKey = updatedAuth.ApiKey;

        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteUserAuthAsync(Guid id)
    {
        var auth = await _dbContext.UserAuths.FindAsync(id);
        if (auth == null) return false;

        _dbContext.UserAuths.Remove(auth);
        await _dbContext.SaveChangesAsync();
        return true;
    }

    // Recupera UserAuth
    public async Task<UserAuth?> GetUserAuthByUserIdAndScopeIdAsync(string userId, string scopeId)
    {
        return await _dbContext.UserAuths
            .FirstOrDefaultAsync(ua => ua.UserId == userId && ua.ScopeId == scopeId);
    }

    // Recupera BusinessAuth
    public async Task<BusinessAuth?> GetBusinessAuthByBusinessIdAndScopeIdAsync(string businessId, string scopeId)
    {
        return await _dbContext.BusinessAuths
            .FirstOrDefaultAsync(ba => ba.BusinessId == businessId && ba.ScopeId == scopeId);
    }
}
