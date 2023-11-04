using Amazon.DynamoDBv2.DocumentModel;
using System.Linq;

namespace ratingsflex.Areas.Movies.Data
{
    public static class DynamoDBEntryExtensions
    {
        public static List<Dictionary<string, string>> AsListOfDictionaries(this DynamoDBEntry entry)
        {
            var listEntry = entry as DynamoDBList;
            if (listEntry == null)
            {
                throw new InvalidOperationException("The entry is not a list.");
            }

            var dictionaries = new List<Dictionary<string, string>>();
            foreach (var item in listEntry.Entries)
            {
                var document = item as Document;
                if (document != null)
                {
                    var dictionary = document.ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.AsString()
                    );
                    dictionaries.Add(dictionary);
                }
            }

            return dictionaries;
        }
    }

}
