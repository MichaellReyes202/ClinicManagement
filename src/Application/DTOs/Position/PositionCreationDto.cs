using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Position
{
    public class PositionCreationDto
    {
        public required string Name { get; set; }
        public required string Description { get; set; }
    }
}
