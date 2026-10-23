namespace GastosApi.Services;

/// <summary>Chave única do cache do relatório de metas (5 min) para poder invalidá-lo quando os dados mudam.</summary>
public static class GoalReportCache
{
    public static string Key(int userId) => $"goal-report-{userId}";
}
