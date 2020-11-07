using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dalamud_CN
{
    class GameRunInDX9Exception : ApplicationException
    {
        public GameRunInDX9Exception(string message) : base(message) { }

        public override string Message
        {
            get
            {
                return base.Message;
            }
        }

    }
}
