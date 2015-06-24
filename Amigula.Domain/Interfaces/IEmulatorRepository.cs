using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amigula.Domain.DTO;

namespace Amigula.Domain.Interfaces
{
    public interface IEmulatorRepository
    {
        EmulatorDto GetEmulatorPaths();
    }
}
