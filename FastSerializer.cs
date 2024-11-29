using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.IO;
using System.Reflection;
using System.Linq;

namespace Medulla
{
    public static class FastSerializer
    {
        private const int MaxStringLength = 1024 * 64;  // 64KB max for strings

        private enum SerializedValueType : byte
        {
            Null = 0,
            ValueType = 1,
            String = 2,
            Array = 3,
            Primitive = 4
        }

        public static byte[] Serialize(object obj)
        {
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms))
            {
                SerializeValue(writer, obj);
                return ms.ToArray();
            }
        }

        public static object Deserialize(byte[] data, Type targetType)
        {
            using (var ms = new MemoryStream(data))
            using (var reader = new BinaryReader(ms))
            {
                return DeserializeValue(reader, targetType);
            }
        }

        private static void SerializeValue(BinaryWriter writer, object value)
        {
            if (value == null)
            {
                writer.Write((byte)SerializedValueType.Null);
                return;
            }

            var type = value.GetType();

            if (type.IsArray)
            {
                SerializeArray(writer, (Array)value);
                return;
            }

            if (type == typeof(string))
            {
                SerializeString(writer, (string)value);
                return;
            }

            if (type.IsPrimitive)
            {
                SerializePrimitive(writer, value);
                return;
            }

            if (type.IsValueType)
            {
                SerializeValueType(writer, value, type);
                return;
            }

            throw new ArgumentException($"Unsupported type: {type.FullName}");
        }

        private static object DeserializeValue(BinaryReader reader, Type targetType)
        {
            var valueType = (SerializedValueType)reader.ReadByte();

            if (valueType == SerializedValueType.Null)
            {
                return null;
            }

            if (valueType == SerializedValueType.Array && targetType.IsArray)
            {
                return DeserializeArray(reader, targetType.GetElementType());
            }

            if (valueType == SerializedValueType.String && targetType == typeof(string))
            {
                return DeserializeString(reader);
            }

            if (valueType == SerializedValueType.Primitive && targetType.IsPrimitive)
            {
                return DeserializePrimitive(reader, targetType);
            }

            if (valueType == SerializedValueType.ValueType && targetType.IsValueType)
            {
                return DeserializeValueType(reader, targetType);
            }

            throw new ArgumentException($"Type mismatch: expected {targetType.Name}, got {valueType}");
        }

        private static void SerializePrimitive(BinaryWriter writer, object value)
        {
            writer.Write((byte)SerializedValueType.Primitive);
            
            switch (Type.GetTypeCode(value.GetType()))
            {
                case TypeCode.Int32:
                    writer.Write((int)value);
                    break;
                case TypeCode.Single:
                    writer.Write((float)value);
                    break;
                default:
                    throw new ArgumentException($"Unsupported primitive type: {value.GetType().Name}");
            }
        }

        private static object DeserializePrimitive(BinaryReader reader, Type type)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Int32:
                    return reader.ReadInt32();
                case TypeCode.Single:
                    return reader.ReadSingle();
                default:
                    throw new ArgumentException($"Unsupported primitive type: {type.Name}");
            }
        }

        private static void SerializeValueType(BinaryWriter writer, object value, Type type)
        {
            writer.Write((byte)SerializedValueType.ValueType);

            // Get only instance fields that are not readonly or constant
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(f => !f.IsInitOnly && !f.IsLiteral)
                .ToArray();

            // Write field count
            writer.Write(fields.Length);

            foreach (var field in fields)
            {
                var fieldValue = field.GetValue(value);
                SerializeValue(writer, fieldValue);
            }
        }

        private static object DeserializeValueType(BinaryReader reader, Type type)
        {
            var result = Activator.CreateInstance(type);

            // Get only instance fields that are not readonly or constant
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(f => !f.IsInitOnly && !f.IsLiteral)
                .ToArray();

            // Read field count and verify
            var fieldCount = reader.ReadInt32();
            if (fieldCount != fields.Length)
            {
                throw new InvalidOperationException($"Field count mismatch. Expected {fields.Length}, got {fieldCount}");
            }

            foreach (var field in fields)
            {
                var fieldValue = DeserializeValue(reader, field.FieldType);
                field.SetValue(result, fieldValue);
            }

            return result;
        }

        private static void SerializeString(BinaryWriter writer, string str)
        {
            writer.Write((byte)SerializedValueType.String);
            
            if (str == null)
            {
                writer.Write(-1);
                return;
            }

            var bytes = Encoding.UTF8.GetBytes(str);
            if (bytes.Length > MaxStringLength)
                throw new ArgumentException($"String too long: {bytes.Length} bytes");

            writer.Write(bytes.Length);
            writer.Write(bytes);
        }

        private static string DeserializeString(BinaryReader reader)
        {
            var length = reader.ReadInt32();
            if (length == -1)
                return null;

            var bytes = reader.ReadBytes(length);
            return Encoding.UTF8.GetString(bytes);
        }

        private static void SerializeArray(BinaryWriter writer, Array arr)
        {
            writer.Write((byte)SerializedValueType.Array);

            if (arr == null)
            {
                writer.Write(-1);
                return;
            }

            writer.Write(arr.Length);
            
            for (int i = 0; i < arr.Length; i++)
            {
                SerializeValue(writer, arr.GetValue(i));
            }
        }

        private static Array DeserializeArray(BinaryReader reader, Type elementType)
        {
            var length = reader.ReadInt32();
            if (length == -1)
                return null;

            var arr = Array.CreateInstance(elementType, length);
            
            for (int i = 0; i < length; i++)
            {
                arr.SetValue(DeserializeValue(reader, elementType), i);
            }

            return arr;
        }
    }
} 