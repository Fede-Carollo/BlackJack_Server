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
        private List<dynamic> _multipleData;
        private dynamic _singleData;

        public string Action { get => _action; set => _action = value; }
        public List<dynamic> MultipleData { get => _multipleData; set => _multipleData = value; }
        public dynamic SingleData { get => _singleData; set => _singleData = value; }

        public ObjMex(string action, List<dynamic> multipleData, dynamic singleData)
        {
            this._action = action;
            this._singleData = singleData;
            this._multipleData = multipleData;
        }

        public ObjMex() { }
    }
}
