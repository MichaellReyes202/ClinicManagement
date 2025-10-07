using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Errors
{
    public class Error
    {
        public ErrorCodes Code { get; }
        public string Description { get; }

        public Error(ErrorCodes code, string description)
        {
            Code = code;
            Description = description;
        }
    }
}
