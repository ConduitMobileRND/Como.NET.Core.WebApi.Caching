namespace Como.WebApi.Caching
{
    public interface ISerializationResolver
    {
        byte[] Serialize(string outputContentType, object value);
        string ComputeHash(object target);
    }
}