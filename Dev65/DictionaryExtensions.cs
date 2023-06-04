namespace Dev65;

public static class DictionaryExtensions
{
    public static void SafeAdd<T1, T2>(this Dictionary<T1, T2> dict, T1 key, T2 value) where T1 : notnull
    {
        if (dict.ContainsKey(key))
        {
            dict[key] = value;
        }
        else
        {
            dict.Add(key, value);
        }
    }
    
}