using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Driver;

namespace aacs.Models
{
    public class PaginatedList<T> : List<T>
    {
        public int PageIndex { get; private set; }
        public int TotalPages { get; private set; }

        public PaginatedList(List<T> items, int count, int pageIndex, int pageSize)
        {
            PageIndex = pageIndex;
            TotalPages = (int)Math.Ceiling(count / (double)pageSize);

            this.AddRange(items);
        }

        public bool HasPreviousPage => PageIndex > 1;
        public bool HasNextPage => PageIndex < TotalPages;

        // Static method to create a PaginatedList from a MongoDB collection
        public static PaginatedList<T> Create(IMongoCollection<T> collection, int pageIndex, int pageSize)
        {
            var count = collection.CountDocuments(FilterDefinition<T>.Empty);  // Total count of documents
            var items = collection.Find(FilterDefinition<T>.Empty)
                                   .Skip((pageIndex - 1) * pageSize)  // Skip for pagination
                                   .Limit(pageSize)  // Limit the number of items per page
                                   .ToList();  // Fetch the paginated items

            return new PaginatedList<T>(items, (int)count, pageIndex, pageSize);
        }
    }
}