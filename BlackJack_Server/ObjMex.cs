using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlackJack_Server
{
    internal class ObjMex
    {
        private string _action;
        private List<object> _data;

        public string Action { get => _action; set => _action = value; }
        public List<object> Data { get => _data; set => _data = value; }
        
        public ObjMex(string action, List<object> data)
        {
            this._action = action;
            this._data = data;
        }

        public ObjMex() { }
    }
}
