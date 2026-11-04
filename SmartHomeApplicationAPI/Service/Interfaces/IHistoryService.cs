using Utilities.Models;

namespace SmartHomeApplicationAPI.Service;

public interface IHistoryService
{
    Task<List<HistoryPointDto>> GetHistoryAsync(string userID, DateTime from, DateTime to);
}