using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace CoffeeScript.RuntimeTypes
{
    class Function : IDynamicMetaObjectProvider
    {
        public Function(object context, object function)
        {
            
        }

        public DynamicMetaObject GetMetaObject(Expression parameter)
        {
            return new RuntimeDynamicMetaObject(parameter, this);
        }

        public void call(dynamic @this, params dynamic[] parameters)
        {
            
        }

        public void apply(dynamic @this, dynamic[] parameters)
        {

        }
    }
}
