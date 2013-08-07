using System;
using System.Dynamic;
using System.Linq.Expressions;

namespace CoffeeScript
{
    /// <summary>
    /// TypeModel and TypeModelMetaObject
    /// 
    /// TypeModel wraps System.Runtimetypes. When Sympl code encounters
    /// a type leaf node in Sympl.Globals and tries to invoke a member, wrapping
    /// the ReflectionTypes in TypeModels allows member access to get the type's
    /// members and not ReflectionType's members.
    /// </summary>
    public class TypeModel : IDynamicMetaObjectProvider
    {
        public TypeModel(Type type)
        {
            ReflType = type;
        }

        public Type ReflType { get; private set; }

        DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter)
        {
            return new TypeModelMetaObject(parameter, this);
        }
    }
}