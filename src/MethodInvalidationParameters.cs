namespace Como.WebApi.Caching
{
    public class MethodInvalidationParameters
    {
        public MethodInvalidationParameters(string methodName, string scopeName, string scopeValue)
        {
            MethodName = methodName;
            ScopeName = scopeName;
            ScopeValue = scopeValue;
        }

        public string MethodName { get; }
        public string ScopeName { get; }
        public string ScopeValue { get; }
    }
}