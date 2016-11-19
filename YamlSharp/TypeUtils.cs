using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace YamlSharp
{
    /// <summary>
    /// Type 関連のユーティリティメソッド
    /// </summary>
    /// <example>
    /// <code>
    /// Type type;
    /// AttributeType attr = type.GetAttribute&lt;AttributeType&gt;();
    /// 
    /// PropertyInfo propInfo;
    /// AttributeType attr = propInfo.GetAttribute&lt;AttributeType&gt;();
    /// 
    /// string name;
    /// Type type = TypeUtils.GetType(name); // search from all assembly loaded
    /// 
    /// 
    /// </code>
    /// </example>
    internal static class TypeUtils
    {
        /// <summary>
        /// Type や PropertyInfo, FieldInfo から指定された型の属性を取り出して返す
        /// 複数存在した場合には最後の値を返す
        /// 存在しなければ null を返す
        /// </summary>
        /// <typeparam name="AttributeType">取り出したい属性の型</typeparam>
        /// <returns>取り出した属性値</returns>
        public static AttributeType GetAttribute<AttributeType>(this System.Reflection.MemberInfo info)
            where AttributeType : Attribute
        {
            var attrs = info.GetCustomAttributes(typeof(AttributeType), true);
            if (attrs.Length > 0)
            {
                return attrs.Last() as AttributeType;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// 現在ロードされているすべてのアセンブリから name という名の型を探して返す
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static Type GetType(string name)
        {
            if (AvailableTypes.ContainsKey(name))
                return AvailableTypes[name];
            Type type = Type.GetType(name);
            if (type == null) // ロードされているすべてのアセンブリから探す
                type = System.AppDomain.CurrentDomain.GetAssemblies().Select(
                        asm => asm.GetType(name)).FirstOrDefault(t => t != null);
            return AvailableTypes[name] = type;
        }
        static Dictionary<string, Type> AvailableTypes = new Dictionary<string, Type>();

        /// <summary>
        /// Check if the type is a ValueType and does not contain any non ValueType members.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsPureValueType(Type type)
        {
            if (type == typeof(IntPtr))
                return false;
            if (type.IsPrimitive)
                return true;
            if (type.IsEnum)
                return true;
            if (!type.IsValueType)
                return false;
            // struct
            foreach (var f in type.GetFields(
                    BindingFlags.GetField | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance))
                if (!IsPureValueType(f.FieldType))
                    return false;
            return true;
        }

        /// <summary>
        /// Returnes true if the specified <paramref name="type"/> is a struct type.
        /// </summary>
        /// <param name="type"><see cref="Type"/> to be analyzed.</param>
        /// <returns>true if the specified <paramref name="type"/> is a struct type; otehrwise false.</returns>
        public static bool IsStruct(Type type)
        {
            return type.IsValueType && !type.IsPrimitive;
        }

        /// <summary>
        /// Compare two objects to see if they are equal or not. Null is acceptable.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool AreEqual(object a, object b)
        {
            if (a == null)
                return b == null;
            if (b == null)
                return false;
            return a.Equals(b) || b.Equals(a);
        }

        /// <summary>
        /// Return if an object is a numeric value.
        /// </summary>
        /// <param name="obj">Any object to be tested.</param>
        /// <returns>True if object is a numeric value.</returns>
        public static bool IsNumeric(object obj)
        {
            if (obj == null)
                return false;
            Type type = obj.GetType();
            return type == typeof(sbyte) || type == typeof(short) || type == typeof(int) || type == typeof(long) ||
                   type == typeof(byte) || type == typeof(ushort) || type == typeof(uint) || type == typeof(ulong) ||
                   type == typeof(float) || type == typeof(double) || type == typeof(decimal);
        }

        /// <summary>
        /// Cast an object to a specified numeric type.
        /// </summary>
        /// <param name="obj">Any object</param>
        /// <param name="type">Numric type</param>
        /// <returns>Numeric value or null if the object is not a numeric value.</returns>
        public static object CastToNumericType(object obj, Type type)
        {
            var doubleValue = CastToDouble(obj);
            if (double.IsNaN(doubleValue))
                return null;

            if (obj is decimal && type == typeof(decimal))
                return obj; // do not convert into double

            object result = null;
            if (type == typeof(sbyte))
                result = (sbyte)doubleValue;
            if (type == typeof(byte))
                result = (byte)doubleValue;
            if (type == typeof(short))
                result = (short)doubleValue;
            if (type == typeof(ushort))
                result = (ushort)doubleValue;
            if (type == typeof(int))
                result = (int)doubleValue;
            if (type == typeof(uint))
                result = (uint)doubleValue;
            if (type == typeof(long))
                result = (long)doubleValue;
            if (type == typeof(ulong))
                result = (ulong)doubleValue;
            if (type == typeof(float))
                result = (float)doubleValue;
            if (type == typeof(double))
                result = doubleValue;
            if (type == typeof(decimal))
                result = (decimal)doubleValue;
            return result;
        }

        /// <summary>
        /// Cast boxed numeric value to double
        /// </summary>
        /// <param name="obj">boxed numeric value</param>
        /// <returns>Numeric value in double. Double.Nan if obj is not a numeric value.</returns>
        public static double CastToDouble(object obj)
        {
            var result = double.NaN;
            var type = obj != null ? obj.GetType() : null;
            if (type == typeof(sbyte))
                result = (sbyte)obj;
            if (type == typeof(byte))
                result = (byte)obj;
            if (type == typeof(short))
                result = (short)obj;
            if (type == typeof(ushort))
                result = (ushort)obj;
            if (type == typeof(int))
                result = (int)obj;
            if (type == typeof(uint))
                result = (uint)obj;
            if (type == typeof(long))
                result = (long)obj;
            if (type == typeof(ulong))
                result = (ulong)obj;
            if (type == typeof(float))
                result = (float)obj;
            if (type == typeof(double))
                result = (double)obj;
            if (type == typeof(decimal))
                result = (double)(decimal)obj;
            return result;
        }

        /// <summary>
        /// Check if type is fully public or not.
        /// Nested class is checked if all declared types are public.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsPublic(Type type)
        {
            return type.IsPublic ||
                (type.IsNestedPublic && type.IsNested && IsPublic(type.DeclaringType));
        }

        public static int GetReferenceHashCode(object obj)
        {
            return RuntimeHelpers.GetHashCode(obj);
        }

        /// <summary>
        /// Equality comparer that uses Object.ReferenceEquals(x, y) to compare class values.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public class EqualityComparerByRef<T> : EqualityComparer<T>
            where T : class
        {
            /// <summary>
            /// Determines whether two objects of type  T are equal by calling Object.ReferenceEquals(x, y).
            /// </summary>
            /// <param name="x">The first object to compare.</param>
            /// <param name="y">The second object to compare.</param>
            /// <returns>true if the specified objects are equal; otherwise, false.</returns>
            public override bool Equals(T x, T y)
            {
                return Object.ReferenceEquals(x, y);
            }

            /// <summary>
            /// Serves as a hash function for the specified object for hashing algorithms and 
            /// data structures, such as a hash table.
            /// </summary>
            /// <param name="obj">The object for which to get a hash code.</param>
            /// <returns>A hash code for the specified object.</returns>
            /// <exception cref="System.ArgumentNullException"><paramref name="obj"/> is null.</exception>
            public override int GetHashCode(T obj)
            {
                return RuntimeHelpers.GetHashCode(obj);
            }

            /// <summary>
            /// Returns a default equality comparer for the type specified by the generic argument.
            /// </summary>
            /// <value>The default instance of the System.Collections.Generic.EqualityComparer&lt;T&gt;
            ///  class for type T.</value>
            new public static EqualityComparerByRef<T> Default { get { return _default; } }
            static EqualityComparerByRef<T> _default = new EqualityComparerByRef<T>();
        }
    }
}
