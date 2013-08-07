using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace CoffeeScript.RuntimeTypes
{
    class RuntimeDynamicMetaObject : DynamicMetaObject
    {
        public RuntimeDynamicMetaObject(Expression expression, object value)
            : base(expression, BindingRestrictions.Empty, value)
        {
        }
    }
}
