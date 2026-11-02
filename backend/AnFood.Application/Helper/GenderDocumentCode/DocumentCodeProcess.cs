using AnFood.Persistence;
using Serilog;
using System.Linq.Expressions;

namespace AnFood.Application.Helper.GenderDocumentCode
{
    public static class DocumentCodeProcess
    {
        public static string CreateDocument<T>(
        IUnitOfWork unit,
        string transCode,
        string branchCode,
        Expression<Func<T, string>> codeSelector,
        int suffixLength = 4)
        where T : class
            {
                var repo = unit.GetRepository<T>();

                DateTime now = DateTime.Now;
                string timeText = now.ToString("yyMM");

                var prefix = $"{transCode}{branchCode}{timeText}-";

                var maxCode = repo.GetAll()
                    .Where(BuildStartsWithPredicate(codeSelector, prefix))
                    .OrderByDescending(codeSelector)
                    .Select(codeSelector)
                    .FirstOrDefault();

                int nextNumber = 1;

                if (!string.IsNullOrWhiteSpace(maxCode) && maxCode.Length >= suffixLength)
                {
                    var suffix = maxCode.Substring(maxCode.Length - suffixLength, suffixLength);

                    if (int.TryParse(suffix, out var current))
                        nextNumber = current + 1;
                }

                return prefix + nextNumber.ToString($"D{suffixLength}");
            }

        private static Expression<Func<T, bool>> BuildStartsWithPredicate<T>(
            Expression<Func<T, string>> selector,
            string prefix)
        {
            var param = selector.Parameters[0];
            var body = selector.Body;

            // selector(x).StartsWith(prefix)
            var startsWith = typeof(string).GetMethod(nameof(string.StartsWith), new[] { typeof(string) })!;
            var call = Expression.Call(body, startsWith, Expression.Constant(prefix));

            return Expression.Lambda<Func<T, bool>>(call, param);
        }
    }
}
