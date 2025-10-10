using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public class PaginatedResponseDto<T>
    {
        public int Count { get; set; }

        public int Pages { get; set; }
        public List<T> Items { get; set; } = new List<T>();
        public PaginatedResponseDto(int count, List<T> items)
        {
            Count = count;
            Pages = (int)Math.Ceiling((double)count / (items.Count == 0 ? 1 : items.Count));
            Items = items;
        }
    }
}
