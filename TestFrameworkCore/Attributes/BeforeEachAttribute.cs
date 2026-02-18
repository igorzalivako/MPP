using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestFrameworkCore.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class BeforeEachAttribute : Attribute { }
}
