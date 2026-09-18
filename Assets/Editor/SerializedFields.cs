using System;
using System.Collections.Generic;
using UnityEditor;

namespace BelowTheWing.EditorTools
{
    public static class SerializedFields
    {
        public static void SetList(UnityEngine.Object target, string field, IReadOnlyList<UnityEngine.Object> values)
        {
            var serialized = new SerializedObject(target);
            var property = Field(serialized, target, field);

            property.arraySize = values.Count;

            for (var i = 0; i < values.Count; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void Set(UnityEngine.Object target, string field, float value)
        {
            var serialized = new SerializedObject(target);

            Field(serialized, target, field).floatValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void Set(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);

            Field(serialized, target, field).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static SerializedProperty Field(SerializedObject serialized, UnityEngine.Object target, string field)
            => serialized.FindProperty(field)
               ?? throw new InvalidOperationException(
                   $"{target.GetType().Name} has no serialized field called '{field}', so the scene " +
                   "being built would carry an empty reference where that value was meant to go.");
    }
}
