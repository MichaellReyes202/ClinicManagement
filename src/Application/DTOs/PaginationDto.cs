using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public record PaginationDto(int Limit = 5, int Offset = 0)
    {
        private const int CantidadMaximaLimit = 20;
        public int Limit { get; init; } = Math.Clamp(Limit, 1, CantidadMaximaLimit);
        public int Offset { get; init; } = Math.Max(0, Offset);
    }
}
