using System;
using System.Reflection;
using System.Text;

public class Serializable {
    delegate int SerializeData(object obj, FieldInfo field, byte[] buffer, int offset);

    FieldInfo[] fields;
    SerializeData[] serializers;
    SerializeData[] deserializers;
    public Serializable() {
        // CACA: Crear un repo de tipos de datos serializables y asignar un id.
        fields = GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
        serializers = new SerializeData[fields.Length];
        deserializers = new SerializeData[fields.Length];
        for (int i = 0, j = fields.Length; i < j; ++i) {
            if (fields[i].FieldType == typeof(byte)) {
                serializers[i] = (obj, field, buffer, offset) => { Array.Copy(BitConverter.GetBytes((byte)field.GetValue(obj)), 0, buffer, offset, 1); return offset + 1; };
                deserializers[i] = (obj, field, buffer, offset) => { field.SetValue(obj, buffer[offset]); return offset + 1; };
            } else if (fields[i].FieldType == typeof(bool)) {
                serializers[i] = (obj, field, buffer, offset) => { Array.Copy(BitConverter.GetBytes((bool)field.GetValue(obj)), 0, buffer, offset, 1); return offset + 1; };
                deserializers[i] = (obj, field, buffer, offset) => { field.SetValue(obj, BitConverter.ToBoolean(buffer, offset)); return offset + 1; };
            } else if (fields[i].FieldType == typeof(short)) {
                serializers[i] = (obj, field, buffer, offset) => { Array.Copy(BitConverter.GetBytes((short)field.GetValue(obj)), 0, buffer, offset, 2); return offset + 2; };
                deserializers[i] = (obj, field, buffer, offset) => { field.SetValue(obj, BitConverter.ToInt16(buffer, offset)); return offset + 2; };
            } else if (fields[i].FieldType == typeof(ushort)) {
                serializers[i] = (obj, field, buffer, offset) => { Array.Copy(BitConverter.GetBytes((ushort)field.GetValue(obj)), 0, buffer, offset, 2); return offset + 2; };
                deserializers[i] = (obj, field, buffer, offset) => { field.SetValue(obj, BitConverter.ToUInt16(buffer, offset)); return offset + 2; };
            } else if (fields[i].FieldType == typeof(int)) {
                serializers[i] = (obj, field, buffer, offset) => { Array.Copy(BitConverter.GetBytes((int)field.GetValue(obj)), 0, buffer, offset, 4); return offset + 4; };
                deserializers[i] = (obj, field, buffer, offset) => { field.SetValue(obj, BitConverter.ToInt32(buffer, offset)); return offset + 4; };
            } else if (fields[i].FieldType == typeof(uint)) {
                serializers[i] = (obj, field, buffer, offset) => { Array.Copy(BitConverter.GetBytes((uint)field.GetValue(obj)), 0, buffer, offset, 4); return offset + 4; };
                deserializers[i] = (obj, field, buffer, offset) => { field.SetValue(obj, BitConverter.ToUInt32(buffer, offset)); return offset + 4; };
            } else if (fields[i].FieldType == typeof(float)) {
                serializers[i] = (obj, field, buffer, offset) => { Array.Copy(BitConverter.GetBytes((float)field.GetValue(obj)), 0, buffer, offset, 4); return offset + 4; };
                deserializers[i] = (obj, field, buffer, offset) => { field.SetValue(obj, BitConverter.ToSingle(buffer, offset)); return offset + 4; };
            } else if (fields[i].FieldType == typeof(string)) {
                serializers[i] = (obj, field, buffer, offset) => { Array.Copy(BitConverter.GetBytes((ushort)field.GetValue(obj)), 0, buffer, offset, 2); offset += 2; return offset; };
                deserializers[i] = (obj, field, buffer, offset) => { ushort len = BitConverter.ToUInt16(buffer, offset); field.SetValue(obj, Encoding.UTF8.GetString(buffer, offset+2, len)); return offset + 2 + len; };
            } else if (fields[i].FieldType == typeof(UnityEngine.Vector2)) {
                serializers[i] = (obj, field, buffer, offset) => { UnityEngine.Vector2 v = (UnityEngine.Vector2)field.GetValue(obj); Array.Copy(BitConverter.GetBytes(v.x), 0, buffer, offset+0, 4); Array.Copy(BitConverter.GetBytes(v.y), 0, buffer, offset+4, 4); return offset + 8; };
                deserializers[i] = (obj, field, buffer, offset) => { field.SetValue(obj, new UnityEngine.Vector2(BitConverter.ToSingle(buffer, offset+0), BitConverter.ToSingle(buffer, offset + 4))); return offset + 8; };
            } else if (fields[i].FieldType == typeof(UnityEngine.Vector3)) {
                serializers[i] = (obj, field, buffer, offset) => { UnityEngine.Vector3 v = (UnityEngine.Vector3)field.GetValue(obj); Array.Copy(BitConverter.GetBytes(v.x), 0, buffer, offset+0, 4); Array.Copy(BitConverter.GetBytes(v.y), 0, buffer, offset+4, 4); Array.Copy(BitConverter.GetBytes(v.z), 0, buffer, offset+8, 4); return offset + 12; };
                deserializers[i] = (obj, field, buffer, offset) => { field.SetValue(obj, new UnityEngine.Vector3( BitConverter.ToSingle(buffer, offset), BitConverter.ToSingle(buffer, offset + 4), BitConverter.ToSingle(buffer, offset + 8))); return offset + 12;
                };
            } else {
                if (typeof(Serializable).IsAssignableFrom(fields[i].FieldType)) {
                    serializers[i] = (obj, field, buffer, offset) => {
                        Serializable val = (Serializable)field.GetValue(obj);
                        if (val == null) val = (Serializable)Activator.CreateInstance(field.FieldType);
                        return val.Serialize(null, buffer, offset);
                    };
                    deserializers[i] = (obj, field, buffer, offset) => {
                        Serializable val = (Serializable)field.GetValue(obj);
                        if (val == null) val = (Serializable)Activator.CreateInstance(field.FieldType);
                        field.SetValue(obj, val);
                        return val.Deserialize(null, buffer, offset);
                    };
                } else
                    UnityEngine.Debug.Log($"Unknow FieldType {fields[i].FieldType}");
            }
        }
    }

    public int Serialize(object obj, byte[] buffer, int offset) {
        if (obj == null) obj = this;
        for (int i = 0, j = fields.Length; i < j; ++i) offset = serializers[i](obj, fields[i], buffer, offset);
        return offset;
    }

    public int Deserialize(object obj, byte[] buffer, int offset) {
        if (obj == null) obj = this;
        for (int i = 0, j = fields.Length; i < j; ++i) offset = deserializers[i](obj, fields[i], buffer, offset);
        return offset;
    }
}