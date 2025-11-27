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

        public PaginatedResponseDto(int count, List<T> items, int limit)
        {
            Count = count;
            Pages = limit > 0 ? (int)Math.Ceiling((double)count / limit) : 0;
            Items = items;
        }
    }
}

